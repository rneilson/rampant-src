using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameMaster : MonoBehaviour {
	// Modes
	public GameModeSpec[] modes;

	// All this does at the moment is initialize the GameSettings (static) class
	void Awake () {
		// Watch for empty mode list
		if (modes.Length == 0) {
			Debug.LogError("No modes specified!", gameObject);
		}
		// Load modes
		GameSettings.LoadModes(modes);
		// Load settings
		GameSettings.LoadSettings();
		// Enable camera depth texture
		GameObject.Find("Camera").GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
	}
}

public static class GameSettings {
	private static Dictionary<string, MenuSetting> settings;
	private static List<GameMode> modeList;
	private static Dictionary<string, int> modeIndicies;
	private static int currentMode;
	private static string settingsFilename = Application.persistentDataPath + "/settings.cfg";
	private static bool restarted;

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

	static void ParseAndLoad (string toLoad) {
		// Parse string -- should be able to assume SavedValueSet here for now
		SavedValueSet saved = SavedValueSet.LoadFromJson(toLoad);

		// Switch based on name string
		switch (saved.Name) {
			case "Master":
				// Main group, parse each entry
				foreach (string masterString in saved.Values) {
					ParseAndLoad(masterString);
				}
				break;
			case "Settings":
				// Settings group, parse and load each in turn
				foreach (string settingString in saved.Values) {
					SavedValue setting = SavedValue.LoadFromJson(settingString);
					LoadSetting(setting.Name, setting.Value);
				}
				break;
			case "Scores":
				// Scores group, parse scores by mode
				foreach (string modeString in saved.Values) {
					// Parse as base, just to get name
					SavedValueBase baseVal = SavedValueBase.LoadFromJson(modeString);
					// Only proceed if we have this mode on file
					if (modeIndicies.ContainsKey(baseVal.Name)) {
						// Find mode index, get mode, and pass saved string for further parsing
						modeList[modeIndicies[baseVal.Name]].LoadScores(modeString);
					}
				}
				break;
		}
	}

	// Publically-accessible functions

	public static bool Restarted {
		get {return restarted; }
	}
	public static GameMode CurrentMode {
		get { return modeList[currentMode]; }
	}

	public static void Quit () {
		SaveSettings();
		Application.Quit();
	}

	public static void Restart () {
		// Save, because mouse speed (and soon kills/waves) will need to be reloaded
		SaveSettings();
		// Unpause, because now-dead scorer paused
		Time.timeScale = 1;
		// Set restarted flag
		restarted = true;
		// Reload scene from beginning
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}

	public static void LoadSettings () {
		// Load non-persistent settings from file
		// Debug.Log("Configuration save file: " + filename);

		// Guard against Unity editor and yet-unwritten cfg file
		if (!Application.isEditor) {
			if (System.IO.File.Exists(settingsFilename)) {
				using (System.IO.StreamReader file = new System.IO.StreamReader(settingsFilename)) {
					// Read file
					string jsonString = file.ReadToEnd();
					// Delegate parsing to ParseAndLoad (recursively)
					ParseAndLoad(jsonString);
				}
			}
		}
	}

