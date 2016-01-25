using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RedCubeGroundControl : MonoBehaviour {

	// Player tracking
	private GameObject target;
	private Vector3 prevPos;
	private Vector3 currPos;
	private Vector3 prevVel;
	private Vector3 currVel;
	private Vector3 currAccel;
	// TODO: store averages as doubles
	private float deltaTime;
	private float avgDeltaTime;
	private float avgSpeed;
	private Vector3 avgAccel;
	private float maxSpeed;
	private float maxAccel;
	// TODO: modify weights at runtime to fine-tune prediction
	private float avgWeight;
	private float lastWeight;

	public int framesToAverage = 20;
	public int framesToPredict = 10;
	public PredictionMode predictMode = PredictionMode.Linear;
	public bool debugInfo = false;

	private Quaternion turnPrediction;
	private Vector3[] posPredictions;
	private Vector3[] prevAccelVectors;

	private float maxInterSpeed = 1.0f;

	// Scorer co-component
	private Scorer scorer;

	// Enemy tracking/coordination
	private EnemyList enemyList = new EnemyList();

	public bool DebugInfo {
		get { return debugInfo; }
	}
	public Vector3 Prediction (int index) {
		if (index < posPredictions.Length) {
			return posPredictions[index];
		}
		else {
			Debug.LogError("Prediction index " + index.ToString() + " out-of-bounds", gameObject);
			return Vector3.zero;
		}
	}
	public int PredictionLength {
		get { return posPredictions.Length; }
	}
	public float AvgDeltaTime {
		get { return avgDeltaTime; }
	}
	public float MaxInterceptorSpeed {
		get { return maxInterSpeed; }
		set { maxInterSpeed = value; }
	}
	public float MaxInterceptorFrameSpeed {
		get { return maxInterSpeed * avgDeltaTime; }
	}

	// Use this for initialization
	void Start () {
		// First, find a player
		//NewTarget(GameObject.FindGameObjectWithTag("Player"));
		// Disabled because there's never a player on startup

		// Get scorer
		scorer = GetComponent<Scorer>();

		// Currently on initialization, not here
		// Create (empty) enemy list
		//enemyList = new EnemyList();

		if (predictMode == PredictionMode.Linear) {
			avgWeight = (float) (framesToAverage - 1) / (float) framesToAverage;
			lastWeight = 1.0f / (float) framesToAverage;
			if (debugInfo) {
				Debug.Log("avgWeight: " + avgWeight.ToString() + ", lastWeight: " + lastWeight.ToString() 
					+ ", totalWeight: " + framesToAverage.ToString(), gameObject);
			}
		}
		else if (predictMode == PredictionMode.Triangular) {
			// Triangular weight average is by triangular numbers, descending, with latest data weighted by last number
			// Oh, you'll know what I mean when you read it
			int totalWeight = 0;
			for (int i=1; i <= framesToAverage; i++) {
				totalWeight += i;
			}
			avgWeight = (float) (totalWeight - framesToAverage) / (float) totalWeight;
			lastWeight = (float) framesToAverage / (float) totalWeight;
			if (debugInfo) {
				Debug.Log("avgWeight: " + avgWeight.ToString() + ", lastWeight: " + lastWeight.ToString() 
					+ ", totalWeight: " + totalWeight.ToString(), gameObject);
			}
		}

		posPredictions = new Vector3[framesToPredict + 1];	// First entry will be currPos
		if (debugInfo) {
			prevAccelVectors = new Vector3[framesToAverage];
		}
		/*
		if (debugInfo) {
			Debug.Log("Predictions Length is " + posPredictions.Length.ToString() 
				+ ", should be " + (framesToPredict + 1).ToString(), gameObject);
		}
		*/


	}
	
	// Update is called once per frame
	void Update () {
		// Show off our work
		if (debugInfo) {
			for (int i = 0; i < posPredictions.Length - 1; i++) {
				Debug.DrawLine(posPredictions[i], posPredictions[i + 1], Color.cyan);
			}
		}
	}

	void FixedUpdate () {
		if (target) {
			UpdateTracking(target.transform.position, Time.fixedDeltaTime);
		}
		else {
			// Try and acquire new target
			NewTarget(scorer.Player);
		}
	}
	
	void UpdateTracking(Vector3 pos, float dT) {
		// First shift old current values to new previous (you know what I mean!)
		prevPos = currPos;
		prevVel = currVel;
		//prevAccel = currAccel;

		// Set new current values
		deltaTime = dT;
		currPos = pos;
		currVel = (currPos - prevPos) / deltaTime;

		// Now we gotta find acceleration in the reference frame of previous velocity
		// First, find the rotation from prevVel to, say, the Z-axis
		Quaternion prevRot = Quaternion.FromToRotation(prevVel.normalized, Vector3.forward);
		// Next, apply that same rotation to currVel and prevVel
		Vector3 currVelRot = prevRot * currVel;
		Vector3 prevVelRot = prevRot * prevVel;
		// Now we take the acceleration in terms of this rotated frame
		currAccel = (currVelRot - prevVelRot);

		if (debugInfo) {
			// For debug, let's keep the last however many accelerations on file
			int len = prevAccelVectors.Length - 1;
			for (int i=0; i < len; i++) {
				prevAccelVectors[i] = prevAccelVectors[i + 1];
			}
			prevAccelVectors[len] = currAccel;
		}

		// Update average dT, speed, acceleration (magnitude)
		avgDeltaTime = avgDeltaTime * avgWeight + deltaTime * lastWeight;
		avgSpeed = avgSpeed * avgWeight + currVel.magnitude * lastWeight;
		avgAccel = avgAccel * avgWeight + currAccel * lastWeight;

		// Update observed maximums
		if (currVel.magnitude > maxSpeed) {
			maxSpeed = currVel.magnitude;
		}
		if (currAccel.magnitude > maxAccel) {
			maxAccel = currAccel.magnitude;
		}

		// Now the tricky part(s)

		// Find the rotation *from* the Z-axis to the current velocity
		Quaternion currRot = Quaternion.FromToRotation(Vector3.forward, currVel);
		// Rotate the average acceleration and add to current velocity to find next displacement
		Vector3 displace = (currVel + (currRot * avgAccel)) * avgDeltaTime;
		// Clamp to max speed if necessary
		if (displace.magnitude > maxSpeed) {
			displace = displace.normalized * maxSpeed * avgDeltaTime;
		}
		// Seed predictions array with current position
		posPredictions[0] = currPos;
		// Calculate first new position
		posPredictions[1] = currPos + displace;
		// Find rotation between displacements
		Quaternion disRot = Quaternion.FromToRotation(currVel, displace);
		// Now iterate, rotating the added displacement each time
		for (int i = 2; i < posPredictions.Length; i++) {
			displace = disRot * displace;	// Rotate displacement vector
			posPredictions[i] = posPredictions[i-1] + displace;	// Add rotated displacement
		}

	}

	public void NewTarget (GameObject newTarget) {
		target = newTarget;
		// If new target acquired, reset tracking
		if (target) {
			currPos = target.transform.position;
			currVel = Vector3.zero;
			currAccel = Vector3.zero;
			//avgTurn = Vector3.zero;
			//lastTurn = Vector3.zero;
			avgAccel = Vector3.zero;
			avgSpeed = 0.0f;
		}
	}

	public void DebugCap () {
		if (scorer.GlobalDebug) {
		// Temp, to make sure initialization works
		Debug.Log(System.String.Format("Types in list: {0}", EnemyList.TypeCount), gameObject);
		Debug.Log(System.String.Format("Nothing type is {0}", 
			EnemyType.Nothing.ToString()), gameObject);
		/*
		EnemyType tmpType = EnemyList.GetType(0);
		Debug.Log(System.String.Format("Type at index 0 is typeNum {0}, typeName {1}", 
			tmpType.typeNum, tmpType.typeName), gameObject);
		tmpType = EnemyList.GetType("Nothing");
		Debug.Log(System.String.Format("Type at name \"Nothing\" is typeNum {0}, typeName {1}", 
			tmpType.typeNum, tmpType.typeName), gameObject);
		}
		*/
		Debug.Log("Current types:");
		foreach (EnemyType x in EnemyList.TypeList) {
			Debug.Log(x.ToString());
		}

		Debug.Log(System.String.Format("Current instances in total list: {0}", enemyList.Count), gameObject);
		Debug.Log(System.String.Format("Number of per-type lists: {0}", enemyList.CountTypes), gameObject);

		// TODO: moar
		}
	}

	public bool AddInstanceToList (EnemyInst inst) {
		return enemyList.Add(inst);
	}

	public bool RemoveInstanceFromList (EnemyInst inst) {
		return enemyList.Remove(inst);
	}
}

public enum PredictionMode : byte {
	Linear = 0,
	Triangular
}

