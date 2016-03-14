﻿//  Copyright 2016 Raymond Neilson
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

public class EnemyPhase : MonoBehaviour {

	public int maxWaves; // Set to 0 if terminal/eternal/steadystate
	public float initialDelay;
	public float waveInterval;
	public bool checkpoint = true;
	public Color pulseColor = Color.red * 0.5f;
	public bool debugInfo = false;

	private float countdown;
	private int waveNum;
	private Scorer scorer;
	private Component[] spawners;

	private bool phaseActive = true;

	public bool Checkpoint { get { return checkpoint; } }

	// Initialize on Awake() instead of Start()
	void Awake () {
		scorer = GameObject.FindGameObjectWithTag("GameController").GetComponent<Scorer>();
		// Find attached spawners
		spawners = gameObject.GetComponents<EnemySpawner>();
		// Send scorer
		foreach (EnemySpawner spawner in spawners) {
			spawner.FindControl(scorer.gameObject);
		}

		countdown = (scorer.PlayerBreak) ? (initialDelay + scorer.PlayerBreakDelay) : initialDelay;
		waveNum = 0;

	}
	
	// Update is called once per frame
	void Update () {
	
	}

	//Put everything in FixedUpdate
	void FixedUpdate () {
		if ((!scorer.Respawn) && (phaseActive)) {
			// Check if previous wave already clear
			if ((scorer.WaveClear) && (countdown > scorer.WaveClearCountdown) && (scorer.Level > 0)) {
				countdown = scorer.WaveClearCountdown;

				if (debugInfo) {
					Debug.Log("Wave " + scorer.Level.ToString() + " clear, cutting respawn to " 
						+ countdown.ToString() + " sec", gameObject);
				}
			}
			if (countdown <= 0.0f) {
				StartWave(++waveNum);

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

		// Send color pulse if first wave of phase
		if (wave == 1) {
			scorer.StartNewPhase(pulseColor);
		}
		else {
			scorer.AddLevel();
		}

		if (debugInfo) {
			Debug.Log("Beginning wave " + wave.ToString());
		}
	}

	// Reset phase to beginning
	public void ResetPhase () {
		countdown = (scorer.PlayerBreak) ? (initialDelay + scorer.PlayerBreakDelay) : initialDelay;
		waveNum = 0;

		foreach (EnemySpawner spawner in spawners) {
			spawner.ResetWave();
		}
	}

	public void ResetPhase (Scorer newScorer) {
		if (scorer == null) {
			scorer = newScorer;
		}
		ResetPhase();
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
