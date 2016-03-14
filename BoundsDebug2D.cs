using UnityEngine;
using System.Collections;

// To debug screen pos/size
public class BoundsDebug2D : MonoBehaviour {

	private Renderer rend;
	private Bounds boxBounds;

	// Selectable draw color
	public Color drawColor = Color.cyan;
	public int rows;
	public int cols;

	// Public for debug (damn Unity inspector not exposing properties!)
	public Vector3 boxCenter;
	public Vector3 boxExtents;
	public Vector3 boxSize;
	public Vector3 boxTopLeft;
	public Vector3 boxTopRight;
	public Vector3 boxBottomLeft;
	public Vector3 boxBottomRight;
	public Vector3 boxPerLine;
	public Vector3 boxPerChar;

	// Use this for initialization
	void Start () {
		if (!rend) {
			// Get renderer for future reference
			rend = GetComponent<Renderer>();
		}

		// Print initial bounds
		Debug.Log("Bounds for object " + gameObject.name);
		boxBounds = rend.bounds;
		Debug.Log("Render bounds: " + boxBounds.ToString(), gameObject);
		UpdateBounds(boxBounds);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnDrawGizmos () {
		if (rend) {
			//if (rend.bounds != boxBounds) {
				boxBounds = rend.bounds;
				UpdateBounds(boxBounds);
			//}
			DrawBoxLines();
		}
		else {
			// Get renderer for future reference
			rend = GetComponent<Renderer>();
		}
	}

	void UpdateBounds (Bounds newBounds) {
		boxCenter = newBounds.center;
		boxExtents = newBounds.extents;
		boxSize = newBounds.size;
		boxTopLeft = new Vector3(boxCenter.x - boxExtents.x, boxCenter.y, boxCenter.z + boxExtents.z);
		boxTopRight = new Vector3(boxCenter.x + boxExtents.x, boxCenter.y, boxCenter.z + boxExtents.z);
		boxBottomLeft = new Vector3(boxCenter.x - boxExtents.x, boxCenter.y, boxCenter.z - boxExtents.z);
		boxBottomRight = new Vector3(boxCenter.x + boxExtents.x, boxCenter.y, boxCenter.z - boxExtents.z);

		// Update the per-line and per-char sizes (have to set rows/cols to be accurate)
		boxPerLine = new Vector3(boxSize.x, 0f, boxSize.z / (float) rows);
		boxPerChar = new Vector3(boxPerLine.x / (float) cols, 0f, boxPerLine.z);
	}

	void DrawBoxLines () {
		Gizmos.color = drawColor;
		Gizmos.DrawLine(boxTopLeft, boxTopRight);
		Gizmos.DrawLine(boxTopRight, boxBottomRight);
		Gizmos.DrawLine(boxBottomRight, boxBottomLeft);
		Gizmos.DrawLine(boxBottomLeft, boxTopLeft);
	}
}
