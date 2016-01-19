using UnityEngine;
using System.Collections;

public class RedCubeBehave : MonoBehaviour {
	
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
	public GameObject deathFade;

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
			myRigidbody.AddForce(bearing.normalized * speed);
		}
	}
	
	void BlowUp () {
		if (loud) {
			Destroy(Instantiate(burster, transform.position, Quaternion.Euler(0, 0, 0)), 0.5f);
		}
		else {
			Destroy(Instantiate(bursterQuiet, transform.position, Quaternion.Euler(0, 0, 0)), 0.5f);
		}
		if (deathFade) {
			Destroy(Instantiate(deathFade, transform.position, Quaternion.identity), 1.0f);
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
