using UnityEngine;
using System.Collections;

public class FixRotation : MonoBehaviour {

	public Vector3 targetRotation;

	private Quaternion targetRot;

	// Use this for initialization
	void Start () {
		targetRot = Quaternion.Euler(targetRotation);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void LateUpdate () {
		transform.rotation = targetRot;
	}
}
