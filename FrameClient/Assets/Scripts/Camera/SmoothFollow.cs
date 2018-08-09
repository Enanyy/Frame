using UnityEngine;
using System.Collections;

public class SmoothFollow : MonoBehaviour {

	public Transform target;
	public float distance = 6.0f;
	public float height = 8.0f;
	public float damping = 1.0f;
	public bool smoothRotation = false;
	public bool followBehind = false;
	public float rotationDamping = 10.0f;
	public bool smoothPosition = false;

	public bool onLateUpdate = true;

	public Vector3 rotation = new Vector3(67,0,0);
	// Use this for initialization
	void Start () {
	   if (target == null) {
			Debug.LogError("target is Null!");	
	   }
	}
	
	// Update is called once per frame
	void Update () {
		if (onLateUpdate == false) {
			FollowTarget ();
		}
	}

	void LateUpdate()
	{
		if (onLateUpdate) {
			FollowTarget ();
		}
	}

	void FollowTarget()
	{
		if (target == null) {
			return;
		}
		Vector3 wantedPosition;
		if (followBehind) {

			wantedPosition = target.TransformPoint (0, height, -distance);
		}
		else {
			//wantedPosition = target.TransformPoint (0, height, distance);
			wantedPosition = target.position + Vector3.forward * distance * (-1);
			wantedPosition.y = height;
		}
		if (smoothPosition) {

			transform.position =  Vector3.Lerp (transform.position, wantedPosition, Time.deltaTime * damping);

		} else {

			transform.position = wantedPosition;

		}

		if (smoothRotation) {
			Quaternion wantedRotation = Quaternion.LookRotation (target.position - transform.position, target.up);
			transform.rotation = Quaternion.Slerp (transform.rotation, wantedRotation, Time.deltaTime * rotationDamping);
		} else {
			//transform.LookAt (target, target.up);
			transform.rotation = Quaternion.Euler(rotation);

		}
	}
}
