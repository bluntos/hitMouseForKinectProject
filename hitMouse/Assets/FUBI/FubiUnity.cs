using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using FubiNET;

public class FubiUnity : MonoBehaviour
{	
	//WL
	public GUISkin guiskin;
	public Rect filterButton = new Rect(30, 360, 100, 40);
	
	//AA: Filtering for kinect Project variables
	public bool m_bUseJointFiltering = false;
	public bool m_bUseVectorFiltering = false;
	public Filter filter = null;				// Our filter class
	public GameObject[] MyGameObjects;			// Gameobject contains sphere
	static Vector2 m_absPixelPosition = new Vector2(0, 0);

	// Global properties
	public bool m_disableFubi = false;	
	public bool m_disableTrackingImage = false;
	public bool m_disableCursorWithGestures = true;	
    public bool m_disableTrackingImageWithSwipeMenu = true;
	
    // Cursor control properties
    public Texture2D m_defaultCursor;
    public float m_cursorScale = 0.15f;
  
    // The current valid FubiUnity instance
    public static FubiUnity instance = null;
    // The current user for all controls
    uint m_currentUser = 0;

    // The depth texture
    private Texture2D m_depthMapTexture;
    // And its pixels
    Color[] m_depthMapPixels;
    // The raw image from Fubi
    byte[] m_rawImage;
    // m_factor for clinching the image
    int m_factor = 2;
    // Depthmap resolution
    int m_xRes = 640;
    int m_yRes = 480;
    // The user image texture
    private Texture2D m_userImageTexture;

    // Vars for the cursor control stuff
    Vector2 m_relativeCursorPosition = new Vector2(0, 0);
    Rect m_mapping;
    float m_aspect = 1.33333f;
    float m_cursorAspect = 1.0f;
    double m_timeStamp = 0;
    double m_lastMouseClick = 0;
    Vector2 m_lastMousePos = new Vector2(0, 0);
    bool m_lastCursorChangeDoneByFubi = false;
    bool m_gotNewFubiCoordinates = false;
    bool m_lastCalibrationSucceded = false;
    Vector2 m_snappingCoords = new Vector2(-1, -1);
    bool m_buttonsDisplayed = false;
    // And the gestures
    bool m_gesturesDisplayed = false;
    bool m_gesturesDisplayedLastFrame = false;
    double m_lastGesture = 0;
	
	//use toggle box to choose which filter to use
	private bool[] filterChoose = new bool[] {false, false, false, false, false, false, false, false, false, false, true};		
	static private int currentFilter = 10;					// default filter is NONE


    // Initialization
    void Start()
    {
        //AA: Our filter class
		filter = new Filter();
		//filter.name= Filter.FILTER_NAME.SIMPLE_AVG;
		
		// First set instance so Fubi.release will not be called while destroying old objects
        instance = this;
        // Remain this instance active until new one is created
        DontDestroyOnLoad(this);
		
		// Destroy old instance of Fubi
		object[] objects = GameObject.FindObjectsOfType(typeof(FubiUnity));
        if (objects.Length > 1)
        {          
            Destroy(((FubiUnity)objects[0]));
        }


        m_lastMouseClick = 0;
        m_lastGesture = 0;
		
        // Init FUBI
		if (!m_disableFubi)
		{
            // Only init if not already done
            if (!Fubi.isInitialized())
            {
                Fubi.init(new FubiUtils.SensorOptions(new FubiUtils.StreamOptions(640, 480, 30), new FubiUtils.StreamOptions(640, 480, 30), 
					new FubiUtils.StreamOptions(-1, -1, -1), FubiUtils.SensorType.OPENNI2), new FubiUtils.FilterOptions());
                if (!Fubi.isInitialized())
                    Debug.Log("Fubi: FAILED to initialize Fubi!");
                else
                {
                    Debug.Log("Fubi: initialized!");
                }
            }
		}
		else
			m_disableTrackingImage = true;

        // Initialize debug image
        m_depthMapTexture = new Texture2D((int)(m_xRes / m_factor), (int)(m_yRes / m_factor), TextureFormat.RGBA32, false);
        m_depthMapPixels = new Color[(int)((m_xRes / m_factor) * (m_yRes / m_factor))];
        m_rawImage = new byte[(int)(m_xRes * m_yRes * 4)];

        m_userImageTexture = null;

		// Disable system cursor
        if (m_defaultCursor != null && m_disableFubi == false)
            Screen.showCursor = false;
        else
            Screen.showCursor = true;
		
        // Default mapping values
//        m_mapping.x = -100.0f;
//        m_mapping.y = 200.0f;
//        m_mapping.height = 550.0f;

		m_mapping.x = -100.0f;
        m_mapping.y = 200.0f;
        m_mapping.height = 550.0f;

        // Get screen aspect
        m_aspect = (float)Screen.width / (float)Screen.height;

        // Calculated Map width with aspect
        m_mapping.width = m_mapping.height / m_aspect;

        if (Fubi.isInitialized())
        {
            // Clear old gesture recognizers
            Fubi.clearUserDefinedRecognizers();

            // And (re)load them
            if (Fubi.loadRecognizersFromXML("UnitySampleRecognizers.xml"))
                Debug.Log("Fubi: gesture recognizers 'BarRecognizers.xml' loaded!");
            else
                Debug.Log("Fubi: loading XML recognizers failed!");

            // load mouse control recognizers
            if (Fubi.loadRecognizersFromXML("MouseControlRecognizers.xml"))
                Debug.Log("Fubi: mouse control recognizers loaded!");
            else
                Debug.Log("Fubi: loading mouse control recognizers failed!");
        }





    }

