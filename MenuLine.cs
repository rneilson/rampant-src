using UnityEngine;
using System.Collections;

// The MenuLine class is mostly a shell for MenuCommandLine et al, 
// to interface with the child objects of the line
public class MenuLine : MonoBehaviour {

	// Relatives
	private MenuControl menu;
	private MenuSegment line;
	private MenuSegment left;
	private MenuSegment right;
	private int lineIndex;				// This line's index

	// Selected text parameters
	private string leftCapText;
	private string rightCapText;
	private FontStyle selectedStyle;

	// Current menu command for this line
	private MenuLineCommand command;

	// This will change once I get the MenuOption class written
	public bool Selectable {
		get { return command.Selectable; }
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

		selectedStyle = menu.SelectedStyle;

		command = new MenuLineCommand(menu, MenuLineType.Text, line.Text, "");
		Deselect();
		line.UpdateText(command.Label);
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

	public void MouseExited (MenuSegmentType segType) {
		menu.DeselectLine(lineIndex);
	}

	public void Select () {
		// These will pass strings from MenuOption later
		line.Selected(selectedStyle);
		left.Selected(selectedStyle, command.CapLeft);
		right.Selected(selectedStyle, command.CapRight);
	}

	public void Deselect () {
		line.Deselected();
		left.Deselected();
		right.Deselected();
	}

	public void UpdateText () {
		// Only update if there's something which can be updated
		// This is also to be called by MenuControl after changing a setting value
		if (command.Updateable) {
			if (command.LineType == MenuLineType.Setting) {
				line.UpdateText(command.Label + GameSettings.GetSetting(command.Target));
			}
			else if (command.LineType == MenuLineType.Score) {
				line.UpdateText(System.String.Format("{0,-9}{1,6}{2,6}", command.Label, 
					GameSettings.GetMode(command.Target).GetScore("Kills"),
					GameSettings.GetMode(command.Target).GetScore("Waves")));
			}
		}
	}

	public void ConfigureLine (MenuLineType lineType, string lineLabel, string lineTarget) {
		// Get the new command parameters
		command = new MenuLineCommand(menu, lineType, lineLabel, lineTarget);
		// Deselect line to clear out end caps etc
		Deselect();
		// Write new line text in
		if (command.Updateable) {
			UpdateText();
		}
		else {
			// We're only going to set text once, and just to label
			line.UpdateText(command.Label);
		}
	}

	public void DebugLineInfo () {
		Debug.Log("Line " + lineIndex + ": type " + command.CommandLine().cmdType.ToString() 
			+ ", label " + command.Label + ", target " + command.Target, gameObject);
	}

	// Teh Commands!
	public MenuCommand CommandLine () {
		return command.CommandLine();
	}
	public MenuCommand CommandLeft () {
		return command.CommandLeft();
	}
	public MenuCommand CommandRight () {
		return command.CommandRight();
	}

}

// This here enum is mostly a way to pass types through Unity's inspector
// Each corresponds (or will) to a subclass of MenuCommandLine
public enum MenuLineType {
	Text = 0,
	Quit,
	Back,
	Goto,
	Setting,
	Restart,
	Score,
	ResetInput
}

// This is for setting up a line with the appropriate config
// Basically a POD class, with a Big Switch Statement Of Doom
public class MenuLineCommand {

	protected MenuLineType lineType;
	protected bool selectable;		// Whether this line is even selectable
	protected bool updateable;			// If true, value will be updated after cmd
	protected string label;			// Initial label -- may be updated with value
	protected string target;			// Target node, property, etc
	protected string capLeft;			// Left cap text when selected
	protected string capRight;		// Right cap text when selected
	protected MenuCommandType cmdLine;	// Command to run for line segment
	protected MenuCommandType cmdLeft;	// Command to run for left segment
	protected MenuCommandType cmdRight;	// Command to run for right segment

	// I don't think we need anything fancy in the normal constructor
	// We'll leave checking line label/cap lengths to the static func
	public MenuLineCommand () {
		this.lineType = MenuLineType.Text;
		this.selectable = false;
		this.updateable = false;
		this.label = "";
		this.target = "";
		this.capLeft = "";
		this.capRight = "";
		this.cmdLine = MenuCommandType.None;
		this.cmdLeft = MenuCommandType.None;
		this.cmdRight = MenuCommandType.None;
	}

	// This is to be used when MenuControl passes in line specs to MenuLine
	// Whole lotta if and switch statements ahead!
	public MenuLineCommand (MenuControl menu, MenuLineType lineType, string lineLabel, string lineTarget) {
		this.lineType = lineType;

		// Text and score lines are non-selectable
		switch (lineType) {
			case MenuLineType.Text:
			case MenuLineType.Score:
				this.selectable = false;
				break;
			default:
				this.selectable = true;
				break;
		}

		// Certain types are updatedatable text, others aren't
		switch (lineType) {
			case MenuLineType.Setting:
			case MenuLineType.Score:
				this.updateable = true;
				break;
			default:
				this.updateable = false;
				break;
		}

		// Truncate label string if necessary
		this.label = (lineLabel.Length > menu.LineColumns) ? lineLabel.Substring(0, menu.LineColumns) : lineLabel;

		// Some can't have targets attached (well, can, but why bother?)
		switch (lineType) {
			case MenuLineType.Text:
			case MenuLineType.Quit:
			case MenuLineType.Restart:
			case MenuLineType.Back:
			case MenuLineType.ResetInput:
				this.target = "";
				break;
			default:
				this.target = lineTarget;
				break;
		}

		// Settings get special end caps
		switch (lineType) {
			case MenuLineType.Setting:
				this.capLeft = "<< ";
				this.capRight = " >>";
				break;
			default:
				this.capLeft = menu.LeftCapText;
				this.capRight = menu.RightCapText;
				break;
		}

		// Almost all get no left/right commands
		this.cmdLeft = MenuCommandType.None;
		this.cmdRight = MenuCommandType.None;

		// Now commands - the big one
		switch (lineType) {
			case MenuLineType.Quit:
				this.cmdLine = MenuCommandType.QuitApp;
				break;
			case MenuLineType.Restart:
				this.cmdLine = MenuCommandType.Restart;
				break;
			case MenuLineType.Back:
				this.cmdLine = MenuCommandType.NodeBack;
				break;
			case MenuLineType.Goto:
				this.cmdLine = MenuCommandType.NodeGoto;
				break;
			case MenuLineType.Setting:
				this.cmdLine = MenuCommandType.SettingToggle;
				this.cmdLeft = MenuCommandType.SettingLower;
				this.cmdRight = MenuCommandType.SettingHigher;
				break;
			case MenuLineType.ResetInput:
				this.cmdLine = MenuCommandType.ResetInput;
				break;
			case MenuLineType.Text:
			case MenuLineType.Score:
			default:
				this.cmdLine = MenuCommandType.None;
				break;
		}
	}

	public MenuLineType LineType {
		get { return lineType; }
	}
	public bool Selectable {
		get { return selectable; }
	}
	public bool Updateable {
		get { return updateable; }
	}
	public string Label {
		get { return label; }
	}
	public string Target {
		get { return target; }
	}
	public string CapLeft {
		get { return capLeft; }
	}
	public string CapRight {
		get { return capRight; }
	}

	public MenuCommand CommandLine () {
		return new MenuCommand(cmdLine, target);
	}
	public MenuCommand CommandLeft () {
		return new MenuCommand(cmdLeft, target);
	}
	public MenuCommand CommandRight () {
		return new MenuCommand(cmdRight, target);
	}
}

