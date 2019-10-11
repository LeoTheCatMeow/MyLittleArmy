using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Titan : Unit {

	[Header("Specific to Type")]
	public GameObject impactEffect;
	private List<Unit> subTargets = new List<Unit>();

	public override void Start() {
		base.Start ();
		MeshRenderer[] meshes = GetComponentsInChildren<MeshRenderer> ();
		foreach (MeshRenderer mesh in meshes) {
			if (mesh.gameObject.name == "ShoulderL" || mesh.gameObject.name == "ShoulderR") {
				mesh.material = subMat;
			}
		}
		perkInfo = "(Passive) Devastating: Each attack damages all enemies in a small area";
		unitMoveSpeed = 0.02f;
	}

	//event receivers
	public override void AttackAnimationHit () {
		base.AttackAnimationHit ();
		float damage = Random.Range (_attack * _stability, _attack) * 0.35f;
		foreach (var pair in game.unitsTable) {
			GameObject i = pair.Value;
			if (i == null || i == currentAttackTarget) {
				continue;
			}
			if (Distance2D (i, gameObject) > 2 * game.unitSpaceDiameter + 0.01f || Mathf.Abs(currentAttackTarget.transform.position.y - transform.position.y) >= 0.25f) {
				continue;
			}
			Vector3 toTarget = i.transform.position - transform.position;
			if (Mathf.Abs (Vector3.Angle (transform.forward, toTarget)) < 35f) {
				Unit u = i.GetComponent<Unit> ();
				u.Hit ((int)damage);
				subTargets.Add (u);
			}
		}

		Instantiate (impactEffect, currentAttackTarget.transform.position, transform.rotation);
	}

	public override void AttackAnimationOver() {
		Unit target = currentAttackTarget.GetComponent<Unit> ();
		if (target._hp == 0) {
			target.OnDeath (this);
			if (!selfDefense && Distance2D (gameObject, currentAttackTarget) < game.unitSpaceDiameter + 0.01f) {
				Move (target.currentUnitSpace, false);
			} else {
				game.pauseInput = false;
			}
			currentAttackTarget = null;
			return;
		} 
		foreach (Unit u in subTargets) {
			if (u._hp == 0) {
				u.OnDeath (this);
			}
		}
		if (!selfDefense && target._focus > 0 && Distance2D (gameObject, currentAttackTarget) <= target._range * game.unitSpaceDiameter + 0.01f) {
			currentAttackTarget.GetComponent<Unit> ().Attack (gameObject, false);
		} else {
			selfDefense = false;
			game.pauseInput = false;
		}
		currentAttackTarget = null;
		subTargets.Clear ();
	}
}
