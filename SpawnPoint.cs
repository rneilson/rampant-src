using UnityEngine;
using System.Collections;

public class SpawnPoint : MonoBehaviour {
	
	public GameObject enemyType;
	public float preTime;
	public float postTime;
	public bool randomRotationY = true;
	public bool randomRotationZ = true;
	
	private float countdown;
	private bool started;
	private bool particled;
	private bool spawned;
	private float phaseIndex;
	private Scorer scorer;
	private RedCubeGroundControl control;
	private ParticleSystem particle;

	// Type/instance management stuff
	private static string thisTypeName = "Spawner";
	private static EnemyType thisType;
	private EnemyInst thisInst;

	static SpawnPoint () {
		thisType = EnemyList.AddOrGetType(thisTypeName);
	}

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

					float spawnZ = (randomRotationZ) ? Random.Range(0f, 48f + (phaseIndex * 6f)) : 0.0f;
					float spawnY = (randomRotationY) ? Random.Range(0f, 360f) : 0.0f;

					GameObject spawn = Instantiate(enemyType, 
						transform.position, Quaternion.Euler(0.0f, spawnY, spawnZ)) as GameObject;
					if (scorer) {
						spawn.SendMessage("FindControl", scorer.gameObject);
					}
					if (postTime > 0) {
						// Remove from control's list
						control.RemoveInstanceFromList(thisInst);

						Destroy(gameObject, postTime);
					}
					else {
						// Remove from control's list
						control.RemoveInstanceFromList(thisInst);

						Destroy(gameObject);
					}
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
	
	public void FindControl (GameObject controller) {
		scorer = controller.GetComponent<Scorer>();
		control = controller.GetComponent<RedCubeGroundControl>();

		// Add to control's list
		thisInst = new EnemyInst(thisType.typeNum, gameObject);
		control.AddInstanceToList(thisInst);
	}

	public void StartCountdown (float delay) {
		countdown = delay;
		started = true;
	}
}
