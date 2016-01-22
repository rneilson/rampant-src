using UnityEngine.UI;
using UnityEngine;
using System.Collections;
using System.Globalization;
using System.Text;
using System;

public class ScrollCodeBox : MonoBehaviour {

	// Components
	private TextMesh display;
	//private Text display;
	private PulseControl timer;

	// Textbox sizes
	private int cols = 24;	// Not including newline
	private int rowlen;		// Including newline
	private int rows = 24;
	private int boxChars;
	private int removeChars;

	// Cursor state
	// Public for debug
	public int cursorPos = 0;		// Current pos in display line
	public int cursorOld = 0;
	public int displayLine = 0;		// Current line in display page
	public int currentLine = 0;		// Current line since start -- for loop sync
	public int sourcePos = 0;		// Current pos in source lines -- for lines > rowlen
	public int sourceLine = 0;		// Current line in source array

	// String data
	private StringBuilder workhorse;
	public String initialText;		// Public for debug
	public String availableChars;	// Public for debug
	public string[] displayLines;	// Lines for display
	public string[] sourceLines;	// Lines from source text
	private string sourceStr;
	private string newlineChar = "\n";
	private char paddingChar = ' ';

	// Controls
	public bool debugInfo = false;
	//public int removeLines = 24;
	public ScrollMode scrollMode = ScrollMode.ByPage;

	// Use this for initialization
	void Start () {
		// Stupid compiler won't let me put these in initialization...
		rowlen = cols + 1;
		boxChars = rows * rowlen;
		//removeLines = (removeLines > rows) ? rows : removeLines;
		//removeChars = removeLines * rowlen;

		// Grab components, obvs
		display = GetComponent<TextMesh>();
		//display = GetComponent<Text>();
		timer = GetComponent<PulseControl>();

		// Debug screen pos/size
		if (debugInfo) {
			Bounds boxBounds = GetComponent<Renderer>().bounds;
			Debug.Log("Render bounds: " + boxBounds.ToString(), gameObject);
			Camera cam = GameObject.Find("Camera").GetComponent<Camera>();
			Vector3 bottomLeft = cam.WorldToScreenPoint(boxBounds.min);
			Vector3 topRight = cam.WorldToScreenPoint(boxBounds.max);
			Vector3 center = cam.WorldToScreenPoint(boxBounds.center) - cam.WorldToScreenPoint(Vector3.zero);
			Debug.Log("Screen points: center " + center.ToString() 
				+ " min " + bottomLeft.ToString() 
				+ ", max " + topRight.ToString() 
				+ ", size " + (topRight - bottomLeft).ToString());
		}

		// Stash initial text just in case
		initialText = display.text;
		display.text = "";

		if (debugInfo) {
			Debug.Log("initialText " + initialText.Length.ToString() 
				+ " chars, boxChars " + boxChars.ToString(), gameObject);
		}

		// TEMP CODE
		// Only here while I figure out what I've got access to
		CharacterInfo[] charArray = display.font.characterInfo;
		int charArrayLen = charArray.Length;
		workhorse = new StringBuilder(charArrayLen * 2);
		for (int i = 0; i < charArrayLen; i++) {
			workhorse.Append(Char.ConvertFromUtf32(charArray[i].index));
		}
		availableChars = workhorse.ToString();

		// Just to get scrolling working
		workhorse.Remove(0, workhorse.Length);
		workhorse.EnsureCapacity(boxChars);

		// Set up source, split on newline
		// We'll change this to a text asset later
		displayLines = new String[rows];
		string[] splitter = {"\r\n", "\n"};
		sourceLines = initialText.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
		sourceStr = CheckSourceString();
	}
	
	// Update is called once per frame
	void Update () {
		// TEMP CODE
		//UpdateDisplayText(initialText);
	}

	// Use FixedUpdate instead for this, see how well it works
	void FixedUpdate () {
		//UpdateDisplayText(initialText);
		UpdateDisplay(timer.Loops, timer.Phase);
	}

	void UpdateDisplay (int curLoops, float curPhase) {
		if (NextDisplaySegment(curLoops, curPhase)) {
			display.text = String.Concat(displayLines);
		}
	}

	bool NextDisplaySegment (int loops, float phase) {
		/* I /was/ going to do this all functional-style, but there are /way/ too many moving parts */
		int newPos = Mathf.FloorToInt(phase * (float) rowlen);
		int newLine = loops;
		string tmpStr;

		// Only do anything if we've advanced since last time
		if ((newPos > cursorPos) || (newLine > currentLine)) {
			// Grab current source string
			string source = GetSourceString();

			// Just in the (absurdly improbable) case we're right at phase one, which should be out 
			// of bounds on any source string -- in which case we'll advance the new cursor position
			// one line, and pretend nobody noticed...
			if (newPos >= rowlen) {
				newPos = 0;
				newLine++;
			}

			// Catch up to present line
			while (currentLine < newLine) {
				// Finish current line
				if (cursorPos < cols) {
					// Most common case
					tmpStr = String.Concat(source.Substring(cursorPos, cols - cursorPos), newlineChar);
				}
				else {
					// We're right at the end of the current source line, so we'll just append a newline
					tmpStr = newlineChar;
				}
				// Advance line, which will also roll over cursorPos
				source = NextLine(tmpStr);
			}

			// Now we can finish the current line if necessary
			AppendDisplay(source.Substring(cursorPos, newPos - cursorPos));
			cursorPos = newPos;

			return true;
		}
		else {
			return false;
		}
	}

