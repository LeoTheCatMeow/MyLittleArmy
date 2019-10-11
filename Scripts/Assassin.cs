using System.Collections;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;

public class Assassin : Unit {

	private Vector3 originalPosition;

	public override void Start() {
		base.Start ();
		perkInfo = "(Passive) Relentless: Attacks deal 2x dmg against isolated targets (no ally nearby)";
		unitMoveSpeed = 0.1f;
	}

	public override void AttackAnimationHit () {
		Unit target = currentAttackTarget.GetComponent<Unit> ();
		float damage = Random.Range (_attack * _stability, _attack);
		if (currentAttackTarget.transform.position.y - gameObject.transform.position.y > 0.25f) {
			damage *= 0.8f;
		}
		if (target.isolated) {
			damage *= 2.0f;
		}
		target.Hit ((int)damage);
		GetComponent<AudioSource> ().PlayOneShot (onHitSoundEffect, 1f);
	}

	//additional event receivers
	public void OnBlink() {
		unitStatus.SetActive (false);
		originalPosition = gameObject.transform.position;
		gameObject.transform.position = currentAttackTarget.transform.position;
	}

	public void OnReturn() {
		Vector3 displacement = (transform.position - originalPosition) / 10f;
		StartCoroutine (Return (displacement));
	}

    //for return motion
	private IEnumerator Return(Vector3 displacement) {
		for (int i = 0; i < 10; i++) {
			transform.position = transform.position - displacement;
			yield return new WaitForSeconds (0.03f);
		}
		unitStatus.SetActive (true);
		AttackAnimationOver ();
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
			//specfic to assassin 
			List<GameObject> isolatedTargets = new List<GameObject>();
			foreach (GameObject i in attackTargets) {
				GameObject u = game.unitsTable [i];
				if (u == null) {
					continue;
				}
				if (u.GetComponent<Unit> ().isolated) {
					isolatedTargets.Add (u); 
				}
			}
			if (isolatedTargets.Contains (preferedTarget.gameObject)) {
				Attack (preferedTarget.gameObject);
				return "success";
			} else if (isolatedTargets.Count == 1) {
				Attack (isolatedTargets [0]);
				return "success";
			} else if (isolatedTargets.Count > 1) {
				GameObject bestChoice = isolatedTargets.OrderBy (x => x.GetComponent<Unit> ()._hp).ThenBy (x => Distance2D (x, preferedTarget.gameObject)).First ();
				Attack (bestChoice);
				return "success";
			}
			//
			if ((strategy == "aggressive" || preferedTarget._range * game.unitSpaceDiameter + 0.01 < Distance2D (gameObject, preferedTarget.gameObject)) && attackTargets.Contains (preferedTarget.currentUnitSpace)) {
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
				} else {
					GameObject bestTarget = outRangedTargets.OrderBy (x => x.transform.position.y).ThenBy (x => game.unitsTable [x].GetComponent<Unit> ()._hp).ThenBy (x => Distance2D (x, preferedTarget.gameObject)).First ();
					Attack (game.unitsTable [bestTarget]);
				}
				return "success";
			} 
			if (attackTargets.Count == 1) {
				Attack (game.unitsTable [attackTargets [0]]);
			} else {
				GameObject bestTarget = attackTargets.OrderBy (x => game.unitsTable [x].GetComponent<Unit> ()._hp).ThenBy (x => Distance2D (x, preferedTarget.gameObject)).First ();
				Attack (game.unitsTable [bestTarget]);
			}
			return "success";
		} else {
			return "no target"; 
		}
	}
}
