using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This is not quite as generic as I'd like, especially regarding available settings/properties
// If I end up reusing this for something, I'll need some way of specifying said 
// settings/properties w/o Unity's interference and weirdness
public class MenuControl : MonoBehaviour {

	public bool debugInfo;

	// Title text
	private TextMesh titleMesh;
	private string titleText;

	// Menu lines
	private MenuLine[] menuLines;
	private int selectedLine;

	// Selected text parameters
	public int lineCols;
	public int capCols;
	public string leftCapText = "[";
	public string rightCapText = "]";
	public FontStyle selectedStyle = FontStyle.Normal;

	// Menu node list and navigation
	public MenuNode[] menuNodes;
	public string rootNode;
	public string startNode;
	private Dictionary<string, MenuNode> nodes = new Dictionary<string, MenuNode>();

	// Settings
	private Dictionary<string, MenuSetting> settings = new Dictionary<string, MenuSetting>();

	// Cursor state
	private CursorLockMode desiredCursorMode;
	private bool desiredCursorVisibility;

	// Visibility layers
	int showLayer;
	int hideLayer;

	public bool Hidden {
		get {
			if (gameObject.layer == hideLayer) {
				return true;
			}
			else {
				return false;
			}
		}
	}
	public int LineColumns {
		get { return lineCols; }
	}
	public int CapColumns {
		get { return capCols; }
	}
	public string LeftCapText {
		get { return leftCapText; }
	}
	public string RightCapText {
		get { return rightCapText; }
	}
	public FontStyle SelectedStyle {
		get {return selectedStyle; }
	}
	public string RootNode {
		get { return rootNode; }
	}

	// Use this for initialization
	void Start () {
		// Title init (assumes child 0)
		titleMesh = transform.GetChild(0).GetComponent<TextMesh>();

		// Line array init
		menuLines = GetComponentsInChildren<MenuLine>();
		if (debugInfo) {
			Debug.Log("Found the following children:");
			foreach (MenuLine line in menuLines) {
				Debug.Log(line.gameObject.name, line.gameObject);
			}
		}

		// Get layers
		showLayer = LayerMask.NameToLayer("TransparentFX");
		hideLayer = LayerMask.NameToLayer("Hidden");

		// Let each line know its index, and clear what it has to
		for (int i = 0; i < menuLines.Length; i++) {
			menuLines[i].LineIndex = i;
			menuLines[i].Deselect();
		}

		// Setup settings dict
		settings["Volume"] = new VolumeSetting("Volume");
		settings["MouseSpeed"] = new MouseSpeedSetting("MouseSpeed", "FireCursor");

		// TODO: parse root menu

		// TEMP
		desiredCursorMode = Cursor.lockState;
		desiredCursorVisibility = Cursor.visible;
		// Show menu and select first line
		ShowMenu(rootNode);

	}
	
	// Update is called once per frame
	void Update () {
		// Only thing here is cursor stuff, I think
		if (Cursor.lockState != desiredCursorMode) {
			Cursor.lockState = desiredCursorMode;
		}
		if (Cursor.visible != desiredCursorVisibility) {
			Cursor.visible = desiredCursorVisibility;
		}
		
	}

	public void SelectLine (int index) {
		if ((index < menuLines.Length) && (menuLines[index].Selectable)) {
			if (selectedLine >= 0) {
				menuLines[selectedLine].Deselect();
			}
			selectedLine = index;
			menuLines[index].Select();
		}
		// else do nothing
	}

	public void DeselectLine (int index) {
		// Only deselect if it's actually selected
		if ((index >= 0) && (selectedLine == index)) {
			menuLines[selectedLine].Deselect();
			selectedLine = -1;
		}
		// else do nothing
	}

	public void SetTitle (string title) {
		titleText = title;
		titleMesh.text = titleText;
	}

	public void ShowMenu (string node) {
		// TODO: get and parse node

		// Show ourselves
		gameObject.layer = showLayer;
		// Show title
		titleMesh.gameObject.layer = showLayer;
		// Show lines
		foreach (MenuLine line in menuLines) {
			line.SetLayer(showLayer);
		}
		// Select first line
		SelectLine(0);

		// Unhide cursor
		desiredCursorVisibility = true;
		desiredCursorMode = CursorLockMode.None;
	}

