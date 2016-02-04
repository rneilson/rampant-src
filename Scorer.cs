using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// TODO: corrupt title text
// TODO [NOPE]: make one of the scrollboxes a log (?)
// TODO [NOPE]: add PID kill message to log on each kill
// TODO [NOPE]: add something fun-sounding to log when bombing
// TODO [NOPE]: add kernel oops message to log on death
// TODO [NOPE]: add restart message on respawn
public class Scorer : MonoBehaviour {
	
	// Game state
	private GameObject playerCurrent;
	private BallMovement playerControl;
	private CameraMovement cameraFollower;
	private bool isPaused = true;
	private bool isStarted = false;

	// Title/menu stuff
	private MenuControl menu;
	private string gameTitle = "_rampant";

	// Unity 5 API changes
	private AudioSource myAudioSource;
	
	// Score tracking
	private int kills;
	private int level;
	private int maxKills;
	private int maxLevel;
	private int totalDeaths;
	private TextMesh scoreKills;
	private TextMesh scoreHigh;
	private TextMesh scoreDeaths;
	private TextMesh scoreLevel;

	// Respawn parameters
	public float respawnTime;
	public float playerBreakDelay = 1.0f;
	public float playerBreakRadius = 1.0f;
	public float maxDisplacement = 4.75f;
	private bool respawn;
	private float respawnCountdown;
	private bool playerBreak = false;		// Should rename at some point
	private Vector3 spawnPos = new Vector3 (0f, 1f, 0f);
	private Vector3 bombPos = new Vector3 (0f, 0.6f, 0f);

	// Player object and friends
	public GameObject playerType;
	public GameObject spawnEffect;
	public AudioClip spawnSound;
	public AudioClip respawnSound;
	public float respawnSoundDelay = 0.0f;
	public float respawnSoundVol = 0.5f;
	public float spawnBombForce = 800f;

	// Enemy phases (ie difficulty stuff)
	private GameObject currentPhase;
	private GameObject prevPhase;
	private int phaseIndex;
	private int phaseShift;
	private int checkpoint;

	// Powerup parameters and state tracking
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
	//private int framesActive = 0;

	// Camera tracking
	public bool cameraTracking = false;

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
	public InputMode InputTarget {
		get {
			// Forward to menu
			return menu.CurrentInput;
		}
	}
	public Vector3 SpawnPosition {
		get { return spawnPos; }
	}
	public float SpawnHeight {
		get { return spawnPos.y; }
	}

	// Use this for initialization
	void Start () {
		cameraFollower = GameObject.Find("Camera").GetComponent<CameraMovement>();
		scoreKills = GameObject.Find("Display-kills").GetComponent<TextMesh>();
		scoreHigh = GameObject.Find("Display-high").GetComponent<TextMesh>();
		scoreDeaths = GameObject.Find("Display-deaths").GetComponent<TextMesh>();
		scoreLevel = GameObject.Find("Display-level").GetComponent<TextMesh>();
		menu = GameObject.Find("Menu").GetComponent<MenuControl>();
		kills = 0;
		level = 0;
		totalDeaths = 0;
		respawn = true;
		respawnCountdown = 0.0f;
		myAudioSource = GetComponent<AudioSource>();

		menu.SetTitle(gameTitle);

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

		// Autostart if game (re)started
		if ((!isStarted) && (GameSettings.Restarted)) {
			if (globalDebug) {
				// Debug.Log("Game (re)started, autostarting as of frame " + framesActive.ToString(), gameObject);
				Debug.Log("Game (re)started, autostarting", gameObject);
			}
			// This is on its own line in case there's anything else that needs doing
			UnPauseGame();
		}

	}
	
	// Update is called once per frame
	void Update () {
		/*
		if (globalDebug) {
			framesActive++;
		}
		*/

		// Check if menu's exited or game's restarted
		if (isPaused) {
			if (menu.CurrentInput == InputMode.Game) {
				if (globalDebug) {
					Debug.Log("Menu exited, unpausing", gameObject);
				}
				UnPauseGame();
			}
		}

		// Check for quit first
		if ((Input.GetKeyDown(KeyCode.Q)) 
			&& ((Input.GetKey(KeyCode.LeftControl)) || (Input.GetKey(KeyCode.RightControl)) ) ) {
			SaveScores();
			GameSettings.Quit();
		}
		
		// if (Input.GetKeyDown(KeyCode.Escape)) {
		//if ((Input.GetButtonDown("Pause")) || (Input.GetKeyDown(KeyCode.Escape))) {
		if (Input.GetButtonDown("Pause")) {
			if (isPaused) {
				if (globalDebug) {
					Debug.Log("Pause key hit, unpausing", gameObject);
				}
				UnPauseGame();
			}
			else {
				PauseGame();
			}
		}

		// If we're not in the menu, then back will pause
		if ((menu.CurrentInput == InputMode.Game) && (Input.GetButtonDown("Back"))) {
			PauseGame();
		}

		/*
		// Check for debug capture
		if (Input.GetButtonDown("DebugCapture")) {
			PauseGame();
			Debug.Log("Debug capture:", gameObject);
			enemyControl.DebugCap();
		}
		*/

		if (!isPaused) {
			if (respawn == true) {
				respawnCountdown -= Time.deltaTime;
				if ((cameraFollower) && (cameraTracking)) {
					cameraFollower.RespawnCountdown(respawnCountdown);
				}
			}
			if (respawnCountdown <= 0f) {
				respawnCountdown = respawnTime;
				respawn = false;
				NewPlayer();
			}
		}

	}
	
