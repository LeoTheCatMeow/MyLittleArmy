using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Knight : Unit {   

	public override void Start() {
		base.Start ();
		perkInfo = "Battle Dash: Move one additional space, can't be used consecutively";
		unitMoveSpeed = 0.1f;
	}

	public override void Attack (GameObject target, bool active = true) {
		base.Attack (target, active);
		if (_focus > 0) {
			perkAvailable = true;
		}
	}

	public override void UsePerk (GameObject target) {
		base.UsePerk (target);
		perkAvailable = false;
		Move (target, false);
	}

	public override void FindPerkTargets (List<GameObject> resultsContainer) {
		UnitSpace origin = currentUnitSpace.GetComponent<UnitSpace> ();
		if (resultsContainer.Count == 0) { 
			foreach (GameObject i in origin.adjacentUnitSpaces) {
				UnitSpace u = i.GetComponent<UnitSpace> ();
				if (game.unitsTable [i] != null || !accessableTerrain.Contains(u.type) || !CheckVerticalD (currentUnitSpace, i)) {
					continue;
				}
				resultsContainer.Add (i);
			}
		}
	}

	protected override string AITryApproach (Dictionary<GameObject, int> map) {
		List<GameObject> moveTargets = new List<GameObject> ();
		FindPaths (moveTargets);
		if (moveTargets.Count > 0) {
			if (!movementAvailable) {
				if (perkAvailable) {
					moveTargets.Clear ();
					FindPerkTargets (moveTargets);
				} else {
					return "unable to move";
				}
			}
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
				if (movementAvailable) {
					Move (closest);
				} else {
					UsePerk (closest);
				}
				return "success";
			} else {
				return "unable to get closer";
			}
		} else {
			return "unable to move";
		}
	}
}
