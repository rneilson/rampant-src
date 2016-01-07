using UnityEngine;
using System.Collections;

public class CameraMovement : MonoBehaviour {
	private GameObject player;
	//private Camera camera;
	private Vector3 startPos;
	//private bool reset = false;
	//private float resetCountdown;

	public float trackFraction; // Fraction of player movement which camera follows
	//public float resetTime;		// Time before player respawn at which camera resets

		// Use this for initialization
	void Start () {
		//camera = GetComponent<Camera>();
		startPos = transform.position;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	//Put everything in FixedUpdate
	void FixedUpdate () {
		// Move position to track player
		// Assumes player spawn is (0, n/a, 0)
		// Please change if above assumption changes
		if (player) {
			transform.position = new Vector3((player.transform.position.x * trackFraction), 
				startPos.y, (player.transform.position.z * trackFraction));		
		}
	}

	void NewPlayer (GameObject newPlayer) {
		// Get new player object
		player = newPlayer;
		// Reset camera position
		transform.position = startPos;
	}

	void ResetPos () {

	}

}