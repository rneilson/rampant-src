using UnityEngine;
using System.Collections;

public class EnemyPhase : MonoBehaviour {

	public string phaseName;
	public int maxWaves; // Set to 0 if terminal/eternal/steadystate
	public float initialDelay;
	public float waveInterval;
	public Color pulseColor = Color.red * 0.5f;
	public bool debugInfo = false;

	private float countdown;
	private int waveNum;
	private Scorer scorer;
	private Component[] spawners;

	private bool phaseActive = true;

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

		if (debugInfo) {
			Debug.Log("Entering phase " + phaseName);
		}
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
