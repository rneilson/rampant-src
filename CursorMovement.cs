using UnityEngine;
using System.Collections;

// TODO: don't unhide cursor until game started
public class CursorMovement : MonoBehaviour {

	// Max displacement values
	public float xmax;
	public float zmax;

	// Mouse speed control
	public float mouseSpeed;

	// Initial position
	private Vector3 initialPos;

	// Layers to use
	int showLayer;
	int hideLayer;

	// Scorer/input
	private Scorer scorer;

	public float MouseSpeed {
		get { return mouseSpeed; }
		set { mouseSpeed = value; }
	}

	// Use this for initialization
	void Start () {
		showLayer = LayerMask.NameToLayer("TransparentFX");
		hideLayer = LayerMask.NameToLayer("Hidden");
		scorer = GameObject.Find("Scorekeeper").GetComponent<Scorer>();

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
		// Only move if game running (may change depending on (forthcoming) input class)
		if ((!scorer.IsPaused) && (scorer.IsStarted)) {
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
	}

	void HideCursor () {
		gameObject.layer = hideLayer;
	}

	void UnhideCursor () {
		gameObject.layer = showLayer;
	}
}
