using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameMaster : MonoBehaviour {
	// All this does at the moment is initialize the GameSettings (static) class
	void Awake () {
		// Load settings
		GameSettings.LoadSettings();
		// Enable camera depth texture
		GameObject.Find("Camera").GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
	}
}

public static class GameSettings {
	private static Dictionary<string, MenuSetting> settings;

	static GameSettings () {
		settings = new Dictionary<string, MenuSetting>();
		Initialize();
	}

	private static void AddSetting (MenuSetting setting) {
		if (!settings.ContainsKey(setting.Name)) {
			settings.Add(setting.Name, setting);
		}
	}

	// Safe to call multiple times
	private static void Initialize () {
		// Setup settings dict
		AddSetting(new VolumeSetting());
		AddSetting(new MouseSpeedSetting("FireCursor"));
		AddSetting(new FullscreenSetting());
		AddSetting(new VsyncSetting());
		AddSetting(new AntialiasSetting());
	}

	// Publically-accessible functions

	public static void LoadSettings () {
		// Will load storable settings from file at some point
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
	public MenuSetting () {}

	// Each setting has its own name
	// Slightly circuitous way of making each a singleton
	public abstract string Name { get; }

	// TODO: allow set?
	public abstract string Value { get; }

	public abstract void Toggle ();
	public abstract void Higher ();
	public abstract void Lower ();
}

public class VolumeSetting : MenuSetting {
	private const int increment = 5;
	private float savedVolume;

	// Don't need anything extra
	// Technically this only controls overall game volume
	// Might need a wrapper class or something for different GameObject volumes
	public VolumeSetting () {
		this.savedVolume = AudioListener.volume;
	}

	public override string Name {
		get { return "Volume"; }
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
	public MouseSpeedSetting (string cursorName) {
		cursor = GameObject.Find(cursorName).GetComponent<CursorMovement>();
	}

	public override string Name {
		get { return "MouseSpeed"; }
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
public class VsyncSetting : MenuSetting {

	public VsyncSetting () {}

	public override string Name {
		get { return "Vsync"; }
	}

	public override string Value {
		get {
			if (QualitySettings.vSyncCount > 0) {
				return "On";
			}
			else {
				return "Off";
			}
		}
	}

	public override void Toggle () {
		if (QualitySettings.vSyncCount > 0) {
			QualitySettings.vSyncCount = 0;
		}
		else {
			QualitySettings.vSyncCount = 1;
		}
	}

	public override void Higher () {
		QualitySettings.vSyncCount = 1;
	}

	public override void Lower () {
		QualitySettings.vSyncCount = 0;
	}
}

public class FullscreenSetting : MenuSetting {
	private bool currentSetting;

	public FullscreenSetting () {
		currentSetting = Screen.fullScreen;
	}

	public override string Name {
		get { return "Fullscreen"; }
	}

	public override string Value {
		get {
			if (currentSetting) {
				return "On";
			}
			else {
				return "Off";
			}
		}
	}

	public override void Toggle () {
		currentSetting = !currentSetting;
		Screen.fullScreen = currentSetting;
	}

	public override void Higher () {
		currentSetting = true;
		Screen.fullScreen = currentSetting;
	}

	public override void Lower () {
		currentSetting = false;
		Screen.fullScreen = currentSetting;
	}
}

public class AntialiasSetting : MenuSetting {
	// AA can be 0, 2, 4, or 8

	public AntialiasSetting () {}

	public override string Name {
		get { return "Antialiasing"; }
	}

	public override string Value {
		get {
			if (QualitySettings.antiAliasing == 0) {
				return "No AA";
			}
			else {
				return QualitySettings.antiAliasing.ToString() + "x AA";
			}
		}
	}

	public override void Toggle () {
		Higher();
	}

	public override void Higher () {
		switch (QualitySettings.antiAliasing) {
			case 0:
				QualitySettings.antiAliasing = 2;
				break;
			case 2:
				QualitySettings.antiAliasing = 4;
				break;
			case 4:
				QualitySettings.antiAliasing = 8;
				break;
			case 8:
				QualitySettings.antiAliasing = 0;
				break;
		}
	}

	public override void Lower () {
		switch (QualitySettings.antiAliasing) {
			case 0:
				QualitySettings.antiAliasing = 8;
				break;
			case 2:
				QualitySettings.antiAliasing = 0;
				break;
			case 4:
				QualitySettings.antiAliasing = 2;
				break;
			case 8:
				QualitySettings.antiAliasing = 4;
				break;
		}
	}
}
