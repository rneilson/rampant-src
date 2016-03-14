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

public class DelayedDeath : MonoBehaviour {

	public bool haltParticles = false;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void DieInTime (float time, GameObject sparker) {
		if (haltParticles) {
			var emission = GetComponent<ParticleSystem>().emission;
			emission.rate = new ParticleSystem.MinMaxCurve(0.0f);
		}

		StartCoroutine(DieDelay(time, sparker));
	}

	IEnumerator DieDelay (float delay, GameObject effect) {
		yield return new WaitForSeconds(delay);

		if (effect) {
			Destroy(Instantiate(effect, transform.position, Quaternion.Euler(-90.0f, 0.0f, 0.0f)), 1.0f);
		}

		Destroy(gameObject);
	}

}
