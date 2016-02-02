using UnityEngine;
using System.Collections;

public class RedCubeBomb : MonoBehaviour {
	
	private GameObject target;
	private Scorer scorer;
	private RedCubeGroundControl control;
	private Vector3 bearing;
	private DeathType dying = DeathType.None;
	private bool armed;
	private const bool spin = true;
	private Vector3 spinAxis = Vector3.right;
	private Vector3 spinRef = Vector3.forward;
	private float torque = 0.1f;

	// Unity 5 API changes
	//private AudioSource myAudioSource;
	private Rigidbody myRigidbody;
	
	// Type/instance management stuff
	private const string thisTypeName = "Bomber";
	private static EnemyType thisType;
	private EnemyInst thisInst;

	// Public parameters
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

	static RedCubeBomb () {
		thisType = EnemyList.AddOrGetType(thisTypeName);
	}

	// Use this for initialization
	void Start () {
		// Unity 5 API changes
		//myAudioSource = GetComponent<AudioSource>();
		myRigidbody = GetComponent<Rigidbody>();

		if (!scorer) {
			FindControl(GameObject.FindGameObjectWithTag("GameController"));
		}
		myRigidbody.drag = drag;
		armed = true;

		// Add to control's list
		thisInst = new EnemyInst(thisType.typeNum, gameObject);
		control.AddInstanceToList(thisInst);
	}
	
	// Update is called once per frame
	void Update () {
		if (dying != DeathType.None)
			BlowUp();
	}
	
	//Put movement in FixedUpdate
	void FixedUpdate () {
		if (target) {
			// Get bearing
			bearing = target.transform.position - transform.position;
			// Blow up if we're in range
			if (bearing.magnitude <= bombTriggerRadius) {
				Die(true);
			}
			// Or try to get in range
			else {
				myRigidbody.AddForce(bearing.normalized * speed);
			}
			// Spin menacingly (if spinning enabled)
			if (spin) {
				myRigidbody.AddTorque(SpinVector(bearing) * torque);
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
		// Drop da bomb
		if (armed) {
			DropBomb(transform.position);
		}
		if (dying != DeathType.Silently) {
			scorer.AddKill();
		}
		KillRelatives(0.4f);
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

	void FindControl (GameObject controller) {
		scorer = controller.GetComponent<Scorer>();
		control = controller.GetComponent<RedCubeGroundControl>();
		NewTarget(scorer.Player);
	}

	void DropBomb (Vector3 pos) {
		Vector3 bombPos = new Vector3 (pos.x, bombHeight, pos.z);
		GameObject daBomb;

		// Spawn effect
		// At player position because it looks better
		daBomb = Instantiate(bombEffect, pos, Quaternion.identity) as GameObject;
		Destroy(daBomb, 1.0f);
		// Turn down flash if dying quietly
		if (dying != DeathType.Loudly) {
			daBomb.GetComponent<LightPulse>().ChangeTargetRelative(-1.2f);
		}
		// Turn down volume if dying quietly
		if (dying == DeathType.Quietly) {
			daBomb.GetComponent<AudioSource>().volume *= 0.25f;
		}
		// Mute if dying silently
		if (dying == DeathType.Silently) {
			daBomb.GetComponent<AudioSource>().volume = 0.0f;
		}
		
		// We're dropping, make sure we're now disarmed
		armed = false;

		/* Nope, not faster, not really
		// New bomb radius code
		float playerExtent = 0.1f;
		float thingExtent = 0.1f;
		float thingDist = 0.0f;

		// Find list of enemies (probably faster than an OverlapSphere (or two))
		GameObject[] things = GameObject.FindGameObjectsWithTag("Enemy");
		// Iterate over and check distances to everyone, killing or pushing as required
		for (int i = 0; i < things.Length; i++) {
			thingDist = (things[i].transform.position - transform.position).magnitude;
			if ((thingDist <= (bombKillRadius + thingExtent)) && (things[i] != gameObject)) {
				things[i].SendMessage("Die", false, SendMessageOptions.DontRequireReceiver);
			}
			// Only push if dying loudly (shot or self-triggering)
			else if ((enableBombPush) && (dying == DeathType.Loudly) && (thingDist <= (bombPushRadius + thingExtent))) {
				Rigidbody targetRigid = things[i].GetComponent<Rigidbody>();
				if (targetRigid) {
					targetRigid.AddExplosionForce(bombForce, bombPos, 0f);
				}
			}
		}
		// Now check player distance and kill/push (only if loud)
		if ((dying == DeathType.Loudly) && (target)) {
			thingDist = (target.transform.position - transform.position).magnitude;
			if ((thingDist <= (bombKillRadius + playerExtent))) {
				target.SendMessage("Die", false, SendMessageOptions.DontRequireReceiver);
			}
			else if ((enableBombPush) && (thingDist <= (bombPushRadius + playerExtent))) {
				Rigidbody targetRigid = target.GetComponent<Rigidbody>();
				if (targetRigid) {
					targetRigid.AddExplosionForce(bombForce, bombPos, 0f);
				}
			}
		}
		*/
			
		int killmask, pushmask;
		Collider[] things;
		// If we're shooting the bomber or self-triggering, can kill player
		if (dying == DeathType.Loudly) {
			killmask = (1 << LayerMask.NameToLayer("Enemy")) | (1 << LayerMask.NameToLayer("Player"));
		}
		else {
			killmask = (1 << LayerMask.NameToLayer("Enemy"));
		}
		
		// Kill things in inner radius
		things = Physics.OverlapSphere(pos, bombKillRadius, killmask);
		if (things.Length > 0) {
			for (int i=0; i<things.Length; i++) {
				things[i].SendMessage("Die", false);
			}
		}
		
		// Only push things if we're dying loudly (do push player)
		if (dying == DeathType.Loudly) {
			pushmask = (1 << LayerMask.NameToLayer("Enemy")) | (1 << LayerMask.NameToLayer("Player"));
			// Push things in outer radius
			things = Physics.OverlapSphere(pos, bombPushRadius, pushmask);
			for (int i=0; i<things.Length; i++) {
				things[i].GetComponent<Rigidbody>().AddExplosionForce(bombForce, bombPos, 0f);
			}
		}

	}

	Vector3 SpinVector (Vector3 bearingVec) {
		Quaternion rot = Quaternion.FromToRotation(spinRef, bearingVec);
		return rot * spinAxis;
	}
	
}
