using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// TODO: change quit key to Esc
// TODO: peel off pause menu into its own class (classes?)
// TODO: corrupt title text
// TODO: make one of the scrollboxes a log (?)
// TODO: add PID kill message to log on each kill
// TODO: add something fun-sounding to log when bombing
// TODO: add kernel oops message to log on death
// TODO: add restart message on respawn
public class Scorer : MonoBehaviour {
	
	private TextMesh scoreKills;
	private TextMesh scoreHigh;
	private TextMesh scoreDeaths;
	private TextMesh scoreLevel;
	private TextMesh titleText;
	private TextMesh subtitleText;
	private float respawnCountdown;
	private GameObject playerCurrent;
	private BallMovement playerControl;
	private CameraMovement cameraFollower;
	private bool isPaused = true;
	private bool isStarted = false;
	private int totalSpawned;
	private string instructionsForceBomb = "Move: left stick/WASD keys\nShoot: right stick/arrow keys\nMouse shoot: left mouse button\nPause: start button/tab\nQuit: Q";
	private string instructionsNoForceBomb = "Move: left stick/WASD keys\nShoot: right stick/arrow keys\nMouse shoot: left mouse button\nPause: start button/tab\nBomb: space/right mouse button\nBomb: left/right trigger\nQuit: Q";
	private string instructions;
	private string prevTitle;
	private string prevSubtitle;

	// Cursor state
	private CursorLockMode desiredCursorMode;
	private bool desiredCursorVisibility;

	// Unity 5 API changes
	private AudioSource myAudioSource;
	
	private int kills;
	private int level;
	private int maxKills;
	private int maxLevel;
	private int totalDeaths;
	public float respawnTime;

	private bool respawn;
	private bool playerBreak = false;		// Should rename at some point
	public float playerBreakDelay = 1.0f;
	public float playerBreakRadius = 1.0f;
	private float maxDisplacement = 4.75f;
	private Vector3 spawnPos = new Vector3 (0f, 1f, 0f);
	private Vector3 bombPos = new Vector3 (0f, 0.6f, 0f);

	// Player object and friends
	public GameObject playerType;
	public GameObject spawnEffect;
	public AudioClip spawnSound;
	public AudioClip respawnSound;
	public float respawnSoundDelay = 0.0f;
	public float respawnSoundVol = 0.5f;

	// Enemy phases (ie difficulty stuff)
	public GameObject[] enemyPhases;
	private GameObject currentPhase;
	private GameObject prevPhase;
	private int phaseIndex;
	private int phaseShift;
	private int checkpoint;
	public int terminalPhase;

	// Powerup state tracking
	public bool forceBombUse;
	public int biggerGunAt;
	public int giveBombEvery;
	public int bombMinusOneAt;
	public int bombMinusTwoAt;
	private int killsUntilPowerup;

	// Plane (etc) for color pulses and material shifts
	public Color playerPulseColor = Color.white * 0.4f;
	private Color currentPulseColor;
	private List<GameObject> arenaPulsers = new List<GameObject>();
	private List<GameObject> arenaShifters = new List<GameObject>();

	// Global debug option
	public bool globalDebug = false;

	// Ground control co-component
	private RedCubeGroundControl enemyControl;

	public bool Respawn {
		get { return respawn; }
	}
	public int Level {
		get { return level; }
	}
	public bool SingleGun {
		get {
			if ((playerControl) && (playerControl.BiggerGun)) {
				return false;
			}
			else {
				return true;
			}
		}
	}
	public GameObject Player {
		get { return playerCurrent; }
	}
	public bool PlayerBreak {
		get { return playerBreak; }
	}
	public float PlayerBreakDelay {
		get { return playerBreakDelay; }
	}
	public float PlayerBreakRadius {
		get { return playerBreakRadius; }
	}
	public float MaxDisplacement {
		get { return maxDisplacement; }
	}
	public int PhaseIndex {
		get { return phaseIndex; }
	}
	public bool IsPaused {
		get { return isPaused; }
	}
	public bool IsStarted {
		get { return isStarted; }
	}
	public bool GlobalDebug {
		get { return globalDebug; }
	}

