using UnityEngine;
using System.Collections;

public class MenuControl : MonoBehaviour {

	public bool debugInfo;

	// Title text
	private TextMesh titleMesh;
	private string titleText;

	// Menu lines
	private MenuLine[] menuLines;
	private int maxLines;
	private int selectedLine;

	// Selected text parameters
	public string leftCapText = "[";
	public string rightCapText = "]";
	public FontStyle selectedStyle = FontStyle.Normal;

	// Visibility layers
	int showLayer;
	int hideLayer;

	public string LeftCapText {
		get { return leftCapText; }
	}
	public string RightCapText {
		get { return rightCapText; }
	}
	public FontStyle SelectedStyle {
		get {return selectedStyle; }
	}

	// Use this for initialization
	void Start () {
		// Title init (assumes child 0)
		titleMesh = transform.GetChild(0).GetComponent<TextMesh>();

		// Line array init
		menuLines = GetComponentsInChildren<MenuLine>();
		maxLines = menuLines.Length;

		if (debugInfo) {
			Debug.Log("Found the following children:");
			foreach (MenuLine line in menuLines) {
				Debug.Log(line.gameObject.name, line.gameObject);
			}
		}

		// Let each line know its index, and clear what it has to
		for (int i = 0; i < menuLines.Length; i++) {
			menuLines[i].LineIndex = i;
			menuLines[i].Deselect();
		}

		// Get layers
		showLayer = LayerMask.NameToLayer("TransparentFX");
		hideLayer = LayerMask.NameToLayer("Hidden");
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void SelectLine (int index) {
		if ((index < menuLines.Length) && (menuLines[index].Selectable)) {
			menuLines[selectedLine].Deselect();
			selectedLine = index;
			menuLines[index].Select();
		}
	}

	public void SetTitle (string title) {
		titleText = title;
		titleMesh.text = title;
	}

	// TODO: pass in menu node (once I have menu nodes)
	public void ShowMenu () {
		foreach (MenuLine line in menuLines) {
			line.SetLayer(showLayer);
		}
		titleMesh.gameObject.layer = showLayer;
		menuLines[0].Select();
	}

	public void HideMenu () {
		menuLines[selectedLine].Deselect();
		titleMesh.gameObject.layer = hideLayer;
		foreach (MenuLine line in menuLines) {
			line.SetLayer(hideLayer);
		}
	}

}

