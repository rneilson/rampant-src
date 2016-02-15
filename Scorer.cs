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
	private bool isTerminal = false;

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
	private TextMesh lastKills;
	private TextMesh highKills;
	private TextMesh scoreLevel;
	private TextMesh lastLevel;
	private TextMesh highLevel;

	// Difficulty paramters (to be moved to mode spec)
	//public float waveClearCountdown = 0.25f;
	//public float playerBreakDelay = 1.0f;
	//public float playerBreakRadius = 1.0f;
	//public float playerBreakFraction = 0.5f;
	//public int biggerGunAt;
	//public int giveBombEvery;
	//public float spawnBombForce = 800f;

	// Respawn parameters
	public float startDelay = 1.0f;
	public float respawnTime;
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
	public GameObject respawnOrb;
	public float respawnOrbDelay = 0.25f;

	// Enemy phases (ie difficulty stuff)
	private EnemyPhase currentPhase;
	private EnemyPhase prevPhase;
	private int phaseIndex;
	private int phaseShift;
	private int checkpoint;
	private int checkpointPhase;

	// Terminal phase parameters
	public Color[] terminalColors;
	public bool randomTerminalColor = false;
	private int terminalColorIndex;

	// Powerup parameters and state tracking
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
	public float RespawnCountdown {
		get { return respawnCountdown; }
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
		get { return GameSettings.CurrentMode.Difficulty.playerBreakDelay; }
	}
	public float PlayerBreakRadius {
		get { return GameSettings.CurrentMode.Difficulty.playerBreakRadius; }
	}
	public float PlayerBreakFraction {
		get { return GameSettings.CurrentMode.Difficulty.playerBreakFraction; }
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
	public bool IsTerminal {
		get { return isTerminal; }
	}
	public bool WaveClear {
		get { return enemyControl.Clear; }
	}
	public float WaveClearCountdown {
		get { return GameSettings.CurrentMode.Difficulty.waveClearCountdown; }
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
		lastKills = GameObject.Find("Last-kills").GetComponent<TextMesh>();
		highKills = GameObject.Find("High-kills").GetComponent<TextMesh>();
		scoreLevel = GameObject.Find("Display-level").GetComponent<TextMesh>();
		lastLevel = GameObject.Find("Last-level").GetComponent<TextMesh>();
		highLevel = GameObject.Find("High-level").GetComponent<TextMesh>();
		menu = GameObject.Find("Menu").GetComponent<MenuControl>();
		kills = 0;
		level = 0;
		totalDeaths = 0;
		respawn = true;
		respawnCountdown = startDelay;
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
				if (GameSettings.NumStarts <= 1) {
					Debug.Log("Game started", gameObject);
				}
				else {
					Debug.Log("Game restarted, total starts: " + GameSettings.NumStarts.ToString(), gameObject);
				}
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

				if (killsUntilPowerup == 0) {
					if (playerControl.BiggerGun) {
						killsUntilPowerup = GameSettings.CurrentMode.Difficulty.giveBombEvery;
						playerControl.GiveBomb(false);
					}
					else {
						playerControl.FireFaster();
						killsUntilPowerup = GameSettings.CurrentMode.Difficulty.giveBombEvery;
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
			Destroy(prevPhase.gameObject, 0.0f);
		}

	}

	public void StartNewPhase (Color pulseColor) {
		if (phaseShift != phaseIndex) {
			if (isTerminal) {
				// Set color from terminal list
				NextTerminalColor();
				NewRespawnColor(TerminalColor());
			}
			else {
				// Set color, just in case we die during phase transition
				NewRespawnColor(pulseColor);
				phaseShift = phaseIndex;
			}
			// Shift if we're shifting
			ShiftGrid(phaseShift + terminalColorIndex);
			// Phase used to do this itself
			FlashGrid(currentPulseColor);
		}
		AddLevel();
	}
	
	public void NextPhase () {
		bool justWentTerminal = false;

		// Advance index, and go to terminal phase if all phases complete
		phaseIndex++;

		if (phaseIndex >= GameSettings.CurrentMode.PhaseCount) {
			// Go to terminal phase
			phaseIndex = GameSettings.CurrentMode.PhaseCount;

			// Set terminal if not already there
			if (!isTerminal) {
				justWentTerminal = true;
				GoTerminal();
			}
		}

		// Deactivate current phase and slate for destruction
		currentPhase.StopPhase();
		prevPhase = currentPhase;
		if (isTerminal) {
			// Load terminal phase
			currentPhase = Instantiate(GameSettings.CurrentMode.Terminal).GetComponent<EnemyPhase>();
		}
		else {
			// Instantiate new phase, and let chips fall
			currentPhase = Instantiate(GameSettings.CurrentMode.GetPhase(phaseIndex)).GetComponent<EnemyPhase>();
		}

		// Save current wave number as checkpoint
		if (GameSettings.CurrentMode.Difficulty.allowCheckpoints) {
			if (((currentPhase.Checkpoint) && (!isTerminal)) 
				|| ((GameSettings.CurrentMode.Difficulty.terminalCheckpoint) && (justWentTerminal))) {
				checkpoint = level;
				checkpointPhase = phaseIndex;
			}
		}
	}
	
	void PlayerDied () {
		// Update high score
		lastKills.text = "Last: " + kills.ToString();
		if (kills > maxKills) {
			maxKills = kills;
			highKills.text = "Best: " + maxKills.ToString();
			// Update mode's high score
			GameSettings.CurrentMode.HighScore("Kills", maxKills);
		}

		// Update high wave
		lastLevel.text = "Last: " + level.ToString();
		if (level > maxLevel) {
			maxLevel = level;
			highLevel.text = "Best: " + maxLevel.ToString();
			// Update mode's high score
			GameSettings.CurrentMode.HighScore("Waves", maxLevel);
		}

		// Set respawn, update counts, etc		
		respawn = true;
		playerBreak = true;
		totalDeaths++;
		ClearTargets();

		RespawnEffects();
	}
	
	void SpawnBomb (Vector3 spawnAt, Vector3 bombAt, float killRadius, float pushRadius) {
		// Spawn effect in center
		Destroy(Instantiate(spawnEffect, spawnAt, Quaternion.Euler(-90, 0, 0)), 1f);
		myAudioSource.PlayOneShot(spawnSound, 1.0f);

		Collider[] enemies;
		int mask = 1 << LayerMask.NameToLayer("Enemy");
		Rigidbody rb;
		
		// Clear enemies
		if (killRadius > 0.0f) {
			enemies = Physics.OverlapSphere(spawnAt, killRadius, mask);
			for (int i=0; i<enemies.Length; i++) {
				enemies[i].SendMessage("Clear", false, SendMessageOptions.DontRequireReceiver);
			}
		}
		
		// Push away remaining enemies
		if (pushRadius > 0.0f) {
			enemies = Physics.OverlapSphere(spawnAt, pushRadius, mask);
			for (int i=0; i<enemies.Length; i++) {
				rb = enemies[i].GetComponent<Rigidbody>();
				if (rb) {
					rb.AddExplosionForce(GameSettings.CurrentMode.Difficulty.spawnBombForce, bombAt, 0);
				}
				else {
					// Try parent instead
					rb = enemies[i].transform.parent.GetComponent<Rigidbody>();
					if (rb) {
						rb.AddExplosionForce(GameSettings.CurrentMode.Difficulty.spawnBombForce, bombAt, 0);
					}
				}
			}
		}
	}
	
	void NewPlayer () {
		// Hide Menu
		menu.HideMenu();
		
		// Bomb enemies near spawn point
		SpawnBomb(spawnPos, bombPos, GameSettings.CurrentMode.Difficulty.spawnKillRadius, 
			GameSettings.CurrentMode.Difficulty.spawnPushRadius);
		
		// Reset kills
		kills = 0;
		scoreKills.text = "Kills: " + kills.ToString();

		// Reset enemy phase and level
		level = checkpoint;
		if (phaseIndex == checkpointPhase) {
			currentPhase.ResetPhase(this);
			if (isTerminal) {
				ResetPulseColor();
			}
		}
		else {
			// Reset phase index
			phaseIndex = checkpointPhase;

			// Deactivate current phase and slate for destruction
			currentPhase.StopPhase();
			prevPhase = currentPhase;

			// Load new phase
			if (phaseIndex < GameSettings.CurrentMode.PhaseCount) {
				// Cancel terminal status, if set
				CancelTerminal();
				// Set phase shift to new index too
				phaseShift = phaseIndex;
				// Get new (old) phase
				currentPhase = Instantiate(GameSettings.CurrentMode.GetPhase(phaseIndex)).GetComponent<EnemyPhase>();
			}
			else {
				// Set phase shift to last index
				phaseShift = GameSettings.CurrentMode.PhaseCount - 1;
				currentPhase = Instantiate(GameSettings.CurrentMode.Terminal).GetComponent<EnemyPhase>();
			}

			// Reset pulse color
			ResetPulseColor();
		}
		scoreLevel.text = "Wave: " + level.ToString();

		// Spawn player, notify camera
		playerCurrent = (GameObject) Instantiate(playerType, spawnPos, Quaternion.Euler(0, 0, 0));
		playerControl = playerCurrent.GetComponent<BallMovement>();
		if ((cameraFollower) && (cameraTracking)) {
			cameraFollower.NewPlayer(playerCurrent);
		}
		
		// Reset powerup threshold
		if (GameSettings.CurrentMode.Difficulty.biggerGunAt > 0) {
			killsUntilPowerup = GameSettings.CurrentMode.Difficulty.biggerGunAt;
		}
		else {
			playerControl.FireFaster();
			killsUntilPowerup = GameSettings.CurrentMode.Difficulty.giveBombEvery;
		}

		// Assign new target
		NewTargets(playerCurrent);

		// Flash grid
		FlashGrid(currentPulseColor);

		// Shift grid
		if (totalDeaths > 0) {
			ShiftGrid(phaseShift + terminalColorIndex);
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
			StartGame();
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

	void RespawnEffects () {
		// Play respawn countdown sound
		if (respawnSound) {
			if (respawnSoundDelay > 0.0f) {
				StartCoroutine(PlayDelayedClip(respawnSound, respawnSoundDelay, respawnSoundVol));
			}
			else {
				myAudioSource.PlayOneShot(respawnSound, respawnSoundVol);
			}
		}

		// Instantiate spawn orbs
		// (They'll take care of themselves)
		if (respawnOrb) {
			if (respawnOrbDelay > 0.0f) {
				StartCoroutine(RespawnOrbsDelayed(respawnOrbDelay));
			}
			else {
				RespawnOrbs();
			}
		}
	}

	void RespawnOrbs () {
		// Left
		Instantiate(respawnOrb, new Vector3(-maxDisplacement, spawnPos.y, 0f), Quaternion.Euler(-90, 0, 0));
		// Right
		Instantiate(respawnOrb, new Vector3(maxDisplacement, spawnPos.y, 0f), Quaternion.Euler(-90, 0, 0));
		// Top
		Instantiate(respawnOrb, new Vector3(0f, spawnPos.y, maxDisplacement), Quaternion.Euler(-90, 0, 0));
		// Bottom
		Instantiate(respawnOrb, new Vector3(0f, spawnPos.y, -maxDisplacement), Quaternion.Euler(-90, 0, 0));
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

	void GoTerminal () {
		isTerminal = true;
		terminalColorIndex = 1;
	}

	void CancelTerminal () {
		isTerminal = false;
		terminalColorIndex = 0;
	}

	void ResetPulseColor () {
		terminalColorIndex = (isTerminal) ? 1 : 0;
		if ((level > 0) || (totalDeaths > 0)) {
			currentPulseColor = currentPhase.pulseColor;
		}
		else {
			currentPulseColor = playerPulseColor;
		}
	}

	Color TerminalColor () {
		// Guard against too-short color list
		if (terminalColorIndex > terminalColors.Length) {
			terminalColorIndex = 1;
		}
		// Get current color
		if (terminalColorIndex > 0) {
			return terminalColors[terminalColorIndex - 1];
		}
		else {
			return currentPhase.pulseColor;
		}
	}

	void NextTerminalColor () {
		int newIndex = terminalColorIndex;

		// Set next color
		if (randomTerminalColor) {
			while (newIndex == terminalColorIndex) {
				newIndex = Random.Range(1, terminalColors.Length + 1);
			}
		}
		else {
			newIndex++;
			if (newIndex > terminalColors.Length) {
				newIndex = 1;
			}
		}
		/*
		if (globalDebug) {
			Debug.Log("Terminal color was " + terminalColorIndex.ToString() + ", now " + newIndex.ToString(), gameObject);
		}
		*/
		terminalColorIndex = newIndex;
	}

	void StartGame () {
		// Mode debug
		if (globalDebug) {
			Debug.Log("Loading mode: " + GameSettings.CurrentMode.Name, gameObject);
		}

		// Get high scores from current mode
		maxKills = GameSettings.CurrentMode.GetScore("Kills");
		highKills.text = "Best: " + maxKills.ToString();
		maxLevel = GameSettings.CurrentMode.GetScore("Waves");
		highLevel.text = "Best: " + maxLevel.ToString();

		// Start first enemy phase
		phaseIndex = 0;
		phaseShift = -1;
		currentPhase = Instantiate(GameSettings.CurrentMode.GetPhase(phaseIndex)).GetComponent<EnemyPhase>();

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

		// Start music, if present
		GameObject[] musicbox = GameObject.FindGameObjectsWithTag("Musicbox");
		if (musicbox.Length > 0) {
			musicbox[0].SendMessage("GameStarted", null, SendMessageOptions.DontRequireReceiver);
		}

		isStarted = true;

		if (respawnCountdown > 0.0f) {
			RespawnEffects();
		}
	}

	IEnumerator PlayDelayedClip (AudioClip toPlay, float delay, float volume) {
		yield return new WaitForSeconds(delay);
		myAudioSource.PlayOneShot(toPlay, volume);
	}

	IEnumerator RespawnOrbsDelayed (float delay) {
		yield return new WaitForSeconds(delay);
		RespawnOrbs();
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
