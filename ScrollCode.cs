using UnityEngine;
using System.Collections;

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

	// Text things
	private string initialText = 
@"------------------------
        _rampant        
------------------------

       _a_game_by
    _raymond_neilson";
    public TextAsset sourceText;

	// Use this for initialization
	void Start () {
		// Sanity checks
		blendFraction = (blendFraction > 1.0f) ? 1.0f : blendFraction;

		// Get screen scaling size
		//screenScale = 

		// Get textboxes
		//boxLeft = GameObject.Find("Scroll Text Left").GetComponent<ScrollCodeBox>();
		//boxRight = GameObject.Find("Scroll Text Right").GetComponent<ScrollCodeBox>();
		GameObject tmpObj;
		tmpObj = GameObject.Find("Scroll Code Left");
		if (tmpObj) {boxLeft = tmpObj.GetComponent<ScrollCodeBox>();}
		tmpObj = GameObject.Find("Scroll Code Right");
		if (tmpObj) {boxRight = tmpObj.GetComponent<ScrollCodeBox>();}

		// Get colors
		if (boxLeft) {colorStartLeft = boxLeft.GetCurrentColor();}
		if (boxRight) {colorStartRight = boxRight.GetCurrentColor();}
		colorTargetLeft = initialTarget * blendFraction;
		colorTargetRight = initialTarget * blendFraction;
		if (blendWithStart) {
			if (boxLeft) {colorTargetLeft = colorTargetLeft * colorStartLeft;}
			if (boxRight) {colorTargetRight = colorTargetRight * colorStartRight;}
		}
		if (boxLeft) {colorFinalLeft = colorStartLeft;}
		if (boxRight) {colorFinalRight = colorStartRight;}

		// Get timer, start counter
		colorTimer = GetComponent<PulseControl>();
		colorTimer.NewPulse();
		colorPulsing = true;

		// Start boxes scrolling with intro text
		if (boxLeft) {boxLeft.StartScrolling(initialText, false);}
		if (boxRight) {boxRight.StartScrolling(initialText, false);}

		// Load sourcecode text asset
		//sourceText = Resources.Load("sourcecode") as TextAsset;
	}
	
	// Update is called once per frame
	void Update () {
		if (colorPulsing) {
			// Update color if timer running
			if (colorTimer.IsStarted) {
				if (colorTimer.State == PulseState.ToTarget) {
					if (boxLeft) {boxLeft.SetColorTo(Color.Lerp(colorStartLeft, colorTargetLeft, colorTimer.Phase));}
					if (boxRight) {boxRight.SetColorTo(Color.Lerp(colorStartRight, colorTargetRight, colorTimer.Phase));}
				}
				else if (colorTimer.State == PulseState.AtTarget) {
					if (boxLeft) {boxLeft.SetColorTo(colorTargetLeft);}
					if (boxRight) {boxRight.SetColorTo(colorTargetRight);}
				}
				else if (colorTimer.State == PulseState.FromTarget) {
					if (boxLeft) {boxLeft.SetColorTo(Color.Lerp(colorFinalLeft, colorTargetLeft, colorTimer.Phase));}
					if (boxRight) {boxRight.SetColorTo(Color.Lerp(colorFinalRight, colorTargetRight, colorTimer.Phase));}
				}
			}
			else {
				colorPulsing = false;
				if (colorTimer.ReturnToStart) {
					if (boxLeft) {boxLeft.SetColorTo(colorFinalLeft);}
					if (boxRight) {boxRight.SetColorTo(colorFinalRight);}
				}
				else {
					if (boxLeft) {boxLeft.SetColorTo(colorTargetLeft);}
					if (boxRight) {boxRight.SetColorTo(colorTargetRight);}
				}
			}
		}
	}

	// Pulse new color
	public void NewPulseMsg (Color toColor) {
		if (boxLeft) {colorStartLeft = boxLeft.GetCurrentColor();}
		if (boxRight) {colorStartRight = boxRight.GetCurrentColor();}
		if (blendWithStart) {
			if (boxLeft) {colorTargetLeft = toColor * colorFinalLeft;}
			if (boxRight) {colorTargetRight = toColor * colorFinalRight;}
		}
		else {
			if (boxLeft) {colorTargetLeft = toColor;}
			if (boxRight) {colorTargetRight = toColor;}
		}
		colorTimer.StopPulse();
		colorTimer.NewPulse();
		colorPulsing = true;
	}

	// Start boxes scrolling main text
	public void GameStarted () {
		// TODO: send sourcecode asset to boxes to scroll
		if (boxLeft) {boxLeft.StartScrolling(sourceText.text, true);}
		if (boxRight) {boxRight.StartScrolling(sourceText.text, true);}
	}
}
