using UnityEngine;
using System.Collections;

public class MaterialPulse : MonoBehaviour {

	public Color emissionColor = Color.white;
	public float emissionFraction = 1.0f;
	public float timeInitialTarget = 0.5f;
	public float timeTargetInitial = 0.5f;
	public bool returnToInitial = true;
	public bool looping = false;
	public bool debugInfo = false;
	public PulseMode pulseMode = PulseMode.Sine;

	private Material matControl;
	private int emissionId;
	private Color emissionInitial;
	private Color emissionTarget;
	private float counter;
	private float phase;
	private PulseState currentState;

	private const float halfPi = Mathf.PI / 2.0f;

	// Use this for initialization
	void Start () {
		// Sanity checks
		if (emissionFraction < 0.0f) {
			emissionFraction = 0.0f;
		}

		// Get initial and target colors
		matControl = GetComponent<Renderer>().material;
		matControl.EnableKeyword("_EMISSION");
		emissionId = Shader.PropertyToID("_EmissionColor");
		emissionInitial = matControl.GetColor(emissionId);
		/*
		if (QualitySettings.activeColorSpace == ColorSpace.Gamma) {
			emissionTarget = emissionColor * Mathf.LinearToGammaSpace(emissionFraction);
		}
		else {
			emissionTarget = emissionColor * emissionFraction;
		}
		*/
		emissionTarget = emissionColor * emissionFraction;
		if (debugInfo) {
			Debug.Log("Initial: " + emissionInitial.ToString() + ", Target: " + emissionTarget.ToString(), gameObject);
		}
		
		currentState = PulseState.Starting;
		phase = 0.0f;
		counter = 0.0f;
		if (debugInfo) {
			Debug.Log("Staring pulse, counter: " + counter.ToString() + ", phase: " 
				+ phase.ToString(), gameObject);
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (currentState != PulseState.Stopped) {

			// If we haven't started yet, let's do that
			if (currentState == PulseState.Starting) {
				if (debugInfo) {
					Debug.Log("Switching currentState to ToTarget, counter: " + counter.ToString() + ", phase: " 
						+ phase.ToString(), gameObject);
				}
				currentState = PulseState.ToTarget;
			}
			else {
				// Add dt to counter
				counter += Time.deltaTime;
			}

			// Check if time reached (and *which* time), and set currentState accordingly
			if (currentState == PulseState.ToTarget) {
				if (counter >= timeInitialTarget) {
					if (returnToInitial) {
						// Debug
						if (debugInfo) {
							Debug.Log("Switching currentState to FromTarget, counter: " + counter.ToString() + ", phase: " 
								+ phase.ToString(), gameObject);
						}
						// Set time to target->initial, less any overshoot
						counter = counter - timeInitialTarget;
						// Switch currentState
						currentState = PulseState.FromTarget;
					}
					else {
						// Debug
						if (debugInfo) {
							Debug.Log("Switching currentState to Stopped, counter: " + counter.ToString() + ", phase: " 
								+ phase.ToString(), gameObject);
						}
						// Set phase to 1, since we're done
						phase = 1.0f;
						// Switch currentState
						currentState = PulseState.Stopped;
					}
				}
			}
			else if (currentState == PulseState.FromTarget) {
				if (counter >= timeTargetInitial) {
					// Check if we're looping
					if (looping) {
						// Debug
						if (debugInfo) {
							Debug.Log("Switching currentState to ToTarget, counter: " + counter.ToString() + ", phase: " 
								+ phase.ToString(), gameObject);
						}
						// Set time to initial->target, less any overshoot
						counter = counter - timeTargetInitial;
						// Switch currentState
						currentState = PulseState.ToTarget;
					}
					else {
						// Debug
						if (debugInfo) {
							Debug.Log("Switching currentState to Stopped, counter: " + counter.ToString() + ", phase: " 
								+ phase.ToString(), gameObject);
						}
						// Set phase to 0, since we're done
						phase = 0.0f;
						// Switch currentState
						currentState = PulseState.Stopped;
					}
				}
			}

			// Calulate phase depending on whether we're (now) going to/from target
			if (currentState == PulseState.ToTarget) {
				phase = (counter / timeInitialTarget);
			}
			else if (currentState == PulseState.FromTarget) {
				phase = 1.0f - (counter / timeTargetInitial);
			}

			// Set color based on phase
			// Color is lerped between initial and target
			if (pulseMode == PulseMode.Linear) {
				matControl.SetColor(emissionId, Color.Lerp(emissionInitial, emissionTarget, phase));
			}
			else if (pulseMode == PulseMode.Sine) {
				matControl.SetColor(emissionId, Color.Lerp(emissionInitial, emissionTarget, Mathf.Sin(phase)));
			}
		}
	}
}
