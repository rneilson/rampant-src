using UnityEngine;
using System.Collections;

// TODO: add reset method
// TODO: make use PulseControl (or PulseTimer?)
public class MaterialShiftEmission : MonoBehaviour {

	// Material list and parameters
	public Color[] colorTargetList;
	public Texture[] textureTargetList;
	public float timeFade = 0.5f;
	public bool debugInfo = false;

	private Renderer rendControl;
	private PulseControl timer;
	private PulseState direction;
	private bool isActive;

	private int emissionIdColor;
	private int emissionIdTexture;
	private Color colorBase;
	private Color colorMid = Color.black;
	private Color colorTarget;
	private Texture textureTarget;

	// Use this for initialization
	void Start () {
		// Get material object from renderer
		rendControl = GetComponent<Renderer>();
		// Enable emissions, just in case
		rendControl.material.EnableKeyword("_EMISSION");
		emissionIdColor = Shader.PropertyToID("_EmissionColor");
		emissionIdTexture = Shader.PropertyToID("_EmissionMap");

		// Get timer (settings don't matter, we'll be overriding them)
		timer = GetComponent<PulseControl>();
		// Direction is nowhere just yet
		direction = PulseState.Stopped;
	
		// Sanity check on materials array
		if ((colorTargetList.Length <= 0) || (textureTargetList.Length <= 0)) {
			Debug.LogError("No targets in shift array!", gameObject);
		}

		// Set as active
		isActive = true;
	}
	
	// Update is called once per frame
	void Update () {
		if (isActive) {
			if (timer.IsStarted) {
				// If running (well, started, but we shouldn't ever actually run into that), just update color
				UpdateMaterial(timer.Phase);
			}
			else {
				// If we're fading out, and we've reached it
				if (direction == PulseState.ToTarget) {
					// Set to black and swap out emission texture if req'd
					rendControl.material.SetColor(emissionIdColor, colorMid);
					if (rendControl.material.GetTexture(emissionIdTexture) != textureTarget) {
						rendControl.material.SetTexture(emissionIdTexture, textureTarget);
					}
					// Start new pulse
					timer.NewPulse(timeFade, 0.0f, 0.0f, 0.0f, false, false, true, PulseMode.Linear);
					direction = PulseState.FromTarget;
				}
				// If we're fading in, and likewise
				else if (direction == PulseState.FromTarget) {
					// Set to full new emission color
					rendControl.material.SetColor(emissionIdColor, colorTarget);
					// Stop
					direction = PulseState.Stopped;
				}
				// direction == PulseState.Stopped is handled by doing sweet fuck all
			}
		}
	}

	// Begin shift to specified index in materials array
	public void BeginShift (int shiftIndex) {
		if (isActive) {
			// Sanity check
			if ((shiftIndex >= colorTargetList.Length) || (shiftIndex >= textureTargetList.Length)) {
				Debug.LogError("BeginShift() called with out-of-bounds index", gameObject);
			}

			// Update target emission color and texture
			colorTarget = colorTargetList[shiftIndex];
			textureTarget = textureTargetList[shiftIndex];

			// Set base color to current
			colorBase = rendControl.material.GetColor(emissionIdColor);

			// Set counters and go
			direction = PulseState.ToTarget;
			timer.NewPulse(timeFade, 0.0f, 0.0f, 0.0f, false, false, true, PulseMode.Linear);

			if (debugInfo) {
				Debug.Log("Beginning shift, index " + shiftIndex.ToString(), gameObject);
			}
		}
	}

	// Update and lerp material properties
	void UpdateMaterial (float phase) {
		if (direction == PulseState.ToTarget) {
			// Lerp materials
			rendControl.material.SetColor(emissionIdColor, Color.Lerp(colorBase, colorMid, phase));
		}
		else if (direction == PulseState.FromTarget) {
			// Lerp materials
			rendControl.material.SetColor(emissionIdColor, Color.Lerp(colorMid, colorTarget, phase));
		}
	}

	// Placeholder
	public void GameStarted () {

	}
}

