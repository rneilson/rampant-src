using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour {
	
	public int roundSizeStart;
	public int roundSizeStep;
	public int roundNumStart;
	public int roundNumStep;
	public float initialDelay;
	public float roundInterval;
	public int waveMin; // inclusive
	public int waveMax; // inclusive
	public IncreaseType[] increaseCycle;
	public GameObject enemySpawn;
	public AudioClip enemySpawnSound;
	
	private Scorer scorer;
	private float countdown;
	private bool counting;
	private int roundCounter;
	private int roundNumCurrent;
	private int roundSizeCurrent;
	//private int waveCurrent;
	private int increaseCounter;
	//private int mask;

	// Unity 5 API changes
	private AudioSource myAudioSource;
	
	// Use this for initialization
	void Start () {
		ResetWave();
		scorer = GameObject.FindGameObjectWithTag("GameController").GetComponent<Scorer>();
		//mask = 1 << LayerMask.NameToLayer("Spawn");

		// Unity 5 API changes
		myAudioSource = GetComponent<AudioSource>();
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
					roundCounter++;
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
				countdown -= Time.deltaTime;
			}
		}
	}

	// Reset to initial state
	public void ResetWave () {
		roundSizeCurrent = roundSizeStart;
		roundNumCurrent = roundNumStart;
		roundCounter = 0;
	//	waveCurrent = 0;
		increaseCounter = 0;
		counting = false;
		countdown = initialDelay;
	}

	// Start a wave -- called by phase class before spawner runs
	public void StartWave (int wave) {
		// Check if we should even be doing anything
		if ((wave >= waveMin) && (wave <= waveMax)) {
			// If still more phase rounds to come, increase as per cycle
			if (wave > waveMin) {
				Increase();
			}

			// Start countdown, reset counters
			counting = true;
			roundCounter = 0;
			countdown = initialDelay;
		}
	}

	void SpawnRound () {
		// Begin wave spawn loop
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		Vector3 playerPos = player.transform.position;
		Vector3 spawnPos;
		Collider[] others;
		bool clear = false;

		// Spawn loop
		for	(int i = roundSizeCurrent; i > 0; i--) {
			clear = false;

			do {
				spawnPos = new Vector3(Random.Range(-4.7f, 4.7f), 1f, Random.Range(-4.7f, 4.7f));
				others = Physics.OverlapSphere(spawnPos, 0.2f);
				if (others.Length == 0 && ((spawnPos - playerPos).magnitude > 2.0f))
					clear = true;
			} while (!clear);

			Instantiate(enemySpawn, spawnPos, Quaternion.Euler(0, 0, 0));
		}

		// Play one instance only of spawn sound
		myAudioSource.PlayOneShot(enemySpawnSound, 1.0f);
	}
	
	// Increase round size by step
	void IncreaseSize () {
		roundSizeCurrent += roundSizeStep;
	}

	// Increase number of rounds by step
	void IncreaseNum () {
		roundNumCurrent += roundNumStep;
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
}

// Enum for round increase types

public enum IncreaseType : byte {
	None = 0,
	Size,
	Num,
	Both
}
