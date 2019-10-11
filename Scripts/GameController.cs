using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour {

	//mats and colors 
	public Material matSelect;
	public Material matNormal;
	public Material matIndicator;
	public Material[] unitMats;
	private Color yellow1 = new Color (251f / 255f, 1f, 0f, 80f / 255f);
	private Color yellow2 = new Color (252f / 255f, 1f, 0f, 80f / 255f);
	private Color red = new Color (1f, 0f, 0f, 80f / 255f);
	private Color blue = new Color (0f, 130f / 255f, 1f, 130f / 255f);
	private Color green = new Color (0f, 1f, 30f / 255f);
	private Color yellowFull = new Color (251f / 255f, 1f, 0f);
	private Color white = new Color (1f, 1f, 1f);

	//references
	public GameObject mainCam;
	public ActionMenu actionMenu;
	public GameObject infoPanel;
	public GameObject roundLabel;
	public GameObject roundOverButton, confirmLabel, confirmButton; 
	public Text actionInfo;
	public Text roundInfo;

	public Image selectionPropertyPanelMainIcon;
	public Sprite unitIcon;
	public Sprite unitSpaceIcon;
	public GameObject selectionPropertyPanelSubIcons;
	public Text unitStatAtk;
	public Text unitStatMob;
	public Text unitStatDef;
	public Text unitStatRan;
	public Text unitStatFoc;
	public Image unitMoraleIcon;
	public Image terrainIcon;
	public Image movementIndicator;
	public Sprite iconNormal;
	public Sprite iconReinforced;
	public Sprite iconOverwhelmed;
	public Sprite iconWood, iconMetal, iconSand, iconRock, iconLava, iconWater;

	public AudioClip unitSpaceSelectionSound, actionSelectionSound, roundLabelSoundEffect, nextTurnSoundEffect, winSoundEffect, lossSoundEffect;

	//UnitSpace and Units
	internal float unitSpaceDiameter = 1.7321f;
	internal GameObject[] units;
	internal GameObject[] unitSpaces;
	internal Dictionary<GameObject, GameObject> unitsTable;
	internal Dictionary<Material, int> totalForces; //keeps track of how much total unit hp each faction still has
	internal Dictionary<Material, int> totalForcesOriginal; //original state
	internal Dictionary<Material, List<Unit>> totalForcesDetails;
	internal GameObject currentlySelectedUnitSpace;
	internal GameObject currentlySelectedUnit;
	internal Unit _currentlySelectedUnit; //the ontrolling script of the currently selected unit 
	private List<GameObject> availablePaths = new List<GameObject> ();
	private List<GameObject> availableTargets = new List<GameObject> ();
	private List<GameObject> availableSwitches = new List<GameObject> ();
	private List<GameObject> availablePerkTargets = new List<GameObject> ();

	//state
	internal Material currentController;
	internal bool pauseInput = true;
	private bool isSelectingActionTarget = false;
	private int round = 0;

	void Awake () {
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = 30;
	}

	void Start () {
		EstablishReferences ();
		InitializeUI ();
	}

	private void EstablishReferences() {
		//associate units and their spaces together in a dictionary
		units = GameObject.FindGameObjectsWithTag ("Unit");
		unitSpaces = GameObject.FindGameObjectsWithTag ("UnitSpace");
		unitsTable = new Dictionary<GameObject, GameObject>();
		foreach (GameObject unitSpace in unitSpaces) {
			UnitSpace us = unitSpace.GetComponent<UnitSpace> ();
			us.game = this;
			us.FindAdjacentUnitSpaces ();
			foreach (GameObject unit in units) {
				//vertical position is ignored when finding pairs
				Vector2 parent = new Vector2 (unitSpace.transform.position.x, unitSpace.transform.position.z);
				Vector2 child = new Vector2 (unit.transform.position.x, unit.transform.position.z);
				//the radius of a unitSpace top is roughly 0.7f  
				if (Vector2.Distance(parent, child) < unitSpaceDiameter / 2 - 0.1f) {
					unitsTable.Add (unitSpace, unit);
					Unit u = unit.GetComponent<Unit> ();
					//center the unit if it is not in the exact center 
					unit.transform.position = new Vector3 (parent.x, unit.transform.position.y, parent.y);
					//pass a reference of the unitSpace to the unit
					u.currentUnitSpace = unitSpace;
					goto next; 
				}
			}
			unitsTable.Add (unitSpace, null); //only executed if the unitSpace isn't occupied by any unit 
			next: continue;
		}
			
		totalForcesOriginal = new Dictionary<Material, int> ();
		totalForces = new Dictionary<Material, int> ();
		totalForcesDetails = new Dictionary<Material, List<Unit>> ();
		foreach (Material mat in unitMats) { //initialize forces
			totalForcesOriginal.Add (mat, 0);
			totalForces.Add (mat, 0);
			totalForcesDetails.Add (mat, new List<Unit> ());
		}
	}

	private void InitializeUI () {
		infoPanel.SetActive (false);
		roundLabel.SetActive (false); 
		selectionPropertyPanelSubIcons.SetActive (false);
		selectionPropertyPanelMainIcon.gameObject.SetActive (false);
		terrainIcon.gameObject.SetActive (false);
	}

	public void StartGame () { //called from noteBoard
		mainCam.GetComponent<CameraControl>().pauseUpdate = false; 
		pauseInput = false; 
		if (round == 0) {
			//initialize units
			foreach (GameObject unit in units) { 
				Unit u = unit.GetComponent<Unit> ();
				u.game = this; 
				u.CheckSurroundings ();
				u.currentUnitSpace.GetComponent<UnitSpace> ().OnLocationBonuses (u, 1);
				totalForcesOriginal [u.mat] += u.hp; 
				totalForces [u.mat] += u.hp;
				totalForcesDetails [u.mat].Add (u);
			}
			//start the game
			round = 1;
			currentController = unitMats [0]; 
			StartCoroutine (NextTurn (unitMats[0]));
		}
	}

	public void SelectUnitSpace (GameObject target) { //called from TouchInputControl
		if (pauseInput || currentController != unitMats[0]) {
			return;
		}
		if (confirmButton.activeSelf) {
			confirmLabel.SetActive (false);
			confirmButton.SetActive (false);
		}
		if (!isSelectingActionTarget) {  //if a UnitSpace is selected to perform actions
			if (currentlySelectedUnitSpace != null) { //if something is selected already, deselect it
				GameObject _body = currentlySelectedUnitSpace.transform.Find ("base").gameObject;
				_body.GetComponent<Renderer> ().material = matNormal;
			}
			//change the base material to reflect the selection
			GameObject body = target.transform.Find ("base").gameObject;
			body.GetComponent<Renderer> ().material = matSelect;
			//play audio
			GetComponent<AudioSource>().PlayOneShot (unitSpaceSelectionSound, 1f);

			if (currentlySelectedUnitSpace != target) {
				//manage global variables, reset memory
				currentlySelectedUnitSpace = target;
				availablePaths.Clear ();
				availableTargets.Clear ();
				availableSwitches.Clear ();
				availablePerkTargets.Clear ();

				//move camera focus 
				mainCam.GetComponent<CameraControl> ().SetFocus (currentlySelectedUnitSpace);
				//highlight the unit currently occupying the space, if there's one
				if (unitsTable [currentlySelectedUnitSpace] != null) {
					currentlySelectedUnit = unitsTable [currentlySelectedUnitSpace];
					_currentlySelectedUnit = currentlySelectedUnit.GetComponent<Unit> ();
					//open up a menu for available actions
					if (currentController == unitMats[0] && _currentlySelectedUnit.mat == unitMats[0]) {
						actionMenu.Activate (currentlySelectedUnit);
					} else if (actionMenu.gameObject.activeSelf) {
						actionMenu.Deactivate ();
					}
					//show unit properties
					selectionPropertyPanelMainIcon.sprite = unitIcon;
					selectionPropertyPanelMainIcon.gameObject.SetActive (true);
					selectionPropertyPanelSubIcons.SetActive (true);
					ShowUnitStats (unitStatAtk, _currentlySelectedUnit._attack, _currentlySelectedUnit.attack);
					ShowUnitStats (unitStatMob, _currentlySelectedUnit._mobility, _currentlySelectedUnit.mobility);
					ShowUnitStats (unitStatFoc, _currentlySelectedUnit._focus, _currentlySelectedUnit.focus);
					ShowUnitStats (unitStatRan, _currentlySelectedUnit._range, _currentlySelectedUnit.range);
					ShowUnitStats (unitStatDef, _currentlySelectedUnit._defense, _currentlySelectedUnit.defense);
					if (_currentlySelectedUnit.movementAvailable) {movementIndicator.color = white;}
					else {movementIndicator.color = yellowFull;}
					if (_currentlySelectedUnit.morale == "reinforced") { unitMoraleIcon.sprite = iconReinforced; } 
					else if (_currentlySelectedUnit.morale == "overwhelmed") { unitMoraleIcon.sprite = iconOverwhelmed; } 
					else if (_currentlySelectedUnit.morale == "normal") { unitMoraleIcon.sprite = iconNormal; }
				} else {
					currentlySelectedUnit = null; 
					_currentlySelectedUnit = null;
					//close the menu since no unit is selected anymore
					actionMenu.Deactivate ();
					//hide unit property panel too
					selectionPropertyPanelMainIcon.sprite = unitSpaceIcon;
					selectionPropertyPanelMainIcon.gameObject.SetActive (true);
					selectionPropertyPanelSubIcons.SetActive (false);
				}
				//always show terrain info
				terrainIcon.gameObject.SetActive (true);
				string terrainType = currentlySelectedUnitSpace.GetComponent<UnitSpace> ().type;
				if (terrainType == "Wood") {terrainIcon.sprite = iconWood;}
				else if (terrainType == "Metal") {terrainIcon.sprite = iconMetal;}
				else if (terrainType == "Sand") {terrainIcon.sprite = iconSand;}
				else if (terrainType == "Rock") {terrainIcon.sprite = iconRock;}
				else if (terrainType == "Lava") {terrainIcon.sprite = iconLava;}
				else if (terrainType == "Water") {terrainIcon.sprite = iconWater;}
			}
		} else if (isSelectingActionTarget) { //if a UnitSpace is selected as the target of an action
			if (matIndicator.color == yellow1) { //indicates a move command
				if (availablePaths.Contains (target)) {
					ClearActionTargetSeletion ();
					_currentlySelectedUnit.Move (target);
					DeselectUnitSpace ();
				}
			} else if (matIndicator.color == yellow2) { //indicates a switch command 
				if (availableSwitches.Contains (target)) { 
					ClearActionTargetSeletion ();
					_currentlySelectedUnit.Switch (target);
					DeselectUnitSpace ();
				}
			} else if (matIndicator.color == red) { //indicates an attack command 
				if (availableTargets.Contains (target)) {
					ClearActionTargetSeletion ();
					_currentlySelectedUnit.Attack (unitsTable [target]);
					DeselectUnitSpace ();
				}
			} else if (matIndicator.color == blue) { //indicates a perk command 
				if (availablePerkTargets.Contains (target)) {
					ClearActionTargetSeletion ();
					_currentlySelectedUnit.UsePerk (target);
					DeselectUnitSpace ();
				}
			} 
		}
	}

	public void DeselectUnitSpace () {
		if (isSelectingActionTarget) { //can't deselect any UnitSpace during action target selection
			return;
		}
		if (currentlySelectedUnitSpace != null) {
			GameObject _body = currentlySelectedUnitSpace.transform.Find ("base").gameObject;
			_body.GetComponent<Renderer> ().material = matNormal;
			currentlySelectedUnitSpace = null; 
			_currentlySelectedUnit = null;
			actionMenu.Deactivate ();
			infoPanel.SetActive (false);
			selectionPropertyPanelSubIcons.SetActive (false);
			selectionPropertyPanelMainIcon.gameObject.SetActive (false);
			terrainIcon.gameObject.SetActive (false);
		}
	}

	private void ShowUnitStats (Text t, float current, float original) {
		t.text = current.ToString();
		if (current > original) {
			t.color = green;
		} else if (current < original) {
			t.color = yellowFull;
		} else {
			t.color = white;
		}
	}

	private void ShowUnitStats (Text t, int current, int original) {
		ShowUnitStats (t, (float)current, (float)original);
	}

	public void SelectAction (GameObject target) { //called from TouchInputControl
		if (!isSelectingActionTarget) {
			isSelectingActionTarget = true;
			actionMenu.CancelMenu ();
			infoPanel.SetActive (true);
			GetComponent<AudioSource> ().PlayOneShot (actionSelectionSound, 1f);

			if (target.name == "UI_TouchSphere_Move") {
				_currentlySelectedUnit.FindPaths (availablePaths);
				ShowActionTargetSelection (yellow1, availablePaths);
				actionInfo.text = _currentlySelectedUnit.moveInfo;

			} else if (target.name == "UI_TouchSphere_Switch") { 
				_currentlySelectedUnit.FindSwitches (availableSwitches);
				ShowActionTargetSelection (yellow2, availableSwitches);
				actionInfo.text = _currentlySelectedUnit.switchInfo;
			
			} else if (target.name == "UI_TouchSphere_Attack") {
				_currentlySelectedUnit.FindTargets (availableTargets);
				ShowActionTargetSelection (red, availableTargets);
				actionInfo.text = _currentlySelectedUnit.attackInfo;

			} else if (target.name == "UI_TouchSphere_Perk") {
				_currentlySelectedUnit.FindPerkTargets (availablePerkTargets);
				ShowActionTargetSelection (blue, availablePerkTargets);
				actionInfo.text = _currentlySelectedUnit.perkInfo;
			}

		} else if (isSelectingActionTarget) { 
			if (target.name == "UI_TouchSphere_Cancel") {
				ClearActionTargetSeletion ();
				infoPanel.SetActive (false);
				GetComponent<AudioSource> ().PlayOneShot (actionSelectionSound, 1f);
			}
		}
	}

	private void ShowActionTargetSelection (Color c, List<GameObject> list) {
		matIndicator.color = c;
		foreach (GameObject i in list) {
			GameObject indicator = i.transform.Find ("indicator").gameObject;
			indicator.SetActive (true);
		}
	}

	private void ClearActionTargetSeletion () {
		isSelectingActionTarget = false;
		if (actionMenu.gameObject.activeSelf) {
			actionMenu.MainMenu (currentlySelectedUnit);
		}
		foreach (var pair in unitsTable) {
			GameObject indicator = pair.Key.transform.Find ("indicator").gameObject;
			indicator.SetActive (false);
		}
	}

	public void ControllerDone() { //reset all units' stamina and focus, apply round bonuses if all factions have taken their turns
		confirmLabel.SetActive (false);
		confirmButton.SetActive (false);
		roundOverButton.GetComponent<Button> ().interactable = false;
		ClearActionTargetSeletion ();
		DeselectUnitSpace ();
		//taking turns 
		for (int i = 0; i < unitMats.Length; i++) {
			if (currentController == unitMats[unitMats.Length - 1]) {
				currentController = unitMats [0];
				roundOverButton.GetComponent<Button> ().interactable = true;
				round++;
				foreach (var pair in unitsTable) { 
					if (pair.Value != null) {
						pair.Value.GetComponent<Unit> ().RoundOver();
					}
				}
				StartCoroutine (NextTurn (currentController));
				break;
			}
			if (currentController == unitMats [i]) {
				currentController = unitMats [i + 1];
				StartCoroutine (NextTurn (currentController));
				break;
			}
		}

		//reset units
		foreach (var pair in unitsTable) {
			if (pair.Value != null) {
				pair.Value.GetComponent<Unit> ().TurnOver();
			}
		}

		//start AI
		if (currentController != unitMats [0]) {
			StartCoroutine(AIMakeDecision ());
		}
	}   

	public IEnumerator NextTurn (Material m) {
		pauseInput = true;
		Animator a = roundLabel.GetComponent<Animator> ();
		roundLabel.SetActive (true);
		roundInfo.text = "Round " + round.ToString (); 
		a.Play ("Show");
		GetComponent<AudioSource> ().PlayOneShot (nextTurnSoundEffect, 1f);
		yield return new WaitForSeconds (2f);
		roundInfo.text = m.name + "'s Turn";
		a.Play ("Show");
		GetComponent<AudioSource> ().PlayOneShot (roundLabelSoundEffect, 1f);
		yield return new WaitForSeconds (2f);
		roundLabel.SetActive (false);
		pauseInput = false;
	}

	public IEnumerator Eliminate (Material m) {
		while (pauseInput) {
			yield return null; 
		}
		pauseInput = true;
		totalForces.Remove (m);
		totalForcesDetails.Remove (m);
		Animator a = roundLabel.GetComponent<Animator> ();
		roundLabel.SetActive (true);
		roundInfo.text = m.name + " Has Been Eliminated";
		a.Play ("Show");
		GetComponent<AudioSource> ().PlayOneShot (roundLabelSoundEffect, 1f);
		yield return new WaitForSeconds (2f);

		if (m == unitMats [0]) {
			roundInfo.text = "Game Over";
			a.Play ("Show");
			GetComponent<AudioSource> ().PlayOneShot (lossSoundEffect, 1f);
			yield return new WaitForSeconds (6f);
			SceneManager.LoadScene ("_Main");
			yield break; //end level 
		}

		bool victory = true;
		foreach (var pair in totalForces) {
			if (pair.Key != unitMats [0] && pair.Value > 0) {
				victory = false;
			}
		}
	
		if (victory) {
			roundInfo.text = "Victorious";
			a.Play ("Show");
			GetComponent<AudioSource> ().PlayOneShot (winSoundEffect, 1f);
			yield return new WaitForSeconds (4f);
			SceneManager.LoadScene ("_Main");
			yield break; //end level
		} 

		roundLabel.SetActive (false);
		pauseInput = false;
	}

	public void PauseGame () {
		mainCam.GetComponent<CameraControl> ().pauseUpdate = true;
		pauseInput = true; 
	}

	public void RestartGame () {
		Scene scene = SceneManager.GetActiveScene ();
		SceneManager.LoadScene (scene.name); 
	}

	public void MainMenu () {
		SceneManager.LoadScene ("_Main");
	}

	//AI
	public IEnumerator AIMakeDecision() {
		while (true) {
			if (!pauseInput) {
				//establish references
				List<Unit> enemies = new List<Unit> ();
				foreach (var pair in totalForcesDetails) {
					if (pair.Key != currentController) {
						enemies.AddRange (pair.Value);
					}
				}
				List<Unit> availableUnits = new List<Unit> (totalForcesDetails [currentController]);

				//all done?
				availableUnits.RemoveAll (i => i.done);
				if (availableUnits.Count == 0) {
					yield return new WaitForSeconds (1f);
					ControllerDone ();
					yield break; 
				}

				//evaluate situation and decide which unit to move 
				float highestEnemyPriority = float.MinValue;
				float highestMyPriority = float.MinValue;
				foreach (Unit u in enemies) {
					if (u.priority > highestEnemyPriority) {
						highestEnemyPriority = u.priority;
					}
					foreach (Unit i in availableUnits) {
						if (i.priority > highestMyPriority) {
							highestMyPriority = i.priority;
						}
						float sortiePriority = u.priority / Unit.Distance2D (u.gameObject, i.gameObject);
						if (sortiePriority > i.sortiePriority) {
							i.sortiePriority = sortiePriority;
							i.preferedTarget = u;
						}
					}
				}
				foreach (Unit u in availableUnits) {
					float formationFactor = 1f; 
					if (u.isolated || u.morale == "overwhelmed") {
						formationFactor = 0.5f;
					}
					u.sortiePriority *= ((float)(u._stamina + u._focus) / (float)(u.stamina + u.focus)) * ((float)(u._hp) / (float)(u.hp)) * formationFactor;
				}
				Unit unitToAct = availableUnits.OrderByDescending (x => x.sortiePriority).First();

				//decide strategy 
				int myForce = totalForces [currentController];
				int enemyForce = 0;
				foreach (var pair in totalForces) {
					if (pair.Key != currentController) {
						enemyForce += totalForces [pair.Key];
					}
				}
				string strategy = "";
				if (enemyForce >= myForce && highestEnemyPriority < highestMyPriority) {
					strategy = "defensive";
				} else {
					strategy = "aggressive";
				}

				//generate a path finding map 
				Dictionary<GameObject, int> map = new Dictionary<GameObject, int> ();
				if (strategy == "defensive") {
					List<GameObject> _unitSpaces = new List<GameObject> (unitSpaces);
					_unitSpaces = _unitSpaces.OrderByDescending (x => x.GetComponent<UnitSpace> ().priority).ThenBy (x => Unit.Distance2D (unitToAct.gameObject, x)).ToList();
					unitToAct.strategicPoint = _unitSpaces[0];
					AIFloodFill (_unitSpaces[0], 0, map, unitToAct);
				} else {
					AIFloodFill (unitToAct.preferedTarget.currentUnitSpace, 0, map, unitToAct);
				}
					
				//set cam focus 
				mainCam.GetComponent<CameraControl> ().SetFocus (unitToAct.gameObject);

				//wait for a bit before taking action
				yield return new WaitForSeconds (1f);
				unitToAct.Act (map, strategy);

				//clear records
				unitToAct.strategicPoint = null; 
				foreach (Unit u in availableUnits) {
					u.sortiePriority = 0f;
					u.preferedTarget = null; 
				}
				yield return new WaitForSeconds (0.5f);
			} else {
				yield return null; 
			}
		}
	}

	public void AIFloodFill (GameObject origin, int index, Dictionary<GameObject, int> resultsContainer, Unit unitToAct) {
		if (index == 0) {
			resultsContainer.Add (origin, 0);
		}
		UnitSpace _origin = origin.GetComponent<UnitSpace> ();
		List<GameObject> spreadTo = new List<GameObject> ();
		foreach (GameObject i in _origin.adjacentUnitSpaces) {
			UnitSpace u = i.GetComponent<UnitSpace> ();
			if (!Unit.CheckVerticalD (origin, i) || u.type == "Water") {
				continue;
			}
			if (!resultsContainer.ContainsKey (i)) {
				if (unitsTable [i] == null || unitsTable [i] == unitToAct.gameObject) {
					resultsContainer.Add (i, index + 1);
				} else {
					resultsContainer.Add (i, index + 2);
				}
				spreadTo.Add (i);
			} else if (resultsContainer.ContainsKey (i) && resultsContainer [i] > index + 1 && unitsTable [i] == null) {
				resultsContainer [i] = index + 1;
				spreadTo.Add (i);
			} else if (resultsContainer.ContainsKey (i) && resultsContainer [i] > index + 2 && unitsTable [i] != null) {
				resultsContainer [i] = index + 2;
				spreadTo.Add (i);
			}
		}
		foreach (GameObject i in spreadTo) {
			AIFloodFill (i, resultsContainer[i], resultsContainer, unitToAct);
		}
	}
}
