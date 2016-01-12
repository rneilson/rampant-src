using UnityEngine;
using System.Collections;

public class LightPulse : MonoBehaviour {

	public float intensityTarget = 2;
	public float timeInitialTarget = 0.5;
	public float timeTargetInitial = 0.5;
	public bool holdAtTarget = false;
	public bool looping = false;

	private Light lightControl;
	private float intensityStart;
	private float intensityDiff;
	private float counter;
	private float phase;
	private PulseMode mode;

	private static float halfPi = Mathf.PI / 2;

	// Use this for initialization
	void Start () {
		lightControl = GetComponent<Light>();
		intensityStart = lightControl.intensity;
		intensityDiff = intensityTarget - intensityStart;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

public enum PulseMode : byte {
	ToTarget = 0,
	FromTarget,
	Holding,
	Stopped
}