	void AppendDisplay (string toAppend) {
		displayLines[displayLine] += toAppend;
	}

	string NextLine (string next) {
		// Carriage return and new line, essentially
		cursorPos = 0;
		currentLine++;
		// Advance display and scroll as required
		NextDisplayLine(next);
		// Advance source as required
		return NextSourceString();
	}

	void NextDisplayLine (string toAppend) {
		// Add string and advance
		AppendDisplay(toAppend);
		displayLine++;

		if (displayLine >= rows) {
			// Scroll in the desired mode
			if (scrollMode == ScrollMode.ByPage) {
				for (int i = 0; i < rows; i++) {
					displayLines[i] = "";
				}
				displayLine = 0;
			}
			else if (scrollMode == ScrollMode.ByLine) {
				for (int i = 0; i < rows - 1; i++) {
					displayLines[i] = displayLines[i+1];
				}
				displayLine = rows - 1;
				displayLines[displayLine] = "";
			}
			else {
				Debug.LogError("Somehow NextDisplayLine() got broke trying to " + scrollMode, gameObject);
			}
		}
	}

	string GetSourceString () {
		// This was more complicated, but I moved all that to CheckSourceString() and NextSourceString()
		return sourceStr;
	}

	string CheckSourceString () {
		// This was all first in GetSourceString(), then NextSourceString()
		// Except I'll have to initialize somehow, so might as well put it in
		// its own function
		string toRet;
		int curLength = sourceLines[sourceLine].Length - sourcePos;

		// Check length of current line and return appropriate amount
		if (curLength == cols) {
			// Just right, ship it
			// Substring in case we're not at string start
			toRet = sourceLines[sourceLine].Substring(sourcePos);
		}
		else if (curLength > cols) {
			// Too long, truncate
			toRet = sourceLines[sourceLine].Substring(sourcePos, cols);
		}
		else if (curLength < cols) {
			// Too short, pad it like hell
			toRet = sourceLines[sourceLine].Substring(sourcePos, curLength) + new String(paddingChar, cols - curLength);
		}
		else {
			Debug.LogError("CheckSourceString broke somehow", gameObject);
			return "";
		}

		return toRet;
	}

	string NextSourceString () {
		// Advance sourcePos before we check anything else
		sourcePos += cols;
		// Check length, get next string or advance position, check length again, round and round we go
		int curLength = sourceLines[sourceLine].Length - sourcePos;
		if (curLength <= 0) {
			// Nothing more on this source line, on to the next
			sourcePos = 0;
			sourceLine++;
			// Source rollover check
			if (sourceLine >= sourceLines.Length) {
				sourceLine = 0;
			}
		}
		sourceStr = CheckSourceString();

		// Return with our ill-gotten gains
		// (I suppose we could make this void and require another GetSourceString call, but yolo)
		return sourceStr;
	}

	/*
	string GetCursorStr (string sourceStr) {
		int pageNum, lineNum, colNum;
		string strToRet;

		// Stash last cursor position
		cursorOld = cursorPos;

		// Find new cursor position
		pageNum = timer.Loops / rows;
		lineNum = timer.Loops % rows;
		colNum = Mathf.FloorToInt(timer.Phase * (float) rowlen);
		cursorPos = ((pageNum * boxChars) + (lineNum * rowlen) + colNum) % sourceStr.Length;

		// Check if we've wrapped around
		if (cursorPos < cursorOld) {
			// Get remainder of source
			strToRet = sourceStr.Substring(cursorOld, sourceStr.Length - cursorOld);
			// Wrap around from start
			strToRet += sourceStr.Substring(0, cursorPos);
		}
		else {
			// Get difference between old and new cursor positions (might be nothing)
			strToRet = sourceStr.Substring(cursorOld, cursorPos - cursorOld);
		}

		return strToRet;

		// For later, as a reminder that corrupted letters might be multiple chars long
		//return sourceStr.Substring(cursorOffset, 1);
	}

	void UpdateDisplayText (string sourceStr) {
		// Append next char from source
		workhorse.Append(GetCursorStr(sourceStr));
		// Check if textbox is full, whittle down if so
		if (workhorse.Length > boxChars) {
			if (debugInfo) {
				Debug.Log("Textbox wrapped around, length " + workhorse.Length.ToString(), gameObject);
			}
			workhorse.Remove(0, removeChars);
		}
		// Update display
		display.text = workhorse.ToString();
	}
	*/

	public Color GetCurrentColor () {
		return display.color;
	}

	public void SetColorTo(Color toColor) {
		display.color = toColor;
	}
}

public enum ScrollMode : byte {
	ByPage = 0,
	ByLine
}