	public static void SaveSettings () {
		// Guard against Unity editor
		if (!Application.isEditor) {
			// Fresh lists
			var masterList = new List<string>();
			var settingList = new List<SavedValue>();	// TODO: change to list of strings
			var scoreList = new List<string>();

			// Check each setting for persistence -- if not, add to saved list
			foreach (MenuSetting setting in settings.Values) {
				if (!setting.Persistent) {
					// TODO: Should have settings stringify themselves, but that's for later
					settingList.Add(new SavedValue(setting.Name, setting.Save()));
				}
			}
			// Add settings to master list
			masterList.Add(SavedValueSet.SaveToJson(new SavedValueSet("Settings", settingList)));

			// Save nonzero scores for each game mode
			foreach (GameMode mode in modeList) {
				scoreList.Add(mode.SaveScores());
			}
			// Add scores to master list
			masterList.Add(SavedValueSet.SaveToJson(new SavedValueSet("Scores", scoreList)));

			// Stringify master list
			string jsonString = SavedValueSet.SaveToJson(new SavedValueSet("Master", masterList));
			// Write to file (will create/overwrite)
			System.IO.File.WriteAllText(settingsFilename, jsonString);
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

	public static void LoadModes (GameModeSpec[] newModes) {
		// Start fresh
		modeList = new List<GameMode>();
		modeIndicies = new Dictionary<string, int>();
		
		// Iterate over modes and add in order
		for (int index = 0; index < newModes.Length; index++) {
			GameMode newMode = new GameMode(newModes[index]);
			modeList.Add(newMode);
			modeIndicies[newMode.Name] = index;
		}

		// Set current mode to first in array as default
		currentMode = 0;
	}
}

// For saving/loading settings from file
[System.Serializable]
public class SavedValueBase {
	public string Name;

	public SavedValueBase (string name) {
		this.Name = name;
	}

	public static SavedValueBase LoadFromJson (string toLoad) {
		return JsonUtility.FromJson<SavedValueBase>(toLoad);
	}

	public static string SaveToJson (SavedValueBase toStringify) {
		return JsonUtility.ToJson(toStringify);
	}
}

[System.Serializable]
public class SavedValue : SavedValueBase {
	public string Value;

	public SavedValue (string name) : base (name) {
		this.Value = "";
	}

	public SavedValue (string name, string val) : base (name) {
		this.Value = val;
	}

	new public static SavedValue LoadFromJson (string toLoad) {
		return JsonUtility.FromJson<SavedValue>(toLoad);
	}

	public static string SaveToJson (SavedValue toStringify) {
		return JsonUtility.ToJson(toStringify);
	}
}

[System.Serializable]
public class SavedValueSet : SavedValueBase {
	public string[] Values;

	public SavedValueSet (string name) : base (name) {
		this.Values = new string[0];
	}

	public SavedValueSet (string name, List<string> listToSave) : base(name) {
		// Already strings, so save list as array
		this.Values = listToSave.ToArray();
	}

	public SavedValueSet (string name, List<SavedValue> listToSave) : base(name) {
		this.Values = new string[listToSave.Count];
		// Stringify each item in list and add to array
		for (int i = 0; i < listToSave.Count; i++) {
			this.Values[i] = SavedValue.SaveToJson(listToSave[i]);
		}
	}

	new public static SavedValueSet LoadFromJson (string toLoad) {
		return JsonUtility.FromJson<SavedValueSet>(toLoad);
	}

	public static string SaveToJson (SavedValueSet toStringify) {
		return JsonUtility.ToJson(toStringify);
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

[System.Serializable]
public class GameModeSpec {
	public string Name;
	public GameObject[] Phases;
	public int TerminalPhase;
}

public class GameMode {
	private string name;
	private List<GameObject> phases;
	private int terminal;
	private Dictionary<string, int> scores;

	public GameMode (GameModeSpec spec) {
		this.name = spec.Name;
		this.phases = new List<GameObject>(spec.Phases);
		if ((spec.TerminalPhase >= 0) && (spec.TerminalPhase < spec.Phases.Length)) {
			this.terminal = spec.TerminalPhase;
		}
		else {
			this.terminal = spec.Phases.Length - 1;
		}
		this.scores = new Dictionary<string, int>();
	}

	public string Name { get { return name; } }
	public int PhaseCount { get { return phases.Count; } }
	public int Terminal { get { return terminal; } }

	public GameObject GetPhase (int index) {
		if ((index >= 0) && (index < phases.Count)) {
			return phases[index];
		}
		else {
			return null;
		}
	}

	public int GetScore (string scoreName) {
		if (scores.ContainsKey(scoreName)) {
			return scores[scoreName];
		}
		else {
			return 0;
		}
	}

	public void HighScore (string scoreName, int scoreVal) {
		// Only update if new score actually higher
		// Will only end up creating score entry if above 0
		if (scoreVal > GetScore(scoreName)) {
			// Ensures new high scores will be created if not already present
			scores[scoreName] = scoreVal;
		}
	}

	// Basically to load from a deserialized StoredValue
	internal void LoadScore (string scoreName, string scoreVal) {
		// Keeping it simple for now
		HighScore(scoreName, System.Int32.Parse(scoreVal));
	}

	// And from a still-serialized StoredValue
	internal void LoadScore (string score) {
		SavedValue saved = SavedValue.LoadFromJson(score);
		LoadScore(saved.Name, saved.Value);
	}

	// Takes a string (of a whole serialized StoredValue) and does the deserialization itself
	internal void LoadScores (string scoresAsString) {
		// First deserialize
		SavedValueSet saved = SavedValueSet.LoadFromJson(scoresAsString);
		// Check if this is really ours (just in case)
		if (saved.Name == this.name) {
			// Pass each stringified value to LoadScore for deserialization
			foreach (string savedVal in saved.Values) {
				LoadScore(savedVal);
			}
		}
	}

	// Could return a SavedValueSet, but that's just pushing the temp instance upward
	// Serializes itself, instead of delegating (unlike the loading process)
	internal string SaveScores () {
		// Pull a fresh list off the pile
		List<SavedValue> values = new List<SavedValue>();
		// Add (only) our stored scores as SavedValue instances
		foreach (string scoreName in scores.Keys) {
			values.Add(new SavedValue(scoreName, scores[scoreName].ToString()));
		}
		// Turn into a SavedValueSet
		SavedValueSet saved = new SavedValueSet(name, values);
		// Now stringify the whole thing
		return SavedValueSet.SaveToJson(saved);
	}

}
