using UnityEngine;
using System.Collections;

public class PulseControl : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

public enum PulseMode : byte {
	Linear = 0,
	Sine
}

public enum PulseState : byte {
	Stopped = 0,
	Starting,
	ToTarget,
	FromTarget
}
