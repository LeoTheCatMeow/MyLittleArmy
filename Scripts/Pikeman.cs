using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Pikeman : Unit {

	public override void Start() {
		base.Start ();
		perkInfo = "(Passive) Piercing Stab: Attacks ignore 50% of the target's armor";
	}

	public override void AttackAnimationHit () {
		Unit target = currentAttackTarget.GetComponent<Unit> ();
		float damage = Random.Range (_attack * _stability, _attack);
		if (currentAttackTarget.transform.position.y - gameObject.transform.position.y > 0.25f) {
			damage *= 0.8f;
		}
		damage += target._defense * 0.5f;
		target.Hit ((int)damage);
		GetComponent<AudioSource> ().PlayOneShot (onHitSoundEffect, 1f);
	}
}
