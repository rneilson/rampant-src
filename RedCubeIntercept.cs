using UnityEngine;
using System.Collections;

public class RedCubeIntercept : MonoBehaviour {
	
	private GameObject target;
	private Scorer scorer;
	private float deltaTime;
	private DeathType dying = DeathType.None;
	private Vector3 bearing = Vector3.zero;
	private Vector3 closing = Vector3.zero;
	private Vector3 prevPos = Vector3.zero;
	private Vector3 currPos = Vector3.zero;
	private float currSpeed = 0.0f;
	private float avgSpeed = 0.0f;
	private RedCubeGroundControl control;
	private bool debugInfo;
	private float avgWeight = 0.82f;
	private float currWeight = 0.18f;

	private const bool spin = true;
	private Vector3 spinRef = Vector3.forward;
	public Vector3 spinAxis = Vector3.forward;
	public float torque = 0.015f;

	// Unity 5 API changes
	//private AudioSource myAudioSource;
	private Rigidbody myRigidbody;
	
	// Type/instance management stuff
	public string thisTypeName = "Interceptor";
	private EnemyType thisType;
	private EnemyInst thisInst;

	// Public parameters
	public float speed;
	public float drag;
	public GameObject burster;
	public GameObject bursterQuiet;
	public GameObject deathFade;
	public float shrapnelForce = 50f;
	public float shrapnelLifetime = 0.5f;
	public GameObject shrapnelSparker;
	public bool scatterChildren = true;

	// Bomb parameters
	public float bombHeight = 0.51f;
	public float bombForce = 50f;
	public float bombPushRadius = 0.5f;
	public GameObject bombEffect;
	private bool armed;

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
		debugInfo = control.DebugInfo;
		currPos = transform.position;

