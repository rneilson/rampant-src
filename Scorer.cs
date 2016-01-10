using UnityEngine;
using System.Collections;

public class Scorer : MonoBehaviour {
	
	private TextMesh scoreKills;
	private TextMesh scoreHigh;
	private TextMesh scoreDeaths;
	private TextMesh scoreLevel;
	private TextMesh titleText;
	private TextMesh subtitleText;
	//private Component[] spawners;
	private float respawnCountdown;
	//private bool respawn;
	private GameObject playerCurrent;
	private BallMovement playerControl;
	private CameraMovement cameraFollower;
	private bool isPaused = true;
	private int totalSpawned;
	private string instructionsForceBomb = "Move: left stick/WASD keys\nShoot: right stick/arrow keys\nMouse shoot: left mouse button\nPause: start button/tab\nQuit: Q";
	private string instructionsNoForceBomb = "Move: left stick/WASD keys\nShoot: right stick/arrow keys\nMouse shoot: left mouse button\nPause: start button/tab\nBomb: space/right mouse button\nBomb: left/right trigger\nQuit: Q";
	private string instructions;

	// Cursor state
	private CursorLockMode desiredCursorMode;
	private bool desiredCursorVisibility;

	// Unity 5 API changes
	private AudioSource myAudioSource;
	
	public int kills;
	public int level;
	public int maxKills;
	public int totalDeaths;
	public float respawnTime;

	private bool respawn;

	public bool Respawn {
		get { return respawn; }
	}
	public int Level {
		get { return level; }
	}

	// Player object and friends
	public GameObject playerType;
	public GameObject spawnEffect;
	public AudioClip spawnSound;

	// Enemy phases (ie difficulty stuff)
	public GameObject[] enemyPhases;
	private GameObject currentPhase;
	private int phaseIndex;
	public int checkpoint;

	// Powerup state tracking
	public bool forceBombUse;
	public int biggerGunAt;
	public int giveBombEvery;
	public int bombMinusOneAt;
	public int bombMinusTwoAt;
	private int killsUntilPowerup;

	// Use this for initialization
	void Start () {
		cameraFollower = GameObject.Find("Camera").GetComponent<CameraMovement>();
		scoreKills = GameObject.Find("Display-kills").GetComponent<TextMesh>();
		scoreHigh = GameObject.Find("Display-high").GetComponent<TextMesh>();
		scoreDeaths = GameObject.Find("Display-deaths").GetComponent<TextMesh>();
		scoreLevel = GameObject.Find("Display-level").GetComponent<TextMesh>();
		titleText = GameObject.Find("TitleText").GetComponent<TextMesh>();
		subtitleText = GameObject.Find("SubtitleText").GetComponent<TextMesh>();
		//spawners = gameObject.GetComponents(typeof(Spawner));
		kills = 0;
		level = 0;
		maxKills = 0;
		totalDeaths = 0;
		respawn = true;
		respawnCountdown = 0.0f;
		totalSpawned = 0;
		myAudioSource = GetComponent<AudioSource>();
		desiredCursorMode = Cursor.lockState;
		desiredCursorVisibility = Cursor.visible;

		// Start first enemy phase
		phaseIndex = 0;
		currentPhase = Instantiate(enemyPhases[phaseIndex]);

		// Check which instructions to make visible
		if (forceBombUse) {
			instructions = instructionsForceBomb;
		}
		else {
			instructions = instructionsNoForceBomb;
		}
		titleText.text = "A Plain Shooter";
		subtitleText.text = "Press start button/tab to begin\n" + instructions;
	}
	
