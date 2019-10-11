using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SwordSaint : Unit {

	private bool isDuel = false;

	public override void Start() {
		base.Start ();
		perkInfo = "Duel: Deal damage based on the target's missing heath, recover 20 hp and gain 6 attack if the target dies";
	}

	public override void UsePerk(GameObject target) {
		base.UsePerk (target);
		currentAttackTarget = game.unitsTable[target];
		target = currentAttackTarget;
		isDuel = true;
		float angle = Mathf.Atan2 (target.transform.position.x - transform.position.x, target.transform.position.z - transform.position.z) * 180f / Mathf.PI;
		transform.eulerAngles = new Vector3 (0f, angle, 0f);
		target.transform.eulerAngles = new Vector3 (0f, angle - 180f, 0f);
		gameObject.GetComponent<Animator> ().Play ("Duel");
	}

	public override void FindPerkTargets(List<GameObject> resultsContainer) {
		FindTargets (resultsContainer);
	}

	public void AttackAnimationVirtualHit() {
		currentAttackTarget.GetComponent<Animator> ().Play ("Hit");
	}

	public override void AttackAnimationHit () {
		float damage;
		Unit target = currentAttackTarget.GetComponent<Unit> ();
		if (isDuel) {
			damage = ((target.hp - target._hp) * 0.15f + 10f) * Random.Range (_attack * _stability, _attack);
		} else {
			damage = Random.Range (_attack * _stability, _attack);
		}
		if (currentAttackTarget.transform.position.y - gameObject.transform.position.y > 0.25f) {
			damage *= 0.8f;
		}
		target.Hit ((int)damage);
	}

	public override void AttackAnimationOver() {
		Unit target = currentAttackTarget.GetComponent<Unit> ();
		if (target._hp == 0) {
			if (isDuel) {
				if (hp - _hp > 20) {
					_hp = hp;
				} else {
					_hp += 20;
				}
				attackAvailable = true;
				perkAvailable = true; 
				if (_attack < 36) { //maximum bonus damage is 3 x 6 = 18
					_attack += 6;
				}
				isDuel = false;
			}

			target.OnDeath (this);
			if (!selfDefense && Distance2D (gameObject, currentAttackTarget) < game.unitSpaceDiameter + 0.01f) {
				Move (target.currentUnitSpace, false);
			} else {
				game.pauseInput = false;
			}
			currentAttackTarget = null;
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
				if (BetterWithDuel (preferedTarget.gameObject)) {
					UsePerk (preferedTarget.currentUnitSpace);
				} else {
					Attack (preferedTarget.gameObject);
				}
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
					if (BetterWithDuel (game.unitsTable [outRangedTargets [0]])) {
						UsePerk (outRangedTargets [0]);
					} else {
						Attack (game.unitsTable [outRangedTargets [0]]);
					}
				} else {
					GameObject bestTarget = outRangedTargets.OrderBy (x => x.transform.position.y).ThenBy (x => game.unitsTable [x].GetComponent<Unit> ()._hp).ThenBy (x => Distance2D (x, preferedTarget.gameObject)).First ();
					if (BetterWithDuel (game.unitsTable [bestTarget])) {
						UsePerk (bestTarget);
					} else {
						Attack (game.unitsTable [bestTarget]);
					}
				}
				return "success";
			} 
			if (attackTargets.Count == 1) {
				if (BetterWithDuel (game.unitsTable [attackTargets [0]])) {
					UsePerk (attackTargets [0]);
				} else {
					Attack (game.unitsTable [attackTargets [0]]);
				}
			} else {
				GameObject bestTarget = attackTargets.OrderBy (x => game.unitsTable [x].GetComponent<Unit> ()._hp).ThenBy (x => Distance2D (x, preferedTarget.gameObject)).First ();
				if (BetterWithDuel (game.unitsTable [bestTarget])) {
					UsePerk (bestTarget);
				} else {
					Attack (game.unitsTable [bestTarget]);
				}
			}
			return "success";
		} else {
			return "no target"; 
		}
	}

	private bool BetterWithDuel (GameObject i) {
		Unit u = i.GetComponent<Unit> ();
		if (((u.hp - u._hp) * 0.15f + 10f) * _stability * (1f - u.meleeResistance) - u._defense >= u._hp) {
			return true;
		}
		return false; 
	}
}
