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

		// Initialize line to text-only, selectable, no command (totally breaking my own rules, but whatev)
		// For testing...yeah...that's it...
		command = new MenuLineCommand(true, false, line.Text, "", menu.LeftCapText, menu.RightCapText, 
			MenuCommandType.None, MenuCommandType.None, MenuCommandType.None);
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
			line.UpdateText(command.Label + menu.GetSetting(command.Target));
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
	Number,
	OnOff,		// Don't use this one yet
	Sequence	// Don't use this one yet
}

// This is for setting up a line with the appropriate config
// Basically a POD class, with a Big Switch Statement Of Doom
public class MenuLineCommand {

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

	public MenuLineCommand (bool selectable, bool updateable, string label, 
		string target, string capLeft, string capRight,
		MenuCommandType cmdLine, MenuCommandType cmdLeft, MenuCommandType cmdRight) {
		// Pretty standard fare
		this.selectable = selectable;
		this.updateable = updateable;
		this.label = label;
		this.target = target;
		this.capLeft = capLeft;
		this.capRight = capRight;
		this.cmdLine = cmdLine;
		this.cmdLeft = cmdLeft;
		this.cmdRight = cmdRight;
	}

	// This is to be used when MenuControl passes in line specs to MenuLine
	// Whole lotta if and switch statements ahead!
	public MenuLineCommand (MenuControl menu, MenuLineType lineType, string lineLabel, string lineTarget) {

		// Only text lines are non-selectable
		this.selectable = (lineType == MenuLineType.Text) ? false : true;

		// Certain types are updatedatable settings, others aren't
		switch (lineType) {
			case MenuLineType.Number:
			case MenuLineType.OnOff:
			case MenuLineType.Sequence:
				this.updateable = true;
				break;
			default:
				this.updateable = false;
				break;
		}

		// Truncate label string if necessary
		this.label = (lineLabel.Length > menu.LineColumns) ? lineLabel.Substring(0, menu.LineColumns) : lineLabel;

		// Text, Quit, and Back can't have targets attached (well, can, but why bother?)
		switch (lineType) {
			case MenuLineType.Text:
			case MenuLineType.Quit:
			case MenuLineType.Back:
				this.target = "";
				break;
			default:
				this.target = lineTarget;
				break;
		}

		// Number and sequence get special end caps
		switch (lineType) {
			case MenuLineType.Number:
			case MenuLineType.Sequence:
				this.capLeft = "<< ";
				this.capRight = " >>";
				break;
			default:
				this.capLeft = menu.LeftCapText;
				this.capRight = menu.RightCapText;
				break;
		}

		// Now commands - the big one
		switch (lineType) {
			case MenuLineType.Text:
				this.cmdLine = MenuCommandType.None;
				this.cmdLeft = MenuCommandType.None;
				this.cmdRight = MenuCommandType.None;
				break;
			case MenuLineType.Quit:
				this.cmdLine = MenuCommandType.QuitApp;
				this.cmdLeft = MenuCommandType.None;
				this.cmdRight = MenuCommandType.None;
				break;
			case MenuLineType.Back:
				this.cmdLine = MenuCommandType.NodeBack;
				this.cmdLeft = MenuCommandType.None;
				this.cmdRight = MenuCommandType.None;
				break;
			case MenuLineType.Goto:
				this.cmdLine = MenuCommandType.NodeGoto;
				this.cmdLeft = MenuCommandType.None;
				this.cmdRight = MenuCommandType.None;
				break;
			case MenuLineType.Number:
				this.cmdLine = MenuCommandType.SettingToggle;
				this.cmdLeft = MenuCommandType.SettingLower;
				this.cmdRight = MenuCommandType.SettingHigher;
				break;
			default:
				this.cmdLine = MenuCommandType.None;
				this.cmdLeft = MenuCommandType.None;
				this.cmdRight = MenuCommandType.None;
				break;
		}
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

