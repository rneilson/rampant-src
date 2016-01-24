using UnityEngine;
using System.Collections;

public class CursorMovement : MonoBehaviour {

	// Max displacement values
	public float xmax;
	public float zmax;
	
	public float mouseSpeed;	// Mouse speed control

	private Vector3 initialPos;	// Initial position

	// Use this for initialization
	void Start () {
		// Hide at start
		HideCursor();

		// Get initial position
		initialPos = transform.position;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void FixedUpdate () {
	}

	void LateUpdate () {
		float xmove, zmove, xpos, zpos;

		// Get input
		xmove = Input.GetAxis("Mouse X");
		zmove = Input.GetAxis("Mouse Y");

		// If mouse moved, unhide cursor and move it around
		if (xmove != 0.0f || zmove != 0.0f) {
			// Unhide cursor
			UnhideCursor();

			// Find new positions
			xpos = transform.position.x + (xmove * mouseSpeed);
			zpos = transform.position.z + (zmove * mouseSpeed);

			// Clip to max/min
			if (xpos > xmax){
				xpos = xmax;
			}
			else if (xpos < -xmax){
				xpos = -xmax;
			}
			if (zpos > zmax) {
				zpos = zmax;
			}
			else if (zpos < -zmax) {
				zpos = -zmax;
			}

			// Set new position
			transform.position = new Vector3(xpos, initialPos.y, zpos);
		}
	}

	void HideCursor () {
		// Layer 11 is "Hidden"
		gameObject.layer = 11;
	}

	void UnhideCursor () {
		// Layer 1 is "TransparentFX"
		gameObject.layer = 1;
	}
}
