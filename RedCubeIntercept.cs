using UnityEngine;
using System.Collections;

public class RedCubeIntercept : MonoBehaviour {
	
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
	
	// Player tracking
	private Vector3 prevPos = new Vector3(0f, 0f, 0f);
	private Vector3 currPos = new Vector3(0f, 0f, 0f);
	private Vector3 prevVel = new Vector3(0f, 0f, 0f);
	private Vector3 currVel = new Vector3(0f, 0f, 0f);
	private Vector3 prevAccel = new Vector3(0f, 0f, 0f);
	private Vector3 currAccel = new Vector3(0f, 0f, 0f);
	private float deltaTime;

	public float speed;
	public float drag;
	public GameObject burster;
	public GameObject bursterQuiet;

	// Use this for initialization
	void Start () {
		// Unity 5 API changes
		//myAudioSource = GetComponent<AudioSource>();
		myRigidbody = GetComponent<Rigidbody>();

		NewTarget(GameObject.FindGameObjectWithTag("Player"));
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
			UpdateTracking(target.transform.position, Time.fixedDeltaTime);
			//bearing = target.transform.position - transform.position;
			myRigidbody.AddForce(bearing.normalized * speed);
		}
		else {
			// Try and acquire new target
			NewTarget(GameObject.FindGameObjectWithTag("Player"));
		}
	}
	
	void BlowUp () {
		if (loud) {
			Destroy(Instantiate(burster, transform.position, Quaternion.Euler(0, 0, 0)), 0.5f);
		}
		else {
			Destroy(Instantiate(bursterQuiet, transform.position, Quaternion.Euler(0, 0, 0)), 0.5f);
		}
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
		// If new target acquired, reset tracking
		if (target) {
			currPos = target.transform.position;
			Vector3 currVel = new Vector3(0f, 0f, 0f);
			Vector3 currAccel = new Vector3(0f, 0f, 0f);
		}
	}

	void UpdateTracking(Vector3 pos, float dT) {
		// Shift old current values to new previous (you know what I mean!)
		prevPos = currPos;
		prevVel = currVel;
		prevAccel = currAccel;

		// Set new current values
		deltaTime = dT;
		currPos = pos;
		currVel = (currPos - prevPos) / deltaTime;
		currAccel = (currVel - prevVel) / deltaTime;
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
