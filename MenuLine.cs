using UnityEngine;
using System.Collections;

public class MenuLine : MonoBehaviour {

	// Relatives
	private MenuControl menu;
	private MenuSegment line;
	private MenuSegment left;
	private MenuSegment right;
	private int lineIndex;

	// Selected text parameters
	private string leftCapText;
	private string rightCapText;
	private FontStyle selectedStyle;

	// This will change once I get the MenuOption class written
	public bool Selectable {
		get { return true; }
	}
	// This will be set by MenuControl
	public int LineIndex {
		get { return lineIndex; }
		set { lineIndex = value; }
	}

	// Use this for initialization
	void Start () {
		// Get relatives up and down
		menu = transform.parent.GetComponent<MenuControl>();
		// We're kinda assuming the {line, left, right} layout here
		line = transform.GetChild(0).GetComponent<MenuSegment>();
		left = transform.GetChild(1).GetComponent<MenuSegment>();
		right = transform.GetChild(2).GetComponent<MenuSegment>();

		// Get selected text style/strings
		leftCapText = menu.LeftCapText;
		rightCapText = menu.RightCapText;
		selectedStyle = menu.SelectedStyle;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void SetLayer (int layer) {
		// Set our own layer
		gameObject.layer = layer;
		// Screw it, I'll just set the children's layers directly
		line.gameObject.layer = layer;
		left.gameObject.layer = layer;
		right.gameObject.layer = layer;
	}

	public void MouseEntered (MenuSegmentType segType) {
		//Debug.Log("Mouse entered " + gameObject.name + "'s " + segType.ToString(), gameObject);
		menu.SelectLine(lineIndex);
	}

	public void Select () {
		// These will pass strings from MenuOption later
		line.Selected(selectedStyle);
		left.Selected(selectedStyle, leftCapText);
		right.Selected(selectedStyle, rightCapText);
	}

	public void Deselect () {
		line.Deselected();
		left.Deselected();
		right.Deselected();
	}
}
