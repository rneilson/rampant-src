using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// TODO: fold in SpawnPoint functionality (?)
public class EnemySpawner : MonoBehaviour {
	
	public float clearRadius = 0.2f;
	public float initialDelay = 0.0f;
	public float timeSpacing = 0.02f;
	public int waveMin = 1; // inclusive
	public int waveMax; // inclusive
	public WaveSpec[] waves;
	public bool debugInfo = false;
	
	private Scorer scorer;
	//private RedCubeGroundControl control;
	private float countdown;
	private bool counting;
	private bool playerBreak;
	private float maxDisplacement;
	//private int predictionFrames;
	private int playerMask;
	private int enemyMask;
	private int spawnMask;
	private List<WaveSpec> waveList;

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
		//predictionFrames = (control.PredictionLength > 51) ? 50 : control.PredictionLength - 1;
		
		playerMask = 1 << LayerMask.NameToLayer("Player");
		enemyMask = 1 << LayerMask.NameToLayer("Enemy");
		spawnMask = 1 << LayerMask.NameToLayer("Spawn");

		// Put waves into list and sort by spacing
		waveList = new List<WaveSpec>(waves);
		// Sort waves by spacing
		WaveCompareSpacing wc = new WaveCompareSpacing();
		waveList.Sort(wc);
		// Now reverse -- we want the furthest-spaced wave first
		waveList.Reverse();

