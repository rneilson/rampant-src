using UnityEngine;
using System.Collections;

public class MaterialShift : MonoBehaviour {

	// Material list and parameters
	public Material[] materials;
	public float shiftTime = 0.5f;
	public bool debugInfo = false;

	private Renderer rendControl;
	public Material matBase;	// Public for debug
	public Material matTarget;	// Public for debug
	private float counter = 0.0f;
	private float phase = 0.0f;
	private PulseState currentState = PulseState.Stopped;
	private bool isActive = false;

	// Use this for initialization
	void Start () {
		// Get material object from renderer
		rendControl = GetComponent<Renderer>();
		// Enable emissions, just in case
		rendControl.material.EnableKeyword("_EMISSION");
	
		// Sanity check on materials array
		if (materials.Length <= 0) {
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
				if (counter >= shiftTime) {
					// Debug
					if (debugInfo) {
						Debug.Log("Switching currentState to Stopped, counter: " + counter.ToString() + ", phase: " 
							+ phase.ToString(), gameObject);
						Debug.Log("matBase: " + matBase.ToString() + ", matTarget: " + matTarget.ToString() 
							+ ", rendControl.material: " + rendControl.material.ToString(), gameObject);
					}
					// Set phase to 1, since we're done
					phase = 1.0f;
					// Switch currentState
					currentState = PulseState.Stopped;
					// Set now-terminal material
					rendControl.material = matTarget;
				}
				else {
					phase = counter / shiftTime;
					rendControl.material.Lerp(matBase, matTarget, phase);
				}
			}
		}
	}

	// Begin shift to specified index in materials array
	public void BeginShift (int shiftIndex) {
		if (isActive) {
			// Sanity check
			if (shiftIndex >= materials.Length) {
				Debug.LogError("BeginShift() called with out-of-bounds index", gameObject);
			}

			// Stash copy of present material as basis
			//matBase = rendControl.material;

			// Might need to do this instead if materials aren't instantiated like I think they are
			matBase = Material.Instantiate(rendControl.material);

			// Set material at index as target
			matTarget = materials[shiftIndex];

			// Set counters and go
			float counter = 0.0f;
			float phase = 0.0f;
			currentState = PulseState.Starting;

			if (debugInfo) {
				Debug.Log("Beginning shift, index " + shiftIndex.ToString(), gameObject);
				/*Debug.Log("matBase: " + matBase.ToString() + ", matTarget: " + matTarget.ToString() 
					+ ", rendControl.material: " + rendControl.material.ToString(), gameObject);*/
			}
		}
	}
}

