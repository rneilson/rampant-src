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

public class RedCubeBomb : MonoBehaviour {
	
	private GameObject target;
	private Scorer scorer;
	private RedCubeGroundControl control;
	private Vector3 bearing;
	private DeathType dying = DeathType.None;
	private bool armed;
	private bool killPlayer = false;
	private const bool spin = true;
	private Vector3 spinRef = Vector3.forward;
	private Vector3 spinAxis;

	// Unity 5 API changes
	//private AudioSource myAudioSource;
	private Rigidbody myRigidbody;
	
	// Type/instance management stuff
	public string thisTypeName = "Bomber";
	private EnemyType thisType;
	private EnemyInst thisInst;

	// Public parameters
	public float speed;
	public float drag;
	public float torque = 0.15f;
	public Vector3 spinAxes = Vector3.right;
	public GameObject burster;
	public GameObject bursterQuiet;
	public GameObject deathFade;
	public float bombHeight = 0.51f;
	public float bombForce = 300f;
	public float bombKillRadius = 1.0f;
	public float bombPushRadius = 2.5f;
	public float bombTriggerRadius = 0.75f;
	public GameObject bombEffect;
	public float shrapnelLifetime = 1.0f;
	public GameObject shrapnelSparker;
	//public bool enableBombPush = false;

	// Use this for initialization
	void Start () {
		// Unity 5 API changes
		//myAudioSource = GetComponent<AudioSource>();
		myRigidbody = GetComponent<Rigidbody>();

		if (!scorer) {
			FindControl(GameObject.FindGameObjectWithTag("GameController"));
		}
		myRigidbody.drag = drag;
		armed = true;

		spinAxis = spinAxes.normalized;

		// Add to control's list
		thisType = EnemyList.AddOrGetType(thisTypeName);
		thisInst = new EnemyInst(thisType.typeNum, gameObject);
		control.AddInstanceToList(thisInst);
	}
	
	// Update is called once per frame
	void Update () {
		if (dying != DeathType.None)
			BlowUp();
	}
	
	//Put movement in FixedUpdate
	void FixedUpdate () {
		if (target) {
			// Get bearing
			bearing = target.transform.position - transform.position;
			// Blow up if we're in range
			if ((bombTriggerRadius > 0.0f) && (bearing.magnitude <= bombTriggerRadius)) {
				killPlayer = true;
				Die(true);
			}
			// Or try to get in range
			else {
				myRigidbody.AddForce(bearing.normalized * speed);
			}
			// Spin menacingly (if spinning enabled)
			if (spin) {
				myRigidbody.AddTorque(SpinVector(bearing) * torque);
			}
		}
	}
	
	void BlowUp () {
		if (dying == DeathType.Loudly) {
			Destroy(Instantiate(burster, transform.position, Quaternion.Euler(-90, 0, 0)), 0.5f);
		}
		else {
			Destroy(Instantiate(bursterQuiet, transform.position, Quaternion.Euler(-90, 0, 0)), 0.5f);
		}
		if (deathFade) {
			Destroy(Instantiate(deathFade, transform.position, Quaternion.identity), 1.0f);
		}
		if (dying != DeathType.Silently) {
			scorer.AddKill();
		}
		KillRelatives(shrapnelLifetime);

		// Drop da bomb
		if (armed) {
			DropBomb(transform.position);
		}

		// Remove from control's list
		control.RemoveInstanceFromList(thisInst);
		
		// Destroy ourselves
		Destroy(gameObject);
	}
	
	void Clear () {
		dying = DeathType.Silently;
		BlowUp();
	}
	
	void Die (bool loudly) {
		if (dying == DeathType.None) {
			dying = (loudly) ? DeathType.Loudly : DeathType.Quietly;
		}
	}
	
	void KillRelatives (float delay) {
		// Detach from parent and/or children and destroy them after a delay
		GameObject tmp;

		// Parent first
		if (transform.parent) {
			tmp = transform.parent.gameObject;
			Destroy(tmp, delay);
		}

		// Next the kids, if any
		for (int i = transform.childCount - 1; i >= 0 ; i--) {
			tmp = transform.GetChild(i).gameObject;
			tmp.transform.parent = null;
			var dd = tmp.GetComponent<DelayedDeath>();
			if (dd) {
				// "...but then again, who does?"
				dd.DieInTime(Random.Range(0.75f, 1.25f) * delay, shrapnelSparker);
			}
			else {
				Destroy(tmp, delay);
			}
		}

	}

	void ClearTarget () {
		target  = null;
	}
	
	void NewTarget (GameObject newTarget) {
		target = newTarget;
	}

	void FindControl (GameObject controller) {
		scorer = controller.GetComponent<Scorer>();
		control = controller.GetComponent<RedCubeGroundControl>();
		NewTarget(scorer.Player);
	}

	void DropBomb (Vector3 pos) {
		Vector3 bombPos = new Vector3 (pos.x, bombHeight, pos.z);
		GameObject daBomb;

		// Spawn effect
		// At player position because it looks better
		daBomb = Instantiate(bombEffect, pos, Quaternion.identity) as GameObject;
		Destroy(daBomb, 1.0f);
		// Turn down flash if dying quietly
		if (dying != DeathType.Loudly) {
			daBomb.GetComponent<LightPulse>().ChangeTargetRelative(-1.2f);
		}
		// Turn down volume if dying quietly
		if (dying == DeathType.Quietly) {
			daBomb.GetComponent<AudioSource>().volume *= 0.1f;
		}
		// Mute if dying silently
		if (dying == DeathType.Silently) {
			daBomb.GetComponent<AudioSource>().volume = 0.0f;
		}
		
		// We're dropping, make sure we're now disarmed
		armed = false;

		int killmask, pushmask;
		Collider[] things;
		
		// Kill things in inner radius
		if (bombKillRadius > 0.0f) {
			// If we're self-triggering, can kill player
			if (killPlayer) {
				killmask = (1 << LayerMask.NameToLayer("Enemy")) | (1 << LayerMask.NameToLayer("Player"));
			}
			else {
				killmask = (1 << LayerMask.NameToLayer("Enemy"));
			}
			things = Physics.OverlapSphere(pos, bombKillRadius, killmask);
			if (things.Length > 0) {
				for (int i=0; i<things.Length; i++) {
					things[i].SendMessage("Die", false);
				}
			}
		}
		
		// Only push things if we're dying loudly (do push player)
		if ((dying == DeathType.Loudly) && (bombPushRadius > 0.0f)) {
			pushmask = (1 << LayerMask.NameToLayer("Enemy")) | (1 << LayerMask.NameToLayer("Player"));
			// Push things in outer radius
			things = Physics.OverlapSphere(pos, bombPushRadius, pushmask);
			for (int i=0; i<things.Length; i++) {
				things[i].GetComponent<Rigidbody>().AddExplosionForce(bombForce, bombPos, 0f);
			}
		}

	}

	Vector3 SpinVector (Vector3 bearingVec) {
		Quaternion rot = Quaternion.FromToRotation(spinRef, bearingVec);
		return rot * spinAxis;
	}
	
}