		// Add to control's list
		thisType = EnemyList.AddOrGetType(thisTypeName);
		thisInst = new EnemyInst(thisType.typeNum, gameObject);
		control.AddInstanceToList(thisInst);
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
		armed = true;
	}
	
	// Update is called once per frame
	void Update () {
		if (dying != DeathType.None) {
			BlowUp();
		}
		else if ((debugInfo) || (scorer.GlobalDebug)) {
			Debug.DrawLine(transform.position, closing + transform.position, Color.magenta);
			Debug.DrawLine(closing + transform.position, bearing + transform.position, Color.blue);
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

		// Average speed
		avgSpeed = avgSpeed * avgWeight + currSpeed * currWeight;

		if (target) {
			UpdateTracking();
			//bearing = target.transform.position - transform.position;
			myRigidbody.AddForce(bearing.normalized * speed);
			// Spin menacingly (if spinning enabled)
		}
		else {
			// Try and acquire new target
			NewTarget(scorer.Player);
		}

		if (spin) {
			myRigidbody.AddTorque(SpinVector(bearing) * torque);
		}
	}
	
	void BlowUp () {
		if (dying == DeathType.Loudly) {
			Destroy(Instantiate(burster, transform.position, Quaternion.Euler(-90, 0, 0)), 0.5f);
		}
		else {
			Destroy(Instantiate(bursterQuiet, transform.position, Quaternion.Euler(-90, 0, 0)), 0.5f);
		}
		if (deathFade) {
			Destroy(Instantiate(deathFade, transform.position, Quaternion.identity), 1.0f);
		}
		if (dying != DeathType.Silently) {
			scorer.AddKill();
		}
		KillRelatives(1.0f);

		// Drop da bomb
		if (armed) {
			// Pick a slight offset for bomb force position
			Vector3 deathPos = new Vector3(transform.position.x, bombHeight, transform.position.z);
			DropBomb(deathPos);
		}

		// Remove from control's list
		control.RemoveInstanceFromList(thisInst);

		// Destroy ourselves
		Destroy(gameObject);
	}
	
	void Clear () {
		dying = DeathType.Silently;
		BlowUp();
	}
	
	void Die (bool loudly) {
		if (dying == DeathType.None) {
			dying = (loudly) ? DeathType.Loudly : DeathType.Quietly;
		}
	}

	void KillRelatives (float delay) {
		// Detach from parent and/or children and destroy them after a delay
		GameObject tmp;
		int numChildren = transform.childCount;

		// Parent first
		if (transform.parent) {
			tmp = transform.parent.gameObject;
			Destroy(tmp, delay);
		}

		// Next the kids, if any
		for (int i = transform.childCount - 1; i >= 0 ; i--) {
			tmp = transform.GetChild(i).gameObject;


			tmp.transform.parent = null;

			// Add rigidbody and setup
			var rb = tmp.AddComponent<Rigidbody>();

			if (scatterChildren) {
				// Get relative velocity
				Vector3 relativeVel = myRigidbody.GetRelativePointVelocity(tmp.transform.position - transform.position);
				// Pick a slight offset for death force position
				float off = 0.02f;
				Vector3 deathPos = transform.position + new Vector3(Random.Range(-off, off), -2.0f * off, Random.Range(-off, off));
				// Setup child rigidbody and apply scatter force
				SetupChildRigid(myRigidbody, rb, numChildren + 1, relativeVel);
				rb.AddExplosionForce(shrapnelForce, deathPos, 0f);
			}
			else {
				// Just give the child a rigidbody and zero velocity
				SetupChildRigid(myRigidbody, rb, numChildren + 1, Vector3.zero);
			}

			// Enable renderer
			var rend = tmp.GetComponent<Renderer>();
			rend.enabled = true;

			// "...but then again, who does?"
			tmp.GetComponent<DelayedDeath>().DieInTime(Random.Range(0.75f, 1.25f) * shrapnelLifetime, shrapnelSparker);
		}

	}
	
	void SetupChildRigid (Rigidbody source, Rigidbody dest, int portion, Vector3 newVelocity) {
		dest.mass = source.mass / (float) portion;
		dest.useGravity = false;
		dest.velocity = newVelocity;
	}
	
	void ClearTarget () {
		target  = null;
	}
	
	void NewTarget (GameObject newTarget) {
		target = newTarget;
	}

	void FindControl (GameObject controller) {
		scorer = controller.GetComponent<Scorer>();
		control = controller.GetComponent<RedCubeGroundControl>();
		NewTarget(scorer.Player);
	}

	void UpdateTracking() {
		Vector3 bearingOption = Vector3.zero;
		Vector3 closingOption = Vector3.zero;
		float closingDiff = 0.0f;
		float diffOption = 0.0f;
		float avgDeltaTime = control.AvgDeltaTime;	// Public for debug
		float frameSpeed = avgSpeed * avgDeltaTime;

		// Next we'll default to heading straight for the target's position
		bearing = control.Prediction(0) - transform.position;
		closing = Vector3.zero;
		closingDiff = bearing.magnitude;

		// Then, we evaluate all predicted target positions and pick the one that gets us closest
		for (int i = 1; i < control.PredictionLength; i++) {
			// Exclude positions out of bounds
			if (InBounds(control.Prediction(i))) {
				// Find vector to target position
				bearingOption = control.Prediction(i) - transform.position;
				// Find how far we'll get towards that in the given time (well, number of (fixed) frames)
				closingOption = (bearingOption.normalized * frameSpeed * (float) i);
				// Check the distance between them
				diffOption = (bearingOption - closingOption).magnitude;
				// Now compare with current closest option
				if (diffOption < closingDiff) {
					bearing = bearingOption;
					closing = closingOption;
					closingDiff = diffOption;
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
	
	void DropBomb (Vector3 bombPos) {
		GameObject daBomb;

		// Spawn effect
		// At player position because it looks better
		daBomb = Instantiate(bombEffect, transform.position, Quaternion.Euler(-90, 0, 0)) as GameObject;
		Destroy(daBomb, 1.0f);

		// Turn down flash if dying quietly
		if (dying != DeathType.Loudly) {
			var lp = daBomb.GetComponent<LightPulse>();
			if (lp) {
				lp.ChangeTargetRelative(-1.2f);
			}
		}
		// Turn down volume if dying quietly
		if (dying == DeathType.Quietly) {
			var audio = daBomb.GetComponent<AudioSource>();
			if (audio) {
				audio.volume *= 0.15f;
			}
		}
		// Mute if dying silently
		if (dying == DeathType.Silently) {
			var audio = daBomb.GetComponent<AudioSource>();
			if (audio) {
				audio.volume *= 0.0f;
			}
		}
		
		// We're dropping, make sure we're now disarmed
		armed = false;

		// Only push things if we're dying loudly (do push player)
		if (dying == DeathType.Loudly) {
			int pushmask = (1 << LayerMask.NameToLayer("Enemy")) | (1 << LayerMask.NameToLayer("Player"));
			// Push things in outer radius
			Collider[] things = Physics.OverlapSphere(bombPos, bombPushRadius, pushmask);
			for (int i=0; i<things.Length; i++) {
				things[i].GetComponent<Rigidbody>().AddExplosionForce(bombForce, bombPos, 0f);
			}
		}

	}

}
