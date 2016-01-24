using UnityEngine;
using System.Collections;
using System;

public class ScrollCode : MonoBehaviour {

	// Related things
	private ScrollCodeBox boxLeft;
	private ScrollCodeBox boxRight;
	private PulseControl colorTimer;

	// Scaling
	//private float baselineX = 1280f;
	//private float baselineY = 640f;
	//private float screenScale;

	// Color things
	public Color initialTarget = Color.white;
	public float blendFraction = 1.0f;
	public bool blendWithStart = true;
	private Color colorStartLeft;
	private Color colorStartRight;
	private Color colorTargetLeft;
	private Color colorTargetRight;
	private Color colorFinalLeft;
	private Color colorFinalRight;
	private bool colorPulsing = false;

	public bool debugInfo = false;

	// Text things
	private string initialText = 
@"------------------------
        _rampant        
------------------------

       _a_game_by
    _raymond_neilson";
    private string[] initialLines;
    public TextAsset sourceText;
    public TextAsset sourceOffsets;
    private string[] sourceLines;
    private int[] sourceLineNums;

	// Use this for initialization
	void Start () {
		// Sanity checks
		blendFraction = (blendFraction > 1.0f) ? 1.0f : blendFraction;

		// Get screen scaling size
		//screenScale = 

		// Get textboxes
		//boxLeft = GameObject.Find("Scroll Text Left").GetComponent<ScrollCodeBox>();
		//boxRight = GameObject.Find("Scroll Text Right").GetComponent<ScrollCodeBox>();
		boxLeft = GameObject.Find("Scroll Code Left").GetComponent<ScrollCodeBox>();
		boxRight = GameObject.Find("Scroll Code Right").GetComponent<ScrollCodeBox>();

		// Get colors
		colorStartLeft = boxLeft.GetCurrentColor();
		colorStartRight = boxRight.GetCurrentColor();
		colorTargetLeft = initialTarget * blendFraction;
		colorTargetRight = initialTarget * blendFraction;
		if (blendWithStart) {
			colorTargetLeft = colorTargetLeft * colorStartLeft;
			colorTargetRight = colorTargetRight * colorStartRight;
		}
		colorFinalLeft = colorStartLeft;
		colorFinalRight = colorStartRight;

		// Get timer, start counter
		colorTimer = GetComponent<PulseControl>();
		colorTimer.NewPulse();
		colorPulsing = true;

		// Setup text
		string[] splitLong = {"\r\n", "\n"};
		char[] splitShort = {'\n'};
		initialLines = ParseSource(initialText, splitLong);
		sourceLines = ParseSource(sourceText.text, splitShort);
		sourceLineNums = ParseOffsets(sourceOffsets.text);

		if (debugInfo) {
			Debug.LogFormat("Loaded source, {0} files, {1} lines", sourceLineNums.Length, sourceLines.Length);
			for (int i=0; i < sourceLineNums.Length; i++) {
				Debug.LogFormat("{0}] {1}", sourceLineNums[i], sourceLines[sourceLineNums[i]]);
			}
		}

		// Start boxes scrolling with intro text
		boxLeft.StartScrolling(initialLines, false, 0);
		boxRight.StartScrolling(initialLines, false, 0);

		// Load sourcecode text asset
		//sourceText = Resources.Load("sourcecode") as TextAsset;
	}
	
	// Update is called once per frame
	void Update () {
		if (colorPulsing) {
			// Update color if timer running
			if (colorTimer.IsStarted) {
				if (colorTimer.State == PulseState.ToTarget) {
					boxLeft.SetColorTo(Color.Lerp(colorStartLeft, colorTargetLeft, colorTimer.Phase));
					boxRight.SetColorTo(Color.Lerp(colorStartRight, colorTargetRight, colorTimer.Phase));
				}
				else if (colorTimer.State == PulseState.AtTarget) {
					boxLeft.SetColorTo(colorTargetLeft);
					boxRight.SetColorTo(colorTargetRight);
				}
				else if (colorTimer.State == PulseState.FromTarget) {
					boxLeft.SetColorTo(Color.Lerp(colorFinalLeft, colorTargetLeft, colorTimer.Phase));
					boxRight.SetColorTo(Color.Lerp(colorFinalRight, colorTargetRight, colorTimer.Phase));
				}
			}
			else {
				colorPulsing = false;
				if (colorTimer.ReturnToStart) {
					boxLeft.SetColorTo(colorFinalLeft);
					boxRight.SetColorTo(colorFinalRight);
				}
				else {
					boxLeft.SetColorTo(colorTargetLeft);
					boxRight.SetColorTo(colorTargetRight);
				}
			}
		}
	}

	// Pulse new color
	public void NewPulseMsg (Color toColor) {
		colorStartLeft = boxLeft.GetCurrentColor();
		colorStartRight = boxRight.GetCurrentColor();
		if (blendWithStart) {
			colorTargetLeft = toColor * colorFinalLeft;
			colorTargetRight = toColor * colorFinalRight;
		}
		else {
			colorTargetLeft = toColor;
			colorTargetRight = toColor;
		}
		colorTimer.NewPulse();
		colorPulsing = true;
	}

	// Start boxes scrolling main text
	public void GameStarted () {
		// Start each side at a different offset
		int leftOffset = UnityEngine.Random.Range(0, sourceLineNums.Length);
		int rightOffset = leftOffset;
		while (rightOffset == leftOffset) {
			rightOffset = UnityEngine.Random.Range(0, sourceLineNums.Length);
		}
		// Okay, now start
		boxLeft.StartScrolling(sourceLines, true, sourceLineNums[leftOffset]);
		boxRight.StartScrolling(sourceLines, true, sourceLineNums[rightOffset]);
	}

	string[] ParseSource (string toParse, char[] splitter) {
		return toParse.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
	}

	string[] ParseSource (string toParse, string[] splitter) {
		return toParse.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
	}

	int[] ParseOffsets (string toParse) {
		char[] splitShort = {'\n'};

		// Grab text representations
		string[] tmpStrs = toParse.Split(splitShort, StringSplitOptions.RemoveEmptyEntries);

		// Start array
		int[] tmpInts = new int[tmpStrs.Length];

		// Loop and convert
		for (int i=0; i < tmpInts.Length; i++) {
			tmpInts[i] = Int32.Parse(tmpStrs[i]);
		}

		return tmpInts;
	}

}
