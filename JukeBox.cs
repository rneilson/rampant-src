//  Copyright 2016 Raymond Neilson
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using UnityEngine;
using System.Collections;

public class JukeBox : MonoBehaviour {

	public AudioClip[] trackList;
	public int menuTrack = 0;
	public int defaultTrack = 0;
	public float maxVolume = 0.5f;
	public bool playMenu = true;

	private int currentTrack;
	private AudioSource audioPlayer;

	public int TrackCount {
		get { return trackList.Length; }
	}
	public int CurrentTrack {
		get { return currentTrack; }
		set {
			int newTrack = ((value < 0) || (value > trackList.Length)) ? 0 : value;
			currentTrack = newTrack;
		}
	}
	public float Volume {
		get {
			return audioPlayer.volume / maxVolume;
		}
		set {
			float newVol;
			if (value > 1.0f) {
				newVol = 1.0f;
			}
			else if (value < 0.0f) {
				newVol = 0.0f;
			}
			else {
				newVol = value;
			}
			audioPlayer.volume = newVol * maxVolume;
		}
	}

	void Awake () {
		audioPlayer = GetComponent<AudioSource>();
		if (menuTrack > trackList.Length) {
			menuTrack = 0;
		}
		if (playMenu) {
			PlayTrack(menuTrack);
		}
		currentTrack = defaultTrack;
	}
	
	void Update () {
	
	}

	void PlayTrack (int trackNum) {
		// Stop current
		audioPlayer.Stop();
		// Set new track
		if ((trackNum == 0) || (trackNum > trackList.Length)) {
			audioPlayer.clip = null;
		}
		else {
			audioPlayer.clip = trackList[trackNum - 1];
			audioPlayer.loop = true;
			audioPlayer.Play();
		}
	}

	void PlayTrack () {
		PlayTrack(currentTrack);
	}

	public void SetTrack (int trackNum) {
		CurrentTrack = trackNum;
		if (GameSettings.Restarted) {
			PlayTrack();
		}
	}

	public string GetTrackName (int trackNum) {
		if ((trackNum > 0) && (trackNum <= trackList.Length)) {
			return trackList[trackNum - 1].name;
		}
		else {
			return "[None]";
		}
	}

	public string GetTrackName () {
		return GetTrackName(currentTrack);
	}

	public void GameStarted () {
		PlayTrack(currentTrack);
	}

}
