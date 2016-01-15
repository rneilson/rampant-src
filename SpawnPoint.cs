﻿using UnityEngine;
using System.Collections;

public class SpawnPoint : MonoBehaviour {
	
	public GameObject enemyType;
	public float preTime;
	public float postTime;
	
	private float spawnTime;
	private bool spawned;
	private float phaseIndex;

	// Use this for initialization
	void Start () {
		spawnTime = Time.fixedTime + preTime;
		spawned = false;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void FixedUpdate () {
		if (spawned == false && Time.fixedTime >= spawnTime) {
			GetComponent<Collider>().enabled = false;
			float spawnZ = Random.Range(0f, (phaseIndex * 10f) + 10f);
			float spawnY = Random.Range(0f, 360f);
			Instantiate(enemyType, transform.position, Quaternion.Euler(spawnZ, 0, spawnY));
			if (postTime > 0)
				Destroy(gameObject, postTime);
			else
				Destroy(gameObject);
			spawned = true;
		}
	}

	void SetPhaseIndex (int phase) {
		phaseIndex = phase;
	}
	
}
