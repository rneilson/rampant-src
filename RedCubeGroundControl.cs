using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

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

	// Enemy tracking/coordination
	private EnemyList enemyList;

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

// Might as well be its own class
public class EnemyList {
	// Shared between all instances (easier, frankly)
	private static int enemyTypeIndex;						// Starting type number
	private static Dictionary<int, EnemyType> enemyTypeByNum;	// Numbers->names
	private static Dictionary<string, EnemyType> enemyTypeByName;	// Names->numbers

	// Per instance (several reasons to use them, not all the same)
	private Dictionary<GameObject, EnemyInst> enemiesTotal;						// All tracked instances
	private Dictionary<int, Dictionary<GameObject, EnemyInst>> enemiesByType;	// A dict of dicts, one per type

	// Static constructor
	static EnemyList () {
		// Set up containers
		enemyTypeIndex = 0;
		enemyTypeByNum = new Dictionary<int, EnemyType>();
		enemyTypeByName = new Dictionary<string, EnemyType>();

		// Set first entry to EnemyType.Nothing
		// This a) initializes the lists properly, and b) ensures proper lookups of EnemyType.Nothing
		enemyTypeByNum.Add(EnemyType.Nothing.typeNum, EnemyType.Nothing);
		enemyTypeByName.Add(EnemyType.Nothing.typeName, EnemyType.Nothing);
	}

	// Instance constructors and helpers
	public EnemyList () {
		InitializeList();
	}

	public EnemyList (List<EnemyInst> enemies) {
		InitializeList();

		if (enemies != null) {
			foreach (EnemyInst enemy in enemies) {
				this.Add(enemy);
			}
		}
	}

	public EnemyList (EnemyInst[] enemies) {
		InitializeList();
		
		if (enemies != null) {
			for (int i = 0; i < enemies.Length; i++) {
				this.Add(enemies[i]);
			}
		}
	}

	private void InitializeList () {
		this.enemiesTotal = new Dictionary<GameObject, EnemyInst>();
		this.enemiesByType = new Dictionary<int, Dictionary<GameObject, EnemyInst>>();
	}

	// Static functions

	// Few checks -- use carefully
	private static EnemyType AddType (int newNum, string newName) {
		if (enemyTypeByNum.ContainsKey(newNum)) {
			throw new ArgumentOutOfRangeException(String.Format("typeNum {0} already in use!", newNum), "newNum");
		}
		if (enemyTypeByName.ContainsKey(newName)) {
			throw new ArgumentOutOfRangeException(String.Format("typeName {0} already in use!", newName), "newName");
		}
		// Use given type index as num
		EnemyType newType = new EnemyType(newNum, newName);
		// Add to both indexes
		enemyTypeByNum.Add(newType.typeNum, newType);
		enemyTypeByName.Add(newType.typeName, newType);
		// Advance enemyTypeIndex to next unused value
		// This will ensure the next run will be consistent
		while (enemyTypeByNum.ContainsKey(enemyTypeIndex)) {
			enemyTypeIndex++;
		}
		// Aaaaand done
		return newType;
	}

	private static EnemyType AddType (string newName) {
		return AddType(enemyTypeIndex, newName);
	}

	// TODO: change to try/catch
	public static EnemyType GetType (int typeNum) {
		if (TypeExists(typeNum)) {
			return enemyTypeByNum[typeNum];
		}
		else {
			return EnemyType.Nothing;
		}
	}

	// TODO: change to try/catch
	public static EnemyType GetType (string typeName) {
		if (typeName == "" ) {
			throw new ArgumentNullException("Can't get unnamed enemy type", typeName);
		}
		else if (TypeExists(typeName)) {
			// Check if it's already in there, return that if it is
			return enemyTypeByName[typeName];
		}
		else {
			return EnemyType.Nothing;
		}
	}

	public static bool TypeExists (int typeNum) {
		return enemyTypeByNum.ContainsKey(typeNum);
	}

	public static bool TypeExists (string typeName) {
		return enemyTypeByName.ContainsKey(typeName);
	}

	// Public ways to check/add types (static, since the list is)
	// Checks for dupes, so pretty safe to blindly use
	public static EnemyType AddOrGetType (string typeName) {
		// Null strings ain't gonna work
		if (typeName == "") {
			throw new ArgumentNullException("Can't add unnamed enemy type", typeName);
		}
		else if (TypeExists(typeName)) {
			// Check if it's already in there, return that if it is
			return GetType(typeName);
		}
		else {
			// Not in there, add it
			return AddType(typeName);
		}
	}

