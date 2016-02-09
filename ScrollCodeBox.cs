using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System;

// TODO: change source from array to iterator over string
// TODO: only advance line on newline in source
// TODO: add source queue
// TODO: add queue checks per frame
// TODO: allow appending to current source line
// TODO: add force-next-source-in-queue public method(s)
// TODO: add interruptions
public class ScrollCodeBox : MonoBehaviour {

	// Components
	private TextMesh display;
	private PulseControl timer;
	private Scorer scorer;

	// Textbox sizes
	public int cols = 25;	// Not including newline
	public int rows = 32;
	private int rowlen;		// Including newline

	// Cursor state
	private int cursorPos = 0;		// Current pos in display line
	private int currentLine = 0;		// Current line since start -- for loop sync
	private int displayLine = 0;		// Current line in display page
	private int sourcePos = 0;		// Current pos in source lines -- for lines > rowlen
	private int sourceLine = 0;		// Current line in source array
	private bool stopped = false;

	// String data
	private StringBuilder lineBuffer;
	private string initialText;		// Public for debug
	private string[] availableChars;	// Public for debug
	private string[] displayLines;	// Lines for display
	private string[] sourceLines;	// Lines from source text
	private StringInfo sourceStr;
	private string newlineChar = "\n";
	private string cursorBlink = " ";

	// Controls
	public bool debugInfo = false;
	public bool loopSource = false;
	public bool showCursor = true;
	public string cursorStr = "_";
	public ScrollMode scrollMode = ScrollMode.ByPage;
	public int corruptionsPerPhase = 2;

	public string InitialText {
		get { return initialText; }
	}

	// Component setup ASAP
	void Awake () {
		// Grab components, obvs
		display = GetComponent<TextMesh>();
		timer = GetComponent<PulseControl>();
		scorer = GameObject.FindGameObjectWithTag("GameController").GetComponent<Scorer>();

		// Stupid compiler won't let me put these in initialization...
		rowlen = cols + 1;
		int boxChars = rows * rowlen;
		lineBuffer = new StringBuilder(rowlen);

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
		int sourceLen = LoadNewSource(ParseSource(initialText));
		if (debugInfo) {
			Debug.Log("Loaded new source, " + sourceLen.ToString() + " lines", gameObject);
		}
		LoadNextSourceString();
	}

	// Use this for initialization
	void Start () {}
	
	// Update is called once per frame
	void Update () {
		// TEMP CODE
		//UpdateDisplayText(initialText);
	}

	// Use FixedUpdate instead for this, see how well it works
	void FixedUpdate () {
		//UpdateDisplayText(initialText);
		UpdateDisplay();
	}

	void UpdateDisplay () {
		if (NextDisplaySegment()) {
			display.text = String.Concat(displayLines);
		}
		else if ((showCursor) && (BlinkCursor())) {
			display.text = String.Concat(displayLines);
		}
	}