	// Update is called once per frame
	void Update () {
		
		// if (Input.GetKeyDown(KeyCode.Escape)) {
		if (Input.GetButtonDown("Pause")) {
			if (isPaused) {
				UnPauseGame();
			}
			else {
				PauseGame();
			}
		}

		if (Cursor.lockState != desiredCursorMode) {
			Cursor.lockState = desiredCursorMode;
		}
		if (Cursor.visible != desiredCursorVisibility) {
			Cursor.visible = desiredCursorVisibility;
		}
		
		if (!isPaused) {
			if (respawn == true) {
				respawnCountdown -= Time.deltaTime;
				//cameraFollower.SendMessage("RespawnCountdown", respawnCountdown);
				cameraFollower.RespawnCountdown(respawnCountdown);
			}
			if (respawnCountdown <= 0f) {
				respawnCountdown = respawnTime;
				respawn = false;
				NewPlayer();
			}
		}
		else {
			if (Input.GetKeyDown(KeyCode.Q))
				Application.Quit();
		}
	}
	
	public void AddKill () {
		if(respawn == false){
			kills++;
			scoreKills.text = "Kills: " + kills.ToString();

			/*
			// Update high score
			if (kills > maxKills) {
				maxKills = kills;
				scoreHigh.text = "Best: " + maxKills.ToString();
			}
			*/

			// Check for weapon upgrade, or bomb, or bomb-minus warning
			if (!playerControl.HasBomb) {
				killsUntilPowerup--;

				if (playerControl.BiggerGun) {
					if (killsUntilPowerup == bombMinusTwoAt) {
						playerControl.BombMinusTwo();
					}
					else if (killsUntilPowerup == bombMinusOneAt) {
						playerControl.BombMinusOne();
					}
					else if (killsUntilPowerup == 0) {
						killsUntilPowerup = giveBombEvery;
						playerControl.GiveBomb(forceBombUse);
						if (forceBombUse) {
							playerControl.UseBomb();
						}
					}
				}
				else {
					if (killsUntilPowerup == 0) {
						playerControl.FireFaster();
						killsUntilPowerup = giveBombEvery;
					}
				}
			}
			
			/* Old powerup code
			int modKills = kills % 50;
			//Debug.Log("Kills: " + kills.ToString() + ", spawned: " + totalSpawned.ToString());
			if (kills >= 50) {
				if (modKills == 40) {
					if (playerCurrent != null)
						playerCurrent.SendMessage("BombMinusTwo");
				}
				else if (modKills == 45) {
					if (playerCurrent != null)
						playerCurrent.SendMessage("BombMinusOne");
				}
				else if (modKills == 0) {
					if (playerCurrent != null)
						playerCurrent.SendMessage("FireFaster");
				}
			}*/
		}
	}
	
	public void AddLevel () {
		level++;
		scoreLevel.text = "Wave: " + level.ToString();
	}
	
	public void AddSpawns (int spawns) {
		totalSpawned += spawns;
	}

	public void NextPhase () {
		// Destroy current phase, obvs
		//Destroy(currentPhase, 0.0f);

		// Deactivate current phase (testing, mostly)
		currentPhase.GetComponent<EnemyPhase>().StopPhase();

		// Save current wave number as checkpoint
		checkpoint = level;

		// Advance index, and loop around if all phases complete
		phaseIndex++;
		if (phaseIndex >= enemyPhases.Length) {
			phaseIndex = 0;
		}

		// Instantiate new phase, and let chips fall
		currentPhase = Instantiate(enemyPhases[phaseIndex]);
	}
	
	void PlayerDied () {
		// Update high score
		if (kills > maxKills) {
			maxKills = kills;
			scoreHigh.text = "Best: " + maxKills.ToString();
		}

		// Set respawn, update counts, etc		
		respawn = true;
		totalDeaths++;
		scoreDeaths.text = "Deaths: " + totalDeaths.ToString();
		titleText.text = kills.ToString() + " kills";
		subtitleText.text = "Total deaths: " + totalDeaths.ToString() + "\nMost kills: " + maxKills.ToString();
		ClearTargets();
		/*
		for (int i =  0; i<spawners.Length; i++) {
			spawners[i].SendMessage("ClearTargets");
		}
		*/
	}
	
