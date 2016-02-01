using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameMaster : MonoBehaviour {
	// All this does at the moment is initialize the GameSettings (static) class
	void Awake () {
		GameSettings.Initialize();
	}
}

public static class GameSettings {
	private static Dictionary<string, MenuSetting> settings;

	static GameSettings () {
		settings = new Dictionary<string, MenuSetting>();
	}

	// Safe to call multiple times
	public static void Initialize () {
		// Setup settings dict
		if (!settings.ContainsKey("Volume")) {
			settings.Add("Volume", new VolumeSetting("Volume"));
		}
		if (!settings.ContainsKey("MouseSpeed")) {
			settings.Add("MouseSpeed", new MouseSpeedSetting("MouseSpeed", "FireCursor"));
		}
	}

	public static bool HasSetting (string settingName) {
		return settings.ContainsKey(settingName);
	}

	public static string GetSetting (string settingName) {
		if (HasSetting(settingName)) {
			return settings[settingName].Value;
		}
		else {
			return "";
		}
	}

	public static void ToggleSetting (string settingName) {
		if (HasSetting(settingName)) {
			settings[settingName].Toggle();
		}
	}

	public static void RaiseSetting (string settingName) {
		if (HasSetting(settingName)) {
			settings[settingName].Higher();
		}
	}

	public static void LowerSetting (string settingName) {
		if (HasSetting(settingName)) {
			settings[settingName].Lower();
		}
	}
}

// Mostly for internal use within MenuControl, in a dict by setting name
public abstract class MenuSetting {
	protected string name;

	public MenuSetting (string name) {
		this.name = name;
	}

	public string Name {
		get { return name; }
	}
	// TODO: allow set?
	public abstract string Value { get; }

	public abstract void Toggle ();
	public abstract void Higher ();
	public abstract void Lower ();
}

public class EmptySetting : MenuSetting {
	public EmptySetting () : base("") {}
	public override string Value { get { return ""; } }
	public override void Toggle () {}
	public override void Higher () {}
	public override void Lower () {}
}

public class VolumeSetting : MenuSetting {
	private const int increment = 5;
	private float savedVolume;

	// Don't need anything extra
	// Technically this only controls overall game volume
	// Might need a wrapper class or something for different GameObject volumes
	public VolumeSetting (string name) : base (name) {
		this.savedVolume = AudioListener.volume;
	}

	// Returns volume in percent
	public override string Value {
		get {
			int vol = Mathf.RoundToInt(AudioListener.volume * 100.0f);
			return System.String.Format("{0,3}%", vol);
		}
	}

	public override void Toggle () {
		// Mutes if volume positive, puts volume back if muted
		if (AudioListener.volume == 0.0f) {
			AudioListener.volume = savedVolume;
		}
		else {
			savedVolume = AudioListener.volume;
			AudioListener.volume = 0.0f;
		}
	}

	public override void Higher () {
		int vol = Mathf.RoundToInt(AudioListener.volume * 100.0f);
		vol += increment;
		if (vol > 100) {
			vol = 100;
		}
		AudioListener.volume = ((float) vol) / 100.0f;
	}

	public override void Lower () {
		int vol = Mathf.RoundToInt(AudioListener.volume * 100.0f);
		vol -= increment;
		if (vol < 0) {
			vol = 0;
		}
		AudioListener.volume = ((float) vol) / 100.0f;
	}

}

public class MouseSpeedSetting : MenuSetting {
	private const float increment = 0.05f;
	private CursorMovement cursor;

	// Don't need anything extra
	// Technically this only controls overall game volume
	// Might need a wrapper class or something for different GameObject volumes
	public MouseSpeedSetting (string name, string cursorName) : base (name) {
		cursor = GameObject.Find(cursorName).GetComponent<CursorMovement>();
	}

	// Returns volume in percent
	public override string Value {
		get {
			float speed = Mathf.Round(cursor.MouseSpeed / increment) * increment;
			return speed.ToString("F2");
		}
	}

	public override void Toggle () {
		// Nothing to toggle, really...
	}

	public override void Higher () {
		float speed = Mathf.Round(cursor.MouseSpeed / increment);
		speed = (speed + 1.0f) * increment;
		if (speed > 1.0f) {
			speed = 1.0f;
		}
		cursor.MouseSpeed = speed;
	}

	public override void Lower () {
		float speed = Mathf.Round(cursor.MouseSpeed / increment);
		speed = (speed - 1.0f) * increment;
		if (speed < 0.0f) {
			speed = 0.0f;
		}
		cursor.MouseSpeed = speed;
	}

}

// TODO: resolution class
