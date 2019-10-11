using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour {

	public float distanceToFocus;
	public float cameraHeight;
	public Vector4 bound; //xMin, xMax, zMin, zMax

	internal float cameraAngle = 55f;
	internal float slowRateOnStop = 0.95f;
	internal Vector2 minRotationAndTranslation = new Vector2 (0.003f, 0.01f);
	internal bool pauseUpdate = true;

	private Vector2 focus; //the camera alwyas looks at the focus from a distance
	private float yRotation = 0.0f; //in radians
	private float deltaRotation = 0.0f;
	private Vector2 deltaTranslation = Vector2.zero;

	void Start () {
		Transform sp = GameObject.FindGameObjectWithTag ("StartPoint").transform;
		focus = new Vector2 (sp.position.x, sp.position.z);
		yRotation = -sp.eulerAngles.y / 180f * Mathf.PI; //the camera will look in the -x direction of sp
	}

	//step 2 determine how the changes should be applied each fixed update 
	void FixedUpdate () {
		if (pauseUpdate) {
			return;
		}

		//rotation
		if (Mathf.Abs(deltaRotation) > minRotationAndTranslation.x) {
			yRotation += deltaRotation;
			deltaRotation *= slowRateOnStop;
		}

		//translation
		if (deltaTranslation.magnitude > minRotationAndTranslation.y) {
			Vector2 fixedTranslation = new Vector2 (Mathf.Cos (yRotation) * deltaTranslation.x + Mathf.Sin (yRotation) * deltaTranslation.y, Mathf.Sin (yRotation) * deltaTranslation.x - Mathf.Cos (yRotation) * deltaTranslation.y);
			focus = new Vector2 (Mathf.Clamp(focus.x + fixedTranslation.x, bound.x, bound.y), Mathf.Clamp(focus.y + fixedTranslation.y, bound.z, bound.w));
			deltaTranslation = deltaTranslation * slowRateOnStop;
		}

		AdjustCamera ();
	}

	//step 3 actual adjustments to camera's position and rotation
	void AdjustCamera () {
		Vector3 targetPosition = new Vector3 (focus.x + Mathf.Cos (yRotation) * distanceToFocus, cameraHeight, focus.y + Mathf.Sin (yRotation) * distanceToFocus);
		transform.position = Vector3.MoveTowards (transform.position, targetPosition, 0.18f);
		float angle = Mathf.LerpAngle (transform.eulerAngles.y, -yRotation * 180.0f / Mathf.PI - 90.0f, 0.1f);
		transform.eulerAngles = new Vector3 (cameraAngle, angle, 0.0f);
	}

	//step 1 determine changes in rotation and translation
	public void RotateCamera (float angle) { //called from TouchInputControl
		deltaRotation = angle;
	}

	public void TranslateCamera (Vector2 vec) { //called from TouchInputControl
		deltaTranslation = vec;
	}

	public void SetFocus (GameObject i) {
		focus = new Vector2 (i.transform.position.x, i.transform.position.z); 
	}
}
