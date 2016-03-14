//  Copyright 2016 Raymond Neilson
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

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
