using UnityEngine;
using System.Collections;

public class ScrollCodeBox : MonoBehaviour {

	private TextMesh display;
	private PulseControl timer;
	private int cols = 24;
	private int rows = 24;	// Not including newline
	private int rowlen;		// Including newline
	private int boxChars;

	// Use this for initialization
	void Start () {
		rowlen = rows + 1;
		boxChars = cols * rowlen;
		// First grab textmesh, obvs
		display = GetComponent<TextMesh>();
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public Color GetCurrentColor () {
		return display.color;
	}

	public void SetColorTo(Color toColor) {
		display.color = toColor;
	}
}
