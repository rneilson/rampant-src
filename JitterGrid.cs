using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Vectrosity;

public class JitterGrid : MonoBehaviour {

	public bool debugInfo = false;
	public int cellsX = 8;
	public int cellsZ = 8;
	public float boundX = 5.0f;
	public float boundZ = 5.0f;
	public float jitterDist = 0.25f;
	public Color[] colors;
	public float lineWidth = 2.0f;
	public Texture lineTex;

	// Dimensions
	private int pointsX;
	private int pointsZ;
	private float stepX;
	private float stepZ;

	// Points and the lines between them
	private bool pointsUpdated = false;
	private JitterPoint[,] points;
	private List<VectorLine> horizontalLines;
	private List<VectorLine> verticalLines;
	// Might need this later
	// private VectorLine vectorPoints;

	private const float TwoPi = Mathf.PI * 2;

	void Awake () {
		// Get dimensions
		pointsX = cellsX + 1;
		pointsZ = cellsZ + 1;
		stepX = (2 * boundX) / (float) cellsX;
		stepZ = (2 * boundZ) / (float) cellsZ;

		// Initialize points array and line lists
		points = new JitterPoint[pointsX, pointsZ];
		horizontalLines = new List<VectorLine>(pointsZ);
		verticalLines = new List<VectorLine>(pointsX);

		// Need temp points lists to initialize lines
		var pointsHori = new List<List<Vector3>>(pointsZ);
		for (int z = 0; z < pointsZ; z++) {
			pointsHori.Add(new List<Vector3>(pointsX));
		}
		var pointsVert = new List<List<Vector3>>(pointsX);
		for (int x = 0; x < pointsX; x++) {
			pointsVert.Add(new List<Vector3>(pointsZ));
		}

		for (int z = 0; z < pointsZ; z++) {
			for (int x = 0; x < pointsX; x++) {
				// Get starting point
				Vector3 basePos = new Vector3(-boundX + ((float) x * stepX), transform.position.y, -boundZ + ((float) z * stepZ));
				// Get offset
				float angle = Random.Range(0, TwoPi);
				float dist = Random.Range(0, jitterDist);
				Vector3 offset = new Vector3(dist * Mathf.Cos(angle), 0.0f, dist * Mathf.Sin(angle));
				// Set position and base color
				points[x, z].position = basePos + offset;
				points[x, z].color = colors[0];
				// Add to temp lists
				pointsHori[z].Add(points[x, z].position);
				pointsVert[x].Add(points[x, z].position);
			}
		}

		// Initialize lines
		for (int z = 0; z < pointsZ; z++) {
			horizontalLines.Add(new VectorLine("Horizontal " + z.ToString(), pointsHori[z], 
				lineWidth, LineType.Continuous, Joins.Weld));
			horizontalLines[z].layer = LayerMask.NameToLayer("TransparentFX");
			horizontalLines[z].SetColor(colors[0]);
			horizontalLines[z].smoothColor = true;
			if (lineTex) {
				horizontalLines[z].texture = lineTex;
			}
		}

		for (int x = 0; x < pointsX; x++) {
			verticalLines.Add(new VectorLine("Vertical " + x.ToString(), pointsVert[x], 
				lineWidth, LineType.Continuous, Joins.Weld));
			verticalLines[x].layer = LayerMask.NameToLayer("TransparentFX");
			verticalLines[x].SetColor(colors[0]);
			verticalLines[x].smoothColor = true;
			if (lineTex) {
				verticalLines[x].texture = lineTex;
			}
		}

		if (debugInfo) {
			Debug.Log("Horizontal lines: " + horizontalLines.Count.ToString(), gameObject);
			Debug.Log("Vertical lines: " + verticalLines.Count.ToString(), gameObject);
		}

		// Set line colors
		if (debugInfo) {
			// Advance line color with each segment
			for (int z = 0; z < pointsZ; z++) {
				for (int x = 0; x <= pointsX; x++) {
					horizontalLines[z].SetColor(colors[x % colors.Length], x);
				}
			}
			for (int x = 0; x < pointsX; x++) {
				for (int z = 0; z <= pointsZ; z++) {
					verticalLines[x].SetColor(colors[z % colors.Length], z);
				}
			}
		}
	}

	void Start () {
		// Draw initial lines
		foreach (VectorLine line in horizontalLines) {
			line.Draw3D();
		}
		foreach (VectorLine line in verticalLines) {
			line.Draw3D();
		}
	}
	
	void Update () {
		// Only update lines if points updated
		if (pointsUpdated) {
			foreach (VectorLine line in horizontalLines) {
				line.Draw3D();
			}
			foreach (VectorLine line in verticalLines) {
				line.Draw3D();
			}
		}
	}
}

public struct JitterPoint {
	public Vector3 position;
	public Color32 color;
}