	public void AddKill () {
		if(respawn == false){
			kills++;
			scoreKills.text = "Kills: " + kills.ToString();

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
		if ((phaseShift != phaseIndex) || (phaseShift == GameSettings.CurrentMode.Terminal)) {
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
	
	public void NextPhase () {
		// Deactivate current phase and slate for destruction
		currentPhase.GetComponent<EnemyPhase>().StopPhase();
		prevPhase = currentPhase;

		// Save current wave number as checkpoint
		checkpoint = level;

		// Advance index, and go to terminal phase if all phases complete
		phaseIndex++;
		if (phaseIndex >= GameSettings.CurrentMode.PhaseCount) {
			phaseIndex = GameSettings.CurrentMode.Terminal;
		}

		// Instantiate new phase, and let chips fall
		currentPhase = Instantiate(GameSettings.CurrentMode.GetPhase(phaseIndex));
	}
	
	void PlayerDied () {
		// Update high score
		if (kills > maxKills) {
			maxKills = kills;
			scoreHigh.text = "Best: " + maxKills.ToString();
			// Update mode's high score
			GameSettings.CurrentMode.HighScore("Kills", maxKills);
		}

		// Update high wave
		if (level > maxLevel) {
			maxLevel = level;
			scoreDeaths.text = "Best: " + maxLevel.ToString();
			// Update mode's high score
			GameSettings.CurrentMode.HighScore("Waves", maxLevel);
		}

		// Set respawn, update counts, etc		
		respawn = true;
		playerBreak = true;
		totalDeaths++;
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
		Rigidbody rb;
		
		// Clear enemies
		enemies = Physics.OverlapSphere(bombAt, killRadius, mask);
		for (int i=0; i<enemies.Length; i++) {
			enemies[i].SendMessage("Clear", false, SendMessageOptions.DontRequireReceiver);
		}
		
		// Push away remaining enemies
		enemies = Physics.OverlapSphere(bombAt, pushRadius, mask);
		for (int i=0; i<enemies.Length; i++) {
			rb = enemies[i].GetComponent<Rigidbody>();
			if (rb) {
				rb.AddExplosionForce(spawnBombForce, bombAt, 0);
			}
			else {
				// Try parent instead
				rb = enemies[i].transform.parent.GetComponent<Rigidbody>();
				if (rb) {
					rb.AddExplosionForce(spawnBombForce, bombAt, 0);
				}
			}
		}
	}
	
	void NewPlayer () {
		// Hide Menu
		menu.HideMenu();
		
		// Bomb enemies near spawn point
		SpawnBomb(spawnPos, bombPos, 2.5f, 5.0f);
		
		// Reset kills
		kills = 0;
		scoreKills.text = "Kills: " + kills.ToString();

		// Reset powerup threshold
		killsUntilPowerup = biggerGunAt;

		// Reset current enemy phase and level
		currentPhase.GetComponent<EnemyPhase>().ResetPhase(this);
		level = checkpoint;
		scoreLevel.text = "Wave: " + level.ToString();

		// Spawn player, notify camera
		playerCurrent = (GameObject) Instantiate(playerType, spawnPos, Quaternion.Euler(0, 0, 0));
		playerControl = playerCurrent.GetComponent<BallMovement>();
		if ((cameraFollower) && (cameraTracking)) {
			cameraFollower.NewPlayer(playerCurrent);
		}
		
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

		// Reset input
		Input.ResetInputAxes();

		// Show menu
		menu.ShowMenu(menu.RootNode);
	}
		
	void UnPauseGame () {
		isPaused = false;

		if (!isStarted) {
			SendStartGame();
		}
		Time.timeScale = 1;

		// Hide menu
		menu.HideMenu();

		// Reset input
		Input.ResetInputAxes();
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
		// Mode debug
		if (globalDebug) {
			Debug.Log("Loading mode: " + GameSettings.CurrentMode.Name, gameObject);
		}

		// Get high scores from current mode
		maxKills = GameSettings.CurrentMode.GetScore("Kills");
		scoreHigh.text = "Best: " + maxKills.ToString();
		maxLevel = GameSettings.CurrentMode.GetScore("Waves");
		scoreDeaths.text = "Best: " + maxLevel.ToString();

		// Start first enemy phase
		phaseIndex = 0;
		phaseShift = -1;
		currentPhase = Instantiate(GameSettings.CurrentMode.GetPhase(phaseIndex));

		// Inform pulsers and shifters that game has started
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

		isStarted = true;
	}

	IEnumerator PlayDelayedClip (AudioClip toPlay, float delay, float volume) {
		yield return new WaitForSeconds(delay);
		myAudioSource.PlayOneShot(toPlay, volume);
	}

	public void SaveScores () {
		// For saving high scores (when player isn't dead yet) before quitting
		if (kills > maxKills) {
			GameSettings.CurrentMode.HighScore("Kills", kills);
		}
		if (level > maxLevel) {
			GameSettings.CurrentMode.HighScore("Waves", level);
		}
	}
}

public enum DeathType : byte {
	None = 0,
	Silently,
	Quietly,
	Loudly
}