    // Update is called once per frame
    void Update()
    {
        // update FUBI
       	Fubi.updateSensor();
		
		filter.name = (Filter.FILTER_NAME)currentFilter;
		

		if (!m_disableTrackingImage)
		{
			uint renderOptions = (uint)(FubiUtils.RenderOptions.Default | FubiUtils.RenderOptions.DetailedFaceShapes | FubiUtils.RenderOptions.FingerShapes);
            Fubi.getImage(m_rawImage, FubiUtils.ImageType.Depth, FubiUtils.ImageNumChannels.C4, FubiUtils.ImageDepth.D8, renderOptions);
            Updatem_depthMapTexture();
		}
    }

    void MoveMouse(float mousePosX, float mousePosY, bool forceDisplay = false)
    {
        // TODO change texture for dwell
        Texture2D cursorImg = m_defaultCursor;

        m_cursorAspect = (float)cursorImg.width / (float)cursorImg.height;
		float width = m_cursorScale * m_cursorAspect * (float)Screen.height;
		float height = m_cursorScale * (float)Screen.height;
		float x = mousePosX * (float)Screen.width - 0.5f*width;
		float y = mousePosY * (float)Screen.height - 0.5f*height;
		Rect pos = new Rect(x, y, width, height);
//		if ((m_buttonsDisplayed || forceDisplay) && m_disableFubi == false)
//		{
//			Debug.Log ("In movemouse: m_buttonsdisplayed " + m_buttonsDisplayed);
			GUI.depth = -3;
	        GUI.Label(pos, cursorImg);
//			m_buttonsDisplayed = false;
//		}
		m_absPixelPosition.x = x;
		m_absPixelPosition.y = y;

//		m_absPixelPosition.x = mousePosX * (float)Screen.width;
//		m_absPixelPosition.y = mousePosY * (float)Screen.height;
    }

    // Upload the depthmap to the texture
    void Updatem_depthMapTexture()
    {
        int YScaled = m_yRes / m_factor;
        int XScaled = m_xRes / m_factor;
        int i = XScaled * YScaled - 1;
        int depthIndex = 0;
        for (int y = 0; y < YScaled; ++y)
        {
            depthIndex += (XScaled - 1) * m_factor * 4; // Skip lines
            for (int x = 0; x < XScaled; ++x, --i, depthIndex -= m_factor * 4)
            {
                m_depthMapPixels[i] = new Color(m_rawImage[depthIndex + 2] / 255.0f, m_rawImage[depthIndex + 1] / 255.0f, m_rawImage[depthIndex] / 255.0f, m_rawImage[depthIndex + 3] / 255.0f);
            }
            depthIndex += m_factor * (m_xRes + 1) * 4; // Skip lines
        }
        m_depthMapTexture.SetPixels(m_depthMapPixels);
        m_depthMapTexture.Apply();
    }
	