	// Use this for initialization
	void Start () {
		cameraFollower = GameObject.Find("Camera").GetComponent<CameraMovement>();
		scoreKills = GameObject.Find("Display-kills").GetComponent<TextMesh>();
		scoreHigh = GameObject.Find("Display-high").GetComponent<TextMesh>();
		scoreDeaths = GameObject.Find("Display-deaths").GetComponent<TextMesh>();
		scoreLevel = GameObject.Find("Display-level").GetComponent<TextMesh>();
		titleText = GameObject.Find("TitleText").GetComponent<TextMesh>();
		subtitleText = GameObject.Find("SubtitleText").GetComponent<TextMesh>();
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

		// Start paused
		/*
		if (isPaused) {
			Time.timeScale = 0;
		}
		*/

		// Start first enemy phase
		phaseIndex = 0;
		phaseShift = -1;
		currentPhase = Instantiate(enemyPhases[phaseIndex]);

		// Sanity check on terminal phase setting
		// Defaults to last phase in array
		if (terminalPhase >= enemyPhases.Length) {
			terminalPhase = enemyPhases.Length - 1;
		}

		// Check which instructions to make visible
		if (forceBombUse) {
			instructions = instructionsForceBomb;
		}
		else {
			instructions = instructionsNoForceBomb;
		}
		titleText.text = "_rampant";
		subtitleText.text = "Press start button/tab to begin\n" + instructions;

		// Get ground control
		enemyControl = GetComponent<RedCubeGroundControl>();
		// Get pulsers
		foreach (GameObject pulser in GameObject.FindGameObjectsWithTag("ArenaPulser")) {
			arenaPulsers.Add(pulser);
		}
		currentPulseColor = playerPulseColor;
		// Get shifters
		foreach (GameObject shifter in GameObject.FindGameObjectsWithTag("ArenaShifter")) {
			arenaShifters.Add(shifter);
		}
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

		// Check for debug capture
		if (Input.GetButtonDown("DebugCapture")) {
			PauseGame();
			Debug.Log("Debug capture:", gameObject);
			enemyControl.DebugCap();
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

			/* Decided not to do that every kill, only on death
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
					if ((killsUntilPowerup == bombMinusTwoAt) && (forceBombUse)) {
						playerControl.BombMinusTwo();
					}
					else if ((killsUntilPowerup == bombMinusOneAt) && (forceBombUse)) {
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
		}
	}
	
	public void AddLevel () {
		level++;
		scoreLevel.text = "Wave: " + level.ToString();
		playerBreak = false;

		// Destroy previous phase, if present
		if (prevPhase) {
			Destroy(prevPhase, 0.0f);
		}

	}

	public void StartNewPhase (Color pulseColor) {
		if ((phaseShift != phaseIndex) || (phaseShift == terminalPhase)) {
			phaseShift = phaseIndex;
			// Shift if we're shifting
			ShiftGrid(phaseShift);
			// Set color, just in case we die during phase transition
			NewRespawnColor(pulseColor);
			// Phase used to do this itself
			FlashGrid(pulseColor);
		}
		AddLevel();
	}
	
	public void AddSpawns (int spawns) {
		totalSpawned += spawns;
	}

	public void NextPhase () {
		// Deactivate current phase and slate for destruction
		currentPhase.GetComponent<EnemyPhase>().StopPhase();
		prevPhase = currentPhase;

		// Save current wave number as checkpoint
		checkpoint = level;

		// Advance index, and go to terminal phase if all phases complete
		phaseIndex++;
		if (phaseIndex >= enemyPhases.Length) {
			phaseIndex = terminalPhase;
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

		// Update high wave
		if (level > maxLevel) {
			maxLevel = level;
			scoreDeaths.text = "Best: " + maxLevel.ToString();
		}

		// Set respawn, update counts, etc		
		respawn = true;
		playerBreak = true;
		totalDeaths++;
		titleText.text = kills.ToString() + " kills";
		subtitleText.text = "Total deaths: " + totalDeaths.ToString() + "\nMost kills: " + maxKills.ToString();
		ClearTargets();

		// Play respawn countdown sound
		if (respawnSound) {
			if (respawnSoundDelay > 0.0f) {
				StartCoroutine(PlayDelayedClip(respawnSound, respawnSoundDelay, respawnSoundVol));
			}
			else {
				myAudioSource.PlayOneShot(respawnSound, respawnSoundVol);
			}
		}
	}
	
	void SpawnBomb (Vector3 spawnAt, Vector3 bombAt, float killRadius, float pushRadius) {
		// Spawn effect in center
		Destroy(Instantiate(spawnEffect, spawnAt, Quaternion.Euler(-90, 0, 0)), 1f);
		myAudioSource.PlayOneShot(spawnSound, 1.0f);

		Collider[] enemies;
		int mask = 1 << LayerMask.NameToLayer("Enemy");
		
		// Clear enemies
		enemies = Physics.OverlapSphere(bombAt, killRadius, mask);
		for (int i=0; i<enemies.Length; i++) {
			enemies[i].SendMessage("Clear", false);
		}
		
		// Push away remaining enemies
		enemies = Physics.OverlapSphere(bombAt, pushRadius, mask);
		for (int i=0; i<enemies.Length; i++) {
			enemies[i].GetComponent<Rigidbody>().AddExplosionForce(500f, bombAt, 0);
		}
	}
	
	void NewPlayer () {
		// Clear title text
		titleText.text = "";
		subtitleText.text = "";
		
		// Bomb enemies near spawn point
		SpawnBomb(spawnPos, bombPos, 2.0f, 5.0f);
		
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
		cameraFollower.NewPlayer(playerCurrent);
		
		// Assign new target
		NewTargets(playerCurrent);

		// Flash grid
		FlashGrid(currentPulseColor);
		// Shift grid
		if (totalDeaths > 0) {
			ShiftGrid(phaseShift);
		}
	}

	void PauseGame () {
		isPaused = true;
		Time.timeScale = 0;
		prevTitle = titleText.text;
		prevSubtitle = subtitleText.text;
		titleText.text = "Paused";
		subtitleText.text = instructions;
		desiredCursorVisibility = true;
		desiredCursorMode = CursorLockMode.None;
	}
		
	void UnPauseGame () {
		isPaused = false;
		if (!isStarted) {
			isStarted = true;
			SendStartGame();
		}
		Time.timeScale = 1;
		titleText.text = prevTitle;
		subtitleText.text = prevSubtitle;
		desiredCursorVisibility = false;
		desiredCursorMode = CursorLockMode.Locked;
	}

	void ClearTargets () {
		GameObject[] enemies;
		enemies = GameObject.FindGameObjectsWithTag("Enemy");
		for (int i = 0; i < enemies.Length; i++) {
			enemies[i].SendMessage("ClearTarget", null, SendMessageOptions.DontRequireReceiver);
		}
	}
	
	void NewTargets (GameObject player) {
		// First give ground control the update
		enemyControl.NewTarget(player);

		GameObject[] enemies;
		enemies = GameObject.FindGameObjectsWithTag("Enemy");
		for (int i = 0; i < enemies.Length; i++) {
			enemies[i].SendMessage("NewTarget", player, SendMessageOptions.DontRequireReceiver);
		}
	}

	public void FlashGrid (Color gridColor) {
		// Update each flasher with new color (same times, though)
		foreach (GameObject pulser in arenaPulsers) {
			if (pulser) {
				pulser.SendMessage("NewPulseMsg", gridColor, SendMessageOptions.DontRequireReceiver);
			}
		}
	}

	public void NewRespawnColor (Color gridColor) {
		currentPulseColor = gridColor;
	}

	public void ShiftGrid (int shiftIndex) {
		foreach (GameObject shifter in arenaShifters) {
			if (shifter) {
				shifter.SendMessage("BeginShift", shiftIndex, SendMessageOptions.DontRequireReceiver);
			}
		}
	}

	void SendStartGame () {
		foreach (GameObject pulser in arenaPulsers) {
			if (pulser) {
				pulser.SendMessage("GameStarted", null, SendMessageOptions.DontRequireReceiver);
			}
		}
		foreach (GameObject shifter in arenaShifters) {
			if (shifter) {
				shifter.SendMessage("GameStarted", null, SendMessageOptions.DontRequireReceiver);
			}
		}
	}

	IEnumerator PlayDelayedClip (AudioClip toPlay, float delay, float volume) {
		yield return new WaitForSeconds(delay);
		myAudioSource.PlayOneShot(toPlay, volume);
	}
}

public enum DeathType : byte {
	None = 0,
	Silently,
	Quietly,
	Loudly
}
