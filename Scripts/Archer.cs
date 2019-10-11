using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Archer : Unit {

	[Header("Specific to Type")]
	public GameObject arrow;
	private GameObject _arrow;
	private int charge = 0;

	public override void Start() {
		base.Start ();
		perkInfo = "Charge: Increase your next attack by 9 and range by 0.5, up to 3 times";
	}

	public override void UsePerk (GameObject target) {
		base.UsePerk (target);
		_attack += 9;
		_range += 0.5f; 
		charge++;
		if (charge == 3 || _focus == 0) {
			perkAvailable = false;
		}
		transform.Find ("ArcherChargeEffect").GetComponent<ParticleSystem> ().Play ();
	}

	public override void FindPerkTargets (List<GameObject> resultsContainer) {
		if (resultsContainer.Count == 0) {
			resultsContainer.Add (currentUnitSpace);
		}
	}

	public override void FindTargets (List<GameObject> resultsContainer) {
		if (resultsContainer.Count == 0) { 
			foreach (var pair in game.unitsTable) {
				GameObject i = pair.Value;
				if (i == null) {
					continue;
				}
				if (i != gameObject && i.GetComponent<Unit> ().mat != game.currentController
					&& Distance2D (gameObject, i) < _range * game.unitSpaceDiameter + 0.01f) {
					resultsContainer.Add (pair.Key);
				}
			}
		}
	}

	public override void RoundOver () {	
		base.RoundOver ();
		if (charge == 3) {
			perkAvailable = false;
		}
	}

	//event receivers
	public void OnRelease() {
		_arrow = Instantiate (arrow, arrow.transform.position, arrow.transform.rotation);
		_arrow.transform.localScale = new Vector3 (26f, 42f, 26f);
		arrow.SetActive (false);
		StartCoroutine (FireArrow ());
	}

	public void OnReload() {
		arrow.SetActive (true);
	}

	//for arrow motion
	private IEnumerator FireArrow() {
		Vector3 destination = currentAttackTarget.transform.Find ("VisualPivot").position;
		Vector3 origin = _arrow.transform.position;
		float height = Vector3.Distance (origin, destination) * 0.1f;
		Vector3 midPoint = new Vector3 ((destination.x + origin.x) / 2, (destination.y + origin.y) / 2 + height, (destination.z + origin.z) / 2);
		float v = 0.4f;
		Vector3 target = midPoint;
		Transform a = _arrow.transform;
		while (_arrow.transform.position != target) {
			Vector3 vel = Vector3.Normalize (target - a.position) * v;
			if (Vector3.Distance (target, a.position) < v) {
				a.position = target;
				target = destination;
			} else {
				a.position = a.position + vel;
			}
			if (target == midPoint) {
				a.rotation = Quaternion.LookRotation (new Vector3 (vel.x, (target.y - a.position.y) * v, vel.z)); 
			} else {
				a.rotation = Quaternion.LookRotation (new Vector3 (vel.x, (a.position.y - midPoint.y) * v, vel.z)); 
			}
			a.eulerAngles = new Vector3 (a.eulerAngles.z, a.eulerAngles.y + 90f, a.eulerAngles.x);
			yield return new WaitForFixedUpdate();
		}
		Destroy (_arrow);
		_arrow = null;
		AttackAnimationHit ();
		yield return new WaitForSeconds (onHitEffect.GetComponent<ParticleSystem> ().main.duration);

		if (charge > 0) {
			_attack -= charge * 9;
			_range -= charge * 0.5f;
			charge = 0;
			if (_focus > 0) {
				perkAvailable = true;
			}
		}
		AttackAnimationOver ();
	}

	public override void AttackAnimationHit () {
		float damage = Random.Range (_attack * _stability, _attack);
		if (currentAttackTarget.transform.position.y - gameObject.transform.position.y > 0.25f) {
			damage *= 0.8f;
		}
		if (currentAttackTarget.transform.position.y - gameObject.transform.position.y < -0.25f) {
			damage *= 1.2f;
		}
		currentAttackTarget.GetComponent<Unit> ().Hit ((int)damage, "ranged");
		GetComponent<AudioSource> ().PlayOneShot (onHitSoundEffect, 1f);
	}

	public override void AttackAnimationOver() {
		Unit target = currentAttackTarget.GetComponent<Unit> ();
		if (target._hp == 0) {
			target.OnDeath (this);
			currentAttackTarget = null;
			game.pauseInput = false;
			return;
		} 
		if (!selfDefense && target._focus > 0 && Distance2D (gameObject, currentAttackTarget) <= target._range * game.unitSpaceDiameter + 0.01f) {
			currentAttackTarget.GetComponent<Unit> ().Attack (gameObject, false);
		} else {
			selfDefense = false;
			game.pauseInput = false;
		}
		currentAttackTarget = null;
	}

	//AI
	protected override string AITryAttack (Dictionary<GameObject, int> map, string strategy) {
		List<GameObject> attackTargets = new List<GameObject> ();
		FindTargets (attackTargets);
		if (attackTargets.Count > 0) {
			if (!attackAvailable) {
				return "have targets, no focus";
			}
			if (morale == "overwhelmed") {
				string result = AITryManeuver (map, "try resposition");
				if (result == "success") {
					return "success";
				}
			}
			bool hasOutRangedTarget = false;
			bool atHeightDisadvantage = false;
			foreach (GameObject i in attackTargets) {
				if (_range > game.unitsTable [i].GetComponent<Unit> ()._range) {
					hasOutRangedTarget = true;
				}
				if (i.transform.position.y > currentUnitSpace.transform.position.y) {
					atHeightDisadvantage = true;
				}
			}
			if (strategy == "aggressive" && (atHeightDisadvantage || !attackTargets.Contains (preferedTarget.currentUnitSpace))) {
				string result = AITryApproach (map);
				if (result == "success") {
					return "success";
				}
			} 
			if ((strategy == "aggressive" || preferedTarget._range * game.unitSpaceDiameter + 0.01 < Distance2D (gameObject, preferedTarget.gameObject)) && attackTargets.Contains (preferedTarget.currentUnitSpace)) {
				if (BetterWithCharge (preferedTarget.gameObject)) {
					UsePerk (gameObject);
					return "success";
				} 
				Attack (preferedTarget.gameObject);
				return "success";
			}
			List<GameObject> outRangedTargets = new List<GameObject> (attackTargets);
			outRangedTargets.RemoveAll (i => game.unitsTable[i].GetComponent<Unit>()._range * game.unitSpaceDiameter + 0.01f > Distance2D(gameObject, i));
			if (hasOutRangedTarget && outRangedTargets.Count == 0) {
				string result = AITryManeuver(map, "try create distance");
				if (result == "success") {
					return "success";
				}
			} else if (outRangedTargets.Count > 0) {
				if (outRangedTargets.Count == 1) {
					Attack (game.unitsTable [outRangedTargets [0]]);
					if (BetterWithCharge (game.unitsTable [outRangedTargets [0]])) {
						UsePerk (gameObject);
						return "success";
					} 
				} else {
					GameObject bestTarget = outRangedTargets.OrderBy (x => x.transform.position.y).ThenBy (x => game.unitsTable [x].GetComponent<Unit> ()._hp).ThenBy (x => Distance2D (x, preferedTarget.gameObject)).First ();
					if (BetterWithCharge (game.unitsTable [bestTarget])) {
						UsePerk (gameObject);
						return "success";
					} 
					Attack (game.unitsTable [bestTarget]);
				}
				return "success";
			} 
			if (attackTargets.Count == 1) {
				if (BetterWithCharge (game.unitsTable [attackTargets [0]])) {
					UsePerk (gameObject);
					return "success";
				} 
				Attack (game.unitsTable [attackTargets [0]]);
			} else {
				GameObject bestTarget = attackTargets.OrderBy (x => game.unitsTable [x].GetComponent<Unit> ()._hp).ThenBy (x => Distance2D (x, preferedTarget.gameObject)).First ();
				if (BetterWithCharge (game.unitsTable [bestTarget])) {
					UsePerk (gameObject);
					return "success";
				} 
				Attack (game.unitsTable [bestTarget]);
			}
			return "success";
		} else {
			if (_focus > 0 && perkAvailable) {
				UsePerk (gameObject);
				return "success";
			} 
			return "no target"; 
		}
	}

	private bool BetterWithCharge (GameObject i) {
		Unit u = i.GetComponent<Unit> ();
		if (perkAvailable && u._defense > 3 && u._hp > _attack - u._defense) {
			return true;
		} 
		return false;
	}

	protected override string AITryApproach (Dictionary<GameObject, int> map) {
		List<GameObject> moveTargets = new List<GameObject> ();
		FindPaths (moveTargets);
		if (moveTargets.Count > 0) {
			if (!movementAvailable) {
				return "unable to move";
			}
			moveTargets.RemoveAll (i => (Distance2D (i, preferedTarget.currentUnitSpace) < (_range - 0.2f) * game.unitSpaceDiameter - 0.01f));
			if (moveTargets.Count == 0) {
				return "unable to get closer";
			}
			GameObject closest = moveTargets.OrderBy (x => map[x]).First();
			moveTargets.RemoveAll (i => map [i] > map [closest]);
			if (moveTargets.Count > 1) {
				closest = moveTargets.OrderByDescending (x => x.GetComponent<UnitSpace>().priority).ThenBy(x => Distance2D (x, preferedTarget.gameObject)).First();
			}
			if (map[closest] < map[currentUnitSpace]) {
				Move (closest);
				return "success";
			} else {
				return "unable to get closer";
			}
		} else {
			return "unable to move";
		}
	}

	protected override void AIFinishUp () {
		if (_focus > 0 && perkAvailable) {
			UsePerk (gameObject);
			if (_focus > 0) {
				return;
			}
		}
		base.AIFinishUp ();
	}
}