		if (debugInfo) {
			Debug.Log("Wave list: ", gameObject);
			foreach (WaveSpec wave in waveList) {
				Debug.Log("Type: " + wave.enemySpawn.name, wave.enemySpawn.gameObject);
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	
	//Put everything in FixedUpdate
	void FixedUpdate () {
		// If currently counting and not waiting for respawn, do stuff
		if ((!scorer.Respawn) && (counting)) {
			if (countdown <= 0.0f) {
				// Play sounds
				foreach (WaveSpec wave in waveList) {
					if (wave.spawnSound) {
						StartCoroutine(PlayDelayedClip(wave.spawnSound, wave.soundDelay, 1.0f));
					}
				}
				// Spawn wave(s)
				SpawnWave();
				// Stop counting, we're there
				counting = false;
			}
			else {
				countdown -= Time.fixedDeltaTime;
			}
		}
	}

	IEnumerator PlayDelayedClip (AudioClip toPlay, float delay, float volume) {
		yield return new WaitForSeconds(delay);
		myAudioSource.PlayOneShot(toPlay, volume);
	}

	public void FindControl (GameObject controller) {
		scorer = controller.GetComponent<Scorer>();
		//control = controller.GetComponent<RedCubeGroundControl>();
	}

	// Reset to initial state
	public void ResetWave () {
		// Reset waves
		foreach (WaveSpec wave in waves) {
			wave.ResetCount();
		}

		counting = false;
		countdown = initialDelay;
	}

	// Start a wave -- called by phase class before spawner runs
	public void StartWave (int wave, bool giveBreak) {
		// Check if we should even be doing anything
		if ((wave >= waveMin) && ((wave <= waveMax) || waveMax == 0)) {
			// Start countdown, reset counters
			counting = true;
			playerBreak = giveBreak;
			countdown = initialDelay; // PlayerBreakDelay only used in EnemyPhase
		}
	}

	bool InBounds (Vector2 point) {
		if ((point.x <= maxDisplacement)
			&& (point.x >= -maxDisplacement)
			&& (point.y <= maxDisplacement)
			&& (point.y >= -maxDisplacement))
		{ return true; }
		else { return false; }
	}

	bool TestPoint (Vector2 point, float minDist, List<PointCandidate> testList) {
		// Tests point against all points in testList
		// Returns false if any are inside minimum distance
		float sqrMin = minDist * minDist;

		foreach (PointCandidate testPoint in testList) {
			float sqrDist = (testPoint.pointPos - point).sqrMagnitude;
			if (sqrDist < sqrMin) {
				return false;
			}
		}

		// If we get here, point passes test
		return true;
	}

	bool TestCandidates (PointCandidate samplePoint, float minDist, List<PointCandidate> testList, out Vector2 newPos) {
		// Generates up to k test points (iteratively) based on samplePoint, ranging from minDist to 2*minDist
		// Then compares test point to all points in testList for minimum distance
		const float twoPi = Mathf.PI * 2;
		const int k = 30;	// Max iterations
		Vector2 samplePos = samplePoint.pointPos;	// Seed position
		Vector2 candidatePos;						// Candidate position
		Collider[] others;							// For spawn-zone clearance
		int mask = playerMask | enemyMask | spawnMask;

		// Loop up to k iterations, testing each, and return true (and out newPos) if successful
		for (int i = 0; i < k; i++) {
			// Pick an angle between 0 and 2*Pi, and a range between minDist and 2*minDist
			float angle = Random.Range(0.0f, 1.0f) * twoPi;
			float range = Random.Range(1.0f, 2.0f) * minDist;

			// Create new offset vector and add to initial sample position
			Vector2 offset = new Vector2(range * Mathf.Cos(angle), range * Mathf.Sin(angle));
			candidatePos = samplePos + offset;

			// Check if in bounds
			if (InBounds(candidatePos)) {
				// Check via overlap sphere if any other enemies/players in safe radius
				others = Physics.OverlapSphere(new Vector3(candidatePos.x, scorer.SpawnHeight, candidatePos.y), 
					clearRadius, mask);
				if (others.Length == 0) {
					// Test position against list, and return results if position further than minDist
					// away from all points in testList
					if (TestPoint(candidatePos, minDist, testList)) {
						newPos = candidatePos;
						return true;
					}
				}
			}
		}

		// If we test k iterations, and all fail
		newPos = new Vector2(0f, 0f);
		return false;
	}

	void SpawnWave () {
		// Fresh lists, one for the points to use, one for active processing
		var spawnPoints = new List<PointCandidate>();

		// Get player position, and add to list as initial sample
		// (We'll remove it before spawning)
		Vector3 playerPos = (scorer.Player) ? scorer.Player.transform.position : scorer.SpawnPosition;
		PointCandidate playerPoint = new PointCandidate(null, 0.0f, playerPos.x, playerPos.z);
		spawnPoints.Add(playerPoint);

		// Each wavespec gets a loop
		foreach (WaveSpec wave in waveList) {
			// Find current list length, and how many to add of this enemy type
			int endLen = wave.Advance() + spawnPoints.Count;

			// Only do anything more if we're adding something at this point
			if (endLen > spawnPoints.Count) {
				// Set minimum spacing depending on playerBreak
				float spacing = (playerBreak) ? wave.minSpacing + scorer.PlayerBreakRadius : wave.minSpacing;

				// Start off the sample-point list with the current spawn points
				var samplePoints = new List<PointCandidate>(spawnPoints);

				// Until we have enough, or there are no more eligible sample points left
				while ((spawnPoints.Count < endLen) && (samplePoints.Count > 0)) {
					// Pick a random index from the sample list
					int i = Random.Range(0, samplePoints.Count);
					PointCandidate sample = samplePoints[i];

					// Use that sample to seed candidates and generate another point, if possible
					Vector2 candidate;
					if (TestCandidates(sample, spacing, spawnPoints, out candidate)) {
						// If candidate found, add to spawn and sample lists
						PointCandidate newPoint = new PointCandidate(
							wave.enemySpawn,								// Type to spawn
							(candidate - playerPoint.pointPos).magnitude,	// Distance from player
							candidate);										// Position to spawn
						spawnPoints.Add(newPoint);
						samplePoints.Add(newPoint);
					}
					else {
						// If no candidate found, remove sample from list (leave in spawnlist though)
						samplePoints.RemoveAt(i);
					}
				}
				
			}
		}

		// Remove player point
		spawnPoints.RemoveAt(0);

		// Sort list by player distance
		PointComparePlayerDist pc = new PointComparePlayerDist();
		spawnPoints.Sort(pc);

		if (debugInfo) {
			Debug.Log("Spawning round of: " + spawnPoints.Count.ToString(), gameObject);
		}

		// Instantiate spawners, with increasing delays
		for (int i = 0; i < spawnPoints.Count; i++) {
			PointCandidate point = spawnPoints[i];

			Vector3 spawnPos = new Vector3(point.pointPos.x, scorer.SpawnHeight, point.pointPos.y);
			if (debugInfo) {
				Debug.Log("Spawning type " + point.enemySpawn.name + " at: " + spawnPos.ToString(), gameObject);
			}

			GameObject obj = Instantiate(point.enemySpawn, spawnPos, Quaternion.identity) as GameObject;
			SpawnPoint spawner = obj.GetComponent<SpawnPoint>();

			// Housekeeping
			spawner.SetPhaseIndex(scorer.PhaseIndex);
			spawner.FindControl(scorer.gameObject);
			spawner.SetDelay((float) i * timeSpacing);
		}
	}

	/*
	void SpawnRound () {
		// Begin wave spawn loop
		//GameObject player = GameObject.FindGameObjectWithTag("Player");
		//Vector3 playerPos = player.transform.position;
		//Vector3 predictPos = control.Prediction(predictionFrames);
		Vector3 spawnPos;
		Collider[] others;
		Collider[] playerCur;
		//Collider[] playerPred;
		bool clear = false;
		// Expand safezone radius if player just respawned
		float safeRadius = (playerBreak) ? Mathf.Min(safeZoneRadius + scorer.PlayerBreakRadius, maxDisplacement) : 
			Mathf.Min(safeZoneRadius, maxDisplacement);
		// Let's say the safe radius at the prediction point is lower
		//float predictRadius = safeRadius / 2.0f;

		// Spawn loop
		for	(int i = roundSizeCurrent; i > 0; i--) {
			clear = false;

			do {
				spawnPos = new Vector3(Random.Range(-maxDisplacement, maxDisplacement), 1f, 
					Random.Range(-maxDisplacement, maxDisplacement));
				others = Physics.OverlapSphere(spawnPos, 0.2f);
				playerCur = Physics.OverlapSphere(spawnPos, safeRadius, playerMask);
				//playerPred = Physics.OverlapSphere(spawnPos, predictRadius, playerMask);
				if ((others.Length == 0) && (playerCur.Length == 0)) {
					//&& ((spawnPos - playerPos).magnitude > safeRadius)) {
					//&& ((spawnPos - predictPos).magnitude > predictRadius)) {
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
	*/
	
}

// Enum for wave types

public enum WaveType : byte {
	None = 0,
	Current,
	IncSize,
	DecSize
}

// Candidate points
public class PointCandidate {
	public GameObject enemySpawn;
	public float playerDist;
	public Vector2 pointPos;

	public PointCandidate () {}

	public PointCandidate (GameObject enemySpawn, float playerDist, Vector2 pointPos) {
		this.enemySpawn = enemySpawn;
		this.playerDist = playerDist;
		this.pointPos = pointPos;
	}

	public PointCandidate (GameObject enemySpawn, float playerDist, float pointX, float pointY) {
		this.enemySpawn = enemySpawn;
		this.playerDist = playerDist;
		this.pointPos = new Vector2(pointX, pointY);
	}
}

public class PointComparePlayerDist : IComparer<PointCandidate> {
	public int Compare (PointCandidate x, PointCandidate y) {
		if (x == null) {
			if (y == null) {
				// Both null, equal
				return 0;
			}
			else {
				// y not null, greater by default
				return -1;
			}
		}
		else {
			if (y == null) {
				// x not null, greater by default
				return 1;
			}
			else {
				// Neither null, compare by playerDist
				if (x.playerDist > y.playerDist) {
					return 1;
				}
				else if (x.playerDist < y.playerDist) {
					return -1;
				}
				else {
					// Must be equidistant
					return 0;
				}
			}
		}
	}
}

// Carrier class for wave setup
[System.Serializable]
public class WaveSpec {
	// Public specs
	public GameObject enemySpawn;
	public AudioClip spawnSound;
	public float soundDelay = 0.0f;
	public float minSpacing;
	public int roundSizeStart;
	public int roundSizeStep;
	public WaveType[] waveCycle;

	// Internal counters (might as well keep 'em separate)
	protected int roundSizeCurrent;
	protected int waveCounter;

	public void ResetCount () {
		roundSizeCurrent = roundSizeStart;
		waveCounter = 0;
	}

	// Increase/decrease size/num/both/neither
	public int Advance () {
		// Advance wave counter and see what we're supposed to do
		if (++waveCounter >= waveCycle.Length) {
			waveCounter = 0;
		}

		// Only doing size increases -- all spawns will be staggered timewise
		switch (waveCycle[waveCounter]) {
			case WaveType.IncSize:
				roundSizeCurrent += roundSizeStep;
				return (roundSizeCurrent > 0) ? roundSizeCurrent : 0;
			case WaveType.DecSize:
				roundSizeCurrent -= roundSizeStep;
				return (roundSizeCurrent > 0) ? roundSizeCurrent : 0;
			case WaveType.Current:
				return (roundSizeCurrent > 0) ? roundSizeCurrent : 0;
			case WaveType.None:
			default:
				return 0;
		}
	}

	public PointCandidate NewPoint (float playerDist, Vector2 pos) {
		return new PointCandidate(enemySpawn, playerDist, pos);
	}

	public PointCandidate NewPoint (float playerDist, float pointX, float pointY) {
		return new PointCandidate(enemySpawn, playerDist, pointX, pointY);
	}
}

public class WaveCompareSpacing : IComparer<WaveSpec> {
	public int Compare (WaveSpec x, WaveSpec y) {
		if (x == null) {
			if (y == null) {
				// Both null, equal
				return 0;
			}
			else {
				// y not null, greater by default
				return -1;
			}
		}
		else {
			if (y == null) {
				// x not null, greater by default
				return 1;
			}
			else {
				// Neither null, compare by minSpacing
				if (x.minSpacing > y.minSpacing) {
					return 1;
				}
				else if (x.minSpacing < y.minSpacing) {
					return -1;
				}
				else {
					// Must be equally-spaced
					return 0;
				}
			}
		}
	}
}
