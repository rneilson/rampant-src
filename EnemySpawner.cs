using UnityEngine;
using System.Collections;

// TODO: fold in SpawnPoint functionality (?)
public class EnemySpawner : MonoBehaviour {
	
	public int roundSizeStart = 1; // Set to 0 if monotonically increasing size
	public int roundSizeStep = 1;
	public int roundNumStart = 1; // Set to 0 if monotonically increasing num
	public int roundNumStep = 1;
	public float initialDelay = 0.0f;
	public float roundInterval = 0.5f;
	public float safeZoneRadius = 2.0f;
	public int waveMin = 1; // inclusive
	public int waveMax; // inclusive
	public IncreaseType[] increaseCycle = {IncreaseType.None};
	public WaveType[] waveCycle = {WaveType.Current};
	public GameObject enemySpawn;
	public AudioClip enemySpawnSound;
	public bool debugInfo = false;
	
	private Scorer scorer;
	private RedCubeGroundControl control;
	private float countdown;
	private bool counting;
	private int roundCounter;
	private int roundNumCurrent;
	private int roundSizeCurrent;
	private int increaseCounter;
	private int waveCounter;
	//private int mask;
	private bool playerBreak;
	private float maxSafeRadius = 4.75f;
	private float maxDisplacement;
	private int predictionFrames;

	// Unity 5 API changes
	private AudioSource myAudioSource;
	
