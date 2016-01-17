using UnityEngine;
using System.Collections;

public class RedCubeGroundControl : MonoBehaviour {

	// Player tracking
	private GameObject target;
	private Vector3 prevPos;
	private Vector3 currPos;
	private Vector3 prevVel;
	private Vector3 currVel;
	//private Vector3 prevAccel;
	private Vector3 currAccel;
	// TODO: store averages as doubles
	private float deltaTime;
	private float avgDeltaTime;
	//public Vector3 avgTurn;		// Public for debug
	//public Vector3 lastTurn;	// Public for debug
	public float avgSpeed;		// Public for debug
	public Vector3 avgAccel;		// Public for debug
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
	public Vector3[] posPredictions;	// Public for debug
	public Vector3[] prevAccelVectors;	// Public for debug

	public float maxInterSpeed = 1.0f;	// Public for debug

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
		NewTarget(GameObject.FindGameObjectWithTag("Player"));

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
			NewTarget(GameObject.FindGameObjectWithTag("Player"));
		}
	}
	
	void NewTarget (GameObject newTarget) {
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

		/* Old version
		// First, find the rotation between last and current velocities
		Quaternion lastRot = Quaternion.FromToRotation(prevVel, currVel);

		// Next, get the euler-angle representation of that rotation for averaging
		lastTurn = lastRot.eulerAngles;

		// Gonna hafta correct for negative turns showing up as >180 deg
		if (lastTurn.x > 180.0f)
			lastTurn.x -= 360.0f;
		if (lastTurn.y > 180.0f)
			lastTurn.y -= 360.0f;
		if (lastTurn.z > 180.0f)
			lastTurn.z -= 360.0f;

		if (debugInfo) {
			// For debug, let's keep the last however many turns on file
			int len = prevTurns.Length - 1;
			for (int i=0; i < len; i++) {
				prevTurns[i] = prevTurns[i + 1];
			}
			prevTurns[len] = lastTurn;
		}

		// Now weight-average everything
		avgTurn = avgTurn * avgWeight + lastTurn * lastWeight;
		// Last, turn it into a quaternion for iterating the position predictions
		turnPrediction = Quaternion.Euler(avgTurn);

		// I lied, *this* is the tricky part

		// Take current velocity, normalize, then scale by avgSpeed and avgDeltaTime
		// This gets the present course
		Vector3 displace = currVel.normalized * avgSpeed * avgDeltaTime;

		// Seed predictions array with current position
		posPredictions[0] = currPos;

		// Now iterate, rotating the added displacement each time
		for (int i = 1; i < posPredictions.Length; i++) {

			// Now find new displacement, rotating acceleration
			displace = displace + (turnPrediction * (displace.normalized * avgAccel * avgDeltaTime));
			// Scale to avg speed
			displace = displace.normalized * avgSpeed * avgDeltaTime;

			// Add rotated displacement to previous position
			posPredictions[i] = posPredictions[i-1] + displace;
		}
		*/

		/* Old version
		// Take current velocity, normalize, then scale by avgSpeed and avgDeltaTime
		Vector3 displace = currVel.normalized * avgSpeed * avgDeltaTime;
		// Seed predictions array with current position
		posPredictions[0] = currPos;
		// Now iterate, rotating the added displacement each time
		for (int i = 1; i < posPredictions.Length; i++) {
			displace = turnPrediction * displace;	// Quaternion/vector multiplication ftw
			posPredictions[i] = posPredictions[i-1] + displace;	// Add rotated displacement
		}
		*/
	}
}

public enum PredictionMode : byte {
	Linear = 0,
	Triangular
}
