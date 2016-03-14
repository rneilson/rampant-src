//  Copyright 2016 Raymond Neilson
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

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
