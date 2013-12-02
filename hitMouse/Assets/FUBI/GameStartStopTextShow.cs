using UnityEngine;
using System.Collections;

public class GameStartStopTextShow : MonoBehaviour {
	
	
	private float textStartShowTime = 0.0f;
	private const float textShowTimeInterval = 1.0f;
	
	// Use this for initialization
	void Start () {
		guiText.enabled = false;
	}
	
	// Update is called once per frame
	void Update () {
		if (hitMouseGame.started && hitMouseGame.startStopButtClicked)
		{
			guiText.text="Game Start";
			guiText.enabled = true;
			hitMouseGame.startStopButtClicked = false;
			textStartShowTime = Time.fixedTime;
		}
		
		if (!hitMouseGame.started && hitMouseGame.startStopButtClicked)
		{
			guiText.text="Game Over";
			guiText.enabled = true;
			hitMouseGame.startStopButtClicked = false;
			textStartShowTime = Time.fixedTime;
		}
		
		if (Time.fixedTime - textStartShowTime > textShowTimeInterval)
		{
			guiText.enabled = false;

		}
	}
}
