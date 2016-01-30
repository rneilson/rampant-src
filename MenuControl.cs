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
	private MenuNodePath currNode = MenuNodePath.None;

	// Settings
	private Dictionary<string, MenuSetting> settings = new Dictionary<string, MenuSetting>();

	// Cursor state
	private CursorLockMode desiredCursorMode;
	private bool desiredCursorVisibility;
	private InputMode currentInput;

	// Input axes
	private InputAxisTracker moveVert = new InputAxisTracker("MoveVertical");
	private InputAxisTracker moveHori = new InputAxisTracker("MoveHorizontal");
	private InputAxisTracker fireVert = new InputAxisTracker("FireVertical");
	private InputAxisTracker fireHori = new InputAxisTracker("FireHorizontal");
	private InputAxisTracker bombTrig = new InputAxisTracker("BombTrigger");

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
	public InputMode CurrentInput {
		get { return currentInput; }
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
		foreach (MenuNode node in menuNodes) {
			nodes[node.name] = node;
		}

		// Show menu and select first line
		ShowMenu(startNode);

	}
	
	// Update is called once per frame
	void Update () {
		// Cursor state stuff
		if (Cursor.lockState != desiredCursorMode) {
			Cursor.lockState = desiredCursorMode;
		}
		if (Cursor.visible != desiredCursorVisibility) {
			Cursor.visible = desiredCursorVisibility;
		}
		
		// Check for commands
		if (currentInput == InputMode.Menu) {
			// Check commands
			MenuCommand cmd = ParseInput();
			if (cmd.cmdType != MenuCommandType.None) {
				if (debugInfo) {
					Debug.Log("Caught input: " + cmd.cmdType.ToString(), gameObject);
				}
				ExecuteCommand(cmd);
			}
		}
	}

	// Internal methods

	void LoadNode (string nodeName) {
		MenuNode node = nodes[nodeName];
		if (debugInfo) {
			Debug.Log("Loading node: " + node.name, gameObject);
		}

		// TODO: once linked list nav is in place, make the first line a back cmd
		int i = 0;
		// Configure listed lines
		while ((i < node.lines.Length) && (i < menuLines.Length)) {
			MenuNodeLine line = node.lines[i];
			menuLines[i].ConfigureLine(line.lineType, line.label, line.target);

			if (debugInfo) {
				menuLines[i].DebugLineInfo();
			}

			i++;
		}
		// Configure remaining lines as blank
		while (i < menuLines.Length) {
			menuLines[i].ConfigureLine(MenuLineType.Text, "", "");

			if (debugInfo) {
				menuLines[i].DebugLineInfo();
			}

			i++;
		}
	}

	void ShowMenu (MenuNodePath node) {
		// Exit menu if node is null/none (empty string)
		if (node.Name == "") {
			HideMenu();
		}
		// Otherwise, get and parse node
		else if (nodes.ContainsKey(node.Name)) {
			if (debugInfo) {
				Debug.Log("Going forward to " + node.Name, gameObject);
			}

			// Save current line and set new current node
			currNode.Line = selectedLine;
			currNode = node;

			// Load up node lines
			LoadNode(node.Name);

			// Show ourselves
			gameObject.layer = showLayer;
			// Show title
			titleMesh.gameObject.layer = showLayer;
			// Show lines
			foreach (MenuLine line in menuLines) {
				line.SetLayer(showLayer);
			}
			// Select saved/first line
			if (node.Line >= 0) {
				SelectLine(node.Line);
			}
			else {
				SelectLine(0);
			}

			// Grab and reset input
			currentInput = InputMode.Menu;
			Input.ResetInputAxes();

			// Unhide cursor
			desiredCursorVisibility = true;
			desiredCursorMode = CursorLockMode.None;
		}
	}

	void BackMenu () {
		if (debugInfo) {
			Debug.Log("Going back to " + currNode.Prev.Name, gameObject);
		}
		ShowMenu(currNode.Prev);
	}

	void SelectUp (int index) {
		int newLine = (index < 0) ? 0 : index;
		do {
			newLine--;
			if (newLine < 0) {
				newLine = menuLines.Length - 1;
			}
		} while (!menuLines[newLine].Selectable);
		SelectLine(newLine);
	}

	void SelectDown (int index) {
		int newLine = (index < 0) ? 0 : index;
		do {
			newLine++;
			if (newLine >= menuLines.Length) {
				newLine = 0;
			}
		} while (!menuLines[newLine].Selectable);
		SelectLine(newLine);
	}

	// For retrieving a command from the appropriate line when mouse clicked
	MenuCommand ParseMouseClick () {
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		// Cast a ray hitting only UI elements
		if (Physics.Raycast(ray, out hit, 15f, (1 << showLayer))) {
			// Only the menu line segments have colliders, so I think we can 
			// get away with assuming we're hitting one of these
			MenuSegment segment = hit.collider.GetComponent<MenuSegment>();
			// Check anyways
			if (segment) {
				return segment.ClickedOn();
			}
			else {
				return MenuCommand.None;
			}
		}
		else {
			return MenuCommand.None;
		}
	}

	// A big heap of ifs -- apologies, but I don't think there's another way to do it
	// (Somewhere, somehow, buried perhaps, all input is a big heap of ifs)
	MenuCommand ParseInput () {
		// Capture input on axes
		moveVert.Capture(Time.unscaledDeltaTime);
		moveHori.Capture(Time.unscaledDeltaTime);
		fireVert.Capture(Time.unscaledDeltaTime);
		fireHori.Capture(Time.unscaledDeltaTime);
		bombTrig.Capture(Time.unscaledDeltaTime);

		// Grab current axis readings
		float moveVertVal = moveVert.Read();
		float moveHoriVal = moveHori.Read();
		float fireVertVal = fireVert.Read();
		float fireHoriVal = fireHori.Read();
		float bombTrigVal = bombTrig.Read();

		// Pause first
		// TODO: change button "Back" to send back command (once linked list in place)
		if (Input.GetButtonDown("Back")) {
			return new MenuCommand(MenuCommandType.NodeBack, "");
		}
		// Mouse button second
		else if (Input.GetMouseButtonDown(0)) {
			return ParseMouseClick();
		}
		// Triggers/spacebar/return third
		else if ((Input.GetButtonDown("BombButton")) 
			|| (Input.GetButtonDown("Select"))
			|| ((bombTrigVal > 0.05f) || (bombTrigVal < -0.05f))) {
			// Just run selected
			return new MenuCommand(MenuCommandType.RunCmdLine, "");
		}
		// Up next
		else if ((moveVertVal > 0.05f) 
			|| (fireVertVal > 0.05f)) {
		//else if ((Input.GetKeyDown(KeyCode.UpArrow))
		//	|| (Input.GetKeyDown(KeyCode.W))) {
			return new MenuCommand(MenuCommandType.SelectUp, "");
		}
		// Then down
		else if ((moveVertVal < -0.05f) 
			|| (fireVertVal < -0.05f)) {
		//else if ((Input.GetKeyDown(KeyCode.DownArrow))
		//	|| (Input.GetKeyDown(KeyCode.S))) {
			return new MenuCommand(MenuCommandType.SelectDown, "");
		}
		// Now right
		else if ((moveHoriVal > 0.05f) 
			|| (fireHoriVal > 0.05f)) {
		//else if ((Input.GetKeyDown(KeyCode.RightArrow))
		//	|| (Input.GetKeyDown(KeyCode.D))) {
			return new MenuCommand(MenuCommandType.RunCmdRight, "");
		}
		// Then left
		else if ((moveHoriVal < -0.05f) 
			|| (fireHoriVal < -0.05f)) {
		//else if ((Input.GetKeyDown(KeyCode.LeftArrow))
		//	|| (Input.GetKeyDown(KeyCode.A))) {
			return new MenuCommand(MenuCommandType.RunCmdLeft, "");
		}
		else {
			return MenuCommand.None;
		}
	}

	void ExecuteCommand (MenuCommand cmd) {
		if ((debugInfo) && (cmd.cmdType != MenuCommandType.None)) {
			Debug.Log("Executing: " + cmd.cmdType.ToString(), gameObject);
		}
		// Awful big-ass switch ahoy!
		// (Sorry)
		// ((Enums are meant for big-ass switches, IMNSHO))
		switch (cmd.cmdType) {
			case MenuCommandType.QuitApp:
				Application.Quit();
				break;
			case MenuCommandType.ExitMenu:
				HideMenu();
				break;
			case MenuCommandType.NodeBack:
				BackMenu();
				break;
			case MenuCommandType.NodeGoto:
				ShowMenu(cmd.cmdTarget);
				break;
			case MenuCommandType.SettingToggle:
				if (settings.ContainsKey(cmd.cmdTarget)) {
					settings[cmd.cmdTarget].Toggle();
					menuLines[selectedLine].UpdateText();
				}
				break;
			case MenuCommandType.SettingHigher:
				if (settings.ContainsKey(cmd.cmdTarget)) {
					settings[cmd.cmdTarget].Higher();
					menuLines[selectedLine].UpdateText();
				}
				break;
			case MenuCommandType.SettingLower:
				if (settings.ContainsKey(cmd.cmdTarget)) {
					settings[cmd.cmdTarget].Lower();
					menuLines[selectedLine].UpdateText();
				}
				break;
			case MenuCommandType.SelectUp:
				SelectUp(selectedLine);
				break;
			case MenuCommandType.SelectDown:
				SelectDown(selectedLine);
				break;
			case MenuCommandType.RunCmdLine:
				ExecuteCommand(menuLines[selectedLine].CommandLine());
				break;
			case MenuCommandType.RunCmdLeft:
				ExecuteCommand(menuLines[selectedLine].CommandLeft());
				break;
			case MenuCommandType.RunCmdRight:
				ExecuteCommand(menuLines[selectedLine].CommandRight());
				break;
			case MenuCommandType.None:
			default:
				break;
		}
	}

	// External methods

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

	public void ShowMenu (string nodeName) {
		// Creates a new nodepath...
		MenuNodePath newNode = new MenuNodePath(currNode, nodeName, 0);
		if (debugInfo) {
			Debug.Log("New path node, target: " + newNode.Name 
				+ ", previous: " + newNode.Prev.Name, gameObject);
		}
		// and tries to go there
		ShowMenu(newNode);
	}

	public void HideMenu () {
		// Set current node to None (all the way back)
		currNode = MenuNodePath.None;

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

		// Hide cursor
		desiredCursorVisibility = false;
		desiredCursorMode = CursorLockMode.Locked;

		// Release input
		currentInput = InputMode.Game;
		Input.ResetInputAxes();

	}

	public string GetSetting (string settingName) {
		return settings[settingName].Value;
	} 

}

