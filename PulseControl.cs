using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PulseControl : MonoBehaviour {

	public float timeToTarget = 0.5f;
	public float timeAtTarget = 0.5f;
	public float timeFromTarget = 0.5f;
	public bool autoStart = false;
	public bool returnToStart = true;
	public bool looping = false;
	public bool debugInfo = false;
	public PulseMode pulseMode = PulseMode.Linear;

	private float counter;
	private float phase;
	private PulseState currentState;

	private const float halfPi = Mathf.PI / 2.0f;

	public PulseState State {
		get { return currentState; }
	}
	public PulseMode Mode {
		get { return pulseMode; }
	}
	public float Phase {
		get {
			if (pulseMode == PulseMode.Linear) {
				return phase;
			}
			else if (pulseMode == PulseMode.Sine) {
				return Mathf.Sin(phase * halfPi);
			}
			else {
				// We should, by all rights, never, *ever* be here
				Debug.LogError("PulseControl in unknown mode: " + pulseMode.ToString(), gameObject);
				return 1.0f;
			}
		}
	}
	public float PhaseRaw {
		get { return phase; }
	}
	public float PulseTime {
		get {
			if (returnToStart) {
				return timeToTarget + timeAtTarget + timeFromTarget;
			}
			else {
				return timeToTarget;
			}
		}
	}
	public float Counter {
		get { return counter; }
	}
	public float Countdown {
		get {
			switch (currentState) {
				case PulseState.Starting:
					return timeToTarget;
				case PulseState.ToTarget:
					return timeToTarget - counter;
				case PulseState.AtTarget:
					return timeAtTarget - counter;
				case PulseState.FromTarget:
					return timeFromTarget - counter;
				case PulseState.Stopped:
					return 0.0f;
				default:
					// We should, by all rights, never, *ever* be here
					Debug.LogError("PulseControl in unknown state: " + currentState.ToString(), gameObject);
					return this.PulseTime;
			}
		}
	}
	public float CountdownTotal {
		get {
			if (currentState == PulseState.Starting) {
				return this.PulseTime;
			}
			else if (currentState == PulseState.ToTarget) {
				return this.PulseTime - counter;
			}
			else if (currentState == PulseState.AtTarget) {
				return timeAtTarget + timeFromTarget - counter;
			}
			else if (currentState == PulseState.FromTarget) {
				return timeFromTarget - counter;
			}
			else if (currentState == PulseState.Stopped) {
				return 0.0f;
			}
			else {
				// We should, by all rights, never, *ever* be here
				Debug.LogError("PulseControl in unknown state: " + currentState.ToString(), gameObject);
				return this.PulseTime;
			}
		}
	}
	public float CountFraction {
		get {
			switch (currentState) {
				case PulseState.Starting:
					return 0.0f;
				case PulseState.ToTarget:
					return counter / timeToTarget;
				case PulseState.AtTarget:
					return counter / timeAtTarget;
				case PulseState.FromTarget:
					return counter / timeFromTarget;
				case PulseState.Stopped:
					return 1.0f;
				default:
					// We should, by all rights, never, *ever* be here
					Debug.LogError("PulseControl in unknown state: " + currentState.ToString(), gameObject);
					return 1.0f;
			}
		}
	}
	public bool AutoStart {
		get {return autoStart; }
	}
	public bool ReturnToStart {
		get {return returnToStart; }
	}
	public bool Looping {
		get {return looping; }
	}

	//	PLEASE NOTE SUBTLE DIFFERENCES BETWEEN THESE (STARTED/RUNNING/CHANGING)
	/*	Since PulseControl runs before anything else, whatever is starting a new pulse is likely doing setup, but also might
		not want to actual run any (possibly expensive) code based on the phase if the phase hasn't changed in the same 
		frame in which the new pulse was started. Therefore, "started" is defined as anything after calling StartNewPulse(),
		"running" is ToTarget and onward (at least one frame), and "changing" is when the phase value is not constant (ie 
		not holding in place). This allows the user of PulseControl to fine-tune if and when the phase is used.

		This is especially valuable, I think, given the staggered execution order and message sending. For example, if
		something calls StartNewPulse() during its Start(), we'll switch to ToTarget during our LateUpdate() -- the caller 
		sees us at Starting during its Update(), and we'll spend at least one frame running. Or, as another example of 
		staggered execution, if a MaterialPulse has its StartPulse() called by Scorer, MaterialPulse will swap out its material, 
		and then later in the same frame, it will have its own Update() called -- and it may wish to differentiate, since both 
		run Update() after PulseControl. 

		If, for whatever reason, you'd like a running state immediately, call QuickStart() after StartNewPulse, which will
		immediately set the state to ToTarget (and thus to running, as opposed to starting). The phase will remain at 0, and 
		thus can be used right away (accurately reflecting a lack of dT between Starting and ToTarget). */
	
	public bool IsStarted {
		get {
			switch (currentState) {
				// If we're starting, we've started -- contrast with below
				case PulseState.Starting:
				case PulseState.ToTarget:
				case PulseState.AtTarget:
				case PulseState.FromTarget:
					return true;
				case PulseState.Stopped:
					return false;
				default:
					// We should, by all rights, never, *ever* be here
					Debug.LogError("PulseControl in unknown state: " + currentState.ToString(), gameObject);
					return false;
			}
		}
	}
	public bool IsRunning {
		get {
			switch (currentState) {
				case PulseState.ToTarget:
				case PulseState.AtTarget:
				case PulseState.FromTarget:
					return true;
				// Here's a litte weirdness: if we're start*ing*, we haven't actually start*ed*, so we're not running yet
				case PulseState.Starting:
				case PulseState.Stopped:
					return false;
				default:
					// We should, by all rights, never, *ever* be here
					Debug.LogError("PulseControl in unknown state: " + currentState.ToString(), gameObject);
					return false;
			}
		}
	}
	public bool IsChanging {
		get {
			switch (currentState) {
				case PulseState.ToTarget:
				case PulseState.FromTarget:
					return true;
				// Here's another litte weirdness: if we're starting, we're not changing *yet*
				case PulseState.Starting:
				case PulseState.AtTarget:
				case PulseState.Stopped:
					return false;
				default:
					// We should, by all rights, never, *ever* be here
					Debug.LogError("PulseControl in unknown state: " + currentState.ToString(), gameObject);
					return false;
			}
		}
	}
	// END SUBTLE DIFFERENCES (AGAIN, NOTE!!!)

	// Use this for initialization
	void Start () {
		phase = 0.0f;
		counter = 0.0f;

		// Sanity checks
		timeToTarget = (timeToTarget < 0.0f) ? 0.0f : timeToTarget;
		timeAtTarget = (timeAtTarget < 0.0f) ? 0.0f : timeAtTarget;
		timeFromTarget = (timeFromTarget < 0.0f) ? 0.0f : timeFromTarget;

		// Initialize to stopped
		currentState = PulseState.Stopped;

		// If autostarting, this frame we'll switch from Starting to ToTarget, and only next frame have the
		// possibility of stopping
		if (autoStart) {
			StartNewPulse();	// State now started, but not yet running (will be later this frame)
		}
	}
	
	// Update is called once per frame
	void Update () {
		UpdateState(Time.deltaTime);
		UpdatePhase();
	}

	void LateUpdate () {
		if (currentState == PulseState.Starting) {
			// If we were started earlier this frame (or late last), we can now switch from Starting
			ChangeState(PulseState.ToTarget);
		}
	}

	void ChangeState (PulseState toState) {
		// Print debug
		if (debugInfo) {
			Debug.Log("Switching currentState to " + toState.ToString() 
				+ ", counter: " + counter.ToString() 
				+ ", phase: " + phase.ToString(), 
				gameObject);
		}
		currentState = toState;
		// TODO: add notifications
	}

	// State and counter update, only meant to be called while running
	// Please see big distinction rant above, just before IsStarted
	void UpdateState (float dT) {
		if (this.IsRunning) {
			// Only change to other states if there's been some actual time since last frame
			// This means there will always be at least one frame from Starting to Stopped
			// (More if game time passage is zero)
			if (dT > 0.0f) {
				// Add dt to counter
				counter += dT;
				// Check if time reached (and *which* time), and set currentState accordingly
				// Note that there is fall-through, so if timeAtTarget and timeFromTarget are 
				// both zero, we'll go straight to Stopped (if at least some small amount of
				// time has passed after Starting)
				if (currentState == PulseState.ToTarget) {
					if (counter >= timeToTarget) {
						// Check if we're returnToStart from target
						if (returnToStart) {
							// Check if we're holding at target
							if (timeAtTarget > 0.0f) {
								// Hold state (sets phase to 1)
								HoldPulse();
							}
							else {
								// Switch currentState
								ChangeState(PulseState.FromTarget);
							}
							// Set time to target->initial, less any overshoot
							counter = counter - timeToTarget;
						}
						else {
							// Stop and set phase to 1, since we're done
							StopPulse(1.0f);
						}
					}
				}
				if (currentState == PulseState.AtTarget) {
					if (counter >= timeAtTarget) {
						// Switch currentState
						ChangeState(PulseState.FromTarget);
						// Set time to target->initial, less any overshoot
						counter = counter - timeAtTarget;
					}
				}
				if (currentState == PulseState.FromTarget) {
					if (counter >= timeFromTarget) {
						// Check if we're looping
						if (looping) {
							// Switch currentState
							ChangeState(PulseState.ToTarget);
							// Set time to initial->target, less any overshoot
							counter = counter - timeFromTarget;
						}
						else {
							// Stop and set phase to 0, since we're done
							StopPulse(0.0f);
						}
					}
				}
			}
		}
	}

	// Update phase value according to cycle
	void UpdatePhase () {
		// Calulate phase depending on whether we're (currently) going to/from target
		if (currentState == PulseState.ToTarget) {
			phase = counter / timeToTarget;
		}
		else if (currentState == PulseState.FromTarget) {
			phase = 1.0f - (counter / timeFromTarget);
		}
	}

	// Start pulse cycle
	public bool StartNewPulse () {
		// Only start if we're stopped
		if (currentState == PulseState.Stopped) {
			ChangeState(PulseState.Starting);
			phase = 0.0f;
			counter = 0.0f;
			if (debugInfo) {
				Debug.Log("Starting pulse, counter: " + counter.ToString() + ", phase: " 
					+ phase.ToString(), gameObject);
			}
			return true;
		}
		else if (currentState == PulseState.Starting) {
			if (debugInfo) {
				Debug.LogWarning("StartNewPulse() called on already-started PulseControl", gameObject);
			}
			return true;
		}
		else {
			// We should really never be here
			if (debugInfo) {
				Debug.LogWarning("StartNewPulse() called on running PulseControl", gameObject);
			}
			return false;
		}
	}

	// Switch to running state immediately, instead of waiting until LateUpdate() (probably meaning
	// next frame, for most callers)
	// Returns true if was starting and is now running
	public bool QuickStart () {
		if (currentState == PulseState.Starting) {
			// If we were started earlier this frame (or late last), we can now switch from Starting
			ChangeState(PulseState.ToTarget);
			return true;
		}
		else {
			return false;
		}
	}

	// Basically emulates an Update() call with given dT and sets phase accordingly -- might be useful
	// for using PulseControl outside of the game loop or...something?
	// Returns true if running (and thus if the call does anything)
	public bool ForceUpdate (float dT) {
		if (this.IsRunning) {
			UpdateState(dT);
			UpdatePhase();
			return true;
		}
		else {
			return false;
		}
	}

	// Hold pulse cycle at target
	void HoldPulse () {
		// Switch currentState
		ChangeState(PulseState.AtTarget);
		// Set phase to one, since we're at target
		phase = 1.0f;
	}

	// Stop pulse cycle at given phase
	void StopPulse (float finalPhase) {
		// Switch currentState
		ChangeState(PulseState.Stopped);
		// Set phase to given value, since we're done
		phase = finalPhase;
	}

	// Start new pulse, possibly interrupting
	public void NewPulse (float newTimeTo, float newTimeAt, float newTimeFrom, bool newReturn, bool newLoop, PulseMode newMode) {
		// Debug
		if (debugInfo) {
			if (this.IsStarted) {
				Debug.Log("New pulse started, interrupting current one", gameObject);
			}
			else {
				Debug.Log("New pulse started, previous one already stopped", gameObject);
			}
			Debug.Log("Current state: " + currentState.ToString() 
				+ ", counter: " + counter.ToString() + ", phase: " + phase.ToString(), 
				gameObject);
		}
		// Stop current/previous pulse
		StopPulse(0.0f);
		// Set new values
		timeToTarget = newTimeTo;
		timeAtTarget = newTimeAt;
		timeFromTarget = newTimeFrom;
		returnToStart = newReturn;
		looping = newLoop;
		pulseMode = newMode;
		// Start new pulse, and away we go
		StartNewPulse();
	}

	// Lazy version, keeps present values
	public void NewPulse () {
		NewPulse(timeToTarget, timeAtTarget, timeFromTarget, returnToStart, looping, pulseMode);
	}
}

public enum PulseMode : byte {
	Linear = 0,
	Sine
}

public enum PulseState : byte {
	Stopped = 0,
	Starting,
	ToTarget,
	FromTarget,
	AtTarget
}
