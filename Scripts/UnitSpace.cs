using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpace : MonoBehaviour {

	internal GameController game;
	internal List<GameObject> adjacentUnitSpaces;
	internal string type; 
	internal int movementValue;
	internal float priority;

	void Start () {
		type = transform.Find ("top").GetComponent<MeshRenderer> ().material.name.Split (' ') [0];
		movementValue = 1;
		priority += transform.position.y;
		if (type == "Sand") {
			movementValue = 2;
			priority -= 0.3f;
		}

		if (type == "metal") {
			priority += 0.5f; 
		}

		if (type == "Rock") {
			movementValue = 3;
			priority += 0.3f;
		}
	}

	public void FindAdjacentUnitSpaces() { //called from game
		adjacentUnitSpaces = new List<GameObject> ();
		foreach (GameObject i in game.unitSpaces) {
			if (i != gameObject && Unit.Distance2D (gameObject, i) < game.unitSpaceDiameter + 0.01f) {
				adjacentUnitSpaces.Add (i);
			}
		}
	}

	public void OnLocationBonuses(Unit u, int state) {//1 for state = enter, -1 for state = leave
		if (type == "Sand") {
			u._stability -= state * 0.3f;
			if (u is Assassin) {
				u._range -= state * 1f;
			}
		}

		if (type == "Metal") {
			u._stability += state * 0.1f;
			u._attack += (int)(state * u.attack * 0.15f);
			u.meleeResistance += state * 0.15f;
		}

		if (type == "Rock") {
			u._attack -= (int)(state * u.attack * 0.3f);
			u._defense += state * 2;
			u.meleeResistance += state * 0.2f;
			u.rangedResistance += state * 0.35f;
		}

		if (type == "Lava") {
			u._attack += (int)(state * u.attack * 0.5f);
			u._defense -= state * u.defense;
			u.meleeResistance -= state * 0.1f;
		}
	} 

	public void RoundOverBonuses (Unit u) {
		if (type == "Wood" && u.unTouched) {
			if (u.hp - u._hp > u.hp / 10) {
				u._hp += u.hp / 10;
			} else {
				u._hp = u.hp;
			}
			u.hpText.text = u._hp.ToString();
			Transform visualPivot = u.gameObject.transform.Find ("VisualPivot");
			Instantiate (u.healEffect, visualPivot.position, Quaternion.identity);
		}

		if (type == "Lava") {
			if (u._hp <= 10) {
				u.hpText.text = "0";
				u.OnDeath (null);
			} else {
				u._hp -= 10;
				u.hpText.text = u._hp.ToString();
			}
		}
	}
}