// For directing input to us, or to the game
public enum InputMode : byte {
	Game = 0,
	Menu
}

// For checking an axis while timescale = 0
public class InputAxisTracker {
	private const float resetTime = 0.35f;
	private const float threshold = 0.05f;
	private float resetCountdown = 0.0f;
	private bool wasCaptured = false;
	private bool wasRead = false;
	private float axisValue;
	private string axisName;

	public InputAxisTracker (string axisName) {
		this.axisName = axisName;
		this.axisValue = 0.0f;
	}

	void Set (float axisReading) {
		axisValue = axisReading;
		resetCountdown = resetTime;
		wasCaptured = true;
		wasRead = false;
	}

	void Reset () {
		axisValue = 0.0f;
		resetCountdown = 0.0f;
		wasCaptured = false;
		wasRead = false;
	}

	public void Capture (float deltaTime) {
		// Get temp reading to check
		float tempCap = Input.GetAxisRaw(axisName);
		// If previously captured, test for axis return to 0 or reset time met
		if (wasCaptured) {
			resetCountdown -= deltaTime;
			if (((tempCap < threshold) && (tempCap > -threshold)) || (resetCountdown <= 0.0f)) {
				Reset();
			}
		}
		// This is not an else statement -- we want the fall-through
		if (!wasCaptured) {
			if ((tempCap > threshold) || (tempCap < -threshold)) {
				Set(tempCap);
			}
		}
	}

