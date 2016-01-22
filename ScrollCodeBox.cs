using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System;

public class ScrollCodeBox : MonoBehaviour {

	// Components
	private TextMesh display;
	private PulseControl timer;
	private Scorer scorer;

	// Textbox sizes
	private int cols = 24;	// Not including newline
	private int rowlen;		// Including newline
	private int rows = 24;

	// Cursor state
	// Public for debug
	public int cursorPos = 0;		// Current pos in display line
	public int displayLine = 0;		// Current line in display page
	public int currentLine = 0;		// Current line since start -- for loop sync
	public int sourcePos = 0;		// Current pos in source lines -- for lines > rowlen
	public int sourceLine = 0;		// Current line in source array

	// String data
	private StringBuilder workhorse;
	public string initialText;		// Public for debug
	public string[] availableChars;	// Public for debug
	public string[] displayLines;	// Lines for display
	public string[] sourceLines;	// Lines from source text
	private StringInfo sourceStr;
	private string newlineChar = "\n";
	private char paddingChar = ' ';

	// Controls
	public bool debugInfo = false;
	public ScrollMode scrollMode = ScrollMode.ByPage;
	public int corruptionsPerPhase = 2;

	// Use this for initialization
	void Start () {
		// Stupid compiler won't let me put these in initialization...
		rowlen = cols + 1;
		int boxChars = rows * rowlen;

		// Grab components, obvs
		display = GetComponent<TextMesh>();
		timer = GetComponent<PulseControl>();
		scorer = GameObject.FindGameObjectWithTag("GameController").GetComponent<Scorer>();

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
		displayLines = new String[rows];

		if (debugInfo) {
			Debug.Log("initialText " + initialText.Length.ToString() 
				+ " chars, boxChars " + boxChars.ToString(), gameObject);
		}

		// TEMP CODE
		// Only here while I figure out what I've got access to
		CharacterInfo[] charArray = display.font.characterInfo;
		int charArrayLen = charArray.Length;
		//workhorse = new StringBuilder(charArrayLen * 2);
		availableChars = new string[charArrayLen];
		for (int i = 0; i < charArrayLen; i++) {
			//workhorse.Append(Char.ConvertFromUtf32(charArray[i].index));
			availableChars[i] = Char.ConvertFromUtf32(charArray[i].index);
		}
		//availableChars = workhorse.ToString();
		//workhorse.Remove(0, workhorse.Length);

		// Set up source, split on newline
		// We'll change this to a text asset later
		// Or maybe have ScrollCode send us at intervals
		int sourceLen = LoadNewSource(initialText);
		if (debugInfo) {
			Debug.Log("Loaded new source, " + sourceLen.ToString() + " lines", gameObject);
		}
		LoadNextSourceString();
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

		// Only do anything if we've advanced since last time
		if ((newPos > cursorPos) || (newLine > currentLine)) {
			// Grab current source string
			//string source = GetSourceString();

			// Just in the (absurdly improbable) case we're right at phase one, which should be out 
			// of bounds on any source string -- in which case we'll advance the new cursor position
			// one line, and pretend nobody noticed...
			if (newPos >= rowlen) {
				newPos = 0;
				newLine++;
			}

			// Catch up to present line
			while (currentLine < newLine) {
				// Finish current line and advance, which will also roll over cursorPos
				AppendDisplayLine(GetSourceString(cursorPos, cols - cursorPos));
			}

			// Now we can finish the current line if necessary
			AppendDisplay(GetSourceString(cursorPos, newPos - cursorPos));
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

	void AppendDisplayLine (string toAppend) {
		// Append to current line, plus newline
		AppendDisplay(toAppend + newlineChar);
		// Carriage return and new line, essentially
		cursorPos = 0;
		currentLine++;
		// Advance display and scroll as required
		NextDisplayLine();
		// Advance source as required
		LoadNextSourceString();
	}

	void NextDisplayLine () {
		// Advance Line
		displayLine++;

		// Check for rollover and scroll
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

	string GetSourceString (int offset, int length) {
		// This was more complicated, but I moved all that to NewSourceString() and LoadNextSourceString()
		return sourceStr.SubstringByTextElements(offset, length);
	}

	StringInfo NewSourceString () {
		// This was all first in GetSourceString(), then LoadNextSourceString()
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
			Debug.LogError("NewSourceString broke somehow", gameObject);
			return new StringInfo("");
		}

		return CorruptSource(toRet);
	}

	void LoadNextSourceString () {
		// MUST BE RUN DURING INITIALIZATION
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
		sourceStr = NewSourceString();
		// Now advance for next run
		sourcePos += cols;
	}

	StringInfo CorruptSource(string toCorrupt) {
		// Check phase and determine corruption passes
		int passes = scorer.PhaseIndex * corruptionsPerPhase;

		// Only run this if we're doing something
		if (passes > 0) {
			// Split incoming string
			int sourceLen = toCorrupt.Length;
			string[] toRet = new string[sourceLen];
			for (int i = 0; i < sourceLen; i++) {
				// Fetch as strings instead of chars
				toRet[i] = toCorrupt.Substring(i, 1);
			}
			// Find indicies to corrupt
			// Le sigh, I /was/ going to do this properly, but /noooo/ Unity's Mono compat sucks
			// SortedSet<int> indicies = new SortedSet<int>();
			Dictionary<int, int> indicies = new Dictionary<int, int>();
			int sourceIndex;
			int availLen = availableChars.Length;
			while (indicies.Count < passes) {
				sourceIndex = UnityEngine.Random.Range(0, sourceLen);
				if (!indicies.ContainsKey(sourceIndex)) {
					indicies.Add(sourceIndex, UnityEngine.Random.Range(0, availLen));
				}
			}
			// Now corrupt them
			foreach (KeyValuePair<int, int> index in indicies) {
				toRet[index.Key] = availableChars[index.Value];
			}
			// Return our ill-gotten gains
			// Will be StringInfo once I change over
			return new StringInfo(String.Concat(toRet));
		}
		else {
			return new StringInfo(toCorrupt);
		}
	}

	string[] ParseSource (string toParse) {
		string[] splitter = {"\r\n", "\n"};
		return toParse.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
	}

	int LoadNewSource (string toLoad) {
		sourcePos = 0;
		sourceLine = 0;
		sourceLines = ParseSource(toLoad);
		return sourceLines.Length;
	}

	public void ReplaceSource (string newSource) {
		int sourceLen = LoadNewSource(newSource);
		if (debugInfo) {
			Debug.Log("Replaced source, " + sourceLen.ToString() + " lines", gameObject);
		}
	}

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