	void showFilterMenu() {
		GUI.skin = guiskin;
		
		GUI.Label (new Rect(30, 120, 200, 30) , "Filter Menu");
		
		int baseHeight = 160;
		int width = 200;
		int height = 30;

		filterChoose[0] = GUI.Toggle(new Rect(30, 160,200,30), filterChoose[0], " SIMPLE AVERAGE 10");

		filterChoose[1] = GUI.Toggle(new Rect(30,200,200,30), filterChoose[1], " MOVING AVERAGE");

		filterChoose[2] = GUI.Toggle(new Rect(30,240,200,30), filterChoose[2], " SIMPLE AVERAGE 5");

		filterChoose[3] = GUI.Toggle(new Rect(30,280,200,30), filterChoose[3], " DOUBLE MOV AVERAGE");

		filterChoose[4] = GUI.Toggle(new Rect(30,320,200,30), filterChoose[4], " EXP SMOOTHING");

		filterChoose[5] = GUI.Toggle(new Rect(30,360,200,30), filterChoose[5], " DOUBLE EXP SMOOTHING");

		filterChoose[6] = GUI.Toggle(new Rect(30,400,200,30), filterChoose[6], " ADAPTIVE DBL EXP");

		filterChoose[7] = GUI.Toggle(new Rect(30,440,200,30), filterChoose[7], " MEDIAN");
		
		filterChoose[8] = GUI.Toggle(new Rect(30,480,200,30), filterChoose[8], " SIMPLE AVG + Median");
		
		filterChoose[9] = GUI.Toggle(new Rect(30,520,200,30), filterChoose[9], " DBL MOV AVG + Median");
		
		filterChoose[10] = GUI.Toggle(new Rect(30,560,200,30), filterChoose[10], " NONE");	
		
		for (int index = 0; index < filterChoose.Length; index++)
		{
			if (filterChoose[index] == true && currentFilter != index)
			{
				filterChoose[currentFilter] = false;
				currentFilter = index;
				//Debug.Log("current Filter" + currentFilter);
			}
			
		}
		
	}
	
	public int getCurrentFilter()
	{
		return currentFilter;
	}

