using UnityEngine;
using System.Collections;

public class LightBurst : MonoBehaviour {

	public float maxIntensity;
	public int rampUpFrames;
	public int rampDownFrames;

	private Light lightControl;
	private float startIntensity;
	private int currentFrame = 0;
	private int maxFrames;
	private float rampUpInterval;
	private float rampDownInterval;

	// Use this for initialization
	void Start () {
		lightControl = GetComponent<Light>();
		startIntensity = lightControl.intensity;
		maxFrames = rampUpFrames + rampDownFrames;
		rampUpInterval = (maxIntensity - startIntensity) / ((float) rampUpFrames);
		rampDownInterval = maxIntensity / ((float) rampDownFrames);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	// Everything in FixedUpdate, natch
	void FixedUpdate () {
		if (currentFrame <= maxFrames) {
			if (currentFrame <= rampUpFrames) {
				lightControl.intensity += (rampUpInterval * ((float) currentFrame));
			}
			else if (currentFrame <= maxFrames) {
				lightControl.intensity -= (rampDownInterval * ((float) (currentFrame - rampUpFrames)));
			}
			currentFrame++;
		}
	}
}
