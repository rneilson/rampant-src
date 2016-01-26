using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// TODO: add radius check methods (total and by-type)
// TODO: add PID generator for each instance added
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
	private EnemyList enemies = new EnemyList();
	private float baseExtent = 0.1f;
	private float extent1d;
	private float extent2d;
	private float extent3d;

	// Enemy tracking/radius debug
	private List<GameObject> debugTracking = new List<GameObject>();
	private Dictionary<int, List<GameObject>> debugTrackByType = new Dictionary<int, List<GameObject>>();
	private float debugRadius = 2.0f;
	private Dictionary<string, Color> debugColors = new Dictionary<string, Color>(); 

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
	public float Extent1D {
		get { return extent1d; }
	}
	public float Extent2D {
		get { return extent2d; }
	}
	public float Extent3D {
		get { return extent3d; }
	}

	// Use this for initialization
	void Start () {
		// First, find a player
		//NewTarget(GameObject.FindGameObjectWithTag("Player"));
		// Disabled because there's never a player on startup

		// Get scorer
		scorer = GetComponent<Scorer>();

		// Radius extents
		extent1d = baseExtent;
		extent2d = Mathf.Sqrt((baseExtent * baseExtent) + (baseExtent * baseExtent));
		extent3d = Mathf.Sqrt((baseExtent * baseExtent) + (baseExtent * baseExtent) + (baseExtent * baseExtent));

		// Debug colors
		debugColors["Seeker"] = Color.red;
		debugColors["Interceptor"] = Color.yellow;
		debugColors["Bomber"] = Color.green;
		debugColors["Extent"] = Color.white;
		debugColors["Radius"] = Color.grey;
		debugColors["OutOfRange"] = Color.black;

		// Currently on initialization, not here
		// Create (empty) enemy list
		//enemies = new EnemyList();

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
		if ((debugInfo) || (scorer.GlobalDebug)) {
			// Draw predicted path
			DebugDrawPredictions();
			// Only draw tracking lines if debugInfo set (not GlobalDebug)
			if ((target) && (debugInfo)) {
				// Draw range tracking
				DebugDrawRanges();
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

	void OnDrawGizmos () {
		if ((target) && (scorer)) {
			if ((debugInfo)) {
				// Draw radius spheres
				Gizmos.color = Color.grey;
				Gizmos.DrawWireSphere(target.transform.position, debugRadius);
				Gizmos.color = Color.white;
				Gizmos.DrawWireSphere(target.transform.position, debugRadius + extent3d);
			}
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

	public bool AddInstanceToList (EnemyInst inst) {
		return enemies.Add(inst);
	}

	public bool RemoveInstanceFromList (EnemyInst inst) {
		return enemies.Remove(inst);
	}

	public List<GameObject> FindAllWithinRadius (Vector3 pos, float radius, float extent) {
		// Fresh list (hopefully not too much slower than an array)
		List<GameObject> results = new List<GameObject>();
		// Square of radius to compare
		float sqrRad = (radius + extent) * (radius + extent);
		// Iterate over all instances on file and check distance
		foreach (EnemyInst x in enemies) {
			// Get distance vector
			Vector3 dist = (x.gameObj.transform.position - pos);
			// Compare square magnitudes, as it's a tiny bit cheaper (fewer sqrt calls)
			if (dist.sqrMagnitude <= sqrRad) {
				results.Add(x.gameObj);
			}
		}
		// Return with ill-gotten gains
		return results;
	}

	public List<GameObject> FindTypeWithinRadius (int typeNum, Vector3 pos, float radius, float extent) {
		// Fresh list (hopefully not too much slower than an array)
		List<GameObject> results = new List<GameObject>();
		// Square of radius to compare
		float sqrRad = (radius + extent) * (radius + extent);
		// Iterate over all instances of type on file and check distance
		foreach (EnemyInst x in enemies.ByType(typeNum)) {
			// Get distance vector
			Vector3 dist = (x.gameObj.transform.position - pos);
			// Compare square magnitudes, as it's a tiny bit cheaper (fewer sqrt calls)
			if (dist.sqrMagnitude <= sqrRad) {
				results.Add(x.gameObj);
			}
		}
		// Return with ill-gotten gains
		return results;
	}

	// Debug functions

	public void DebugCap () {
		if (scorer.GlobalDebug) {
			/*
			// Temp, to make sure initialization works
			Debug.Log(System.String.Format("Types in list: {0}", EnemyList.TypeCount), gameObject);
			Debug.Log(System.String.Format("Nothing type is {0}", 
				EnemyType.Nothing.ToString()), gameObject);
			EnemyType tmpType = EnemyList.GetType(0);
			Debug.Log(System.String.Format("Type at index 0 is typeNum {0}, typeName {1}", 
				tmpType.typeNum, tmpType.typeName), gameObject);
			tmpType = EnemyList.GetType("Nothing");
			Debug.Log(System.String.Format("Type at name \"Nothing\" is typeNum {0}, typeName {1}", 
				tmpType.typeNum, tmpType.typeName), gameObject);
			}
			*/
			// Types available and held
			Debug.Log("Current types:");
			foreach (EnemyType x in EnemyList.TypeList) {
				Debug.Log(x.ToString());
			}
			Debug.Log(System.String.Format("Current instances in total list: {0}", enemies.Count), gameObject);
			Debug.Log(System.String.Format("Number of per-type lists: {0}", enemies.CountTypes), gameObject);
			foreach (int x in enemies.TypesHeld) {
				Debug.Log(System.String.Format("{0}s: {1}", EnemyList.GetTypeName(x), enemies.CountByType(x)), gameObject);
			}

			if (debugInfo) {
				// Get all enemies to track/range
				debugTracking = FindAllWithinRadius(target.transform.position, debugRadius, extent3d);
				Debug.Log(System.String.Format("{0} instance(s) within {1}", debugTracking.Count, debugRadius), gameObject);
				// Create new empty per-type dict
				debugTrackByType = new Dictionary<int, List<GameObject>>();
				// Get lists of each type and load into dict
				foreach (int x in enemies.TypesHeld) {
					List<GameObject> results = FindTypeWithinRadius(x, target.transform.position, debugRadius, extent3d);
					debugTrackByType.Add(x, results);
					Debug.Log(System.String.Format("{0} {1}(s) within {2}", 
						debugTrackByType[x].Count, EnemyList.GetTypeName(x), debugRadius), gameObject);
				}
			}
			// TODO: moar?
		}
	}

	void DebugDrawPredictions () {
		for (int i = 0; i < posPredictions.Length - 1; i++) {
			Debug.DrawLine(posPredictions[i], posPredictions[i + 1], Color.cyan);
		}
	}

	void DebugDrawRanges () {
		// Only draw color to radius + extent
		float drawRadius = debugRadius + extent3d;
		Color radiusColor = debugColors["Extent"];
		Color rangeColor = debugColors["OutOfRange"];
		Vector3 targetPos = target.transform.position;

		// Do each type separately
		foreach (KeyValuePair<int, List<GameObject>> kvp in debugTrackByType) {
			// Set color per type
			Color typeColor = debugColors[EnemyList.GetTypeName(kvp.Key)];
			// Now draw lines for each
			foreach (GameObject gameObj in kvp.Value) {
				if (gameObj) {
					Vector3 objectPos = gameObj.transform.position;
					Vector3 distance = objectPos - targetPos;
					Vector3 rangePos = (distance.normalized * drawRadius) + targetPos;
					if (distance.sqrMagnitude <= drawRadius * drawRadius) {
						// In range, use type color to position, plus white out to radius
						Debug.DrawLine(targetPos, objectPos, typeColor);
						Debug.DrawLine(objectPos, rangePos, radiusColor);
					}
					else {
						// Out of range, draw radius in type color and remainder in black
						Debug.DrawLine(targetPos, rangePos, typeColor);
						Debug.DrawLine(rangePos, objectPos, rangeColor);
					}
				}
			}
		}
	}

}

public enum PredictionMode : byte {
	Linear = 0,
	Triangular
}

