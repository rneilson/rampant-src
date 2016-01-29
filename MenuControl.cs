﻿using UnityEngine;
using System.Collections;

public class MenuControl : MonoBehaviour {

	public bool debugInfo;

	// Menu lines
	private MenuLine[] menuLines;
	private int maxLines;
	private int selectedLine;

	// Selected text parameters
	public string leftCapText = "[";
	public string rightCapText = "]";
	public FontStyle selectedStyle = FontStyle.Normal;

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
}

