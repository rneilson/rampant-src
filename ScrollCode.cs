﻿using UnityEngine;
using System.Collections;

public class ScrollCode : MonoBehaviour {

	// Related things
	private ScrollCodeBox boxLeft;
	private ScrollCodeBox boxRight;
	private PulseControl colorTimer;

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

	// Use this for initialization
	void Start () {
		// Sanity checks
		blendFraction = (blendFraction > 1.0f) ? 1.0f : blendFraction;

		// Get textboxes
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
		colorTimer.StartNewPulse();
		colorPulsing = true;
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
	void NewPulseMsg (Color toColor) {
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
}
