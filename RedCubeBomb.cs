using UnityEngine;
using System.Collections;

public class RedCubeBomb : MonoBehaviour {
	
	private GameObject target;
	private Scorer scorer;
	//private float speed = 10f;
	//private float drag = 4f;
	private Vector3 bearing;
	private bool dead;
	private bool loud;
	private bool armed;

	// Unity 5 API changes
	//private AudioSource myAudioSource;
	private Rigidbody myRigidbody;
	

	public float speed;
	public float drag;
	public GameObject burster;
	public GameObject bursterQuiet;
	public float bombHeight = 0.51f;
	public float bombForce = 300f;
	public float bombKillRadius = 1.0f;
	public float bombPushRadius = 2.5f;
	public float bombTriggerRadius = 0.75f;
	public GameObject bombEffect;
	public bool enableBombPush = false;

	// Use this for initialization
	void Start () {
		// Unity 5 API changes
		//myAudioSource = GetComponent<AudioSource>();
		myRigidbody = GetComponent<Rigidbody>();

		target = GameObject.FindGameObjectWithTag("Player");
		scorer = GameObject.FindGameObjectWithTag("GameController").GetComponent<Scorer>();
		myRigidbody.drag = drag;
		dead = false;
		loud = false;
		armed = true;
	}
	
	// Update is called once per frame
	void Update () {
		if (dead)
			BlowUp();
	}
	
	//Put movement in FixedUpdate
	void FixedUpdate () {
		if (target) {
			bearing = target.transform.position - transform.position;
			if (bearing.magnitude <= bombTriggerRadius) {
				Die(true);
			}
			else {
				myRigidbody.AddForce(bearing.normalized * speed);
			}
		}
	}
	
	void BlowUp () {
		if (loud) {
			Destroy(Instantiate(burster, transform.position, Quaternion.Euler(0, 0, 0)), 0.5f);
		}
		else {
			Destroy(Instantiate(bursterQuiet, transform.position, Quaternion.Euler(0, 0, 0)), 0.5f);
		}

		// Drop da bomb
		if (armed) {
			DropBomb(transform.position);
		}

		scorer.AddKill();
		Destroy(gameObject);
	}
	
	void Clear () {
		Destroy(Instantiate(bursterQuiet, transform.position, Quaternion.Euler(0, 0, 0)), 1);
		Destroy(gameObject);
	}
	
	void Die (bool loudly) {
		if (!dead) {
			dead = true;
			loud = loudly;
		}
		//collider.enabled = false;
	}
	
	void ClearTarget () {
		target  = null;
	}
	
	void NewTarget (GameObject newTarget) {
		target = newTarget;
	}

	void DropBomb (Vector3 pos) {
		Vector3 bombPos = new Vector3 (pos.x, bombHeight, pos.z);
		GameObject daBomb;

		// Spawn effect
		// At player position because it looks better
		daBomb = Instantiate(bombEffect, pos, Quaternion.identity) as GameObject;
		Destroy(daBomb, 1.0f);
		// Turn down flash if dying quietly
		if (!loud) {
			daBomb.GetComponent<LightPulse>().ChangeTargetRelative(-1.5f);
		}
		
		// New bomb radius code
		float thingSize = 0.2f;
		float thingDist = 0.0f;

		// We're dropping, make sure we're now disarmed
		armed = false;

		// Find list of enemies (probably faster than an OverlapSphere (or two))
		GameObject[] things = GameObject.FindGameObjectsWithTag("Enemy");
		// Iterate over and check distances to everyone, killing or pushing as required
		for (int i = 0; i < things.Length; i++) {
			thingDist = (things[i].transform.position - transform.position).magnitude;
			if ((thingDist <= (bombKillRadius + thingSize)) && (things[i] != gameObject)) {
				things[i].SendMessage("Die", false);
			}
			// Only push if dying loudly (shot or self-triggering)
			else if ((enableBombPush) && (loud) && (thingDist <= (bombPushRadius + thingSize))) {
				things[i].GetComponent<Rigidbody>().AddExplosionForce(bombForce, bombPos, 0f);
			}
		}
		// Now check player distance and kill/push (only if loud)
		if ((loud) && (target)) {
			thingDist = (target.transform.position - transform.position).magnitude;
			if ((thingDist <= (bombKillRadius + thingSize))) {
				target.SendMessage("Die", false);
			}
			else if ((enableBombPush) && (thingDist <= (bombPushRadius + thingSize))) {
				target.GetComponent<Rigidbody>().AddExplosionForce(bombForce, bombPos, 0f);
			}
		}
			
		/* We're gonna do this differently -- physics was bogging down
		int killmask, pushmask;
		Collider[] things;
		// If we're shooting the bomber or self-triggering, can kill player
		if (loud) {
			killmask = (1 << LayerMask.NameToLayer("Enemy")) | (1 << LayerMask.NameToLayer("Default"));
		}
		else {
			killmask = (1 << LayerMask.NameToLayer("Enemy"));
		}
		
		// Kill things in inner radius
		things = Physics.OverlapSphere(bombPos, bombKillRadius, killmask);
		if (things.Length > 0) {
			for (int i=0; i<things.Length; i++) {
				things[i].SendMessage("Die", false);
			}
		}
		
		// Only push things if we're dying loudly (and don't push player)
		if (loud) {
			pushmask = (1 << LayerMask.NameToLayer("Enemy"));
			// Push things in outer radius
			things = Physics.OverlapSphere(bombPos, bombPushRadius, pushmask);
			for (int i=0; i<things.Length; i++) {
				things[i].GetComponent<Rigidbody>().AddExplosionForce(bombForce, bombPos, 0f);
			}
		}
		*/

	}
	
	// On collision
	/* void OnCollisionEnter(Collision collision) {
		GameObject thingHit = collision.gameObject;
		
		if (thingHit.tag == "Bullet" && dead == false) {
			dead = true;
			BlowUp(true);
		}
	} */
}