	// Use this for initialization
	void Start () {
		ResetWave();
		if (!scorer) {
			FindControl(GameObject.FindGameObjectWithTag("GameController"));
		}
		
		//mask = 1 << LayerMask.NameToLayer("Spawn");
		maxDisplacement = scorer.MaxDisplacement;

		// Unity 5 API changes
		myAudioSource = scorer.GetComponent<AudioSource>();

		// Predictions frames ahead to use
		predictionFrames = (control.PredictionLength > 25) ? 25 : control.PredictionLength;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	
	//Put everything in FixedUpdate
	void FixedUpdate () {
		// If currently counting and not waiting for respawn, do stuff
		if ((!scorer.Respawn) && (counting)) {
			if (countdown <= 0.0f) {
				// Spawn if we're spawning
				// Technically SpawnRound won't do anything if roundSizeCurrent = 0, but hey,
				// let's spare the poor machine some cycles
				if (roundSizeCurrent > 0) {
					SpawnRound();
				}

				// Set false here as a guard
				// If roundSizeCurrent is 0, but roundNumCurrent is not, then we don't want
				// to keep running the checks
				counting = false;

				// Check if another round coming
				if (roundCounter < roundNumCurrent) {
					countdown = roundInterval;
					counting = true;
				}
			}
			else {
				countdown -= Time.fixedDeltaTime;
			}
		}
	}

	public void FindControl (GameObject controller) {
		scorer = controller.GetComponent<Scorer>();
		control = controller.GetComponent<RedCubeGroundControl>();
	}

	// Reset to initial state
	public void ResetWave () {
		roundSizeCurrent = roundSizeStart;
		roundNumCurrent = roundNumStart;
		roundCounter = 0;
		waveCounter = 0;
		increaseCounter = 0;
		counting = false;
		countdown = initialDelay;
	}

	// Start a wave -- called by phase class before spawner runs
	public void StartWave (int wave, bool giveBreak) {
		// Check if we should even be doing anything
		if ((wave >= waveMin) && ((wave <= waveMax) || waveMax == 0)) {
			/* [OLD] kept for reference
			// If still more phase rounds to come, increase as per cycle
			if (wave > waveMin) {
				Increase();
			}
			*/

			// Check cycle array, don't do anything if skipping this wave
			WaveType currentWave = waveCycle[waveCounter];
			if (currentWave != WaveType.None) {
				// Increase/decrease as necessary
				IncreaseWave(currentWave);

				// Start countdown, reset counters
				counting = true;
				roundCounter = 0;
				playerBreak = giveBreak;
				//countdown = (playerBreak) ? (initialDelay + scorer.PlayerBreakDelay) : initialDelay;
				countdown = initialDelay; // PlayerBreakDelay only used in EnemyPhase
			}

			// Advance counter and loop if necessary
			waveCounter++;
			if (waveCounter >= waveCycle.Length) {
				waveCounter = 0;
			}
		}
	}

	void SpawnRound () {
		// Begin wave spawn loop
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		Vector3 playerPos = player.transform.position;
		Vector3 predictPos = control.Prediction(predictionFrames);
		Vector3 spawnPos;
		Collider[] others;
		bool clear = false;
		// Expand safezone radius if player just respawned
		float safeRadius = (playerBreak) ? Mathf.Min(safeZoneRadius + scorer.PlayerBreakRadius, maxSafeRadius) : 
			Mathf.Min(safeZoneRadius, maxSafeRadius);
		// Let's say the safe radius at the prediction point is lower
		float predictRadius = safeRadius / 2.0f;

		// Spawn loop
		for	(int i = roundSizeCurrent; i > 0; i--) {
			clear = false;

			do {
				spawnPos = new Vector3(Random.Range(-maxDisplacement, maxDisplacement), 1f, 
					Random.Range(-maxDisplacement, maxDisplacement));
				others = Physics.OverlapSphere(spawnPos, 0.2f);
				if ((others.Length == 0) 
					&& ((spawnPos - playerPos).magnitude > safeRadius) 
					&& ((spawnPos - predictPos).magnitude > predictRadius)) {
					clear = true;
				}
			} while (!clear);

			GameObject spawner = Instantiate(enemySpawn, spawnPos, Quaternion.Euler(0, 0, 0)) as GameObject;
			spawner.SendMessage("SetPhaseIndex", scorer.PhaseIndex);
			spawner.SendMessage("FindControl", scorer.gameObject);
		}

		// Play one instance only of spawn sound
		myAudioSource.PlayOneShot(enemySpawnSound, 1.0f);

		if (debugInfo) {
			Debug.Log("Spawned round of " + roundSizeCurrent.ToString());
		}

		// Advance round counter
		roundCounter++;
	}
	
	// Increase round size by step
	void IncreaseSize () {
		roundSizeCurrent += roundSizeStep;
	}

	// Decrease round size by step
	void DecreaseSize () {
		roundSizeCurrent -= roundSizeStep;
	}

	// Increase number of rounds by step
	void IncreaseNum () {
		roundNumCurrent += roundNumStep;
	}

	// Decrease number of rounds by step
	void DecreaseNum () {
		roundNumCurrent -= roundNumStep;
	}

	// Increase by whatever's in the queue
	void Increase () {
		IncreaseType increaseCurrent = increaseCycle[increaseCounter];

		if ((increaseCurrent == IncreaseType.Size) || (increaseCurrent == IncreaseType.Both)) {
			IncreaseSize();
		}
		if ((increaseCurrent == IncreaseType.Num) || (increaseCurrent == IncreaseType.Both)) {
			IncreaseNum();
		}

		increaseCounter++;
		if (increaseCounter >= increaseCycle.Length) {
			increaseCounter = 0;
		}
	}

	// Increase/decrease size/num/both/neither
	void IncreaseWave (WaveType waveType) {
		switch (waveType) {
			case WaveType.IncBoth:
				IncreaseSize();
				IncreaseNum();
				return;
			case WaveType.IncSize:
				IncreaseSize();
				return;
			case WaveType.IncNum:
				IncreaseNum();
				return;
			case WaveType.DecBoth:
				DecreaseSize();
				DecreaseNum();
				return;
			case WaveType.DecSize:
				DecreaseSize();
				return;
			case WaveType.DecNum:
				DecreaseNum();
				return;
			case WaveType.Current:
				return;
			case WaveType.None:
				return;
			default:
				return;
		}
	}
}

// Enum for round increase types

public enum IncreaseType : byte {
	None = 0,
	Size,
	Num,
	Both
}

// Enum for wave types

public enum WaveType : byte {
	None = 0,
	Current,
	IncSize,
	DecSize,
	IncNum,
	DecNum,
	IncBoth,
	DecBoth
}