	public void HideMenu () {
		// Hide cursor
		desiredCursorVisibility = false;
		desiredCursorMode = CursorLockMode.Locked;

		// Deselect current line
		DeselectLine(selectedLine);
		// Hide lines
		foreach (MenuLine line in menuLines) {
			line.SetLayer(hideLayer);
		}
		// Hide title
		titleMesh.gameObject.layer = hideLayer;
		// Hide ourselves
		gameObject.layer = hideLayer;
	}

	public string GetSetting (string settingName) {
		return settings[settingName].Value;
	}

}

// For passing commands up and down the tree
public enum MenuCommandType : byte {
	None = 0,
	QuitApp,
	ExitMenu,
	GotoNode,
	SettingToggle,
	SettingHigher,
	SettingLower,
	SelectUp,
	SelectDown,
	RunCmdLine,
	RunCmdLeft,
	RunCmdRight,
}

// To be returned from input queries
public struct MenuCommand {
	public MenuCommandType cmdType;
	public string cmdTarget;

	public MenuCommand (MenuCommandType cmdType, string cmdTarget) {
		this.cmdType = cmdType;
		this.cmdTarget = cmdTarget;
	}

	public static MenuCommand None = new MenuCommand(MenuCommandType.None, "");
}

// For menu setup
public struct MenuNodeLine {
	public MenuLineType lineType;
	public string label;
	public string target;
}
public struct MenuNode {
	public string name;
	public MenuNodeLine[] lines;
}

// Mostly for internal use within MenuControl, in a dict by setting name
public abstract class MenuSetting {
	protected string name;

	public MenuSetting (string name) {
		this.name = name;
	}

	public string Name {
		get { return name; }
	}
	// TODO: allow set?
	public abstract string Value { get; }

	public abstract void Toggle ();
	public abstract void Higher ();
	public abstract void Lower ();
}

public class VolumeSetting : MenuSetting {
	private const int increment = 5;
	private float savedVolume;

	// Don't need anything extra
	// Technically this only controls overall game volume
	// Might need a wrapper class or something for different GameObject volumes
	public VolumeSetting (string name) : base (name) {
		this.savedVolume = AudioListener.volume;
	}

	// Returns volume in percent
	public override string Value {
		get {
			int vol = Mathf.RoundToInt(AudioListener.volume * 100.0f);
			return System.String.Format("{0,3}%", vol);
		}
	}

	public override void Toggle () {
		// Mutes if volume positive, puts volume back if muted
		if (AudioListener.volume == 0.0f) {
			AudioListener.volume = savedVolume;
		}
		else {
			savedVolume = AudioListener.volume;
			AudioListener.volume = 0.0f;
		}
	}

	public override void Higher () {
		int vol = Mathf.RoundToInt(AudioListener.volume * 100.0f);
		vol += increment;
		if (vol > 100) {
			vol = 100;
		}
		AudioListener.volume = ((float) vol) / 100.0f;
	}

	public override void Lower () {
		int vol = Mathf.RoundToInt(AudioListener.volume * 100.0f);
		vol -= increment;
		if (vol < 0) {
			vol = 0;
		}
		AudioListener.volume = ((float) vol) / 100.0f;
	}

}

public class MouseSpeedSetting : MenuSetting {
	private const float increment = 0.05f;
	private CursorMovement cursor;

	// Don't need anything extra
	// Technically this only controls overall game volume
	// Might need a wrapper class or something for different GameObject volumes
	public MouseSpeedSetting (string name, string cursorName) : base (name) {
		cursor = GameObject.Find(cursorName).GetComponent<CursorMovement>();
	}

	// Returns volume in percent
	public override string Value {
		get {
			float speed = Mathf.Round(cursor.MouseSpeed / increment) * increment;
			return speed.ToString("F2");
		}
	}

	public override void Toggle () {
		// Nothing to toggle, really...
	}

	public override void Higher () {
		float speed = Mathf.Round(cursor.MouseSpeed / increment);
		speed = (speed + 1.0f) * increment;
		if (speed > 1.0f) {
			speed = 1.0f;
		}
		cursor.MouseSpeed = speed;
	}

	public override void Lower () {
		float speed = Mathf.Round(cursor.MouseSpeed / increment);
		speed = (speed - 1.0f) * increment;
		if (speed < 0.0f) {
			speed = 0.0f;
		}
		cursor.MouseSpeed = speed;
	}

}

// TODO: resolution class
