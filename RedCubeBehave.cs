using UnityEngine;
using System.Collections;

public class RedCubeBehave : MonoBehaviour {
	
	private GameObject target;
	private Scorer scorer;
	private RedCubeGroundControl controller;
	//private float speed = 10f;
	//private float drag = 4f;
	private Vector3 bearing;
	private DeathType dying = DeathType.None;

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

		if (!scorer) {
			FindControl(GameObject.FindGameObjectWithTag("GameController"));
		}
		myRigidbody.drag = drag;
	}
	
	// Update is called once per frame
	void Update () {
		if (dying != DeathType.None)
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
		if (dying == DeathType.Loudly) {
			Destroy(Instantiate(burster, transform.position, Quaternion.Euler(0, 0, 0)), 0.5f);
		}
		else {
			Destroy(Instantiate(bursterQuiet, transform.position, Quaternion.Euler(0, 0, 0)), 0.5f);
		}
		if (deathFade) {
			Destroy(Instantiate(deathFade, transform.position, Quaternion.identity), 1.0f);
		}
		if (dying != DeathType.Silently) {
			scorer.AddKill();
		}
		Destroy(gameObject);
	}
	
	void Clear () {
		dying = DeathType.Silently;
	}
	
	void Die (bool loudly) {
		if (dying == DeathType.None) {
			dying = (loudly) ? DeathType.Loudly : DeathType.Quietly;
		}
		//collider.enabled = false;
	}
	
	void ClearTarget () {
		target  = null;
	}
	
	void NewTarget (GameObject newTarget) {
		target = newTarget;
	}
	
	void FindControl (GameObject control) {
		scorer = control.GetComponent<Scorer>();
		controller = control.GetComponent<RedCubeGroundControl>();
		NewTarget(scorer.Player);
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
