using UnityEngine;
using System.Collections;

public class MenuSegment : MonoBehaviour {

	public MenuSegmentType segmentType;

	private MenuLine parentLine;
	private TextMesh displayText;

	// Use this for initialization
	void Start () {
		parentLine = transform.parent.GetComponent<MenuLine>();
		displayText = GetComponent<TextMesh>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnMouseEnter () {
		parentLine.MouseEntered(segmentType);
	}

	public void Selected (FontStyle selectStyle) {
		displayText.fontStyle = selectStyle;
	}

	public void Selected (FontStyle selectStyle, string selectText) {
		Selected(selectStyle);
		displayText.text = selectText;
	}

	public void Deselected () {
		displayText.fontStyle = FontStyle.Normal;

		// Totally cheating a bit here, should pass something in instead
		if (segmentType != MenuSegmentType.Line) {
			displayText.text = "";
		}
	}
}

public enum MenuSegmentType : byte {
	Line = 0,
	Left,
	Right
}
