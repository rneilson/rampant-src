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

public class CornerPush : MonoBehaviour {

	private Light cornerLight;
	private GameObject player;
	private Rigidbody playerRB;
	private Scorer scorer;
	private Vector3 pushPos;

	// Push parameters
	public float maxIntensity = 3.0f;
	public float minIntensity = 1.0f;
	public float pushRadius = 1.25f;
	public float pushForce = 50.0f;

	void Awake () {
		cornerLight = GetComponent<Light>();
		scorer = GameObject.Find("Scorekeeper").GetComponent<Scorer>();

		// Figure out which corner we're in, normalized (ish) on the X/Z plane
		float tempX = transform.position.x / Mathf.Abs(transform.position.x);
		float tempZ = transform.position.z / Mathf.Abs(transform.position.z);
		// Set pushPos to be on the same plane as the player, and at the maximum displacement
		pushPos = new Vector3(tempX * scorer.MaxDisplacement, scorer.SpawnPosition.y, tempZ * scorer.MaxDisplacement);
	}

	// Use this for initialization
	void Start () {
		// Start off with zero intensity
		cornerLight.intensity = 0;
	
		if (scorer.GlobalDebug) {
			Debug.Log("Corner pusher, " + gameObject.name + ", position " + pushPos.ToString());
		}
	}
	
	// Update is called once per frame
	void Update () {
		// Check for player
		CheckForPlayer();

		// Update intensity
		if (player) {
			float playerDist = PlayerDistanceFraction(VectorToPlayer().magnitude);
			if (playerDist > 0.0f) {
				cornerLight.intensity = (playerDist * (maxIntensity - minIntensity)) + minIntensity;
			}
			// Turn off light if was on
			else if (cornerLight.intensity > 0.0f) {
				cornerLight.intensity = 0.0f;
			}
		}
		else {
			cornerLight.intensity = 0.0f;
		}
	}

	void FixedUpdate () {
		// Check for player
		CheckForPlayer();

		// Push player away with inverse-distance force
		if (player) {
			Vector3 pushVec = VectorToPlayer();
			// If in radius, push accordingly
			if (pushVec.magnitude <= pushRadius) {
				Vector3 force = pushVec.normalized * pushForce * PlayerDistanceFraction(pushVec.magnitude);
				if (scorer.GlobalDebug) {
					Debug.Log("Pushing player, force:" + force.magnitude.ToString() + ", vector: " + force.normalized.ToString());
				}
				playerRB.AddForce(force);
			}
		}
	}

	void CheckForPlayer () {
		if (!player) {
			player = scorer.Player;
			if (player) {
				playerRB = player.GetComponent<Rigidbody>();
			}
		}
	}

	// Only call if player active
	Vector3 VectorToPlayer () {
		return player.transform.position - pushPos;
	}

	// Set intensity to inverse of player distance
	float PlayerDistanceFraction (float distance) {
		float invDist = 1.0f - (distance / pushRadius);
		if (invDist > 0.0f) {
			return invDist;
		}
		else {
			return 0.0f;
		}
	}
}
