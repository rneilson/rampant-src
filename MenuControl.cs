using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This is not quite as generic as I'd like, especially regarding available settings/properties
// If I end up reusing this for something, I'll need some way of specifying said 
// settings/properties w/o Unity's interference and weirdness
public class MenuControl : MonoBehaviour {

	public bool debugInfo;

	private Scorer scorer;

	// Title text
	private TextMesh titleMesh;
	private TextMesh versionMesh;

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
	private Dictionary<string, MenuNode> nodes = new Dictionary<string, MenuNode>();
	private MenuNodePath currNode = MenuNodePath.None;

	// Settings
	//private Dictionary<string, MenuSetting> settings = new Dictionary<string, MenuSetting>();

	// Cursor state
	private CursorLockMode desiredCursorMode;
	private bool desiredCursorVisibility;
	private InputMode currentInput;

	// Input axes
	private InputAxisTracker moveVert = new InputAxisTracker("MoveVertical");
	private InputAxisTracker moveHori = new InputAxisTracker("MoveHorizontal");
	private InputAxisTracker fireVert = new InputAxisTracker("FireVertical");
	private InputAxisTracker fireHori = new InputAxisTracker("FireHorizontal");
	private InputAxisTracker selVert = new InputAxisTracker("SelectVertical");
	private InputAxisTracker selHori = new InputAxisTracker("SelectHorizontal");
	private InputAxisTracker bombTrig = new InputAxisTracker("BombTrigger");
	private InputAxisTracker bombTrigL = new InputAxisTracker("BombTriggerL");
	private InputAxisTracker bombTrigR = new InputAxisTracker("BombTriggerR");

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
		// Get scorer
		scorer = GameObject.Find("Scorekeeper").GetComponent<Scorer>();

		// Title init (assumes children 0 and 1)
		titleMesh = transform.GetChild(0).GetComponent<TextMesh>();
		versionMesh = transform.GetChild(1).GetComponent<TextMesh>();
		SetTitle(GameSettings.GameName, GameSettings.Version);

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

		/*
		// Setup settings dict
		settings["Volume"] = new VolumeSetting("Volume");
		settings["MouseSpeed"] = new MouseSpeedSetting("MouseSpeed", "FireCursor");
		*/

		// Parse menu node list
		foreach (MenuNode node in menuNodes) {
			nodes[node.name] = node;
		}

		// Show menu and select first line
		ShowMenu(rootNode);

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
		
		// Check for input
		if (currentInput == InputMode.Game) {
			// Check for screenshot command
			// Here instead of in Scorer because I'm consolidating the hide-menu-before-capping code with the 
			// take-a-screenshot code
			if (Input.GetButtonDown("Screenshot")) {
				StartCoroutine(ScreenCapture(false));
			}
		}
		else if (currentInput == InputMode.Menu) {
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
		// Grab node name
		MenuNode node = nodes[nodeName];

		if (debugInfo) {
			Debug.Log("Loading node: " + node.name, gameObject);
		}

		int lineIndex = 0;
		int sourceIndex = 0;

		// Make the first line a back cmd
		string backName;

		// Check if there's a previous node
		if ((currNode.Prev == null) || (currNode.Prev.Name == "")) {
			// Check if game's started
			if (scorer.IsStarted) {
				backName = "Resume";
			}
			// Otherwise, don't even show this line
			else {
				backName = "";
			}
		}
		// Otherwise, use the generic "back"
		// Could use prev node name, but that'll have to wait
		// until I change nodes to have display names
		else {
			backName = "Back";
		}

		if ((node.autoCreateBack) && (backName != "")) {
			menuLines[0].ConfigureLine(MenuLineType.Back, backName, "");
			if (debugInfo) {
				menuLines[0].DebugLineInfo();
			}
			lineIndex++;
		}

		// Configure listed lines
		while ((sourceIndex < node.lines.Length) && (lineIndex < menuLines.Length)) {
			MenuNodeLine line = node.lines[sourceIndex];
			menuLines[lineIndex].ConfigureLine(line.lineType, line.label, line.target);

			if (debugInfo) {
				menuLines[lineIndex].DebugLineInfo();
			}

			lineIndex++;
			sourceIndex++;
		}
		// Configure remaining lines as blank
		while (lineIndex < menuLines.Length) {
			menuLines[lineIndex].ConfigureLine(MenuLineType.Text, "", "");

			if (debugInfo) {
				menuLines[lineIndex].DebugLineInfo();
			}

			lineIndex++;
		}
	}

