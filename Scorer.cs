using UnityEngine;
using System.Collections;

public class Scorer : MonoBehaviour {
	
	private TextMesh scoreKills;
	private TextMesh scoreHigh;
	private TextMesh scoreDeaths;
	private TextMesh scoreLevel;
	private TextMesh titleText;
	private TextMesh subtitleText;
	private Component[] spawners;
	private float respawnCountdown;
	//private bool respawn;
	private GameObject playerCurrent;
	private bool isPaused = true;
	private int totalSpawned;
	private string instructions = "Move: left stick/WASD keys\nShoot: right stick/arrow keys\nMouse shoot: left mouse button\nPause: start button/tab\nQuit: Q";

	// Unity 5 API changes
	private AudioSource myAudioSource;
	
	public int kills;
	public int level;
	public int maxKills;
	public int totalDeaths;
	public float respawnTime;
	public bool respawn;

	public GameObject playerType;
	public GameObject spawnEffect;
	public AudioClip spawnSound;
	public CameraMovement cameraFollower;

	// Use this for initialization
	void Start () {
		cameraFollower = GameObject.Find("Camera").GetComponent<CameraMovement>();
		scoreKills = GameObject.Find("Display-kills").GetComponent<TextMesh>();
		scoreHigh = GameObject.Find("Display-high").GetComponent<TextMesh>();
		scoreDeaths = GameObject.Find("Display-deaths").GetComponent<TextMesh>();
		scoreLevel = GameObject.Find("Display-level").GetComponent<TextMesh>();
		titleText = GameObject.Find("TitleText").GetComponent<TextMesh>();
		subtitleText = GameObject.Find("SubtitleText").GetComponent<TextMesh>();
		spawners = gameObject.GetComponents(typeof(Spawner));
		kills = 0;
		level = 0;
		maxKills = 0;
		totalDeaths = 0;
		respawn = true;
		respawnCountdown = 0.0f;
		totalSpawned = 0;
		titleText.text = "A Plain Shooter";
		subtitleText.text = "Press start button/tab to begin\n" + instructions;
		myAudioSource = GetComponent<AudioSource>();
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
		
		if (!isPaused) {
			if (respawn == true) {
				respawnCountdown -= Time.deltaTime;
				cameraFollower.SendMessage("RespawnCountdown", respawnCountdown);
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

			// Update high score
			if (kills > maxKills) {
				maxKills = kills;
				scoreHigh.text = "Best: " + maxKills.ToString();
			}

			// Check for weapon upgrade, or bomb, or bomb-minus warning
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
			}
		}
	}
	
	public void AddLevel () {
		level++;
		scoreLevel.text = "Wave: " + level.ToString();
	}
	
	public void AddSpawns (int spawns) {
		totalSpawned += spawns;
	}
	
	void PlayerDied () {
		respawn = true;
		totalDeaths++;
		scoreDeaths.text = "Deaths: " + totalDeaths.ToString();
		titleText.text = kills.ToString() + " kills";
		subtitleText.text = "Total deaths: " + totalDeaths.ToString() + "\nMost kills: " + maxKills.ToString();
		for (int i =  0; i<spawners.Length; i++) {
			spawners[i].SendMessage("ClearTargets");
		}
	}
	
	void spawnBomb (Vector3 pos, float killRadius, float pushRadius) {
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
		spawnBomb(spawnPos, 2.0f, 5.0f);
		
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
		
		// Spawn player, reset kills
		kills = 0;
		scoreKills.text = "Kills: " + kills.ToString();
		playerCurrent = (GameObject) Instantiate(playerType, spawnPos, Quaternion.Euler(0, 0, 0));
		cameraFollower.SendMessage("NewPlayer", playerCurrent);
		for (int i =  0; i<spawners.Length; i++) {
			spawners[i].SendMessage("NewTargets");
		}
	}

	void PauseGame () {
		isPaused = true;
		Time.timeScale = 0;
		titleText.text = "Paused";
		subtitleText.text = instructions;
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
	}
		
	void UnPauseGame () {
		isPaused = false;
		Time.timeScale = 1;
		titleText.text = "";
		subtitleText.text = "";
		Cursor.visible = false;
		//Cursor.lockState = CursorLockMode.Confined;
		Cursor.lockState = CursorLockMode.Locked;
	}

}
