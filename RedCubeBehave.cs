using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RedCubeBehave : MonoBehaviour {
	
	private GameObject target;
	private Scorer scorer;
	private RedCubeGroundControl control;
	private Vector3 bearing;
	private DeathType dying = DeathType.None;

	// Unity 5 API changes
	//private AudioSource myAudioSource;
	private Rigidbody myRigidbody;
	
	// Type/instance management stuff
	public string thisTypeName = "Seeker";
	private EnemyType thisType;
	private EnemyInst thisInst;

	// Public parameters
	public float speed;
	public float drag;
	public GameObject burster;
	public GameObject bursterQuiet;
	public GameObject deathFade;

	// Interceptor avoidance
	//private bool avoidInterceptors;
	//private List<GameObject> interceptorsClose = new List<GameObject>();

	// Use this for initialization
	void Start () {
		// Unity 5 API changes
		//myAudioSource = GetComponent<AudioSource>();
		myRigidbody = GetComponent<Rigidbody>();

		if (!scorer) {
			FindControl(GameObject.FindGameObjectWithTag("GameController"));
		}
		myRigidbody.drag = drag;

		// Add to control's list
		thisType = EnemyList.AddOrGetType(thisTypeName);
		thisInst = new EnemyInst(thisType.typeNum, gameObject);
		control.AddInstanceToList(thisInst);
		//avoidInterceptors = control.SeekersAvoidInterceptors;
	}
	
	// Update is called once per frame
	void Update () {
		if (dying != DeathType.None)
			BlowUp();
	}
	
	//Put movement in FixedUpdate
	void FixedUpdate () {
		if (target) {
			bearing = FindBearing(target.transform.position - transform.position);
			// Normalized in FindBearing
			myRigidbody.AddForce(bearing * speed);
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

	Vector3 FindBearing (Vector3 toTarget) {
		// Start with a straight line to target
		Vector3 curBearing = toTarget.normalized;
		/*
		if (avoidInterceptors) {
			// Now move away from each interceptor in turn
			for (int i = 0; i < interceptorsClose.Count; i++) {
				curBearing += AvoidInterceptor(interceptorsClose[i], toTarget);
			}
			// Clear out interceptor list for next frame
			interceptorsClose.Clear();
			// Renormalize
			return curBearing.normalized;
		}
		*/
		//else {
			// Didn't add anything, return as-is
			return curBearing;
		//}
	}

	/*
	Vector3 AvoidInterceptor (GameObject interceptor, Vector3 toTarget) {
		// Set here as const, keep the namespace clean (it's not, but y'know)
		const float avoidFactor = 1.0f;

		// Check distance to interceptor
		Vector3 interDist = transform.position - interceptor.transform.position;

		// Only avoid if interceptor is closer than target
		if (interDist.sqrMagnitude < toTarget.sqrMagnitude) {
			return interDist.normalized * avoidFactor;
		}
		else {
			return Vector3.zero;
		}
	}

	public void InterceptorClose (GameObject interceptor) {
		if (avoidInterceptors) {
			interceptorsClose.Add(interceptor);
		}
	}
	*/

}
