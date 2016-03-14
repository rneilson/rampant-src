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

public class BulletCollider : MonoBehaviour {
	
	public GameObject sparker;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	// On collision
	void OnCollisionEnter(Collision collision) {
		GameObject thingHit = collision.gameObject;
		ContactPoint contact = collision.contacts[0];
		
		// Layer 13 is undergrid (bullet fence too)
		if (thingHit.layer != 13) {
			Destroy(Instantiate(sparker, contact.point, Quaternion.LookRotation(contact.normal)), 0.5f);
		} 
				
		if (thingHit.tag == "Enemy") {
			thingHit.SendMessage("Die", true, SendMessageOptions.DontRequireReceiver);
		}
		
		Destroy(gameObject);
	}
}
