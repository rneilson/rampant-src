using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

// Might as well be its own class
// TODO: switch inst and type to classes too
// TODO: move type functions to type class
// TODO: add constructor overloads to inst class
// TODO: genericize
public class EnemyList : IEnumerable {
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
		enemyTypeByNum = new Dictionary<int, EnemyType>();
		enemyTypeByName = new Dictionary<string, EnemyType>();

		// Set first entry to EnemyType.Nothing
		// This a) initializes the lists properly, and b) ensures proper lookups of EnemyType.Nothing
		enemyTypeByNum.Add(EnemyType.Nothing.typeNum, EnemyType.Nothing);
		enemyTypeByName.Add(EnemyType.Nothing.typeName, EnemyType.Nothing);
		// EnemyType.Nothing should always be type 0, and everything else higher
		enemyTypeIndex = EnemyType.Nothing.typeNum + 1;
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

	// Static properties and functions

	public static int TypeCount {
		get {
			// Just here for debug, really
			if (enemyTypeByNum.Count != enemyTypeByName.Count) {
				throw new ArgumentOutOfRangeException("Count", String.Format("ByNum count {0} != ByName count {1}!", 
					enemyTypeByNum.Count, enemyTypeByName.Count));
			}
			return enemyTypeByNum.Count;
		}
	}

	public static int TypeIndex {
		get { return enemyTypeIndex; }
	}

	public static List<EnemyType> TypeList {
		get {
			// Could use either dict
			return new List<EnemyType>(enemyTypeByNum.Values);
		}
	}

	// Few checks -- use carefully
	private static EnemyType AddType (int newNum, string newName) {
		if (enemyTypeByNum.ContainsKey(newNum)) {
			throw new ArgumentOutOfRangeException("newNum", String.Format("typeNum {0} already in use!", newNum));
		}
		if (enemyTypeByName.ContainsKey(newName)) {
			throw new ArgumentOutOfRangeException("newName", String.Format("typeName {0} already in use!", newName));
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
			throw new ArgumentNullException("typeName", "Can't get unnamed enemy type");
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
			throw new ArgumentNullException("typeName", "Can't get unnamed enemy type");
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

	// Instance properties and functions

	// Enumerator for total list
	IEnumerator IEnumerable.GetEnumerator() {
		return (IEnumerator) GetEnumerator();
	}
	public IEnumerator<EnemyInst> GetEnumerator() {
		return this.enemiesTotal.Values.GetEnumerator();
	}

	// Nested class for use with foreach when specifying type
	public EnemyListByType ByType (int index) {
		if (this.HasType(index)) {
			return new EnemyListByType(enemiesByType[index]);
		}
		else {
			// Fail safely, if circuitously
			return new EnemyListByType(new Dictionary<GameObject, EnemyInst>());
		}
	}
	public class EnemyListByType : IEnumerable {
		readonly Dictionary<GameObject, EnemyInst> enemyList;
		internal EnemyListByType (Dictionary<GameObject, EnemyInst> enemies) {
			enemyList = enemies;
		}
		IEnumerator IEnumerable.GetEnumerator() {
			return (IEnumerator) GetEnumerator();
		}
		public IEnumerator<EnemyInst> GetEnumerator() {
			return enemyList.Values.GetEnumerator();
		}
	}

	public int Count {
		get { return this.enemiesTotal.Count; }
	}

	public int CountTypes {
		get { return this.enemiesByType.Count; }
	}

	// Fastest way, I think, to get the collection of types on board
	public List<int> TypesHeld {
		get { return new List<int>(this.enemiesByType.Keys); }
	}

	public bool HasType (EnemyType enemyType) {
		// Slightly redundant, but I'd like to be sure (it *is* a value type, after all)
		return this.HasType(enemyType.typeNum) && this.HasType(enemyType.typeName);
	}

	public bool HasType (int typeNum) {
		if (typeNum > 0) {
			return this.enemiesByType.ContainsKey(typeNum);
		}
		else {
			return false;
		}
	}

	public bool HasType (string typeName) {
		EnemyType enemy = GetType(typeName);
		if (enemy) {
			return this.HasType(enemy.typeNum);
		}
		else {
			// Note that we will never have EnemyType.Nothing stored
			return false;
		}
	}

	public int CountByType (int index) {
		if (TypeExists(index) && this.HasType(index)) {
			return this.enemiesByType[index].Count;
		}
		else {
			return 0;
		}
	}

	public int CountByType (string index) {
		if (TypeExists(index) && this.HasType(index)) {
			return this.enemiesByType[GetType(index).typeNum].Count;
		}
		else {
			return 0;
		}
	}

	// Warning: these might be a bit slow to be calling all the time
	// Mostly useful if you want to sort it later or something
	// (Well, depending on the boxing/unboxing penalty of using the enumerators)
	public List<EnemyInst> ListAll () {
		return new List<EnemyInst>(enemiesTotal.Values);
	}

	public List<EnemyInst> ListType(int index) {
		if (this.HasType(index)) {
			return new List<EnemyInst>(enemiesByType[index].Values);
		}
		else {
			// Fail safely, if redundantly
			return new List<EnemyInst>();
		}
	}

	public List<EnemyInst> ListType(string index) {
		if (this.HasType(index)) {
			return new List<EnemyInst>(enemiesByType[GetType(index).typeNum].Values);
		}
		else {
			// Fail safely, if redundantly
			return new List<EnemyInst>();
		}
	}

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
			this.enemiesByType.Add(inst.typeNum, new Dictionary<GameObject, EnemyInst>());
		}
		// Check if we already have inst...
		// ...in total?
		if (!this.enemiesTotal.ContainsKey(inst.gameObj)) {
			// Add inst to overall dict
			this.enemiesTotal.Add(inst.gameObj, inst);
			addedTotal = true;
		}
		// ...by type?
		if (!this.enemiesByType[inst.typeNum].ContainsKey(inst.gameObj)) {
			// Add inst to per-type dict
			this.enemiesByType[inst.typeNum].Add(inst.gameObj, inst);
			addedType = true;
		}

		// Return true if added to either
		return addedTotal || addedType;
	}

	public bool Remove (EnemyInst inst) {
		bool removedTotal = false, removedType = false;
		// This one's simple
		removedTotal = this.enemiesTotal.Remove(inst.gameObj);
		// This one's not -- gotta check if type key exists first
		if (enemiesByType.ContainsKey(inst.typeNum)) {
			removedType = this.enemiesByType[inst.typeNum].Remove(inst.gameObj);
		}

		// Return true if removed from either
		return removedTotal || removedType;
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

	public override string ToString() {
		return typeNum.ToString() + ": " + typeName.ToString();
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

	public override string ToString() {
		return typeNum.ToString() + ": " + gameObj.ToString();
	}
}
