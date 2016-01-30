using UnityEngine;
using System.Collections;

// TODO: obsolete? (Is anything still using this?)
public class MaterialShift : MonoBehaviour {

	// Material list and parameters
	public Material materialBase;	// Public for debug
	public Material materialTarget;	// Public for debug
	public Material[] materialTargetList;
	public float timeToTarget = 0.5f;
	public float timeFromTarget = 0.5f;
	private bool returnToBase = true;	// Always return to base for now
	public bool debugInfo = false;

	private Renderer rendControl;
	private float counter = 0.0f;
	private float phase = 0.0f;
	private PulseState currentState = PulseState.Stopped;
	private bool isActive = false;
	private int emissionId;
	//private Color emissionBase;
	private Color emissionTarget;

	// Use this for initialization
	void Start () {
		// Get material object from renderer
		rendControl = GetComponent<Renderer>();
		// Enable emissions, just in case
		rendControl.material.EnableKeyword("_EMISSION");
		emissionId = Shader.PropertyToID("_EmissionColor");
		//emissionBase = materialBase.GetColor(emissionId);
	
		// Sanity check on materials array
		if (materialTargetList.Length <= 0) {
			Debug.LogError("No materials in shift array!", gameObject);
		}

		// Set as active
		isActive = true;
	}
	
	// Update is called once per frame
	void Update () {
		if ((isActive) && (currentState != PulseState.Stopped)) {

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
				if (counter >= timeToTarget) {
					if (returnToBase) {
						if (debugInfo) {
							Debug.Log("Switching currentState to FromTarget, counter: " + counter.ToString() + ", phase: " 
								+ phase.ToString(), gameObject);
							Debug.Log("materialBase: " + materialBase.ToString() + ", materialTarget: " 
								+ materialTarget.ToString() 
								+ ", rendControl.material: " + rendControl.material.ToString(), gameObject);
						}
						// Set time, less any overshoot
						counter = counter - timeToTarget;
						// Switch currentState
						currentState = PulseState.FromTarget;
					}
					else {
						if (debugInfo) {
							Debug.Log("Switching currentState to Stopped, counter: " + counter.ToString() + ", phase: " 
								+ phase.ToString(), gameObject);
							Debug.Log("materialBase: " + materialBase.ToString() + ", materialTarget: " 
								+ materialTarget.ToString() 
								+ ", rendControl.material: " + rendControl.material.ToString(), gameObject);
						}
						// Set phase to 1, since we're done
						phase = 1.0f;
						// Switch currentState
						currentState = PulseState.Stopped;
						// Set material to target
						rendControl.material = materialTarget;
					}
				}
			}
			else if (currentState == PulseState.FromTarget) {
				if (counter >= timeFromTarget) {
					if (debugInfo) {
						Debug.Log("Switching currentState to Stopped, counter: " + counter.ToString() + ", phase: " 
							+ phase.ToString(), gameObject);
						Debug.Log("materialBase: " + materialBase.ToString() + ", materialTarget: " + materialTarget.ToString() 
							+ ", rendControl.material: " + rendControl.material.ToString(), gameObject);
					}
					// Set phase to 0, since we're done
					phase = 0.0f;
					// Switch currentState
					currentState = PulseState.Stopped;
					// Set material back to base
					rendControl.material = materialBase;
				}
			}

			// Set lerp fraction
			if (currentState == PulseState.ToTarget) {
				phase = counter / timeToTarget;
				UpdateMaterial();
			}
			else if (currentState == PulseState.FromTarget) {
				phase = 1.0f - (counter / timeFromTarget);
				UpdateMaterial();
			}
		}
	}

	// Begin shift to specified index in materials array
	public void BeginShift (int shiftIndex) {
		if ((isActive) && (currentState == PulseState.Stopped)) {
			// Sanity check
			if (shiftIndex >= materialTargetList.Length) {
				Debug.LogError("BeginShift() called with out-of-bounds index", gameObject);
			}

			// Set material at index as target
			materialTarget = materialTargetList[shiftIndex];
			// Update target emission color
			emissionTarget = materialTarget.GetColor(emissionId);
			// Set current material to target (hopefully this'll be lerped properly)
			rendControl.material = materialTarget;
			// Enable emissions
			//rendControl.material.EnableKeyword("_EMISSION");

			// Set counters and go
			counter = 0.0f;
			phase = 0.0f;
			currentState = PulseState.Starting;

			if (debugInfo) {
				Debug.Log("Beginning shift, index " + shiftIndex.ToString() 
					+ ", target color " + emissionTarget.ToString(), gameObject);
				/*Debug.Log("materialBase: " + materialBase.ToString() + ", materialTarget: " + materialTarget.ToString() 
					+ ", rendControl.material: " + rendControl.material.ToString(), gameObject);*/
			}
		}
	}

	// Update and lerp material properties
	void UpdateMaterial () {
		// Lerp materials
		rendControl.material.Lerp(materialBase, materialTarget, phase);
	}

	// Placeholder
	public void GameStarted () {

	}
}

