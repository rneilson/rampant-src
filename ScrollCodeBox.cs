using UnityEngine;
using System.Collections;
using System.Text;
using System;

public class ScrollCodeBox : MonoBehaviour {

	// Components
	private TextMesh display;
	private PulseControl timer;

	// Textbox sizes
	private int cols = 24;
	private int rows = 24;	// Not including newline
	private int rowlen;		// Including newline
	private int boxChars;

	// Cursor state
	private int cursorPos = 0;
	private int cursorLine = 0;
	private int cursorOffset = 0;

	// String data
	private StringBuilder workhorse;
	public String initialText;
	public String availableChars;

	// Use this for initialization
	void Start () {
		// Stupid compiler won't let me put these in initialization...
		rowlen = rows + 1;
		boxChars = cols * rowlen;

		// Grab components, obvs
		display = GetComponent<TextMesh>();
		timer = GetComponent<PulseControl>();

		// Stash initial text just in case
		initialText = display.text;

		// TEMP CODE
		// Only here while I figure out what I've got access to
		display.text = "";
		CharacterInfo[] charArray = display.font.characterInfo;
		int charArrayLen = charArray.Length;
		workhorse = new StringBuilder(charArrayLen * 2);
		for (int i = 0; i < charArrayLen; i++) {
			workhorse.Append(Char.ConvertFromUtf32(charArray[i].index));
		}
		availableChars = workhorse.ToString();

		// MORE TEMP CODE
		// Just to get scrolling working
		workhorse = new StringBuilder(initialText);

	}
	
	// Update is called once per frame
	void Update () {
		cursorLine = timer.Loops;
		cursorPos = Mathf.FloorToInt(timer.Phase * (float) rowlen);

		// TEMP CODE
		cursorOffset = ((cursorLine * rowlen) + cursorPos) % boxChars;
		display.text = workhorse.ToString(0, cursorOffset);
	}

	public Color GetCurrentColor () {
		return display.color;
	}

	public void SetColorTo(Color toColor) {
		display.color = toColor;
	}
}
