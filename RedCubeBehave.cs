using UnityEngine;
using System.Collections;

public class RedCubeBehave : MonoBehaviour {
	
	private GameObject target;
	private Scorer scorer;
	private float speed = 10f;
	private float drag = 4f;
	private Vector3 bearing;
	private bool dead;
	private bool loud;
	
	public GameObject burster;
	public GameObject bursterQuiet;

	// Use this for initialization
	void Start () {
		target = GameObject.FindGameObjectWithTag("Player");
		scorer = GameObject.FindGameObjectWithTag("GameController").GetComponent<Scorer>();
		rigidbody.drag = drag;
		dead = false;
		loud = false;
	}
	
	// Update is called once per frame
	void Update () {
		if (dead)
			BlowUp();
	}
	
	//Put movement in FixedUpdate
	void FixedUpdate () {
		if (target) {
			bearing = target.transform.position - transform.position;
			rigidbody.AddForce(bearing.normalized * speed);
		}
	}
	
	void BlowUp () {
		if (loud) {
			Destroy(Instantiate(burster, transform.position, Quaternion.Euler(0, 0, 0)), 1);
		}
		else {
			Destroy(Instantiate(bursterQuiet, transform.position, Quaternion.Euler(0, 0, 0)), 1);
		}
		scorer.AddKill();
		Destroy(gameObject);
	}
	
	void Clear () {
		Destroy(Instantiate(bursterQuiet, transform.position, Quaternion.Euler(0, 0, 0)), 1);
		Destroy(gameObject);
	}
	
	void Die (bool loudly) {
		if (!dead)
			dead = true;
		if (loudly)
			loud = true;
		else
			loud = false;
		//collider.enabled = false;
	}
	
	void ClearTarget () {
		target  = null;
	}
	
	void NewTarget (GameObject newTarget) {
		target = newTarget;
	}
	
	// On collision
	/* void OnCollisionEnter(Collision collision) {
		GameObject thingHit = collision.gameObject;
		
		if (thingHit.tag == "Bullet" && dead == false) {
			dead = true;
			BlowUp(true);
		}
	} */
}
