using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionMenu : MonoBehaviour {


	public GameObject iconMove, iconSwitch, iconAttack, iconPerk, iconMark, iconCancel;
	public GameObject sphereMove, sphereSwitch, sphereAttack, spherePerk, sphereCancel;
	private float heightAboveUnits = 0.8f;
	private GameObject mainCam;

	void Start () {
		gameObject.SetActive (false);
		mainCam = GameObject.FindGameObjectWithTag ("MainCamera");
	}

	void Update () {
		if (gameObject.activeSelf) {
			Adjust ();
		}
	}

	public void Activate (GameObject target) {
		transform.position = target.transform.Find ("VisualPivot").transform.position + new Vector3 (0f, heightAboveUnits, 0f);
		iconPerk.transform.Find("sprite").GetComponent<SpriteRenderer> ().sprite = target.GetComponent<Unit> ().perkIcon;
		Adjust ();
		gameObject.SetActive (false); 
		gameObject.SetActive (true);
		MainMenu (target);
	}

	public void Deactivate () {
		gameObject.SetActive (false);
	}

	public void MainMenu (GameObject target) {
		Unit unit = target.GetComponent<Unit> ();
		iconMark.SetActive (true);
		iconCancel.SetActive (false);
		sphereCancel.SetActive (false);
		iconMove.SetActive (unit.movementAvailable);
		iconSwitch.SetActive (unit.movementAvailable);
		iconAttack.SetActive (unit.attackAvailable);
		iconPerk.SetActive (unit.perkAvailable);
		sphereMove.SetActive (unit.movementAvailable);
		sphereSwitch.SetActive (unit.movementAvailable);
		sphereAttack.SetActive (unit.attackAvailable);
		spherePerk.SetActive (unit.perkAvailable);
	}

	public void CancelMenu () {
		iconMark.SetActive (false);
		iconMove.SetActive (false);
		iconSwitch.SetActive (false);
		sphereSwitch.SetActive (false);
		iconPerk.SetActive (false);
		spherePerk.SetActive (false);
		iconAttack.SetActive (false);
		sphereMove.SetActive (false);
		sphereAttack.SetActive (false);
		iconCancel.SetActive (true);
		sphereCancel.SetActive (true);
	}

	private void Adjust () { //to face the camera 
		//float angle = Mathf.Atan2 (mainCam.transform.position.z - transform.position.z, mainCam.transform.position.x - transform.position.x) * 180f / Mathf.PI;
		//gameObject.transform.eulerAngles = new Vector3 (0f, 90f - angle, 0f);
		gameObject.transform.eulerAngles = new Vector3 (0f, mainCam.transform.eulerAngles.y + 180f, 0f);
	}
}
