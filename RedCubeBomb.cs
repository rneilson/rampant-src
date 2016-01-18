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
		DropBomb(transform.position);

		scorer.AddKill();
		Destroy(gameObject);
	}
	
	void Clear () {
		Destroy(Instantiate(bursterQuiet, transform.position, Quaternion.Euler(0, 0, 0)), 1);
		Destroy(gameObject);
	}
	
	void Die (bool loudly) {
		if (!dead)
			dead = true;
		if (loudly)
			loud = true;
		else
			loud = false;
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
		int killmask, pushmask;
		Collider[] things;

		// Spawn effect
		// At player position because it looks better
		Destroy(Instantiate(bombEffect, pos, Quaternion.identity), 1.0f);
		
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
		
		// If we're shooting the bomber or self-triggering, don't push player
		if (loud) {
			pushmask = (1 << LayerMask.NameToLayer("Enemy"));
		}
		else {
			pushmask = (1 << LayerMask.NameToLayer("Enemy")) | (1 << LayerMask.NameToLayer("Default"));
		}
		// Push things in outer radius
		things = Physics.OverlapSphere(bombPos, bombPushRadius, pushmask);
		for (int i=0; i<things.Length; i++) {
			things[i].GetComponent<Rigidbody>().AddExplosionForce(bombForce, bombPos, 0f);
		}
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