	public float Read () {
		if (wasRead) {
			return 0.0f;
		}
		else {
			wasRead = true;
			return axisValue;
		}
	}
}

// For passing commands up and down the tree
public enum MenuCommandType : byte {
	None = 0,
	QuitApp,
	ExitMenu,
	NodeGoto,
	NodeBack,
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
[System.Serializable]
public class MenuNodeLine {
	public MenuLineType lineType;
	public string label;
	public string target;
}
[System.Serializable]
public class MenuNode {
	public string name;
	public MenuNodeLine[] lines;
}
public class MenuNodePath {
	// Pretty bare-bones linked list, with, like, hardly any methods
	// (Don't need them at the moment)
	private MenuNodePath prev;
	private string name;
	private int line;

	public MenuNodePath Prev { get { return prev; } }
	public string Name { get { return name; } }
	public int Line { 
		get { return line; } 
		set {
			if (name != "") {
				line = value;
			}
		}
	}

	public MenuNodePath (MenuNodePath prev, string name, int line) {
		this.prev = prev;
		this.name = name;
		this.line = line;
	}

	public MenuNodePath () : this(null, "", 0) {}
	
	// Trick to make it refer to itself as previous
	private MenuNodePath MakeRecursive () {
		this.prev = this;
		return this;
	} 

	// Base empty node
	public static MenuNodePath None = (new MenuNodePath()).MakeRecursive();
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
