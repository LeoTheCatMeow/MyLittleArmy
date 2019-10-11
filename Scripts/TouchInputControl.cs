using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TouchInputControl : MonoBehaviour, IPointerUpHandler, IDragHandler, IPointerDownHandler{

	internal float dragSensitivity = 1.5f;
	internal float maxDeltaPerDrag = 60f;
	internal float turnSensitivity = 0.1f;
	private Vector2 startPosition;

	private Camera mainCam;
	private CameraControl camController;
	private Rect touchArea;
	private Vector2 deltaCameraFocus;
	private float deltaCameraAngle;

	private GameController gameController;

	void Start () {
		mainCam = GameObject.FindGameObjectWithTag ("MainCamera").GetComponent<Camera> ();
		camController = mainCam.GetComponent<CameraControl> ();
		touchArea = gameObject.GetComponent<RectTransform> ().rect;
		gameController = GameObject.FindGameObjectWithTag ("GameController").GetComponent<GameController> ();
	}

	public void OnPointerDown(PointerEventData data) {
		startPosition = data.position;
	} 

	public void OnDrag(PointerEventData data) {
		if (Input.touchCount == 1 || Input.GetMouseButton(0)) {
			Vector2 d = data.delta;
			float dY = Mathf.Clamp (d.y, -maxDeltaPerDrag, maxDeltaPerDrag);
			float dX = Mathf.Clamp (d.x, -maxDeltaPerDrag, maxDeltaPerDrag);
			//Y axis on screen corresponds to X axis in game, and X axis on screen corresponds to Z axis in game
			deltaCameraFocus = new Vector2 (dY / touchArea.height * dragSensitivity, dX / touchArea.height * dragSensitivity); 
			camController.TranslateCamera (deltaCameraFocus);
		} else if (Input.touchCount == 2 || Input.GetMouseButton(1)) {
			deltaCameraAngle = data.delta.x / touchArea.width * turnSensitivity * Mathf.PI; //in radians
			camController.RotateCamera (-deltaCameraAngle);
		}
	}

	public void OnPointerUp(PointerEventData data) {
		if ((Input.touchCount == 1 || Input.GetMouseButtonUp(0)) && Vector2.Distance(startPosition, data.position) < 60f) { 
			Ray ray = mainCam.ScreenPointToRay (data.position);
			RaycastHit hit;
			int layerMask = 1 << 8; //only target the 8th layer
			if (Physics.Raycast (ray, out hit, Mathf.Infinity, layerMask)) {
				GameObject recipient = hit.collider.transform.parent.gameObject;
				if (recipient.CompareTag ("UnitSpace")) {
					gameController.SelectUnitSpace (recipient);
				} else if (recipient.CompareTag ("UI")) {
					gameController.SelectAction (hit.collider.gameObject);
				} 
			} else {
				gameController.DeselectUnitSpace ();
			}
		}
	}
}
