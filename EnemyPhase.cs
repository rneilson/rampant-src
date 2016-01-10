using UnityEngine;
using System.Collections;

public class EnemyPhase : MonoBehaviour {

	public string phaseName;
	public int maxWaves; // Set to 0 if terminal/eternal/steadystate
	public float initialDelay;
	public float waveInterval;

	private float countdown;
	private int waveNum;
	private Scorer scorer;
	private Component[] spawners;

	private bool isActive;

	// Use this for initialization
	void Start () {
		scorer = GameObject.FindGameObjectWithTag("GameController").GetComponent<Scorer>();
		spawners = gameObject.GetComponents<EnemySpawner>();
		countdown = initialDelay;
		waveNum = 0;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	//Put everything in FixedUpdate
	void FixedUpdate () {
		if (!scorer.Respawn) {
			if (countdown <= 0.0f) {
				waveNum++;
				StartWave();
				if ((maxWaves > 0) && (waveNum >= maxWaves)) {
					scorer.NextPhase();
				}
			}
			else {
				countdown -= Time.deltaTime;
			}
		}
	}

	// Begin wave
	public void StartWave () {
		scorer.AddLevel();

		foreach (EnemySpawner spawner in spawners) {
			spawner.StartWave(waveNum);
		}
	}
}
