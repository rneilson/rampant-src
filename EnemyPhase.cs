﻿using UnityEngine;
using System.Collections;

public class EnemyPhase : MonoBehaviour {

	public string phaseName;
	public int maxWaves; // Set to 0 if terminal/eternal/steadystate
	public float initialDelay;
	public float waveInterval;
	public Color pulseColor = Color.red * 0.5f;

	private float countdown;
	private int waveNum;
	private Scorer scorer;
	private Component[] spawners;

	private bool phaseActive = true;

	// Use this for initialization
	void Start () {
		scorer = GameObject.FindGameObjectWithTag("GameController").GetComponent<Scorer>();
		spawners = gameObject.GetComponents<EnemySpawner>();
		//countdown = initialDelay;
		countdown = (scorer.PlayerBreak) ? (initialDelay + scorer.PlayerBreakDelay) : initialDelay;
		waveNum = 0;

		// Debug
		Debug.Log("Entering phase " + phaseName);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	//Put everything in FixedUpdate
	void FixedUpdate () {
		if ((!scorer.Respawn) && (phaseActive)) {
			if (countdown <= 0.0f) {
				waveNum++;
				StartWave(waveNum);
				if ((maxWaves > 0) && (waveNum >= maxWaves)) {
					scorer.NextPhase();
				}
			}
			else {
				countdown -= Time.fixedDeltaTime;
			}
		}
	}

	// Begin wave
	public void StartWave (int wave) {

		foreach (EnemySpawner spawner in spawners) {
			spawner.StartWave(wave, scorer.PlayerBreak);
		}

		countdown = waveInterval;

		// Once complete, tell scorer
		scorer.AddLevel();

		// Send color pulse if first wave of phase
		if (wave == 1) {
			scorer.FlashGrid(pulseColor);
		}

		// Debug
		Debug.Log("Beginning wave " + wave.ToString());
	}

	// Reset phase to beginning
	public void ResetPhase () {
		//countdown = initialDelay;
		countdown = (scorer.PlayerBreak) ? (initialDelay + scorer.PlayerBreakDelay) : initialDelay;
		waveNum = 0;

		foreach (EnemySpawner spawner in spawners) {
			spawner.ResetWave();
		}
	}

	// Start phase back up (testing)
	public void StartPhase () {
		phaseActive = true;
	}

	// Shut down (mostly for testing (normally it would be destroyed))
	public void StopPhase () {
		phaseActive = false;
	}
}