	// Instance functions
	public bool Add (EnemyInst inst) {
		bool addedTotal = false, addedType = false;
		// First, check type
		if (!TypeExists(inst.typeNum)) {
			// Enemy type not found, decide how to handle
			if (inst.typeNum > 0) {
				// Somehow unknown, let's add as such
				AddType(inst.typeNum, "unknown" + inst.typeNum.ToString());
			}
			else {
				// Non-type (0), not adding
				return false;
			}
		}
		// Type number exists (or it does now), now check if we have it on board
		if (!this.HasType(inst.typeNum)) {
			// Not already on board, add type num
			enemiesByType.Add(inst.typeNum, new Dictionary<GameObject, EnemyInst>());
		}
		// Check if we already have inst...
		// ...in total?
		if (!enemiesTotal.ContainsKey(inst.gameObj)) {
			// Add inst to overall dict
			enemiesTotal.Add(inst.gameObj, inst);
			addedTotal = true;
		}
		// ...by type?
		if (!enemiesByType[inst.typeNum].ContainsKey(inst.gameObj)) {
			// Add inst to per-type dict
			enemiesByType[inst.typeNum].Add(inst.gameObj, inst);
			addedType = true;
		}

		// Return true if added to either
		return addedTotal || addedType;
	}

	public bool HasType (EnemyType enemyType) {
		// Slightly redundant, but I'd like to be sure (it *is* a value type, after all)
		return this.HasType(enemyType.typeNum) && this.HasType(enemyType.typeName);
	}

	public bool HasType (int typeNum) {
		if (typeNum > 0) {
			return enemiesByType.ContainsKey(typeNum);
		}
		else {
			return false;
		}
	}

	public bool HasType (string typeName) {
		EnemyType retVal = GetType(typeName);
		if (retVal) {
			return HasType(retVal.typeNum);
		}
		else {
			// Note that we will never have EnemyType.Nothing stored
			return false;
		}
	}

}

public struct EnemyType {
	public readonly int typeNum;
	public readonly string typeName;

	// Empty type, returns false
	public static readonly EnemyType Nothing = new EnemyType(0, "Nothing");

	public EnemyType (int typeNum, string typeName) {
		this.typeNum = typeNum;
		this.typeName = typeName;
	}

	public override bool Equals (object obj) {
		if (obj is EnemyType) {
			return this.Equals((EnemyType) obj);
		}
		return false;
	}

	public static bool operator true (EnemyType x) {
		return x.typeNum > 0 && x.typeName != "";
	}

	public static bool operator false (EnemyType x) {
		return x.typeNum <= 0 || x.typeName == "";
	}

	public bool Equals (EnemyType x) {
		return (typeNum == x.typeNum) && (typeName == x.typeName);
	}

	public override int GetHashCode () {
		return typeNum.GetHashCode() ^ typeName.GetHashCode();
	}

	public static bool operator == (EnemyType lhs, EnemyType rhs) {
		return lhs.Equals(rhs);
	}

	public static bool operator != (EnemyType lhs, EnemyType rhs) {
		return !(lhs.Equals(rhs));
	}
}

public struct EnemyInst {
	public readonly int typeNum;
	public readonly GameObject gameObj;

	// Empty type, returns false
	public static readonly EnemyInst Nothing = new EnemyInst(0, null);

	public EnemyInst (int typeNum, GameObject gameObj) {
		this.typeNum = typeNum;
		this.gameObj = gameObj;
	}

	public override bool Equals (object obj) {
		if (obj is EnemyInst) {
			return this.Equals((EnemyInst) obj);
		}
		return false;
	}

	public static bool operator true (EnemyInst x) {
		return x.typeNum > 0 && x.gameObj != null;
	}

	public static bool operator false (EnemyInst x) {
		return x.typeNum <= 0 || x.gameObj == null;
	}

	public bool Equals (EnemyInst x) {
		return (typeNum == x.typeNum) && (gameObj == x.gameObj);
	}

	public override int GetHashCode () {
		return typeNum.GetHashCode() ^ gameObj.GetHashCode();
	}

	public static bool operator == (EnemyInst lhs, EnemyInst rhs) {
		return lhs.Equals(rhs);
	}

	public static bool operator != (EnemyInst lhs, EnemyInst rhs) {
		return !(lhs.Equals(rhs));
	}
}