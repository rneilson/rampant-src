using UnityEngine.UI;
using UnityEngine;
using System.Collections;
using System.Globalization;
using System.Text;
using System;

public class ScrollCodeBox : MonoBehaviour {

	// Components
	// private TextMesh display;
	private Text display;
	private PulseControl timer;

	// Textbox sizes
	private int cols = 24;	// Not including newline
	private int rowlen;		// Including newline
	private int rows = 24;
	private int boxChars;
	private int removeChars;

	// Cursor state
	public int cursorPos = 0;	// Public for debug
	public int cursorOld = 0;	// Public for debug
	//private int cursorPos = 0;
	//private int cursorLine = 0;
	//private int cursorPage = 0;
	//private int cursorOffset = 0;

	// String data
	private StringBuilder workhorse;
	public String initialText;		// Public for debug
	public String availableChars;	// Public for debug

	// Controls
	public bool debugInfo = false;
	public int removeLines = 24; 

	// Use this for initialization
	void Start () {
		// Stupid compiler won't let me put these in initialization...
		rowlen = cols + 1;
		boxChars = rows * rowlen;
		removeLines = (removeLines > rows) ? rows : removeLines;
		removeChars = removeLines * rowlen;

		// Grab components, obvs
		// display = GetComponent<TextMesh>();
		display = GetComponent<Text>();
		timer = GetComponent<PulseControl>();

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

		// MORE TEMP CODE
		// Just to get scrolling working
		workhorse.Remove(0, workhorse.Length);
		workhorse.EnsureCapacity(boxChars);
	}
	
	// Update is called once per frame
	void Update () {
		// TEMP CODE
		UpdateDisplayText(initialText);
	}

	string GetCursorStr (string sourceStr) {
		int cursorPage, cursorLine, cursorCol, cursorOffset;
		string strToRet;

		// Stash last cursor position
		cursorOld = cursorPos;

		// Find new cursor position
		cursorPage = timer.Loops / rows;
		cursorLine = timer.Loops % rows;
		cursorCol = Mathf.FloorToInt(timer.Phase * (float) rowlen);
		cursorPos = ((cursorPage * boxChars) + (cursorLine * rowlen) + cursorCol) % sourceStr.Length;

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
			/*
			if (debugInfo) {
				Debug.Log("Textbox wrapped around, length " + workhorse.Length.ToString(), gameObject);
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
			*/
			workhorse.Remove(0, removeChars);
		}
		// Update display
		display.text = workhorse.ToString();
	}

	public Color GetCurrentColor () {
		return display.color;
	}

	public void SetColorTo(Color toColor) {
		display.color = toColor;
	}
}
