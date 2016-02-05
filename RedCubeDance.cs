using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RedCubeDance : MonoBehaviour {
	
	private GameObject target;
	private Scorer scorer;
	private float deltaTime;
	private DeathType dying = DeathType.None;
	private Vector3 bearing = Vector3.zero;
	private Vector3 closing = Vector3.zero;
	private Vector3 prevPos = Vector3.zero;
	private Vector3 currPos = Vector3.zero;
	private Vector3 dodgeDir = Vector3.zero;
	private float currSpeed = 0.0f;
	private float avgSpeed = 0.0f;
	private RedCubeGroundControl control;
	private bool debugInfo;
	private float avgWeight = 0.82f;
	private float currWeight = 0.18f;
	private bool dodgedLastFrame = false;

	private const bool spin = true;
	private Vector3 spinAxis = Vector3.up;
	private Vector3 spinRef = Vector3.forward;
	private float torque = 0.25f;

	// Unity 5 API changes
	//private AudioSource myAudioSource;
	private Rigidbody myRigidbody;
	
	// Type/instance management stuff
	private const string thisTypeName = "Dancer";
	private static EnemyType thisType;
	private EnemyInst thisInst;

	// Public parameters
	public float speed;
	public float drag;
	public float dodgeForce = 20f;
	public float dodgeRadius = 1.0f;
	public DodgeMode dodgeMode = DodgeMode.Simple;
	public BearingConflict bearingConflict = BearingConflict.Add;
	public GameObject burster;
	public GameObject bursterQuiet;
	public GameObject deathFade;
	public float deathForce = 50f;
	public float shrapnelLifetime = 0.5f;
	public Material shrapnelMaterial;
	public GameObject shrapnelSparker;

	// Prefab detach & delay-kill
	//public int numChildren;
	//public GameObject[] allChildren;

	int bulletMask;

	static RedCubeDance () {
		thisType = EnemyList.AddOrGetType(thisTypeName);
	}

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

		bulletMask = 1 << LayerMask.NameToLayer("Bullet");
	}
	
	// Update is called once per frame
	void Update () {
		if (dying != DeathType.None) {
			BlowUp();
		}
		else if ((debugInfo) || (scorer.GlobalDebug)) {
			Debug.DrawLine(transform.position, closing + transform.position, Color.magenta);
			Debug.DrawLine(closing + transform.position, bearing + transform.position, Color.blue);
			Debug.DrawLine(transform.position, transform.position + dodgeDir, Color.green);
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

		// Average speed (only update if not dodging -- screws with the average)
		if (!dodgedLastFrame) {
			avgSpeed = avgSpeed * avgWeight + currSpeed * currWeight;
		}

		// Spin menacingly (if spinning enabled)
		if (spin) {
			myRigidbody.AddTorque(SpinVector(bearing) * torque);
		}

		if (target) {
			UpdateTracking();
		}
		else {
			bearing = Vector3.zero;
			// Try and acquire new target
			NewTarget(scorer.Player);
		}

		// Dodge bullets
		switch (dodgeMode) {
			case DodgeMode.Simple:
				dodgeDir = DodgeBulletsSimple(dodgeRadius);
				break;
			case DodgeMode.Scaled:
				dodgeDir = DodgeBulletsScaled(dodgeRadius);
				break;
			case DodgeMode.HalfScaled:
				dodgeDir = DodgeBulletsHalfScaled(dodgeRadius);
				break;
			case DodgeMode.ClosestSimple:
				dodgeDir = DodgeBulletsClosestSimple(dodgeRadius);
				break;
			case DodgeMode.ClosestScaled:
				dodgeDir = DodgeBulletsClosestScaled(dodgeRadius);
				break;
		}
		if (dodgeDir.magnitude > 1.0f) {
			dodgeDir = dodgeDir.normalized;
		}

		if (dodgeDir.magnitude < Mathf.Epsilon) {
			// Clear to proceed on bearing
			myRigidbody.AddForce(bearing.normalized * speed);
			dodgedLastFrame = false;
		}
		else {
			// We're dodging something
			dodgedLastFrame = true;

			// Question is how
			if (Vector3.Dot(bearing, dodgeDir) >= 0.0f) {
				// No conflict, do both
				myRigidbody.AddForce(bearing.normalized * speed);
				myRigidbody.AddForce(dodgeDir * dodgeForce);
			}
			else {
				Vector3 correction;
				// Pick a conflict resolution
				switch (bearingConflict) {
					case BearingConflict.Add:
						// Forcibly (sic) do both
						// Mostly by doing nothing to either vector
						break;
					case BearingConflict.OverrideBearing:
						// Dodging more important than chasing
						bearing = Vector3.zero;
						break;
					case BearingConflict.OrthoBearing:
						// Make bearing orthogonal to dodge vector
						// (I'd do cross products here, but I think Unity uses left-handed coordinates)
						// ((Which suck, and I refuse on principle))
						// (((I also don't want to muck about with doing everything backwards)))
						// ((((So I'll do it the hard way with dot product and correction vectors))))
						correction = dodgeDir.normalized * (Vector3.Dot(bearing, dodgeDir) / dodgeDir.magnitude);
						bearing = (bearing + correction).normalized;
						break;
					case BearingConflict.OrthoDodgevec:
						// Make dodge vector orthogonal to bearing
						// (Same disclaimer/complaint as above)
						correction = bearing.normalized * (Vector3.Dot(bearing, dodgeDir) / bearing.magnitude);
						bearing = (dodgeDir + correction).normalized;
						break;
				}

				// Now apply our forces
				if (bearing.magnitude > 0.0f) {
					myRigidbody.AddForce(bearing.normalized * speed);
				}
				if (dodgeDir.magnitude > 0.0f) {
					myRigidbody.AddForce(dodgeDir * dodgeForce);
				}
			}
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
		Rigidbody rb;
		int newLayer = LayerMask.NameToLayer("Ignore Raycast");
		int forceLayer = LayerMask.NameToLayer("Forcefield");
		int numChildren = transform.childCount;

		// Parent first
		if (transform.parent) {
			tmp = transform.parent.gameObject;
			Destroy(tmp, delay);
		}

		// Next the kids, if any
		for (int i = numChildren - 1; i >= 0 ; i--) {

			tmp = transform.GetChild(i).gameObject;

			// If it's the forcefield, just kill it
			if (tmp.layer == forceLayer) {
				tmp.transform.parent = null;
				Destroy(tmp, 0.1f);
			}
			else {
				// Get relative velocity
				Vector3 relativeVel = myRigidbody.GetRelativePointVelocity(tmp.transform.position - transform.position);
				// Pick a slight offset for death force position
				float off = 0.02f;
				Vector3 deathPos = transform.position + new Vector3(Random.Range(-off, off), 0.0f, Random.Range(-off, off));

				tmp.transform.parent = null;
				tmp.tag = "Shrapnel";
				tmp.layer = newLayer;

				// Add rigidbody and setup
				rb = tmp.AddComponent<Rigidbody>();
				SetupChildRigid(myRigidbody, rb, numChildren + 1, relativeVel);
				rb.AddExplosionForce(deathForce, deathPos, 0f);

				// Change material
				Renderer rend = tmp.GetComponent<Renderer>();
				rend.material = shrapnelMaterial;

				// "...but then again, who does?"
				tmp.GetComponent<DelayedDeath>().DieInTime(Random.Range(0.75f, 1.25f) * shrapnelLifetime, shrapnelSparker);
			}

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

	Vector3 DodgeBulletsSimple (float scanRadius) {
		Vector3 dodgeVec = Vector3.zero;
		Collider[] bullets = Physics.OverlapSphere(transform.position, scanRadius, bulletMask);

		// Check each bullet in radius, if any
		for (int i = 0; i < bullets.Length; i++) {
			// Get our position relative to the bullet
			Vector3 bulletPos = transform.position - bullets[i].transform.position;
			// Get bullet's velocity
			Vector3 bulletVel = bullets[i].GetComponent<Rigidbody>().velocity;
			// Compare our pos relative to bullet with bullet's velo -- if angle is < 90 deg,
			// we're possibly converging, and we'll dodge
			float dot = Vector3.Dot(bulletPos, bulletVel);
			if (dot > 0.0f) {
				// Get projection along bullet velo vec
				// (Algebra scribbled on paper, sorry)
				Vector3 proj = (dot / bulletVel.magnitude) * bulletVel.normalized;

				// We want the direction perpendicular to the bullet velo
				// So we take our projection, add it to the bullet's position, and sub from there
				Vector3 dir = transform.position - (bullets[i].transform.position + proj);

				// Now we add it to the (aggregate) dodge vector
				dodgeVec += dir.normalized;
			}
		}

		return dodgeVec;
	}
	
	// Same as simple, but scale the dodge by radius
	Vector3 DodgeBulletsScaled (float scanRadius) {
		Vector3 dodgeVec = Vector3.zero;
		Collider[] bullets = Physics.OverlapSphere(transform.position, scanRadius, bulletMask);

		// Check each bullet in radius, if any
		for (int i = 0; i < bullets.Length; i++) {
			// Get our position relative to the bullet
			Vector3 bulletPos = transform.position - bullets[i].transform.position;
			// Get bullet's velocity
			Vector3 bulletVel = bullets[i].GetComponent<Rigidbody>().velocity;
			// Compare our pos relative to bullet with bullet's velo -- if angle is < 90 deg,
			// we're possibly converging, and we'll dodge
			float dot = Vector3.Dot(bulletPos, bulletVel);
			if (dot > 0.0f) {
				// Get projection along bullet velo vec
				// (Algebra scribbled on paper, sorry)
				Vector3 proj = (dot / bulletVel.magnitude) * bulletVel.normalized;

				// We want the direction perpendicular to the bullet velo
				// So we take our projection, add it to the bullet's position, and sub from there
				Vector3 dir = transform.position - (bullets[i].transform.position + proj);

				// Now we add it to the (aggregate) dodge vector, at 1 - (distance / radius)
				dodgeVec += (dir.normalized - (dir / scanRadius));
			}
		}

		return dodgeVec;
	}
	
	// Same as scaled, but only scale if more than half-radius
	Vector3 DodgeBulletsHalfScaled (float scanRadius) {
		Vector3 dodgeVec = Vector3.zero;
		Collider[] bullets = Physics.OverlapSphere(transform.position, scanRadius, bulletMask);

		// Check each bullet in radius, if any
		for (int i = 0; i < bullets.Length; i++) {
			// Get our position relative to the bullet
			Vector3 bulletPos = transform.position - bullets[i].transform.position;
			// Get bullet's velocity
			Vector3 bulletVel = bullets[i].GetComponent<Rigidbody>().velocity;
			// Compare our pos relative to bullet with bullet's velo -- if angle is < 90 deg,
			// we're possibly converging, and we'll dodge
			float dot = Vector3.Dot(bulletPos, bulletVel);
			if (dot > 0.0f) {
				// Get projection along bullet velo vec
				// (Algebra scribbled on paper, sorry)
				Vector3 proj = (dot / bulletVel.magnitude) * bulletVel.normalized;

				// We want the direction perpendicular to the bullet velo
				// So we take our projection, add it to the bullet's position, and sub from there
				Vector3 dir = transform.position - (bullets[i].transform.position + proj);

				// Now we add it to the (aggregate) dodge vector
				if (dir.magnitude < scanRadius / 2.0f) {
					// Full amount if closer than half radius
					dodgeVec += dir.normalized;
				}
				else {
					// Otherwise, scale the outer half
					dodgeVec += dir.normalized * (2.0f - (2.0f * dir.magnitude / scanRadius));
				}
			}
		}

		return dodgeVec;
	}

	// Same as simple, but only deal with the closest
	Vector3 DodgeBulletsClosestSimple (float scanRadius) {
		Vector3 dodgeVec = Vector3.zero;
		var bulletList = new List<BulletInfo>();
		Collider[] bullets = Physics.OverlapSphere(transform.position, scanRadius, bulletMask);

		// Check each bullet in radius, if any
		for (int i = 0; i < bullets.Length; i++) {
			// Get our position relative to the bullet
			Vector3 bulletPos = transform.position - bullets[i].transform.position;
			// Get bullet's velocity
			Vector3 bulletVel = bullets[i].GetComponent<Rigidbody>().velocity;
			// Compare our pos relative to bullet with bullet's velo -- if angle is < 90 deg,
			// we're possibly converging, and we'll dodge
			BulletInfo thisBullet = new BulletInfo(bulletPos, bulletVel);
			if ((thisBullet.Dot > 0.0f) && (thisBullet.Dist > 0.0f)) {
				bulletList.Add(thisBullet);
			}
		}

		if (bulletList.Count > 0) {
			// Get the closest bullet
			var bc = new BulletDistCompare();
			bulletList.Sort(bc);
			BulletInfo bullet = bulletList[0];

			// Get projection along bullet velo vec
			// (Algebra scribbled on paper, sorry)
			Vector3 proj = (bullet.Dot / bullet.Vel.magnitude) * bullet.Vel.normalized;

			// We want the direction perpendicular to the bullet velo
			// So we take our projection, add it to the bullet's position, and sub from there
			// (Or rather, skip a step, and sub the projection from the position (which was from
			// the bullet to us anyways))
			Vector3 dir = bullet.Pos - proj;

			// Now we have our dodge vector
			dodgeVec = dir.normalized;
		}
		return dodgeVec;
	}

	// Same as scaled, but only deal with the closest
	Vector3 DodgeBulletsClosestScaled (float scanRadius) {
		Vector3 dodgeVec = Vector3.zero;
		var bulletList = new List<BulletInfo>();
		Collider[] bullets = Physics.OverlapSphere(transform.position, scanRadius, bulletMask);

		// Check each bullet in radius, if any
		for (int i = 0; i < bullets.Length; i++) {
			// Get our position relative to the bullet
			Vector3 bulletPos = transform.position - bullets[i].transform.position;
			// Get bullet's velocity
			Vector3 bulletVel = bullets[i].GetComponent<Rigidbody>().velocity;
			// Compare our pos relative to bullet with bullet's velo -- if angle is < 90 deg,
			// we're possibly converging, and we'll dodge
			BulletInfo thisBullet = new BulletInfo(bulletPos, bulletVel);
			if ((thisBullet.Dot > 0.0f) && (thisBullet.Dist > 0.0f)) {
				bulletList.Add(thisBullet);
			}
		}

		if (bulletList.Count > 0) {
			// Get the closest bullet
			var bc = new BulletDistCompare();
			bulletList.Sort(bc);
			BulletInfo bullet = bulletList[0];

			// Get projection along bullet velo vec
			// (Algebra scribbled on paper, sorry)
			Vector3 proj = (bullet.Dot / bullet.Vel.magnitude) * bullet.Vel.normalized;

			// We want the direction perpendicular to the bullet velo
			// So we take our projection, add it to the bullet's position, and sub from there
			// (Or rather, skip a step, and sub the projection from the position (which was from
			// the bullet to us anyways))
			Vector3 dir = bullet.Pos - proj;

			// Now we set our dodge vector, at 1 - (distance / radius)
			dodgeVec = (dir.normalized - (dir / scanRadius));
		}
		return dodgeVec;
	}
	

}

public enum DodgeMode : byte {
	Simple = 0,
	Scaled,
	HalfScaled,
	ClosestSimple,
	ClosestScaled
}

public enum BearingConflict : byte {
	Add = 0,
	OverrideBearing,
	OrthoBearing,
	OrthoDodgevec
}

public struct BulletInfo {
	public Vector3 Pos;
	public Vector3 Vel;
	public float Dist;
	public float Dot;

	public BulletInfo (Vector3 Pos, Vector3 Vel) {
		this.Pos = Pos;
		this.Vel = Vel;
		this.Dist = Pos.magnitude;
		this.Dot = Vector3.Dot(Pos, Vel);
	}

}

public class BulletDistCompare : IComparer<BulletInfo> {
	public int Compare (BulletInfo x, BulletInfo y) {
		if (x.Dist > y.Dist) {
			return 1;
		}
		else if (x.Dist < y.Dist) {
			return -1;
		}
		else {
			return 0;
		}
	}
}