    // Called for rendering the gui
    void OnGUI()
    {

		showFilterMenu();

		if (!m_disableTrackingImage){
	        // Debug image
	        
			GUI.depth = -4;
	        GUI.DrawTexture(new Rect(Screen.width-m_xRes/m_factor, Screen.height-m_yRes/m_factor, m_xRes / m_factor, m_yRes / m_factor), m_depthMapTexture);
		}
		
        // Cursor
		m_gotNewFubiCoordinates = false;
        if (Fubi.isInitialized())
        {
			// Take closest user
            uint userID = Fubi.getClosestUserID();
			if (userID != m_currentUser)
			{
				m_currentUser = userID;
				m_lastCalibrationSucceded = false;
			}
            if (userID > 0)
            {
				if (!m_lastCalibrationSucceded)
					m_lastCalibrationSucceded = calibrateCursorMapping(m_currentUser);
                FubiUtils.SkeletonJoint joint = FubiUtils.SkeletonJoint.RIGHT_HAND;
                FubiUtils.SkeletonJoint relJoint = FubiUtils.SkeletonJoint.RIGHT_SHOULDER;
                //if (leftHand)
                //{
                //    joint = FubiUtils.SkeletonJoint.LEFT_HAND;
                //    relJoint = FubiUtils.SkeletonJoint.LEFT_SHOULDER;
                //}
			
				// Get hand and shoulder position and check their confidence
                double timeStamp;
				float handX, handY, handZ, confidence;
                Fubi.getCurrentSkeletonJointPosition(userID, joint, out handX, out handY, out handZ, out confidence, out timeStamp);
                if (confidence > 0.5f)
                {
                    float relX, relY, relZ;
                    Fubi.getCurrentSkeletonJointPosition(userID, relJoint, out relX, out relY, out relZ, out confidence, out timeStamp);
					if (confidence > 0.5f)
                    {
						
						// AA: Filtering should happen here for the hand and relative joints separately
						// If true, use the smoothed joints for calculating screen coordinates

						if(m_bUseJointFiltering) {
							Vector3 handPos = filter.Update(new Vector3(handX, handY, handZ), Filter.JOINT_TYPE.JOINT);
							Vector3 relJointPos = filter.Update(new Vector3(relX, relY, relZ), Filter.JOINT_TYPE.RELATIVEJOINT);
							Debug.Log ("hand x y z " + handX + " " + handY + " " + handZ);
							Debug.Log ("handpos x y z " + handPos.x + " " + handPos.y + " " + handPos.z);
							handZ = handPos.z;
							handY = handPos.y;
							handX = handPos.x;
							
							relZ = relJointPos.z;
							relY = relJointPos.y;
							relX = relJointPos.x;
							
						}
						// AA: End  
						
						// Take relative coordinates
						float zDiff = handZ - relZ;
						float yDiff = handY - relY;
						float xDiff = handX - relX;
						// Check if hand is enough in front of shoulder
						if ((yDiff >0 && zDiff < -150.0f) || (Mathf.Abs(xDiff) > 150.0f && zDiff < -175.0f) || zDiff < -225.0f)
						{
							// Now get the possible cursor position                       
	
	                        // Convert to screen coordinates
	                        float newX, newY;
	                        float mapX = m_mapping.x;
	                        //if (leftHand)
	                        //    // Mirror x  area for left hand
	                        //    mapX = -m_mapping.x - m_mapping.width;
	                        newX = (xDiff - mapX) / m_mapping.width;
	                        newY = (m_mapping.y - yDiff) / m_mapping.height; // Flip y for the screen coordinates
	
	                        // Filtering
	                        // New coordinate is weighted more if it represents a longer distance change
	                        // This should reduce the lagging of the cursor on higher distances, but still filter out small jittering
	                        float changeX = newX - m_relativeCursorPosition.x;
	                        float changeY = newY - m_relativeCursorPosition.y;
	
	                        if (changeX != 0 || changeY != 0 && timeStamp != m_timeStamp)
	                        {
	                            float changeLength = Mathf.Sqrt(changeX * changeX + changeY * changeY);
	                            float filterFactor = changeLength; //Mathf.Sqrt(changeLength);
	                            if (filterFactor > 1.0f) {
									 filterFactor = 1.0f;
									Debug.Log ("filterfactor is 1");
								}
								else{
									Debug.Log ("filterfactor is " + filterFactor);
									
								}
								
	                            
	
	                            // Apply the tracking to the current position with the given filter factor
								// AA: Filtering should happen here for joint-to-relativejoint (VECTOR) filtering
								// AA: filtering code
								
								Vector2 tempNew = new Vector2(newX,newY);
								
								// If true, use the calculated factor for smoothing, else just use the new
								if(m_bUseVectorFiltering) {
									//filterFactor
									m_relativeCursorPosition = filter.Update(m_relativeCursorPosition, tempNew, filterFactor);
								}
								else {	// Just give equal weight to both
									m_relativeCursorPosition = filter.Update(m_relativeCursorPosition, tempNew, 0.5f);
									
								}
								
	                            
								
	                            m_timeStamp = timeStamp;
	
	                            // Send it, but only if it is more or less within the screen
								if (m_relativeCursorPosition.x > -0.1f && m_relativeCursorPosition.x < 1.1f
									&& m_relativeCursorPosition.y > -0.1f && m_relativeCursorPosition.y < 1.1f)
								{
									// AA: Disable snapping
//									if (!m_disableSnapping && m_snappingCoords.x >=0 && m_snappingCoords.y >= 0)
//									{
//										MoveMouse(m_snappingCoords.x, m_snappingCoords.y);
//										m_snappingCoords.x = -1;
//										m_snappingCoords.y = -1;
//									}
//									else
	                            	MoveMouse(m_relativeCursorPosition.x, m_relativeCursorPosition.y);
									m_gotNewFubiCoordinates = true;
									m_lastCursorChangeDoneByFubi = true;
								}
	                        }
						}
                    }
                }
            }
        }
        // AA: FUBI does not move mouse if the confidence value is too low 
		
		if (!m_gotNewFubiCoordinates)	// AA: this only executes when input is coming from mouse
        {
			// Got no mouse coordinates from fubi this frame
            Vector2 mousePos = Input.mousePosition;
			// Only move mouse if it wasn't changed by fubi the last time or or it really has changed
			if (!m_lastCursorChangeDoneByFubi || mousePos != m_lastMousePos)
			{
            	m_relativeCursorPosition.x = mousePos.x / (float)Screen.width;
				m_relativeCursorPosition.y = 1.0f - (mousePos.y / (float)Screen.height);
            	// Get mouse X and Y position as a percentage of screen width and height
            	MoveMouse(m_relativeCursorPosition.x, m_relativeCursorPosition.y, true);
				m_lastMousePos = mousePos;
				m_lastCursorChangeDoneByFubi = false;
			}
        }
    }

