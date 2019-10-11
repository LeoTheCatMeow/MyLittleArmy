using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Swordman : Unit {

	[Header("Specific to Type")]
	public GameObject shield;
	public GameObject onHitGuardEffect;
	public AudioClip onHitGuardSoundEffect;
	private bool isGuarding = false;
	private bool recentlyGuarded = false;

	public override void Start() {
		base.Start ();
		perkInfo = "Guard: Gain 30 armor against the next attack";
	}

	public override void Attack (GameObject target, bool active = true) {
		if (recentlyGuarded && active == false) {
			recentlyGuarded = false;
			game.pauseInput = false;
			return;
		}
		if (isGuarding) {
			gameObject.GetComponent<Animator> ().SetFloat ("guardAnimSpeed", 1f);
		}
		game.pauseInput = true;
		StartCoroutine(ProceedToAttack (target, active));
	}

	private IEnumerator ProceedToAttack (GameObject target, bool active = true) {
		while (isGuarding) {
			yield return null; 
		}
		base.Attack (target, active);
	}

	public override void UsePerk (GameObject target) {
		base.UsePerk (target);
		_defense += 30;
		isGuarding = true;
		perkAvailable = false;
		gameObject.GetComponent<Animator> ().Play ("Guard");
	}

	public override void Hit (int damage, string type = "melee") {
		unTouched = false;
		if (type == "melee") {
			damage = (int)(damage * (1f - meleeResistance));
		} else {
			damage = (int)(damage * (1f - rangedResistance));
		}
		damage -= _defense;
		if (damage <= 0) {
			damage = 1;
		}
		if (_hp > damage) {
			_hp -= damage;
		} else {
			_hp = 0;
		}

		//UI update 
		hpIndicator.color = new Color (1f - ((float)_hp - 0.5f * (float)hp) / (0.5f * (float)hp), 1f + ((float)_hp - 0.5f * (float)hp) / (0.5f * (float)hp), 0f);
		hpText.text = _hp.ToString ();

		//anim and effects
		dmgText.text = damage.ToString ();
		dmgText.gameObject.GetComponent<Animator> ().Play ("Show");
		if (isGuarding) {
			Instantiate (onHitGuardEffect, shield.transform.position, Quaternion.Euler (0f, transform.eulerAngles.y, 0f));
			GetComponent<Animator> ().SetFloat ("guardAnimSpeed", 1f);
			GetComponent<AudioSource> ().PlayOneShot (onHitGuardSoundEffect, 1f);
			recentlyGuarded = true; 
		} else {
			GetComponent<Animator> ().Play ("Hit");
			Instantiate (onHitEffect, transform.Find ("VisualPivot").transform.position, Quaternion.Euler (0f, transform.eulerAngles.y + 180f, 0f));
		}
	}

	public override void FindPerkTargets (List<GameObject> resultsContainer) {
		if (resultsContainer.Count == 0) {
			resultsContainer.Add (currentUnitSpace);
		}
	}

	//event receivers
	public void OnGuard() {
		GetComponent<Animator> ().SetFloat ("guardAnimSpeed", 0f);
	}

	public void OffGuard() {
		_defense -= 30;
		isGuarding = false;
		if (_focus > 0) {
			perkAvailable = true;
		}
	}

	public override void RoundOver () {
		base.RoundOver ();
		if (isGuarding) {
			perkAvailable = false;
		}
	}

	//AI
	protected override void AIFinishUp() {
		if (_focus > 0 && !isGuarding) {
			UsePerk (gameObject);
		}
		base.AIFinishUp ();
	}
}
