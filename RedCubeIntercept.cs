using UnityEngine;
using System.Collections;

public class RedCubeIntercept : MonoBehaviour {
	
	private GameObject target;
	private Scorer scorer;
	public float deltaTime;	// Public for debug
	private bool dead = false;
	private bool loud = false;
	public Vector3 bearing = Vector3.zero;	// Public for debug
	public Vector3 closing = Vector3.zero;	// Public for debug
	public float timeToIntercept = 0.0f;	// Public for debug
	public Vector3 prevPos = Vector3.zero;	// Public for debug
	public Vector3 currPos = Vector3.zero;	// Public for debug
	public float currSpeed = 0.0f;	// Public for debug
	//public float currSpeedFrame = 0.0f;	// Public for debug
	public float avgSpeed = 0.0f;	// Public for debug
	private RedCubeGroundControl controller;
	private bool debugInfo;
	private float avgWeight = 0.82f;
	private float currWeight = 0.18f;

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
		currPos = transform.position;

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
		// Update dT, position, and velocity info
		deltaTime = Time.fixedDeltaTime;
		prevPos = currPos;
		currPos = transform.position;
		Vector3 currVel = (currPos - prevPos) / deltaTime;
		currSpeed = currVel.magnitude;
		//currSpeedFrame = currSpeed * deltaTime;

		// Average speed
		avgSpeed = avgSpeed * avgWeight + currSpeed * currWeight;

		/*
		// Update controller's max speed if req'd
		if (currSpeed > controller.MaxInterceptorSpeed) {
			if (debugInfo) {
				Debug.Log("Updating max speed, was " + controller.MaxInterceptorSpeed.ToString() + ", now "
					+ currSpeed.ToString(), gameObject);
				Debug.Log("Previous position: " + prevPos.ToString() 
					+ ", current position: " + currPos.ToString(), gameObject);
			}
			controller.MaxInterceptorSpeed = currSpeed;
		}
		*/

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

		// Detach and kill children (delayed 1.5s)
		GameObject childtmp;
		for (int i = 0; i < transform.childCount; i++) {
			childtmp = transform.GetChild(i).gameObject;
			childtmp.transform.parent = null;
			Destroy(childtmp, 1.0f);
		}
		Destroy(gameObject);
	}
	
	void Clear () {
		Destroy(Instantiate(bursterQuiet, transform.position, Quaternion.Euler(0, 0, 0)), 1);

		// Detach and kill children (delayed 1.5s)
		GameObject childtmp;
		for (int i = 0; i < transform.childCount; i++) {
			childtmp = transform.GetChild(i).gameObject;
			childtmp.transform.parent = null;
			Destroy(childtmp, 1.0f);
		}
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
		float diffOption = 0.0f;
		float avgDeltaTime = controller.AvgDeltaTime;	// Public for debug
		float frameSpeed = avgSpeed * avgDeltaTime;

		// Next we'll default to heading straight for the target's position
		bearing = controller.Prediction(0) - transform.position;
		closing = Vector3.zero;
		closingDiff = bearing.magnitude;
		timeToIntercept = 0.0f;

		// Then, we evaluate all predicted target positions and pick the one that gets us closest
		for (int i = 1; i < controller.PredictionLength; i++) {
			// Exclude positions out of bounds
			if (InBounds(controller.Prediction(i))) {
				// Find vector to target position
				bearingOption = controller.Prediction(i) - transform.position;
				// Find how far we'll get towards that in the given time (well, number of (fixed) frames)
				closingOption = (bearingOption.normalized * frameSpeed * (float) i);
				// Check the distance between them
				diffOption = (bearingOption - closingOption).magnitude;
				// Now compare with current closest option
				if (diffOption < closingDiff) {
					bearing = bearingOption;
					closing = closingOption;
					closingDiff = diffOption;
					timeToIntercept = (float) i * avgDeltaTime;
				}
			}
		}
	}

	bool InBounds(Vector3 pos) {
		if (pos.x > scorer.MaxDisplacement) {
			return false;
		}
		else if (pos.x < -scorer.MaxDisplacement) {
			return false;
		}
		else if (pos.z > scorer.MaxDisplacement) {
			return false;
		}
		else if (pos.z < -scorer.MaxDisplacement) {
			return false;
		}
		else {
			return true;
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