    // Called on deactivation
    void OnDestroy()
    {
		if (this == instance)
		{
			Fubi.release();
        	Debug.Log("Fubi released!");
		}
    }
	
	static bool rectContainsCursor(Rect r)
	{
		// convert to relative screen coordinates
		r.x /= (float)Screen.width;
		r.y /= (float)Screen.height;
		r.width /= (float)Screen.width;
		r.height /= (float)Screen.height;
		
		// get cursor metrics
		float cursorWHalf = instance.m_cursorScale * instance.m_cursorAspect / 2.0f;
		float cursorHHalf = instance.m_cursorScale / 2.0f;
		Vector2 cursorCenter = instance.m_relativeCursorPosition;
		
		// check whether it is inside
		return (instance.m_gotNewFubiCoordinates &&
				(r.Contains(cursorCenter)
				 || r.Contains( cursorCenter + new Vector2(-cursorWHalf, -cursorHHalf) )
				 || r.Contains( cursorCenter + new Vector2(cursorWHalf, cursorHHalf) )
				 || r.Contains( cursorCenter + new Vector2(cursorWHalf, -cursorHHalf) )
				 || r.Contains( cursorCenter + new Vector2(-cursorWHalf, cursorHHalf) ) ));
	}

    static private bool clickRecognized()
    {
        bool click = false;
        if (Fubi.getCurrentTime() - instance.m_lastMouseClick > 0.5f)
        {
            uint userID = instance.m_currentUser;
            if (userID > 0)
            {
                // Check for mouse click as defined in xml
                FubiTrackingData[] userStates;
                if (Fubi.getCombinationRecognitionProgressOn("mouseClick", userID, out userStates, false) == FubiUtils.RecognitionResult.RECOGNIZED)
                {
                    if (userStates != null && userStates.Length > 0)
                    {
                        double clickTime = userStates[userStates.Length - 1].timeStamp;
                        // Check that click occured no longer ago than 1 second
                        if (Fubi.getCurrentTime() - clickTime < 1.0f)
                        {
                            click = true;
                            instance.m_lastMouseClick = clickTime;
                            // Reset all recognizers
                            Fubi.enableCombinationRecognition(FubiPredefinedGestures.Combinations.NUM_COMBINATIONS, userID, false);
                        }
                    }
                }
                
                if (!click)
                    Fubi.enableCombinationRecognition("mouseClick", userID, true);

                if (Fubi.recognizeGestureOn("mouseClick", userID) == FubiUtils.RecognitionResult.RECOGNIZED)
                {
                    //Debug.Log("Mouse click recognized.");
                    click = true;
                    instance.m_lastMouseClick = Fubi.getCurrentTime();
                    // Reset all recognizers
                    Fubi.enableCombinationRecognition(FubiPredefinedGestures.Combinations.NUM_COMBINATIONS, userID, false);
                }
            }
        }
        return click;
    }
	
	static public bool FubiButton(Rect r, string text, GUIStyle style)
	{
		bool cursorDisabled = instance.m_disableCursorWithGestures && instance.m_gesturesDisplayedLastFrame;
		
		instance.m_buttonsDisplayed = !cursorDisabled;
		GUI.depth = -2;
		bool click = false;
		Rect checkRect = new Rect();
		checkRect.x = r.x - r.height;
		checkRect.y = r.y - r.height;
		checkRect.width = r.width + 2*r.height;
		checkRect.height = r.height + 2*r.height;
		if (!cursorDisabled && rectContainsCursor(checkRect))
		{
			instance.m_snappingCoords = new Vector2(r.center.x / Screen.width, r.center.y / Screen.height);
			GUI.Button(r, text, style);
            if (clickRecognized())
                click = true;            
		}
		else
		{
			click = GUI.Button(r, text, style);
		}
		return click;
	}
	
	public Vector2 getPixelPosition()	{
		return m_absPixelPosition;
	}


