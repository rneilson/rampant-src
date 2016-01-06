using UnityEngine;
using System.Collections;

public class Scorer : MonoBehaviour {
	
	private TextMesh scoreKills;
	private TextMesh scoreLevel;
	private TextMesh titleText;
	private TextMesh subtitleText;
	private Component[] spawners;
	private float respawnCountdown;
	private bool respawn;
	private GameObject playerCurrent;
	private bool isPaused = false;
	private int totalSpawned;

	// Unity 5 API changes
	private AudioSource myAudioSource;
	
	public int kills;
	public int level;
	public int maxKills;
	public int totalDeaths;

	public GameObject playerType;
	public GameObject spawnEffect;
	public AudioClip spawnSound;

	// Use this for initialization
	void Start () {
		scoreKills = GameObject.Find("Display-kills").GetComponent<TextMesh>();
		scoreLevel = GameObject.Find("Display-level").GetComponent<TextMesh>();
		titleText = GameObject.Find("TitleText").GetComponent<TextMesh>();
		subtitleText = GameObject.Find("SubtitleText").GetComponent<TextMesh>();
		spawners = gameObject.GetComponents(typeof(Spawner));
		kills = 0;
		level = 0;
		maxKills = 0;
		totalDeaths = 0;
		respawn = true;
		respawnCountdown = 2;
		totalSpawned = 0;
		subtitleText.text = "Move: left stick/WASD keys\nShoot: right stick/arrow keys\nPause: start button/escape key";
		myAudioSource = GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	void Update () {
		scoreKills.text = "Kills: " + kills.ToString();
		scoreLevel.text = "Wave: " + level.ToString();
		
		// if (Input.GetKeyDown(KeyCode.Escape)) {
		if (Input.GetButtonDown("Pause")) {
			if (isPaused) {
				isPaused = false;
				Time.timeScale = 1;
				titleText.text = "";
				subtitleText.text = "";
			}
			else {
				isPaused = true;
				Time.timeScale = 0;
				titleText.text = "Paused";
				subtitleText.text = "Q to quit";
			}
		}
		
		if (!isPaused) {
			if (respawn == true) {
				respawnCountdown -= Time.deltaTime;
			}
			if (respawnCountdown <= 0f) {
				respawnCountdown = 1f;
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
		kills++;
		//Debug.Log("Kills: " + kills.ToString() + ", spawned: " + totalSpawned.ToString());
		if ((kills % 50) == 0) {
			if (playerCurrent != null)
				playerCurrent.SendMessage("FireFaster");
		}
	}
	
	public void AddKills (int killed) {
		kills += killed;
		//Debug.Log("Kills: " + kills.ToString() + ", killed: " + killed.ToString() + ", spawned: " + totalSpawned.ToString());
	}
	
	public void AddLevel () {
		level++;
	}
	
	public void AddSpawns (int spawns) {
		totalSpawned += spawns;
	}
	
	void PlayerDied () {
		respawn = true;
		totalDeaths++;
		if (kills > maxKills)
			maxKills = kills;
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
		
		// Spawn player
		kills = 0;
		playerCurrent = (GameObject) Instantiate(playerType, spawnPos, Quaternion.Euler(0, 0, 0));
		for (int i =  0; i<spawners.Length; i++) {
			spawners[i].SendMessage("NewTargets");
		}
	}
		
}
