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
			thingHit.SendMessage("Die", true);
		}
		
		Destroy(gameObject);
	}
}
