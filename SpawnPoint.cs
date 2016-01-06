using UnityEngine;
using System.Collections;

public class SpawnPoint : MonoBehaviour {
	
	public GameObject enemyType;
	public float preTime;
	public float postTime;
	
	private float spawnTime;
	private bool spawned;

	// Use this for initialization
	void Start () {
		spawnTime = Time.fixedTime + preTime;
		spawned = false;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void FixedUpdate () {
		if (spawned == false && Time.fixedTime >= spawnTime) {
			GetComponent<Collider>().enabled = false;
			Instantiate(enemyType, transform.position, transform.rotation);
			if (postTime > 0)
				Destroy(gameObject, postTime);
			else
				Destroy(gameObject);
			spawned = true;
		}
	}
	
}
