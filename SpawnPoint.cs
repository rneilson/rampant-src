using UnityEngine;
using System.Collections;

public class SpawnPoint : MonoBehaviour {
	
	public GameObject enemyType;
	public float preTime;
	public float postTime;
	
	private float countdown;
	private bool started;
	private bool particled;
	private bool spawned;
	private float phaseIndex;
	private Scorer scorer;
	private ParticleSystem particle;

	void Awake () {
		particle = GetComponent<ParticleSystem>();
		started = false;
		particled = false;
		spawned = false;
	}

	// Use this for initialization
	void Start () {}
	
	// Update is called once per frame
	void Update () {
		if ((!spawned) && (started)) {
			// Hit countdown
			if (countdown <= 0.0f) {
				// Already did particle effect, spawn
				if (particled) {
					GetComponent<Collider>().enabled = false;
					float spawnZ = Random.Range(0f, (phaseIndex * 5f));
					float spawnY = Random.Range(0f, 360f);
					GameObject spawn = Instantiate(enemyType, transform.position, Quaternion.Euler(spawnZ, 0, spawnY)) as GameObject;
					if (scorer) {
						spawn.SendMessage("FindControl", scorer.gameObject);
					}
					if (postTime > 0)
						Destroy(gameObject, postTime);
					else
						Destroy(gameObject);
					spawned = true;
				}
				// Start particle effect
				else {
					particle.Play();
					particled = true;
					countdown += preTime;
				}
			}
			// We're kind of assuming that we've been started the frame before
			countdown -= Time.deltaTime;
		}
	}
	
	void FixedUpdate () {}

	public void SetPhaseIndex (int phase) {
		phaseIndex = phase;
	}
	
	public void FindControl (GameObject control) {
		scorer = control.GetComponent<Scorer>();
	}

	public void StartCountdown (float delay) {
		countdown = delay;
		started = true;
	}
}
