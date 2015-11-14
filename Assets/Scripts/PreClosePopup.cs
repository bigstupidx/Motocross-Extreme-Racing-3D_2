﻿using UnityEngine;
using System.Collections;

public class PreClosePopup : MonoBehaviour {

	public static bool showPopup = false;
	private float scale = 0f;

	public MusicSfx musicOBJ;
	void Update () 
	{
		if (Input.GetKeyDown(KeyCode.Escape) && showPopup)
		{
			showPopup = false;
			resumeGame();
		}
	}
	public void exitGame()
	{
		Application.Quit ();
	}
	public void rate()
	{
		Application.OpenURL ("https://play.google.com/store/apps/details?id=com.i6.rc_motorbike_racing_3d");
	}
	public void resumeGame()
	{
		Time.timeScale = 1f;
		Game.isRunning = true;
		gameObject.SetActive (false);
		if(musicOBJ != null)
			musicOBJ.releaseTMP ();
	}

}
