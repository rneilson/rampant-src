using UnityEngine;
using System.Collections;

public class RedCubeIntercept : MonoBehaviour {
	
	private GameObject target;
	private Scorer scorer;
	public float deltaTime;	// Public for debug
	private DeathType dying = DeathType.None;
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

	private const bool spin = true;
	private Vector3 spinAxis = Vector3.forward;
	private Vector3 spinRef = Vector3.forward;
	private float torque = 0.015f;

	// Unity 5 API changes
	//private AudioSource myAudioSource;
	private Rigidbody myRigidbody;
	
	public float speed;
	public float drag;
	public GameObject burster;
	public GameObject bursterQuiet;
	public GameObject deathFade;

	// Prefab detach & delay-kill
	//public int numChildren;
	//public GameObject[] allChildren;

	// Use this for initialization
	void Start () {
		// Unity 5 API changes
		//myAudioSource = GetComponent<AudioSource>();
		myRigidbody = GetComponent<Rigidbody>();
		myRigidbody.drag = drag;

		if (!scorer) {
			FindControl(GameObject.FindGameObjectWithTag("GameController"));
			if (scorer.GlobalDebug) {
				Debug.Log("Scorer not passed on spawn!", gameObject);
			}
		}
		debugInfo = controller.DebugInfo;
		currPos = transform.position;

		/*
		// Some debug
		if (transform.childCount > 0) {
			numChildren = transform.childCount;
			allChildren = new GameObject[numChildren];
			for (int i = 0; i < numChildren; i++) {
				allChildren[i] = transform.GetChild(i).gameObject;
			}
		}
		*/
	}
	
	// Update is called once per frame
	void Update () {
		if (dying != DeathType.None) {
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
			// Spin menacingly (if spinning enabled)
			if (spin) {
				myRigidbody.AddTorque(SpinVector(bearing) * torque);
			}
		}
		else {
			// Try and acquire new target
			NewTarget(scorer.Player);
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
		KillRelatives(1.0f);
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

	void KillRelatives (float delay) {
		// Detach from parent and/or children and destroy them after a delay
		GameObject tmp;

		// Parent first
		if (transform.parent) {
			tmp = transform.parent.gameObject;
			Destroy(tmp, delay);
		}

		// Next the kids, if any
		for (int i = transform.childCount - 1; i >= 0 ; i--) {
			tmp = transform.GetChild(i).gameObject;
			tmp.transform.parent = null;
			Destroy(tmp, delay);
		}

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

	Vector3 SpinVector (Vector3 bearingVec) {
		Quaternion rot = Quaternion.FromToRotation(spinRef, bearingVec);
		return rot * spinAxis;
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
