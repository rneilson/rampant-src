using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//[ExecuteInEditMode]
public class JitterGrid : MonoBehaviour {

	public bool debugInfo = false;
	public int cellsX = 8;
	public int cellsZ = 8;
	public float boundX = 5.0f;
	public float boundZ = 5.0f;
	public float jitterMin = 0.25f;
	public float jitterMax = 0.5f;
	public Color[] colors;
	public Mesh lineMesh;
	public Material lineMat;
	public float lineWidth = 2.0f;
	public Vector3 lineRot = Vector3.zero;

	// Dimensions
	private int pointsX;
	private int pointsZ;
	private float stepX;
	private float stepZ;

	// Points and the lines between them
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
				float dist = Random.Range(jitterMin, jitterMax);
				Vector3 offset = new Vector3(dist * Mathf.Cos(angle), 0.0f, dist * Mathf.Sin(angle));
				// Set position and base color
				points[x, z].position = basePos + offset;
				points[x, z].colorIndex = 0;
				// Add to temp lists
				pointsHori[z].Add(points[x, z].position);
				pointsVert[x].Add(points[x, z].position);
			}
		}

		// Initialize lines
		int useLayer = LayerMask.NameToLayer("TransparentFX");
		for (int z = 0; z < pointsZ; z++) {
			horizontalLines.Add(new VectorLine(pointsHori[z], lineMesh, lineMat, lineWidth, lineRot, useLayer, colors[0]));
		}

		for (int x = 0; x < pointsX; x++) {
			verticalLines.Add(new VectorLine(pointsVert[x], lineMesh, lineMat, lineWidth, lineRot, useLayer, colors[0]));
		}

		if (debugInfo) {
			Debug.Log("Horizontal lines: " + horizontalLines.Count.ToString(), gameObject);
			Debug.Log("Vertical lines: " + verticalLines.Count.ToString(), gameObject);
		}

		// Set line colors
		/*
		if (debugInfo) {
			// Advance line color with each segment
			for (int z = 0; z < pointsZ; z++) {
				int indexBase = z % colors.Length;
				for (int x = 0; x <= pointsX; x++) {
					horizontalLines[z].SetColor(colors[(indexBase + x) % colors.Length], x);
				}
			}
			for (int x = 0; x < pointsX; x++) {
				int indexBase = x % colors.Length;
				for (int z = 0; z <= pointsZ; z++) {
					verticalLines[x].SetColor(colors[(indexBase + z) % colors.Length], z);
				}
			}
		}
		*/
	}

	void Start () {}
	
	void Update () {
		foreach (VectorLine line in horizontalLines) {
			line.Draw3D();
		}
		foreach (VectorLine line in verticalLines) {
			line.Draw3D();
		}
	}

	/*
	void OnDrawGizmos () {
		if (debugInfo) {
			foreach (VectorLine line in horizontalLines) {
				line.Draw3D();
			}
			foreach (VectorLine line in verticalLines) {
				line.Draw3D();
			}
		}
	}
	*/
}

public struct JitterPoint {
	public Vector3 position;
	public int colorIndex;
}

// TODO: generate mesh instead of passing Unity quad down three levels
public struct VectorLineSegment {
	public Mesh mesh;
	//public Matrix4x4 matrix;

	public VectorLineSegment (Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Color32 color) {
		//this.mesh = (Mesh) Object.Instantiate(mesh);
		Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);

		// Create mesh
		this.mesh = new Mesh();
		Vector3[] newVertices = new Vector3[4];
		Vector2[] newUV = new Vector2[4];
		// Simple quad:
		// 1-3
		// |\|
		// 0-2
		newVertices[0] = new Vector3(-0.5f, -0.5f, 0f);
		newVertices[1] = new Vector3(-0.5f, 0.5f, 0f);
		newVertices[2] = new Vector3(0.5f, -0.5f, 0f);
		newVertices[3] = new Vector3(0.5f, 0.5f, 0f);

		// Apply the matrix to the new vertices
		for (int i = 0; i < newVertices.Length; i++) {
			newVertices[i] = matrix.MultiplyPoint3x4(newVertices[i]);
		}

		// Generate UVs
		newUV[0] = new Vector2(0f, 0f);
		newUV[1] = new Vector2(0f, 1f);
		newUV[2] = new Vector2(1f, 0f);
		newUV[3] = new Vector2(1f, 1f);

		// Assign stuff
		this.mesh.vertices = newVertices;
		this.mesh.uv = newUV;
		this.mesh.triangles = new int[] {0, 1, 2, 3, 2, 1};

		// Let the engine do the normals
		this.mesh.RecalculateNormals();

		// Assign base color
		Color32[] newColors = new Color32[this.mesh.vertices.Length];
		for (int i = 0; i < newColors.Length; i++) {
			newColors[i] = color;
		}
		this.mesh.colors32 = newColors;
	}

	// For now assuming standard Unity quad as mesh
	public void SetStartColor (Color32 color) {
		Color32[] newColors = this.mesh.colors32;
		newColors[0] = color;
		newColors[1] = color;
		this.mesh.colors32 = newColors;
	}

	// For now assuming standard Unity quad as mesh
	public void SetEndColor (Color32 color) {
		Color32[] newColors = this.mesh.colors32;
		newColors[2] = color;
		newColors[3] = color;
		this.mesh.colors32 = newColors;
	}
}

public class VectorLine {
	private List<VectorLineSegment> segments;
	private Mesh baseMesh;
	private Quaternion baseRot;
	private float baseWidth;
	private Material drawMat;
	private int drawLayer;

	public VectorLine (List<Vector3> points, Mesh mesh, Material mat, float width, Vector3 rot, int layer, Color color) {
		this.baseMesh = mesh;
		this.drawMat = mat;
		this.baseWidth = width;
		this.baseRot = Quaternion.Euler(rot);
		this.drawLayer = layer;
		this.segments = new List<VectorLineSegment>(points.Count);

		// Add segments
		for (int i = 0; i < points.Count - 1; i++) {
			Vector3 segPos = points[i+1] - points[i];
			segments.Add(new VectorLineSegment(
				baseMesh, 
				points[i] + (segPos / 2.0f), 							// Position is halfway between points
				Quaternion.FromToRotation(Vector3.right, segPos) * baseRot,	// Take base rotation, then rotate to align between points
				new Vector3(segPos.magnitude, baseWidth, 1.0f), 		// Scale based on width (Y) and distance between points (X)
				(Color32) color));
		}
	}

	public void SetColor (Color color) {
		Color32 newColor = (Color32) color;
		foreach (VectorLineSegment segment in segments) {
			segment.SetStartColor(newColor);
			segment.SetEndColor(newColor);
		}
	}

	public void SetColor (Color color, int index) {
		// This assigns to segments, but is indexed by points (just to clarify)
		if ((index >= 0) && (index < segments.Count)) {
			// Set start color for any but last point in line
			segments[index].SetStartColor((Color32) color);
		}
		if ((index > 0) && (index <= segments.Count)) {
			// Set end color for previous segment, if any
			segments[index - 1].SetEndColor((Color32) color);
		}
	}

	public void Draw3D () {
		foreach (VectorLineSegment segment in segments) {
			Graphics.DrawMesh(segment.mesh, Matrix4x4.identity, drawMat, drawLayer, null, 0, null, false, false);
		}
	}
}
