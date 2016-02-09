using UnityEngine;
using System.Collections;

public class Shrapnel : MonoBehaviour {

	public GameObject shrapnelPiece;
	public GameObject shrapnelSpark;
	public int piecesCount = 3;
	public float explosionForce = 20f;
	public float bombOffset = 0.02f;
	public float bombHeight = -0.02f;
	public float placementRadius = 0.05f;
	public float destroyDelay = 0.5f;
	public bool selfDestruct = false;

	private bool fired = false;

	// Use this for initialization
	void Start () {

	
	}
	
	// Update is called once per frame
	void Update () {
		if (!fired) {
			fired = true;

			// See how far from center we're instantiating
			Vector3 offset = new Vector3(placementRadius, 0f, 0f);
			// Get an initial rotation, plus a per-piece rotation
			Quaternion initialRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
			Quaternion pieceRot = Quaternion.Euler(0f, 360f / (float) piecesCount, 0f);
			Vector3 initialVec = initialRot * offset;

			for (int i = 0; i < piecesCount; i++) {
				// This is to find each piece's rotated position
				Vector3 instVec = initialVec;
				// Unity doesn't really...do...multiplying a quaternion by a scalar
				// (Few things can't be brute-forced by a for loop)
				for (int j = 0; j < i; j++) {
					instVec = pieceRot * instVec;
				}

				// Instantiate our shrapnel piece
				GameObject piece = Instantiate(shrapnelPiece, transform.position + instVec, 
					Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f))) as GameObject;

				// Send them on their way
				Vector3 bombPos = transform.position + new Vector3(Random.Range(-bombOffset, bombOffset), 
					bombHeight, Random.Range(-bombOffset, bombOffset));
				piece.GetComponent<Rigidbody>().AddExplosionForce(explosionForce, bombPos, 0f);
				
				// Try using the cool way
				DelayedDeath dd = piece.GetComponent<DelayedDeath>();
				if (dd) {
					// Kill in time
					dd.DieInTime(destroyDelay, shrapnelSpark);
				}
				else {
					// Aw, too bad, no pretties
					Destroy(piece, destroyDelay);
				}
			}


			// Now clean ourselves up
			if (selfDestruct) {
				Destroy(gameObject, destroyDelay);
			}
		}
	
	}
}
