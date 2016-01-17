using UnityEngine;
using System.Collections;

public class RedCubeIntercept : MonoBehaviour {
	
	private GameObject target;
	private Scorer scorer;
	//private float speed = 10f;
	//private float drag = 4f;
	//private float deltaTime;
	private bool dead = false;
	private bool loud = false;
	public Vector3 bearing = Vector3.zero;	// Public for debug
	public Vector3 closing = Vector3.zero;	// Public for debug
	public float timeToIntercept = 0.0f;	// Public for debug
	private RedCubeGroundControl controller;
	private bool debugInfo;

	// Unity 5 API changes
	//private AudioSource myAudioSource;
	private Rigidbody myRigidbody;
	
	public float speed;
	public float drag;
	public GameObject burster;
	public GameObject bursterQuiet;

	// Use this for initialization
	void Start () {
		// Unity 5 API changes
		//myAudioSource = GetComponent<AudioSource>();
		myRigidbody = GetComponent<Rigidbody>();
		myRigidbody.drag = drag;

		GameObject gamecontrol = GameObject.FindGameObjectWithTag("GameController");
		scorer = gamecontrol.GetComponent<Scorer>();
		controller = gamecontrol.GetComponent<RedCubeGroundControl>();
		debugInfo = controller.DebugInfo;

		NewTarget(GameObject.FindGameObjectWithTag("Player"));
	}
	
	// Update is called once per frame
	void Update () {
		if (dead) {
			BlowUp();
		}
		else if (debugInfo) {
			Debug.DrawLine(transform.position, closing + transform.position, Color.green);
			Debug.DrawLine(closing + transform.position, bearing + transform.position, Color.yellow);
		}
	}
	
	//Put movement in FixedUpdate
	void FixedUpdate () {
		if (target) {
			UpdateTracking();
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
	}

	void UpdateTracking() {
		Vector3 bearingOption = Vector3.zero;
		Vector3 closingOption = Vector3.zero;
		float closingDiff = 0.0f;
		float frameSpeed = controller.MaxInterceptorFrameSpeed;
		float avgDT = controller.AvgDeltaTime;

		// First, let's update the controller's recorded max speed just in case
		if (myRigidbody.velocity.magnitude > controller.MaxInterceptorSpeed) {
			controller.MaxInterceptorSpeed = myRigidbody.velocity.magnitude;
			frameSpeed = controller.MaxInterceptorFrameSpeed;
		}

		// Next we'll default to heading straight for the target's position
		bearing = controller.Prediction(0) - transform.position;
		closing = Vector3.zero;
		closingDiff = bearing.magnitude;
		timeToIntercept = 0.0f;

		// Then, we evaluate all predicted target positions and pick the one that gets us closest
		for (int i = 1;	i < controller.PredictionLength; i++) {
			// Find vector to target position
			bearingOption = controller.Prediction(i) - transform.position;
			// Find how close we'll get to that in the given time (well, number of (fixed) frames)
			closingOption = (bearingOption.normalized * frameSpeed * (float) i);
			// Now compare with current closest option
			if ((bearingOption - closingOption).magnitude < closingDiff) {
				bearing = bearingOption;
				closing = closingOption;
				closingDiff = (bearingOption - closingOption).magnitude;
				timeToIntercept = (float) i * avgDT;
			}
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
