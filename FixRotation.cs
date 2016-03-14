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

public class FixRotation : MonoBehaviour {

	public Vector3 targetRotation;

	private Quaternion targetRot;

	// Use this for initialization
	void Start () {
		targetRot = Quaternion.Euler(targetRotation);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void LateUpdate () {
		transform.rotation = targetRot;
	}
}