	void SpawnBomb (Vector3 pos, float killRadius, float pushRadius) {
		Collider[] enemies;
		int mask = 1 << LayerMask.NameToLayer("Enemy");
		//Debug.Log(mask);
		
		// Clear enemies
		enemies = Physics.OverlapSphere(pos, killRadius, mask);
		//Debug.Log(enemies.Length);
		for (int i=0; i<enemies.Length; i++) {
			enemies[i].SendMessage("Clear", false);
		}
		
		// Push away remaining enemies
		enemies = Physics.OverlapSphere(pos, pushRadius, mask);
		//Debug.Log(enemies.Length);
		for (int i=0; i<enemies.Length; i++) {
			enemies[i].GetComponent<Rigidbody>().AddExplosionForce(500f, pos, 0);
		}
	}
	
	void NewPlayer () {
		// Clear title text
		titleText.text = "";
		subtitleText.text = "";
		
		// Spawn effect in center
		Vector3 spawnPos = new Vector3 (0, 1, 0);
		Destroy(Instantiate(spawnEffect, spawnPos, Quaternion.Euler(-90, 0, 0)), 1f);
		myAudioSource.PlayOneShot(spawnSound, 1.0f);
		
		// Bomb enemies near spawn point
		SpawnBomb(spawnPos, 2.0f, 5.0f);
		
		// Old bomb routine
		/* GameObject[] enemies;
		enemies = GameObject.FindGameObjectsWithTag("Enemy");
		for (int i = 0; i < enemies.Length; i++) {
			var enemy = enemies[i];
			enemy.rigidbody.AddExplosionForce(1000f, spawnPos, 5.0f);
			if ((enemy.transform.position - spawnPos).magnitude < 2.0f) {
				enemy.SendMessage("BlowUp", false);
			}
		} */
		
		// Reset kills
		kills = 0;
		scoreKills.text = "Kills: " + kills.ToString();

		// Reset powerup threshold
		killsUntilPowerup = biggerGunAt;

		// Reset current enemy phase and level
		currentPhase.GetComponent<EnemyPhase>().ResetPhase();
		level = checkpoint;
		scoreLevel.text = "Wave: " + level.ToString();

		// Spawn player, notify camera
		playerCurrent = (GameObject) Instantiate(playerType, spawnPos, Quaternion.Euler(0, 0, 0));
		playerControl = playerCurrent.GetComponent<BallMovement>();
		//cameraFollower.SendMessage("NewPlayer", playerCurrent);
		cameraFollower.NewPlayer(playerCurrent);
		
		// Assign new target
		NewTargets(playerCurrent);
		/*
		for (int i =  0; i<spawners.Length; i++) {
			spawners[i].SendMessage("NewTargets");
		}
		*/
	}

	void PauseGame () {
		isPaused = true;
		Time.timeScale = 0;
		titleText.text = "Paused";
		subtitleText.text = instructions;
		desiredCursorVisibility = true;
		//Cursor.lockState = CursorLockMode.None;
		desiredCursorMode = CursorLockMode.None;
	}
		
	void UnPauseGame () {
		isPaused = false;
		Time.timeScale = 1;
		titleText.text = "";
		subtitleText.text = "";
		desiredCursorVisibility = false;
		//Cursor.lockState = CursorLockMode.Confined;
		//Cursor.lockState = CursorLockMode.Locked;
		desiredCursorMode = CursorLockMode.Locked;
	}

	void ClearTargets () {
		GameObject[] enemies;
		enemies = GameObject.FindGameObjectsWithTag("Enemy");
		for (int i = 0; i < enemies.Length; i++) {
			enemies[i].SendMessage("ClearTarget");
		}
	}
	
	void NewTargets (GameObject player) {
		GameObject[] enemies;
		enemies = GameObject.FindGameObjectsWithTag("Enemy");
		for (int i = 0; i < enemies.Length; i++) {
			enemies[i].SendMessage("NewTarget", player);
		}
	}
}