	void MakeVisible () {
		// Show ourselves
		gameObject.layer = showLayer;

		// Show title
		titleMesh.gameObject.layer = showLayer;
		versionMesh.gameObject.layer = showLayer;

		// Show lines
		foreach (MenuLine line in menuLines) {
			line.SetLayer(showLayer);
		}

		// Show cursor
		desiredCursorVisibility = true;
		desiredCursorMode = CursorLockMode.None;
	}

	void MakeInvisible () {
		// Hide lines
		foreach (MenuLine line in menuLines) {
			line.SetLayer(hideLayer);
		}

		// Hide title
		titleMesh.gameObject.layer = hideLayer;
		versionMesh.gameObject.layer = hideLayer;

		// Hide ourselves
		gameObject.layer = hideLayer;

		// Hide cursor
		desiredCursorVisibility = false;
		desiredCursorMode = CursorLockMode.Locked;
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
			MakeVisible();

			// Select saved/first line
			if (node.Line >= 0) {
				SelectLine(node.Line);
			}
			else {
				int selected = 0;
				while (!menuLines[selected].Selectable) {
					selected++;
				}
				SelectLine(selected);
			}

			// Grab and reset input
			currentInput = InputMode.Menu;
			ResetInput();
		}
	}

	void BackMenu () {
		if (debugInfo) {
			Debug.Log("Going back to " + currNode.Prev.Name, gameObject);
		}
		ShowMenu(currNode.Prev);
	}

	void BackMenu (string backTo) {
		MenuNodePath node = currNode;
		// Walk back until we find our node or hit root node
		while ((node.Name != backTo) && (node.Prev.Name != "")) {
			node = node.Prev;
		}
		// Go there
		ShowMenu(node);
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
		int newLine = (index < -1) ? -1 : index;
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
		// Update input dT
		InputAxisTracker.Update(Time.unscaledDeltaTime);

		// Grab current axis readings
		float threshold = InputAxisTracker.Threshold;
		float moveVertVal = moveVert.Read();
		float moveHoriVal = moveHori.Read();
		float fireVertVal = fireVert.Read();
		float fireHoriVal = fireHori.Read();
		float selVertVal = selVert.Read();
		float selHoriVal = selHori.Read();
		float bombTrigVal = bombTrig.Read();
		float bombTrigLVal = bombTrigL.Read();
		float bombTrigRVal = bombTrigR.Read();

		// Screenshot first, because with the hiding/showing, we don't want to do anything else this frame
		if (Input.GetButtonDown("Screenshot")) {
			return MenuCommand.ScreenCap;
		}
		// Back button
		else if (Input.GetButtonDown("Back")) {
			return MenuCommand.Back;
		}
		// Mouse button
		else if (Input.GetMouseButtonDown(0)) {
			return ParseMouseClick();
		}
		// Triggers/spacebar/return
		else if ((Input.GetButtonDown("BombKey")) 
			|| (Input.GetButtonDown("BombButton")) 
			|| (Input.GetButtonDown("Select")) 
			|| ((bombTrigVal > threshold) || (bombTrigVal < -threshold)) 
			|| ((bombTrigLVal > threshold) || (bombTrigLVal < -threshold)) 
			|| ((bombTrigRVal > threshold) || (bombTrigRVal < -threshold))) {
			// Just run selected
			return MenuCommand.RunCmdLine;
		}
		// Up next
		else if ((moveVertVal > threshold) 
			|| (fireVertVal > threshold) 
			|| (selVertVal > threshold)) {
		//else if ((Input.GetKeyDown(KeyCode.UpArrow))
		//	|| (Input.GetKeyDown(KeyCode.W))) {
			return MenuCommand.SelectUp;
		}
		// Then down
		else if ((moveVertVal < -threshold) 
			|| (fireVertVal < -threshold) 
			|| (selVertVal < -threshold)) {
		//else if ((Input.GetKeyDown(KeyCode.DownArrow))
		//	|| (Input.GetKeyDown(KeyCode.S))) {
			return MenuCommand.SelectDown;
		}
		// Now right
		else if ((moveHoriVal > threshold) 
			|| (fireHoriVal > threshold) 
			|| (selHoriVal > threshold)) {
		//else if ((Input.GetKeyDown(KeyCode.RightArrow))
		//	|| (Input.GetKeyDown(KeyCode.D))) {
			return MenuCommand.RunCmdRight;
		}
		// Then left
		else if ((moveHoriVal < -threshold) 
			|| (fireHoriVal < -threshold) 
			|| (selHoriVal < -threshold)) {
		//else if ((Input.GetKeyDown(KeyCode.LeftArrow))
		//	|| (Input.GetKeyDown(KeyCode.A))) {
			return MenuCommand.RunCmdLeft;
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
				scorer.SaveScores();
				GameSettings.Quit();
				break;
			case MenuCommandType.Restart:
				scorer.SaveScores();
				GameSettings.Restart();
				break;
			case MenuCommandType.ExitMenu:
				HideMenu();
				break;
			case MenuCommandType.NodeBack:
				BackMenu();
				break;
			case MenuCommandType.NodeBackTo:
				BackMenu(cmd.cmdTarget);
				break;
			case MenuCommandType.NodeGoto:
				ShowMenu(cmd.cmdTarget);
				break;
			case MenuCommandType.SettingToggle:
				GameSettings.ToggleSetting(cmd.cmdTarget);
				menuLines[selectedLine].UpdateText();
				break;
			case MenuCommandType.SettingHigher:
				GameSettings.RaiseSetting(cmd.cmdTarget);
				menuLines[selectedLine].UpdateText();
				break;
			case MenuCommandType.SettingLower:
				GameSettings.LowerSetting(cmd.cmdTarget);
				menuLines[selectedLine].UpdateText();
				break;
			case MenuCommandType.SelectUp:
				SelectUp(selectedLine);
				break;
			case MenuCommandType.SelectDown:
				SelectDown(selectedLine);
				break;
			case MenuCommandType.RunCmdLine:
				if (selectedLine >= 0) {
					ExecuteCommand(menuLines[selectedLine].CommandLine());
				}
				break;
			case MenuCommandType.RunCmdLeft:
				if (selectedLine >= 0) {
					ExecuteCommand(menuLines[selectedLine].CommandLeft());
				}
				break;
			case MenuCommandType.RunCmdRight:
				if (selectedLine >= 0) {
					ExecuteCommand(menuLines[selectedLine].CommandRight());
				}
				break;
			case MenuCommandType.ScreenCap:
				StartCoroutine(ScreenCapture(true));
				break;
			case MenuCommandType.ResetInput:
				GameSettings.ResetInputSettings();
				ShowMenu(cmd.cmdTarget);
				break;
			case MenuCommandType.None:
			default:
				break;
		}
	}

	void ResetInput () {
		// Reset Unity's input
		Input.ResetInputAxes();
		// Lock in zero values for our custom input for first fraction of a second
		moveVert.Read();
		moveHori.Read();
		fireVert.Read();
		fireHori.Read();
		selVert.Read();
		selHori.Read();
		bombTrig.Read();
		bombTrigL.Read();
		bombTrigR.Read();
	}

	IEnumerator ScreenCapture (bool hideMenu) {
		if (hideMenu) {
			MakeInvisible();
		}
		else {
			GameSettings.Pause();
		}

		yield return 0;

		GameSettings.ScreenCap();

		yield return 0;

		if (hideMenu) {
			MakeVisible();
		}
		else {
			GameSettings.Resume();
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

	public void SetTitle (string title, string version = "") {
		titleMesh.text = title;
		if (version != "") {
			versionMesh.text = version;
		}
	}

	public void ShowMenu (string nodeName) {
		// Creates a new nodepath...
		MenuNodePath newNode = new MenuNodePath(currNode, nodeName, -1);
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

		// Hide ourselves
		MakeInvisible();

		// Release input
		currentInput = InputMode.Game;
		Input.ResetInputAxes();

		// Tell GameSettings to resume
		GameSettings.Resume();

	}

	public string GetSetting (string settingName) {
		return GameSettings.GetSetting(settingName);
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
	private const float threshold = 0.1f;
	private static float deltaTime;
	private float resetCountdown = 0.0f;
	private bool wasCaptured = false;
	private bool wasRead = false;
	private float axisValue;
	private string axisName;

	public static float Threshold {
		get { return threshold; }
	}

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

	public static void Update (float dT) {
		deltaTime = dT;
	}

	void Capture () {
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
		// Capture input if possible
		Capture();

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
	Restart,
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
	ScreenCap,
	ResetInput,
	NodeBackTo
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
	public static MenuCommand Back = new MenuCommand(MenuCommandType.NodeBack, "");
	public static MenuCommand RunCmdLine = new MenuCommand(MenuCommandType.RunCmdLine, "");
	public static MenuCommand SelectUp = new MenuCommand(MenuCommandType.SelectUp, "");
	public static MenuCommand SelectDown = new MenuCommand(MenuCommandType.SelectDown, "");
	public static MenuCommand RunCmdRight = new MenuCommand(MenuCommandType.RunCmdRight, "");
	public static MenuCommand RunCmdLeft = new MenuCommand(MenuCommandType.RunCmdLeft, "");
	public static MenuCommand ScreenCap = new MenuCommand(MenuCommandType.ScreenCap, "");
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
	public bool autoCreateBack = true;
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

