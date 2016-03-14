//  Copyright 2016 Raymond Neilson
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using UnityEngine;
using System.Collections;

public class CameraMovement : MonoBehaviour {
	private GameObject player;
	private Vector3 startPos;
	private Rigidbody rb;
	private bool movingToStart = false;

	public float trackFraction; // Fraction of player movement which camera follows
	public float resetTime;		// Time before player respawn at which camera resets

	// Use this for initialization
	void Start () {
		//camera = GetComponent<Camera>();
		startPos = transform.position;
		rb = GetComponent<Rigidbody>();
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

	public void NewPlayer (GameObject newPlayer) {
		// Get new player object
		player = newPlayer;
		// Reset camera position
		movingToStart = false;
		transform.position = startPos;
		rb.velocity = new Vector3(0, 0, 0);
		rb.isKinematic = true;
	}

	public void RespawnCountdown (float countdown) {
		if((!movingToStart) && (countdown <= resetTime)) {
			movingToStart = true;
			rb.isKinematic = false;
			rb.velocity = new Vector3((startPos.x - transform.position.x) / countdown, 0, 
				(startPos.z - transform.position.z) / countdown);
		}
	}

}