	// AA: The 'DisplayFubiCroppedUserImage' function could be used to explain the problems with filtering
    private bool DisplayFubiCroppedUserImage(int x, int y, bool forceReload)
    {
        if (m_userImageTexture == null || forceReload == true)
        {
            // First get user image
            Fubi.getImage(m_rawImage, FubiUtils.ImageType.Color, FubiUtils.ImageNumChannels.C4, FubiUtils.ImageDepth.D8, (uint)FubiUtils.RenderOptions.None, (uint)FubiUtils.JointsToRender.ALL_JOINTS, FubiUtils.DepthImageModification.Raw, m_currentUser, FubiUtils.SkeletonJoint.HEAD, true);

            // Now look for the image borders
            int xMax = m_xRes; int yMax = m_yRes;
            int index = 0;
            for (int x1 = 0; x1 < m_xRes; ++x1, index += 4)
            {
                if (m_rawImage[index + 3] == 0)
                {
                    xMax = x1;
                    break;
                }
            }
            index = 0;
            for (int y1 = 0; y1 < m_yRes; ++y1, index += (m_xRes + 1) * 4)
            {
                if (m_rawImage[index + 3] == 0)
                {
                    yMax = y1;
                    break;
                }
            }

            // Create the texture
            m_userImageTexture = new Texture2D(xMax, yMax, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[xMax*yMax];

            // And copy the pixels
            int i = xMax * yMax - 1;
            index = 0;
            for (int yy = 0; yy < yMax; ++yy)
            {
                index += (xMax - 1) * 4; // Move to line end
                for (int xx = 0; xx < xMax; ++xx, --i, index -= 4)
                {
                    pixels[i] = new Color(m_rawImage[index] / 255.0f, m_rawImage[index + 1] / 255.0f, m_rawImage[index + 2] / 255.0f, m_rawImage[index + 3] / 255.0f);
                }
                index += (m_xRes + 1) * 4; // Move to next line
            }

            m_userImageTexture.SetPixels(pixels);
            m_userImageTexture.Apply();
        }

        GUI.depth = -4;
        GUI.DrawTexture(new Rect(x, y, m_userImageTexture.width, m_userImageTexture.height), m_userImageTexture);

        return false;
    }





	// AA: This function is doing som kind of mapping using the RIGHT shoulder, elbow and hand
	bool calibrateCursorMapping(uint id)
    {
		m_aspect = (float)Screen.width / (float)Screen.height;
        if (id > 0)
        {
            FubiUtils.SkeletonJoint elbow = FubiUtils.SkeletonJoint.RIGHT_ELBOW;
            FubiUtils.SkeletonJoint shoulder = FubiUtils.SkeletonJoint.RIGHT_SHOULDER;
            FubiUtils.SkeletonJoint hand = FubiUtils.SkeletonJoint.RIGHT_HAND;

            float confidence;
            double timeStamp;
            float elbowX, elbowY, elbowZ;
            Fubi.getCurrentSkeletonJointPosition(id, elbow, out elbowX, out elbowY, out elbowZ, out confidence, out timeStamp);
            if (confidence > 0.5f)
            {
                float shoulderX, shoulderY, shoulderZ;
                Fubi.getCurrentSkeletonJointPosition(id, shoulder, out shoulderX, out shoulderY, out shoulderZ, out confidence, out timeStamp);
                if (confidence > 0.5f)
                {
                    double dist1 = Mathf.Sqrt(Mathf.Pow(elbowX - shoulderX, 2) + Mathf.Pow(elbowY - shoulderY, 2) + Mathf.Pow(elbowZ - shoulderZ, 2));
                    float handX, handY, handZ;
                    Fubi.getCurrentSkeletonJointPosition(id, hand, out handX, out handY, out handZ, out confidence, out timeStamp);
                    if (confidence > 0.5f)
                    {
                        double dist2 = Mathf.Sqrt(Mathf.Pow(elbowX - handX, 2) + Mathf.Pow(elbowY - handY, 2) + Mathf.Pow(elbowZ - handZ, 2));
                        m_mapping.height = (float)(dist1 + dist2);
                        // Calculate all others in depence of maph
                        m_mapping.y = 200.0f / 550.0f * m_mapping.height;
                        m_mapping.width = m_mapping.height / m_aspect;
                        m_mapping.x = -100.0f / (550.0f / m_aspect) * m_mapping.width;
						return true;
                    }
                }
            }
        }
		return false;
    }
}