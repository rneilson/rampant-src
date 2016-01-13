using UnityEngine;
using System.Collections;

public class LightPulse : MonoBehaviour {

	public float intensityTarget = 2.0f;
	public float timeInitialTarget = 0.5f;
	public float timeTargetInitial = 0.5f;
	public bool returnToInitial = true;
	public bool looping = false;
	public bool debugInfo = false;

	private Light lightControl;
	private float intensityStart;
	private float intensityDiff;
	private float counter;
	private float phase;
	private PulseMode mode;

	private static float halfPi = Mathf.PI / 2.0f;

	// Use this for initialization
	void Start () {
		lightControl = GetComponent<Light>();
		intensityStart = lightControl.intensity;
		intensityDiff = intensityTarget - intensityStart;
		mode = PulseMode.ToTarget;
		phase = 0.0f;
		counter = 0.0f - Time.deltaTime;	// Update() will add this back in
	}
	
	// Update is called once per frame
	void Update () {

		if (mode != PulseMode.Stopped) {
			// Add dt to counter
			counter += Time.deltaTime;

			// Check if time reached (and *which* time), and set mode accordingly
			if (mode == PulseMode.ToTarget) {
				if (counter >= timeInitialTarget) {
					// Debug
					if (debugInfo) {
						Debug.Log("Switching mode to FromTarget, counter: " + counter.ToString() + ", phase: " 
							+ phase.ToString() + ", intensity: " + lightControl.intensity, gameObject);
					}
					// Set time to target->initial, less any overshoot
					counter = 0.0f + (counter - timeInitialTarget);
					// Switch mode
					mode = PulseMode.FromTarget;
				}
			}
			else if (mode == PulseMode.FromTarget) {
				if (counter >= timeTargetInitial) {
					// Check if we're looping
					if (looping) {
						// Debug
						if (debugInfo) {
							Debug.Log("Switching mode to ToTarget, counter: " + counter.ToString() + ", phase: " 
								+ phase.ToString() + ", intensity: " + lightControl.intensity, gameObject);
						}
						// Set time to initial->target, less any overshoot
						counter = 0.0f + (counter - timeTargetInitial);
						// Switch mode
						mode = PulseMode.ToTarget;
					}
					else {
						// Debug
						if (debugInfo) {
							Debug.Log("Switching mode to Stopped, counter: " + counter.ToString() + ", phase: " 
								+ phase.ToString() + ", intensity: " + lightControl.intensity, gameObject);
						}
						// Set phase to Pi, since we're done
						phase = Mathf.PI;
						// Switch mode
						mode = PulseMode.Stopped;
					}
				}
			}

			// Calulate phase depending on whether we're (now) going to/from target
			// Phase goes between 0->Pi/2 for initial->target, and Pi/2->Pi for target->initial
			// Intensity is calc'd by sin(phase)
			if (mode == PulseMode.ToTarget) {
				phase = (counter / timeInitialTarget) * halfPi;
			}
			else if (mode == PulseMode.FromTarget) {
				phase = ((counter / timeTargetInitial) * halfPi) + halfPi;
			}

			// Set intensity based on phase
			lightControl.intensity = intensityStart + (intensityDiff * Mathf.Sin(phase));
		}

	}
}

public enum PulseMode : byte {
	Stopped = 0,
	ToTarget,
	FromTarget
}
