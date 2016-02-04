using UnityEngine;
using System.Collections;

public class DelayedDeath : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void DieInTime (float time, GameObject sparker) {
		StartCoroutine(DieDelay(time, sparker));
	}

	IEnumerator DieDelay (float delay, GameObject effect) {
		yield return new WaitForSeconds(delay);
		Destroy(Instantiate(effect, transform.position, Quaternion.Euler(-90.0f, 0.0f, 0.0f)), 1.0f);
		Destroy(gameObject);
	}

}
