using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class hitMouseGame : MonoBehaviour {
	
	public GUISkin guiskin;
	public GameObject[] MyGameObjects;			// Gameobject contains sphere
	
	public GUIText scoreText;					// Score GUIText
	public GUIText timeText;					// Time GUIText
	
	public AudioClip hitSoundEffect;			// hit sound effect

	private const float gameTotalTimeInterval = 30.0f;					// game total time length is 60s

	public static bool started = false;								// whether game has started
	private Rect startStopButton = new Rect(30,30,130,50);			// start Stop button
	public static bool startStopButtClicked = false;			
	

	
	private Rect summaryButton = new Rect(30,620,130, 50);
	private bool summaryShowing = false;
	
	private float startTime = 0.0f;
	private float timeRemain = gameTotalTimeInterval;
	
	private int score = 0;	
	
	private FubiUnity fubiUnity = new FubiUnity();
	
	private Vector2 pixelPosition = new Vector2(0, 0);	
	
	private float[] mousesTimeInterval = new float[6];
	private const float selectNewMouseInterval = 2f;
	private float selectNewMouseStartTime = 0.0f;
	private const float mouseUnavailableTimeInterval = 5f;
	List<int> mouseAvailableList;
	List<int> mouseUnavailableList;
	
	private int[] filterScores = new int[11];
	private int currentUsingFilter = 10;
	

		
	
	void startGame() {
		mouseUnavailableList = new List<int> (new int[] {0,1,2,3,4,5});
		mouseAvailableList = new List<int> ();
		
		currentUsingFilter = fubiUnity.getCurrentFilter();
		
		//Debug.Log("current using Fily" + currentUsingFilter);

		Array.Clear(mousesTimeInterval, 0, mousesTimeInterval.Length);
		
		foreach(GameObject obj in MyGameObjects)
		{
			Vector3 tempPosition = obj.transform.position;
			tempPosition.y = -1;
			obj.transform.position = tempPosition;
		}
		
		started = true;	
		startStopButtClicked = true;		//this is used in GameStartStopTextShow
		startTime = Time.fixedTime;
		timeRemain = gameTotalTimeInterval;
		score = 0;
		selectNewMouseStartTime = Time.fixedTime;
		
		
	}
	
	void stopGame()	{
	
		started = false;
		startStopButtClicked = true;		//this is used in GameStartStopTextShow
		mouseUnavailableList.Clear();
		mouseUnavailableList = new List<int> (new int[] {0,1,2,3,4,5});
		mouseAvailableList.Clear();
		
		filterScores[currentUsingFilter] = score;
		
		foreach(GameObject obj in MyGameObjects)
		{
			Vector3 tempPosition = obj.transform.position;
			tempPosition.y = -1;
			obj.transform.position = tempPosition;
		}
	}
	
	void selectMouseToBeAvailable() {
		if (Time.fixedTime - selectNewMouseStartTime >= selectNewMouseInterval)	
		{
			System.Random rnd = new System.Random();

			
			int index = rnd.Next(mouseUnavailableList.Count);
		
			//Debug.Log("*******selectMouseToBeAvailable***********" + mouseUnavailableList[index] + "****************");
			mousesTimeInterval[mouseUnavailableList[index]] = Time.fixedTime;
			mouseAvailableList.Add(mouseUnavailableList[index]);
			mouseUnavailableList.RemoveAt(index);
			selectNewMouseStartTime = Time.fixedTime;
		}
			
	}
	
	void mouseToBeUnavaiable()	{		
			
		for(int index = 0; index < mousesTimeInterval.Length; index++)	
		{
			if (Time.fixedTime - mousesTimeInterval[index] >= mouseUnavailableTimeInterval)	
			{
				Vector3 tempPosition = MyGameObjects[index].transform.position;
				tempPosition.y = -1;
				MyGameObjects[index].transform.position = tempPosition;
				//Debug.Log("*******mouseToBeUnavaiable***********" + index + "****************");
				mouseUnavailableList.Add(index);
				mouseAvailableList.Remove(index);
				mousesTimeInterval[index] = 0.0f;
			}
		}
}
	
	
	void mouseAnimation() {
		Vector3 tempPosition = Vector3.zero;

		foreach(int index in mouseAvailableList)
		{
			if (MyGameObjects[index].transform.position.y < 30)
			{
				tempPosition = MyGameObjects[index].transform.position;
				tempPosition.y += 60 * Time.deltaTime;
				MyGameObjects[index].transform.position = tempPosition;
			}
		}
	}
	
	
	void updateMenuPanel()	{
		

		if (started)
		{
			timeRemain = gameTotalTimeInterval - (Time.fixedTime - startTime);
			if (timeRemain <= 0)
				stopGame();
		}
		
		timeText.text = "Time : " + Convert.ToInt32(timeRemain).ToString() + "s";
	 
		scoreText.text =  "Score : " + score.ToString();

	}
		
	void updateGamePanel()	{
		
		
		
		if (started)
		{
			mouseAnimation();
			
			selectMouseToBeAvailable();
		
			mouseToBeUnavaiable();
		}
		
	
		pixelPosition = fubiUnity.getPixelPosition();

		Ray ray = Camera.mainCamera.ScreenPointToRay(new Vector2(pixelPosition.x,Screen.height -  pixelPosition.y));


		
		// check for underlying objects
		RaycastHit hit;
		if(Physics.Raycast(ray, out hit))
		{
			for (int index = 0; index < MyGameObjects.Length; index++)
			{
				if(hit.collider.gameObject == MyGameObjects[index] && MyGameObjects[index].transform.position.z > 25 
					&& started)
				{

					mouseUnavailableList.Add(index);
					mouseAvailableList.Remove(index);
					mousesTimeInterval[index] = 0.0f;
					score += 10;
					audio.Play();
				}
			}
		
		}
	
	}
		
	// Use this for initialization
	void Start () {
		//mouseUnavailableList = new List<int> (new int[] {0,1,2,3,4,5});
		//mouseAvailableList = new List<int> ();
		
		Array.Clear(filterScores, 0, filterScores.Length);
	}
	
	// Update is called once per frame
	void Update () {
		updateGamePanel();
		
	}
	

	void summaryFrameUpdate() {
	
		if (!summaryShowing && FubiUnity.FubiButton(summaryButton, "Show Summary", "button"))
		{
			summaryShowing = true;
		}
		else if (summaryShowing && FubiUnity.FubiButton(summaryButton, "Hide Summary", "button"))
		{
			summaryShowing = false;
		}
		
		if (summaryShowing)
		{
			//TODO:add summary frame]]
			string summaryText = "";
			GUI.Box(new Rect((Screen.width/2-300), (Screen.height/2 -300), 600, 600), "Summary"); 
			
			GUI.Label(new Rect((Screen.width/2-250 + 50), (Screen.height/2 -200 + 50), 300, 30), "SIMPLE AVERAGE 10 : " + filterScores[0]);
			GUI.Label(new Rect((Screen.width/2-250 + 50), (Screen.height/2 -200 + 90), 300, 30), "MOVING AVERAGE : " + filterScores[1]);
			GUI.Label(new Rect((Screen.width/2-250 + 50), (Screen.height/2 -200 + 130), 300, 30), "SIMPLE AVERAGE 5 : " + filterScores[2]);
			GUI.Label(new Rect((Screen.width/2-250 + 50), (Screen.height/2 -200 + 170), 300, 30), "DOUBLE MOV AVERAGE : " + filterScores[3]);
			GUI.Label(new Rect((Screen.width/2-250 + 50), (Screen.height/2 -200 + 210), 300, 30), "EXP SMOOTHING : " + filterScores[4]);
			GUI.Label(new Rect((Screen.width/2-250 + 50), (Screen.height/2 -200 + 250), 300, 30), "DOUBLE EXP SMOOTHING : " + filterScores[5]);
			GUI.Label(new Rect((Screen.width/2-250 + 50), (Screen.height/2 -200 + 290), 300, 30), "ADAPTIVE DBL EXP : " + filterScores[6]);
			GUI.Label(new Rect((Screen.width/2-250 + 50), (Screen.height/2 -200 + 330), 300, 30), "MEDIAN : " + filterScores[7]);
			GUI.Label(new Rect((Screen.width/2-250 + 50), (Screen.height/2 -200 + 370), 300, 30), "SIMPLE AVG + Median: " + filterScores[8]);
			GUI.Label(new Rect((Screen.width/2-250 + 50), (Screen.height/2 -200 + 410), 300, 30), "DBL MOV AVG + Median : " + filterScores[9]);
			GUI.Label(new Rect((Screen.width/2-250 + 50), (Screen.height/2 -200 + 450), 300, 30), "NONE : " + filterScores[10]);
		}
	}
		
	void OnGUI () {
		GUI.skin = guiskin;
		
		// if game has not been started
		if (!started && FubiUnity.FubiButton(startStopButton, "Start Game", "button"))
		{
			startGame();
		}
		else if (started && FubiUnity.FubiButton(startStopButton, "Stop Game", "button"))
		{
			stopGame();			 
		}

		summaryFrameUpdate();

		updateMenuPanel();
		
	}
}
