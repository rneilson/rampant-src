using UnityEngine;
using System.Collections;

public class RespawnOrb : MonoBehaviour {

	private Scorer scorer;

	// Use this for initialization
	void Start () {
		// Get scorer
		scorer = GameObject.Find("Scorekeeper").GetComponent<Scorer>();

		// Set death time
		Destroy(gameObject, scorer.RespawnCountdown);

		// Set velocity
		Rigidbody rb = GetComponent<Rigidbody>();
		if (rb) {
			rb.velocity = (scorer.SpawnPosition - transform.position) / scorer.RespawnCountdown;
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
