using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour {

	//stats
	[Header("General Stats")]
	public int hp;
	internal int _hp;
	public int attack;
	internal int _attack;
	internal float stability = 0.9f;
	internal float _stability;
	public int mobility;
	internal int _mobility;
	internal int stamina = 1;
	internal int _stamina;
	public int defense;
	internal int _defense;
	internal float meleeResistance = 0f;
	internal float rangedResistance = 0f;
	public float range;
	internal float _range;
	public int focus;
	internal int _focus;
	protected float unitMoveSpeed = 0.06f;

	//references
	internal GameController game;
	protected GameObject mainCam;
	protected GameObject unitStatus;
	internal GameObject currentAttackTarget;
	internal GameObject currentUnitSpace;
	internal GameObject lastPosition;
	public SpriteRenderer hpIndicator;
	public Text hpText, dmgText;
	public GameObject onHitEffect, healEffect, deathAnim;
	public AudioClip onPerkSoundEffect, onMoveSoundEffect, onHitSoundEffect; 
	public Sprite perkIcon;
	public Material mat, subMat;

	//AI
	public float priority; //importance as a target 
	internal float sortiePriority = 0f; //priority in move order
	internal Unit preferedTarget = null; 
	internal GameObject strategicPoint = null; 
	internal bool done = false;
	//AI substats
	internal float escapeThreshold = 0.35f; //%hp when the AI will seek to escape 

	//state 
	internal bool movementAvailable = true;
	internal bool attackAvailable = true;
	internal bool perkAvailable = true;
	internal bool unTouched = true; 
	protected bool selfDefense = false;
	internal string morale;
	protected float[] moraleBonuses = new float[] {0, 0}; //attack, defense
	internal bool isolated; //no ally nearby

	//info 
	internal string moveInfo;
	internal string switchInfo;
	internal string attackInfo;
	internal string perkInfo;
	internal string accessableTerrain;

	//methods
	public virtual void Start () {
		_hp = hp;
		_attack = attack;
		_stability = stability;
		_mobility = mobility;
		_stamina = stamina;
		_focus = focus;
		_range = range;
		_defense = defense;

		hpText.text = _hp.ToString();
		unitStatus = hpIndicator.transform.parent.gameObject;
		mainCam = GameObject.FindGameObjectWithTag ("MainCamera");
		MeshRenderer[] meshes = GetComponentsInChildren<MeshRenderer> ();
		foreach (MeshRenderer mesh in meshes) {
			if (mesh.gameObject.tag == "Weapon") {
				continue;
			}
			if (mesh.materials.Length > 1) {
				Material[] newMats = new Material[] { mat, subMat };
				mesh.materials = newMats;
			}
			mesh.material = mat;
		}

		moveInfo = "Command - Move: Choose an empty location";
		switchInfo = "Command - Switch: Choose a nearby ally";
		attackInfo = "Command - Attack: Choose an enemy within range";
		accessableTerrain = "Wood Rock Metal Sand Lava";
	}

	void Update () {
		float angle = Mathf.Atan2 (mainCam.transform.position.z - transform.position.z, mainCam.transform.position.x - transform.position.x) * 180f / Mathf.PI;
		unitStatus.transform.eulerAngles = new Vector3 (0f, -angle - 90f, 0f);
	}

	public virtual void Switch (GameObject target) {
		GameObject temp = game.unitsTable [target];
		game.unitsTable [target] = gameObject;
		game.unitsTable [currentUnitSpace] = temp;
		StartCoroutine (temp.GetComponent<Unit> ()._Move (currentUnitSpace, false));
		StartCoroutine (_Move (target));
		GetComponent<AudioSource> ().PlayOneShot (onMoveSoundEffect, 1f);
	}

	public virtual void Move (GameObject target, bool active = true) {
		game.unitsTable [target] = gameObject;
		if (game.unitsTable [currentUnitSpace] == gameObject) {
			game.unitsTable [currentUnitSpace] = null;
		}
		StartCoroutine (_Move (target, active));
		GetComponent<AudioSource> ().PlayOneShot (onMoveSoundEffect, 1f);
	}

	protected IEnumerator _Move (GameObject target, bool active = true) {
		game.pauseInput = true;
		if (active) { //passive movements don't count
			_stamina--;
			movementAvailable = false;
		}
		unTouched = false;
		currentUnitSpace.GetComponent<UnitSpace> ().OnLocationBonuses (this, -1);
		lastPosition = currentUnitSpace;
		unitStatus.SetActive (false);

		Vector3 t = target.transform.position;
		while (transform.position != t) {
			Vector2 movement = new Vector2 (t.x - transform.position.x, t.z - transform.position.z);
			if (movement.magnitude < unitMoveSpeed) { //default unit move speed is 0.07
				transform.position = new Vector3 (t.x, transform.position.y, t.z);
				break;
			} 
			transform.position  = transform.position + new Vector3 (movement.normalized.x * unitMoveSpeed, 0f, movement.normalized.y * unitMoveSpeed);
			yield return new WaitForFixedUpdate ();
		}
			
		currentUnitSpace = target;
		CheckSurroundings (true);
		currentUnitSpace.GetComponent<UnitSpace> ().OnLocationBonuses (this, 1);

		unitStatus.SetActive (true);
		game.pauseInput = false;
	}

	public void CheckSurroundings(bool extended = false) { //called from game or after every move
		//reset bonus stats
		_attack -= (int)moraleBonuses[0];
		meleeResistance -= (int)moraleBonuses[1];

		//check surroundings
		int enemiesAround = 0;
		int alliesAround = 0;
		List<GameObject> adjacentUnitSpaces = currentUnitSpace.GetComponent<UnitSpace> ().adjacentUnitSpaces;
		List<GameObject> alreadyChecked = new List<GameObject> ();
		foreach (GameObject u in adjacentUnitSpaces) {
			GameObject i = game.unitsTable[u];
			if (i == null) {
				continue;
			}
			Material otherMat = i.GetComponent<Unit> ().mat;
			if (mat == otherMat) {
				alliesAround++;
			} else {
				enemiesAround++;
			}
			if (extended) {
				i.GetComponent<Unit> ().CheckSurroundings ();
				alreadyChecked.Add (i);
			}
		}
		if (extended) {
			List<GameObject> lastAdjacentUnitSpaces = lastPosition.GetComponent<UnitSpace> ().adjacentUnitSpaces;
			foreach (GameObject u in lastAdjacentUnitSpaces) {
				GameObject i = game.unitsTable[u];
				if (i == null) {
					continue;
				}

				if (i == gameObject) {
					continue;
				}
				if (!alreadyChecked.Contains (i)) {
					i.GetComponent<Unit> ().CheckSurroundings ();
				}
			}
		}
		if (alliesAround > 0 && alliesAround >= enemiesAround) {
			morale = "reinforced";
			moraleBonuses = new float[] { 0f, 0.1f };
		} else if (enemiesAround > 2 && enemiesAround - alliesAround > 2) {
			morale = "overwhelmed";
			moraleBonuses = new float[] { -0.3f * attack, -0.1f };
		} else {
			morale = "normal";
			moraleBonuses = new float[] { 0f, 0f };
		}
		if (alliesAround == 0) {
			isolated = true;
		} else {
			isolated = false;
		}

		//apply bonust stats
		_attack += (int)moraleBonuses[0];
		meleeResistance += (int)moraleBonuses[1];
	}

	public virtual void Attack (GameObject target, bool active = true) {
		game.pauseInput = true; 
		unTouched = false;
		_focus--;
		currentAttackTarget = target;
		if (_focus == 0) {
			attackAvailable = false;
			perkAvailable = false;
		}
		if (!active) { //this is a counter attack instead of an aggressive decision
			selfDefense = true;
		}
		float angle = Mathf.Atan2 (target.transform.position.x - transform.position.x, target.transform.position.z - transform.position.z) * 180f / Mathf.PI;
		transform.eulerAngles = new Vector3 (0f, angle, 0f);
		target.transform.eulerAngles = new Vector3 (0f, angle - 180f, 0f);
		GetComponent<Animator> ().Play ("Attack");
	}

	public virtual void Hit (int damage, string type = "melee") {
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
		GetComponent<Animator> ().Play ("Hit");
		Instantiate (onHitEffect, transform.Find ("VisualPivot").transform.position, Quaternion.Euler (0f, transform.eulerAngles.y + 180f, 0f));
	}

	public virtual void UsePerk (GameObject target) {
		unTouched = false;
		_focus--; 
		if (_focus == 0) {
			attackAvailable = false;
			perkAvailable = false;
		}
		GetComponent<AudioSource> ().PlayOneShot (onPerkSoundEffect, 1f);
	}

	//search methods 
	public virtual void FindPaths (List<GameObject> resultsContainer) {
		if (resultsContainer.Count == 0) { //if no path info is ready to show, find paths
			_FindPaths (currentUnitSpace, _mobility, resultsContainer); //rescursive flood-fill search 
		}
	}

	protected virtual void _FindPaths (GameObject origin, int steps, List<GameObject> resultsContainer) {
		UnitSpace _origin = origin.GetComponent<UnitSpace> ();
		foreach (GameObject i in _origin.adjacentUnitSpaces) {
			UnitSpace u = i.GetComponent<UnitSpace> ();
			if (game.unitsTable [i] != null || !accessableTerrain.Contains(u.type) || !CheckVerticalD (currentUnitSpace, i)) {
				continue;
			}
			float verticalDifference = i.transform.position.y - origin.transform.position.y; //order matters! don't use CheckVerticalD here
			if (!i.Equals (origin) && Distance2D (origin, i) < game.unitSpaceDiameter + 0.01f) { 
				if (steps >= 1 && verticalDifference <= 0f) { //on the same level or going downhill
					if (!resultsContainer.Contains (i)) { 
						resultsContainer.Add (i);
					}
					_FindPaths (i, steps - i.GetComponent<UnitSpace>().movementValue, resultsContainer);
				} else if (verticalDifference < 0.75f && origin == currentUnitSpace) { //going up hill
					resultsContainer.Add (i);
				}
			}
		}
	}

	public virtual void FindTargets (List<GameObject> resultsContainer) {
		if (resultsContainer.Count == 0) { 
			foreach (var pair in game.unitsTable) {
				GameObject i = pair.Value;
				if (i == null) {
					continue;
				}
				if (i != gameObject && i.GetComponent<Unit> ().mat != game.currentController
					&& Distance2D (gameObject, i) < _range * game.unitSpaceDiameter + 0.01f
					&& CheckVerticalD (i, gameObject)) {
					resultsContainer.Add (pair.Key);
				}
			}
		}
	}

	public virtual void FindSwitches (List<GameObject> resultsContainer) {
		if (resultsContainer.Count == 0) { 
			List<GameObject> adjacentUnitSpaces = currentUnitSpace.GetComponent<UnitSpace> ().adjacentUnitSpaces;
			foreach (GameObject u in adjacentUnitSpaces) {
				GameObject i = game.unitsTable [u];
				if (i == null) {
					continue;
				}
				if (i.GetComponent<Unit> ().mat == mat && CheckVerticalD (i, gameObject)) {
					resultsContainer.Add (u);
				}
			}
		}
	}

	public virtual void FindPerkTargets (List<GameObject> resultsContainer) { }

    //game methods
	public void TurnOver () { //called from game
		_focus = focus;
	}

	public virtual void RoundOver () { //called from game 
		//check bonuses
		currentUnitSpace.GetComponent<UnitSpace> ().RoundOverBonuses (this);

		//reset variables
		_stamina = stamina;
		movementAvailable = true;
		attackAvailable = true;
		perkAvailable = true;
		done = false;
	}

	public void OnDeath (Unit Killer) {
		GameObject obj = Instantiate (deathAnim, transform.position, Quaternion.identity);
		//change the effect color based on the color of the unit
		ParticleSystem.MainModule m = obj.transform.Find ("Smoke").GetComponent<ParticleSystem> ().main;
		m.startColor = mat.color;

		game.unitsTable [currentUnitSpace] = null;
		foreach (var pair in game.unitsTable) {
			GameObject i = pair.Value;
			if (i == null) {
				continue;
			}
			if (i != gameObject && Distance2D (gameObject, i) < game.unitSpaceDiameter + 0.01f) {
				i.GetComponent<Unit> ().CheckSurroundings ();
			}
		}

		game.totalForces [mat] -= hp; 
		game.totalForcesDetails [mat].Remove (this);
		if (game.totalForces [mat] == 0) {
			game.StartCoroutine (game.Eliminate (mat));
		}

		Destroy (gameObject);
	}

	//animation event receiver
	public virtual void AttackAnimationHit () {
		float damage = Random.Range (_attack * _stability, _attack);
		if (currentAttackTarget.transform.position.y - gameObject.transform.position.y > 0.25f) {
			damage *= 0.8f;
		}
		currentAttackTarget.GetComponent<Unit> ().Hit ((int)damage);
		GetComponent<AudioSource> ().PlayOneShot (onHitSoundEffect, 1f);
	}

	public virtual void AttackAnimationOver() {
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
		if (!selfDefense && target._focus > 0 && Distance2D (gameObject, currentAttackTarget) <= target._range * game.unitSpaceDiameter + 0.01f) {
			currentAttackTarget.GetComponent<Unit> ().Attack (gameObject, false);
		} else {
			selfDefense = false;
			game.pauseInput = false;
		}
		currentAttackTarget = null;
	}

	//AI 
	public virtual void Act (Dictionary<GameObject, int> map, string strategy) {
		string result = "";
		if (strategy == "defensive") {
			result = AITryAquireStrategicPoint (map);
			if (result == "success") {
				return;
			}
		}
		result = AITryAttack (map, strategy);
		if (result == "success") {
			return;
		} else if (result == "no target") {
			if (strategy == "aggressive") {
				result = AITryApproach (map);
				if (result == "success") {
					return;
				} else if (result == "unable to move") {
					goto Finish;
				} else if (result == "unable to get closer") {
					result = AITryManeuver (map, result);
					if (result == "success") {
						return;
					} else if (result == "unable or no need") {
						goto Finish;
					}
				}
			} else if (strategy == "defensive") {
				result = AITryAquireStrategicPoint (map);
				if (result == "success") {
					return; 
				} else if (result == "unable or no need") {
					goto Finish;
				}
			}
		} else if (result == "have targets, no focus") {
			if (morale == "overwhelmed") {
				result = AITryManeuver (map, "try resposition");
				if (result == "success") {
					goto Finish;
				}
			} else {
				result = AITryManeuver (map, "seek switches");
				goto Finish;
			}
		} 
		Finish: AIFinishUp ();
	}

	protected virtual string AITryAquireStrategicPoint (Dictionary<GameObject, int> map) {
		if (!movementAvailable) {
			return "unable or no need";
		}
		if (currentUnitSpace.GetComponent<UnitSpace> ().priority >= strategicPoint.GetComponent<UnitSpace> ().priority) {
			return "unable or no need";
		}
		List<GameObject> moveTargets = new List<GameObject> ();
		FindPaths (moveTargets);
		moveTargets.RemoveAll (x => map [x] > map [currentUnitSpace]);
		if (moveTargets.Count > 0) {
			if (moveTargets.Contains(strategicPoint)) {
				Move (strategicPoint);
			} else {
				GameObject currentBestChoice = moveTargets.OrderBy (x => map[x]).ThenByDescending (x => x.GetComponent<UnitSpace>().priority).First ();
				Move (currentBestChoice); 
			}
			return "success";
		} else {
			return "unable or no need";
		} 
	}

	protected virtual string AITryAttack (Dictionary<GameObject, int> map, string strategy) {
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

	protected virtual string AITryApproach (Dictionary<GameObject, int> map) {
		if (!movementAvailable) {
			return "unable to move";
		}
		List<GameObject> moveTargets = new List<GameObject> ();
		FindPaths (moveTargets);
		if (moveTargets.Count > 0) {
			//this next line is a little problematic, the intention is to prevent ranged units to get too upfront, but the method can negatively affect formation
			moveTargets.RemoveAll (i => (CheckVerticalD (i, preferedTarget.currentUnitSpace) && Distance2D (i, preferedTarget.currentUnitSpace) < (_range - 0.2f) * game.unitSpaceDiameter - 0.01f));
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

	protected virtual string AITryManeuver (Dictionary<GameObject, int> map, string state) {
		List<GameObject> moveTargets = new List<GameObject> ();
		FindPaths (moveTargets);
		List<GameObject> switchTargets = new List<GameObject> ();
		FindSwitches (switchTargets);
		if (!movementAvailable) {
			return "unable or no need";
		}
		if (state == "unable to get closer") {
			if (switchTargets.Count > 0) {
				switchTargets = switchTargets.OrderBy (x => Distance2D (x, preferedTarget.gameObject)).ToList ();
				foreach (GameObject i in switchTargets) {
					Unit u = game.unitsTable [i].GetComponent<Unit> ();
					if (u._hp < _hp && map[i] < map[currentUnitSpace]) {
						Switch (i);
						return "success";
					} else if (u._range > _range && map[i] < map[currentUnitSpace]) {
						Switch (i);
						return "success";
					}
				}
				return "unable or no need";
			} else {
				return "unable or no need";
			}
		} else if (state == "try create distance") {
			if (switchTargets.Count > 0) {
				switchTargets = switchTargets.OrderBy (x => Distance2D (x, gameObject)).ToList ();
				foreach (GameObject i in switchTargets) {
					if (game.unitsTable [i].GetComponent<Unit> ()._range < _range) {
						Switch (i);
						return "success";
					}
				}
				return "unable or no need";
			} else {
				return "unable or no need";
			}
		} else if (state == "try reposition") {
			if (moveTargets.Count > 0) {
				for (int i = moveTargets.Count - 1; i >= 0; i--) {
					if (isABadSpot (moveTargets [i])) {
						moveTargets.Remove (moveTargets [i]);
					}
				}
				if (moveTargets.Count > 0) {
					GameObject bestChoice = moveTargets.OrderBy (x => map [x]).ThenByDescending (x => x.GetComponent<UnitSpace> ().priority).First ();
					Move (bestChoice);
					return "success";
				} else {
					return "unable or no need";
				}
			} else {
				return "unable or no need";
			}
		} else if (state == "seek switches") {
			if (switchTargets.Count > 0 && (float)_hp / (float)hp <= escapeThreshold) {
				GameObject farthest = switchTargets.OrderByDescending (x => map[x]).First ();
				if (map[farthest] > map[currentUnitSpace]) {
					Switch (farthest);
					return "success";
				} else {
					return "unable or no need";
				}
			} else {
				return "unable or no need";
			}
		} 
		return null; 
	}

	protected virtual void AIFinishUp () {
		done = true;
	}
		
	//useful
	public static float Distance2D (GameObject a, GameObject b) {
		Vector2 A = new Vector2 (a.transform.position.x, a.transform.position.z);
		Vector2 B = new Vector2 (b.transform.position.x, b.transform.position.z);
		return Vector2.Distance (A, B);
	}  

	public static float Distance2D (Vector3 a, Vector3 b) {
		Vector2 A = new Vector2 (a.x, a.z);
		Vector2 B = new Vector2 (b.x, b.z);
		return Vector2.Distance (A, B);
	}  

	public static bool CheckVerticalD (GameObject a, GameObject b) { 
		if (Mathf.Abs (a.transform.position.y - b.transform.position.y) < 0.75f) {
			return true;
		} else {
			return false;
		}
	}

	public bool isABadSpot (GameObject u) {
		List<GameObject> adjacentUnitSpaces = u.GetComponent<UnitSpace> ().adjacentUnitSpaces;
		int enemies = 0;
		int allies = 0;
		foreach (GameObject _u in adjacentUnitSpaces) {
			GameObject i = game.unitsTable [_u];
			if (i == null) {
				continue;
			}
			if (i.GetComponent<Unit> ().mat == mat) {
				allies++;
			} else {
				enemies++;
			}
		}
		if (enemies > 2 && enemies - allies > 1) {
			return true;
		} else {
			return false; 
		}
	}
}
