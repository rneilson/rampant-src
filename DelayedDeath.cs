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