	bool NextDisplaySegment () {
		/* I /was/ going to do this all functional-style, but there are /way/ too many moving parts */
		int newPos, newLine;

		if (!stopped) {
			newPos = (timer.Phase > 0.0f) ? Mathf.FloorToInt(timer.Phase * (float) rowlen) : 0;
			newLine = timer.Loops;
		}
		else {
			newPos = cursorPos;
			newLine = currentLine;
		}

		// Only do anything if we've advanced since last time
		if ((newPos > cursorPos) || (newLine > currentLine)) {
			bool canContinue = true;

			// Just in the (absurdly improbable) case we're right at phase one, which should be out 
			// of bounds on any source string -- in which case we'll advance the new cursor position
			// one line, and pretend nobody noticed...
			if (newPos >= rowlen) {
				newPos = 0;
				newLine++;
			}

			// If the line is too short, do a forced rollever on the timer and recalc pos/line
			if (newPos > GetSourceLength()) {
				/*
				if (debugInfo) {
					Debug.Log("Line too short, newPos: " + newPos.ToString() 
						+ ", newLine: " + newLine.ToString() 
						+ ", source length: " + GetSourceLength().ToString(), gameObject);
				}
				*/
				timer.StopPulse();
				timer.ForceRollover();
				newPos = (timer.Phase > 0.0f) ? Mathf.FloorToInt(timer.Phase * (float) rowlen) : 0;
				newLine = timer.Loops;
				/*
				if (debugInfo) {
					Debug.Log("After restart, newPos: " + newPos.ToString() 
						+ ", newLine: " + newLine.ToString() 
						+ ", source length: " + GetSourceLength().ToString(), gameObject);
				}
				*/
			}

			// Catch up to present line
			while (currentLine < newLine) {
				// Finish current line and advance, which will also roll over cursorPos
				canContinue = AppendDisplayWithNewline(GetSourceString(cursorPos, cols - cursorPos));
			}

			if (!canContinue) {
				// Stop running
				StopDisplay();
				if (debugInfo) {
					Debug.Log("Reached end of source, cursorPos: " + cursorPos.ToString() 
						+ ", currentLine: " + currentLine.ToString() 
						+ ", newPos: " + newPos.ToString() 
						+ ", newLine: " + newLine.ToString(),
						gameObject);
				}
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
		if (showCursor) {
			// Assumes there's always a cursor there if in use
			lineBuffer.Remove((lineBuffer.Length - cursorStr.Length), cursorStr.Length);
			lineBuffer.Append(toAppend);
			lineBuffer.Append(cursorStr);
		}
		else {
			lineBuffer.Append(toAppend);
		}
		// Now update display line
		displayLines[displayLine] = lineBuffer.ToString();
	}

	void AppendDisplay (string toAppend, string newlineStr) {
		if (showCursor) {
			// Assumes there's always a cursor there
			lineBuffer.Remove((lineBuffer.Length - cursorStr.Length), cursorStr.Length);
		}
		// Append text and newline
		lineBuffer.Append(toAppend).Append(newlineStr);
		// Now update display line
		displayLines[displayLine] = lineBuffer.ToString();
	}

	bool AppendDisplayWithNewline (string toAppend) {
		// Append to current line, plus newline
		AppendDisplay(toAppend, newlineChar);
		// Advance display and scroll as required
		NextDisplayLine();
		// Try to advance source
		return LoadNextSourceString();
	}

	void NextDisplayLine () {
		// Advance line markers and cursorPos
		// Equivalent to carriage return and new line, essentially
		cursorPos = 0;
		currentLine++;
		displayLine++;

		// Check for rollover and scroll
		if (displayLine >= rows) {
			// Scroll in the desired mode
			if (scrollMode == ScrollMode.ByPage) {
				ClearDisplay();
			}
			else if (scrollMode == ScrollMode.ByLine) {
				for (int i = 0; i < rows - 1; i++) {
					displayLines[i] = displayLines[i+1];
				}
				ClearDisplayLine(rows - 1);
			}
			else {
				Debug.LogError("Somehow NextDisplayLine() got broke trying to " + scrollMode, gameObject);
			}
		}
		else {
			// Still gotta clear the linebuffer
			ClearLine();
		}
	}

	void ClearLine () {
		// Clear line buffer
		// Gotta use Remove() because Unity doesn't like Clear()
		lineBuffer.Remove(0, lineBuffer.Length);

		// Append cursor if used
		if (showCursor) {
			lineBuffer.Append(cursorStr);
		}
	}

	void ClearDisplayLine (int lineNum) {
		// Clear line buffer
		ClearLine();
		// Set new display line
		displayLine = lineNum;
		displayLines[displayLine] = lineBuffer.ToString();
	}

	void ClearDisplay () {
		for (int i = rows - 1; i > 0; i--) {
			displayLines[i] = "";
		}
		ClearDisplayLine(0);
	}

	void ResetDisplay () {
		// Reset cursor position
		cursorPos = 0;
		currentLine = 0;
		// Clear whatever's onscreen
		ClearDisplay();
		// Reset timer
		timer.StopAndResetPulse();
	}

	void StopDisplay () {
		stopped = true;

		if (showCursor) {
			// Reset timer, splitting pulse time between at/after target
			timer.NewPulse(0f, timer.timeToTarget / 2.0f, 0f, timer.timeToTarget / 2.0f, true, true, true, PulseMode.Linear);
		}
		else {
			// Stop timer cycle
			timer.StopPulse();
			if (debugInfo) {
				Debug.Log("Display timer stopped, phase: " + timer.PhaseRaw.ToString() 
					+ ", loops: " + timer.Loops.ToString(), gameObject);
			}
		}
	}

	bool BlinkCursor () {
		if (timer.State != timer.LastState) {
			// Show cursor on odd loops, blank on even ones
			if (timer.State == PulseState.AtTarget) {
				lineBuffer.Remove((lineBuffer.Length - cursorStr.Length), cursorStr.Length);
				lineBuffer.Append(cursorBlink);
			}
			else if (timer.State == PulseState.AfterTarget) {
				lineBuffer.Remove((lineBuffer.Length - cursorBlink.Length), cursorBlink.Length);
				lineBuffer.Append(cursorStr);
			}
			// Now update display line
			displayLines[displayLine] = lineBuffer.ToString();
			// Yes, we need updating
			return true;
		}
		else {
			return false;
		}
	}

	int GetSourceLength () {
		return sourceStr.LengthInTextElements;
	}

	string GetSourceString (int offset, int length) {
		// This was more complicated, but I moved all that to NewSourceString() and LoadNextSourceString()
		if (offset >= sourceStr.LengthInTextElements) {
			return "";
		}
		else if (length <= 0) {
			return "";
		}
		else {
			int maxLen = sourceStr.LengthInTextElements - offset;
			int retLen = (length > maxLen) ? maxLen : length;
			return sourceStr.SubstringByTextElements(offset, retLen);
		}
	}

	StringInfo NewSourceString () {
		// This was all first in GetSourceString(), then LoadNextSourceString()
		// Except I'll have to initialize somehow, so might as well put it in
		// its own function
		string toRet;
		int curLength = sourceLines[sourceLine].Length - sourcePos;

		// Check length of current line and return appropriate amount
		if (curLength <= cols) {
			// Fits, ship it
			// Substring in case we're not at string start
			toRet = sourceLines[sourceLine].Substring(sourcePos);
		}
		else if (curLength > cols) {
			// Too long, truncate
			toRet = sourceLines[sourceLine].Substring(sourcePos, cols);
		}
		/*
		else if (curLength < cols) {
			// Too short, pad it like hell
			toRet = sourceLines[sourceLine].Substring(sourcePos, curLength) + new String(paddingChar, cols - curLength);
		}
		*/
		else {
			Debug.LogError("NewSourceString broke somehow", gameObject);
			return new StringInfo("");
		}

		return CorruptSource(toRet);
	}

	bool LoadNextSourceString () {
		// Call at initialization if you're autostarting
		// Check length, get next string or advance position, check length again, round and round we go

		// Bounds check, bail if fail
		if (sourceLine >= sourceLines.Length) {
			return false;
		}

		// How much do we have left on the current line?
		int curLength = sourceLines[sourceLine].Length - sourcePos;
		if (curLength <= 0) {
			// Nothing more on this source line, on to the next
			sourcePos = 0;
			sourceLine++;
			// Source rollover check
			if (sourceLine >= sourceLines.Length) {
				if (loopSource) {
					// Just go back to the beginning
					sourceLine = 0;
				}
				else {
					// Set empty source string
					// (essentially a newline before EOF)
					sourceStr = new StringInfo("");
					return false;
				}
			}
		}
		sourceStr = NewSourceString();
		// Now advance for next run
		sourcePos += cols;
		return true;
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
			// Limit passes to, at most, the number of chars to corrupt
			if (passes > sourceLen) {
				passes = sourceLen;
			}
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
		char[] splitter = {'\r', '\n'};
		return toParse.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
	}

	int LoadNewSource (string[] toLoad) {
		sourcePos = 0;
		sourceLine = 0;
		//sourceLines = ParseSource(toLoad);
		sourceLines = toLoad;
		return sourceLines.Length;
	}

	public void ReplaceSource (string[] newSource, int offset=0) {
		// Doesn't call LoadNextSourceString() on purpose, because that way we can
		// replace the source while running
		int sourceLen = LoadNewSource(newSource);
		if (offset < sourceLen) {
			sourceLine = offset;
		}
		if (debugInfo) {
			Debug.Log("Replaced source, " + sourceLen.ToString() + " lines", gameObject);
		}
	}

	public void StartScrolling (string[] newSource, bool loopNewSource, int offset=0) {
		// Clear and reset display
		ResetDisplay();
		if (newSource.Length > 0) {
			loopSource = loopNewSource;
			// Load up new source
			ReplaceSource(newSource, offset);
			LoadNextSourceString();
			// Fire away
			timer.NewPulse();
		}
		else {
			Debug.LogWarning("StartScrolling() called with empty string array!", gameObject);
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
