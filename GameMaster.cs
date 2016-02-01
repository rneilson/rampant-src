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
	private static string filename = Application.persistentDataPath + "/settings.cfg";

	static GameSettings () {
		settings = new Dictionary<string, MenuSetting>();
		Initialize();
	}

	static void AddSetting (MenuSetting setting) {
		if (!settings.ContainsKey(setting.Name)) {
			settings.Add(setting.Name, setting);
		}
	}

	// Safe to call multiple times
	static void Initialize () {
		// Setup settings dict
		AddSetting(new VolumeSetting());
		AddSetting(new MouseSpeedSetting("FireCursor"));
		AddSetting(new ResolutionSetting());
		AddSetting(new FullscreenSetting());
		AddSetting(new VsyncSetting());
		AddSetting(new AntialiasSetting());
	}

	static void LoadSetting (string name, string val) {
		if (HasSetting(name)) {
			settings[name].Load(val);
		}
	}

	// Publically-accessible functions

	public static void Quit () {
		SaveSettings();
		Application.Quit();
	}

	public static void LoadSettings () {
		// Load non-persistent settings from file
		// Debug.Log("Configuration save file: " + filename);
		string jsonString;

		// Guard against Unity editor and yet-unwritten cfg file
		if (!Application.isEditor) {
			if (System.IO.File.Exists(filename)) {
				using (System.IO.StreamReader file = new System.IO.StreamReader(filename)) {
					// Read file
					jsonString = file.ReadToEnd();
					// Parse file into SavedSettings
					SavedSettings saved = SavedSettings.LoadFromJson(jsonString);
					// Load applicable settings
					foreach (SavedSettingValue setting in saved.savedSettings) {
						LoadSetting(setting.name, setting.val);
					}
				}
			}
		}
	}

	public static void SaveSettings () {
		// Will save non-persistent settings to file at some point
		string jsonString = SavedSettings.SaveToJson(new SavedSettings(settings));

		// Guard against Unity editor and yet-unwritten cfg file
		if (!Application.isEditor) {
			System.IO.File.WriteAllText(filename, jsonString);
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

// For saving/loading settings from file
[System.Serializable]
public class SavedSettingValue {
	public string name;
	public string val;

	public SavedSettingValue (string name, string val) {
		this.name = name;
		this.val = val;
	}
}

[System.Serializable]
public class SavedSettings {
	public SavedSettingValue[] savedSettings;

	public SavedSettings () {
		savedSettings = new SavedSettingValue[0];
	}

	public SavedSettings (Dictionary<string, MenuSetting> settingDict) {
		// Fresh list
		var settingList = new List<SavedSettingValue>();
		// Check each setting for persistence -- if not, add to saved list
		foreach (MenuSetting setting in settingDict.Values) {
			if (!setting.Persistent) {
				settingList.Add(new SavedSettingValue(setting.Name, setting.Save()));
			}
		}
		// Save list as array, so Unity can serialize it
		// (This would be easier with a proper JSON library)
		// ((But that would mean pulling in a whole JSON library))
		savedSettings = settingList.ToArray();
	}

	public static SavedSettings LoadFromJson (string settingString) {
		return JsonUtility.FromJson<SavedSettings>(settingString);
	}

	public static string SaveToJson (SavedSettings settingsToStringify) {
		return JsonUtility.ToJson(settingsToStringify, true);
	}

}

// Mostly for internal use within MenuControl, in a dict by setting name
public abstract class MenuSetting {
	public MenuSetting () {}

	// Each setting has its own name
	// Slightly circuitous way of making each a singleton
	public abstract string Name { get; }
	public abstract bool Persistent { get; }

	// TODO: allow set?
	public abstract string Value { get; }

	public abstract void Toggle ();
	public abstract void Higher ();
	public abstract void Lower ();
	public abstract void Load (string settingValue);
	public virtual string Save () { return Value; }
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
	public override bool Persistent {
		get { return false; }
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

	public override void Load (string settingValue) {
		AudioListener.volume = System.Single.Parse(settingValue);
	}

	public override string Save () {
		return AudioListener.volume.ToString("F2");
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
	public override bool Persistent {
		get { return false; }
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

	public override void Load (string settingValue) {
		cursor.MouseSpeed = System.Single.Parse(settingValue);
	}

	public override string Save () {
		return cursor.MouseSpeed.ToString("F2");
	}

}

// TODO: resolution class
public class VsyncSetting : MenuSetting {

	public VsyncSetting () {}

	public override string Name {
		get { return "Vsync"; }
	}
	public override bool Persistent {
		get { return false; }
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

	public override void Load (string settingValue) {
		if (settingValue == "On") {
			QualitySettings.vSyncCount = 1;
		}
		else if (settingValue == "Off") {
			QualitySettings.vSyncCount = 0;
		}
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
	public override bool Persistent {
		get { return true; }
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

	public override void Load (string settingValue) {}

}

public class AntialiasSetting : MenuSetting {
	// AA can be 0, 2, 4, or 8

	public AntialiasSetting () {}

	public override string Name {
		get { return "Antialiasing"; }
	}
	public override bool Persistent {
		get { return false; }
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

	public override void Load (string settingValue) {
		QualitySettings.antiAliasing = System.Int32.Parse(settingValue);
	}

	public override string Save () {
		return QualitySettings.antiAliasing.ToString();
	}
}

public class ResolutionSetting : MenuSetting {
	private int currentSetting;
	private int currentWidth;
	private int currentHeight;

	public ResolutionSetting () {
		// Unity doesn't give us anything to link current res to the list of supported res
		// So we find it ourselves
		currentWidth = Screen.width;
		currentHeight = Screen.height;
		int i = 0;
		while ((Screen.resolutions[i].width != currentWidth) 
			&& (Screen.resolutions[i].height != currentHeight)) {
			i++;
			// Fail safely, if inaccurately
			if (i >= Screen.resolutions.Length) {
				i = 0;
				break;
			}
		}
		currentSetting = i;
	}

	public override string Name {
		get { return "Resolution"; }
	}
	public override bool Persistent {
		get { return true; }
	}

	public override string Value {
		get {
			return currentWidth.ToString() + "x" + currentHeight.ToString();
		}
	}

	public override void Toggle () {
		Higher();
	}

	public override void Higher () {
		// Advance index and loop if req'd
		if (++currentSetting >= Screen.resolutions.Length) { currentSetting = 0; }
		currentWidth = Screen.resolutions[currentSetting].width;
		currentHeight = Screen.resolutions[currentSetting].height;
		// Set res to new index
		Screen.SetResolution(currentWidth, currentHeight, Screen.fullScreen);
	}

	public override void Lower () {
		// Retreat index and loop if req'd
		if (--currentSetting < 0) { currentSetting = Screen.resolutions.Length - 1; }
		currentWidth = Screen.resolutions[currentSetting].width;
		currentHeight = Screen.resolutions[currentSetting].height;
		// Set res to new index
		Screen.SetResolution(currentWidth, currentHeight, Screen.fullScreen);
	}

	public override void Load (string settingValue) {}

}

