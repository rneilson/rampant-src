using UnityEngine;
using System.Collections;

public class MenuSegment : MonoBehaviour {

	public MenuSegmentType segmentType;

	private MenuLine parentLine;
	private TextMesh displayText;

	public string Text {
		get {
			if (displayText) {
				return displayText.text;
			}
			else {
				return "";
			}
		}
	}

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

	void OnMouseExit () {
		parentLine.MouseExited(segmentType);
	}

	public MenuCommand ClickedOn () {
		MenuCommand toRet;
		// Switch on segment type (defaults to returning line cmd (only for mouseclicks))
		switch (segmentType) {
			case MenuSegmentType.Left:
				toRet = parentLine.CommandLeft();
				if (toRet.cmdType == MenuCommandType.None) {
					goto default;
				}
				break;
			case MenuSegmentType.Right:
				toRet = parentLine.CommandRight();
				if (toRet.cmdType == MenuCommandType.None) {
					goto default;
				}
				break;
			default:
				// If clicked on line, or if cap and no cmd, return whatever the line cmd is
				// (which can still be None, but at least we checked)
				toRet = parentLine.CommandLine();
				break;
		}
		return toRet;
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

	public void UpdateText (string newText) {
		displayText.text = newText;
	}
}

public enum MenuSegmentType : byte {
	Line = 0,
	Left,
	Right
}
