using UnityEngine;
using System.Collections;

// TODO: add reset method
// TODO: make use PulseControl (or PulseTimer?)
public class MaterialPulse : MonoBehaviour {

	public Color emissionColor = Color.white;
	public float emissionFraction = 1.0f;
	public float timeInitialTarget = 0.5f;
	public float timeTargetInitial = 0.5f;
	public bool autoStart = true;
	public bool returnToInitial = true;
	public bool looping = false;
	public bool debugInfo = false;
	public PulseMode pulseMode = PulseMode.Sine;
	public Color blendColor = Color.white;
	public bool blendIncoming = true;

	private Material matControl;
	private int emissionId;
	private Color emissionInitial;
	private Color emissionTarget;
	private Color emissionFinal;
	private float counter;
	private float phase;
	private float timeTargetFinal;
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

		// Set final values now, which might be updated if interrupted
		emissionFinal = emissionInitial;
		timeTargetFinal = timeTargetInitial;

		// Initialize to stopped
		currentState = PulseState.Stopped;
		
		// Start only if autostarting
		if (autoStart) {
			StartPulse();
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
						// Set now-terminal color
						SetColorByPhase(emissionInitial, emissionTarget);
					}
				}
			}
			else if (currentState == PulseState.FromTarget) {
				if (counter >= timeTargetFinal) {
					// Check if we're looping
					if (looping) {
						// Debug
						if (debugInfo) {
							Debug.Log("Switching currentState to ToTarget, counter: " + counter.ToString() + ", phase: " 
								+ phase.ToString(), gameObject);
						}
						// Set time to initial->target, less any overshoot
						counter = counter - timeTargetFinal;
						// Switch currentState
						currentState = PulseState.ToTarget;
						// In case we've been updated, set new initial value
						emissionInitial = emissionFinal;
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
						// Set now-terminal color
						SetColorByPhase(emissionFinal, emissionTarget);
					}
				}
			}

			// Calulate phase and current color depending on whether we're (now) going to/from target
			if (currentState == PulseState.ToTarget) {
				phase = counter / timeInitialTarget;
				// Set color based on phase
				SetColorByPhase(emissionInitial, emissionTarget);

				/* Old code, cleanup
				// Color is lerped between initial and target
				if (pulseMode == PulseMode.Linear) {
					matControl.SetColor(emissionId, Color.Lerp(emissionInitial, emissionTarget, phase));
				}
				else if (pulseMode == PulseMode.Sine) {
					matControl.SetColor(emissionId, Color.Lerp(emissionInitial, emissionTarget, Mathf.Sin(phase)));
				}
				*/
			}
			else if (currentState == PulseState.FromTarget) {
				phase = 1.0f - (counter / timeTargetFinal);
				// Set color based on phase
				SetColorByPhase(emissionFinal, emissionTarget);

				/* Old code, cleanup
				// Color is lerped between final and target
				if (pulseMode == PulseMode.Linear) {
					matControl.SetColor(emissionId, Color.Lerp(emissionFinal, emissionTarget, phase));
				}
				else if (pulseMode == PulseMode.Sine) {
					matControl.SetColor(emissionId, Color.Lerp(emissionFinal, emissionTarget, Mathf.Sin(phase)));
				}
				*/
			}

		}
	}

	// Set color according to current phase
	void SetColorByPhase (Color baseColor, Color targetColor) {
		// Uses current phase value
		// Color is lerped between final and target
		if (pulseMode == PulseMode.Linear) {
			matControl.SetColor(emissionId, Color.Lerp(baseColor, targetColor, phase));
		}
		else if (pulseMode == PulseMode.Sine) {
			matControl.SetColor(emissionId, Color.Lerp(baseColor, targetColor, Mathf.Sin(phase * halfPi)));
		}
	}

	// Start pulse cycle
	void StartPulse () {
		// Only start if we're stopped
		if (currentState == PulseState.Stopped) {
			currentState = PulseState.Starting;
			phase = 0.0f;
			counter = 0.0f;
			if (debugInfo) {
				Debug.Log("Starting pulse, counter: " + counter.ToString() + ", phase: " 
					+ phase.ToString(), gameObject);
				Debug.Log("Initial color: " + emissionInitial.ToString() 
					+ ", Target color: " + emissionTarget.ToString()
					+ ", Final color: " + emissionFinal.ToString(), gameObject);
			}
		}
		else {
			// We should really never be here
			Debug.LogError("StartPulse() called on running MaterialPulse!", gameObject);
		}
	}

	// Various ways and means of setting a new target (and optionally a new final)
	// Presumes autostarting, of course -- wouldn't be called otherwise

	// Starts a new pulse, with a new target, a new final, and new counters for each
	// Other variants will call this one
	public void NewPulse (Color targetColor, float targetFraction, float timeToTarget, 
		Color finalColor, float finalFraction, float timeToFinal, bool loopNewValues)
	{
		// Set initial to current color
		emissionInitial = matControl.GetColor(emissionId);

		// Set new target values
		emissionTarget = targetColor * targetFraction;
		timeInitialTarget = timeToTarget;

		// Set new final values
		emissionFinal = finalColor * finalFraction;
		timeTargetFinal = timeToFinal;

		// Set looping status
		looping = loopNewValues;

		/* No, don't assume that!
		// Yes, assume we're returning to initial (well, new final (there's a one-way function later))
		returnToInitial = true;
		*/

		// Start pulse with new values
		currentState = PulseState.Stopped;
		StartPulse();
	}

	// Starts a new pulse, but with current final values
	public void NewPulse (Color targetColor, float targetFraction, float timeToTarget, bool loopNewValues) {
		NewPulse(targetColor, targetFraction, timeToTarget, emissionFinal, 1.0f, timeTargetFinal, loopNewValues);
	}

	// Starts a new pulse, but with current final values and current times
	public void NewPulse (Color targetColor, float targetFraction, bool loopNewValues) {
		NewPulse(targetColor, targetFraction, timeInitialTarget, emissionFinal, 1.0f, timeTargetFinal, loopNewValues);
	}

	// Starts a new pulse, changes only the color (lazy!)
	public void NewPulse (Color targetColor) {
		NewPulse(targetColor, 1.0f, timeInitialTarget, emissionFinal, 1.0f, timeTargetFinal, looping);
	}

	// Wrapper function, for use with SendMessage(), because apparently it doesn't do overloaded methods
	public void NewPulseMsg (Color targetColor) {
		Color newTarget = (blendIncoming) ? blendColor * targetColor : targetColor;
		NewPulse(newTarget, 1.0f, timeInitialTarget, emissionFinal, 1.0f, timeTargetFinal, looping);
	}

	// Starts a new pulse with no return/final value
	public void NewPulseNoReturn (Color targetColor, float targetFraction, float timeToTarget) {
		// Set initial to current color
		emissionInitial = matControl.GetColor(emissionId);

		// Set new target values
		emissionTarget = targetColor * targetFraction;
		timeInitialTarget = timeToTarget;

		// Set looping status
		looping = false;

		// This is a one-way trip
		returnToInitial = false;

		// Start pulse with new values
		currentState = PulseState.Stopped;
		StartPulse();
	}

	// There may be a time when you're lazy enough to do a one-way and not know what time to use
	// So we default to timeInitialTarget
	public void NewPulseNoReturn (Color targetColor) {
		NewPulseNoReturn(targetColor, 1.0f, timeInitialTarget);
	}

	// Placeholder
	public void GameStarted () {

	}
}
