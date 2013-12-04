=================================
    FUBI - Unity3D integration
(Full Body Interaction Framework)
=================================
Version 0.8.0


Copyright (C) 2010-2013 Felix Kistler

http://www.hcm-lab.de/fubi.html


The framework is licensed under the terms of the Eclipse Public License (EPL).

FUBI makes use of the following third-party open source software included in code form:
 - RapidXml (http://rapidxml.sourceforge.net)
 
FUBI can be used with the following third-party open source software not included in the FUBI download:
 - OpenCV (http://opencv.willowgarage.com)
 - OpenNI (httl://www.openni.org)

FUBI can be used with the following third-party closed source software not included in the FUBI download:
 - Microsoft Kinect SDK (http://www.microsoft.com/en-us/kinectforwindows/)
 - NiTE (http://www.openni.org/files/nite/)
 
A documentation with pictures and more detailed tutorials can be found here:
http://www.informatik.uni-augsburg.de/lehrstuehle/hcm/projects/tools/fubi/doc/


Installation of third-party components
======================================

You need to install the following third-party components:
1. OpenNI:
 http://www.openni.org/openni-sdk/
 --> OpenNI Binaries --> OpenNI 2.xx (Win32) --> extract and run OpenNI-Windows-x86-2.xx.msi
2. NITE:
 http://www.openni.org/files/nite/
 --> Download (You need to register for this) --> extract and run NiTE-Windows-x86-2.xx.msi
3. Sensor driver: --IMPORTANT-- Install this AFTER the other two installations!
 a) ASUS Xtion (Pro live): Should be automatically installed with OpenNI.
 c) Kinect for Xbox/Windows: See 4. Kinect SDK 1.x

Alternatively or in addition you can install:
4. Kinect SDK 1.x:
 http://www.microsoft.com/en-us/kinectforwindows/develop/developer-downloads.aspx
 --> Download and install the latest Kinect for Windows SDK and Kinect for Windows Developer Toolkit

5. We recommend to additionally install OpenCV, currently, the latest supported version is 2.4.3:
For Windows: http://sourceforge.net/projects/opencvlibrary/files/opencv-win/2.4.3/OpenCV-2.4.3.exe/download
If you do not want to use OpenCV, comment out the line "#define USE_OPENCV" at top of the FubiConfig.h
If you already have installed OpenCV 2.2 with an earlier release, you can also use that: You have to comment out the line "#define USE_OPENCV" and uncomment "#define USE_OPENCV_22" at top of the FubiConfig.h


Running the Unity3D sample
==========================

As Unity3D (also the non-commercial version) supports C# scripts, you can directly access FUBI via the C# Wrapper as done in the Fubi C# sample application. However, the Unity3D sample also shows how to use FUBI to control the Unity3D GUI and how to integrate FUBI gestures in your game created with Unity3D.

In the Hierarchy window you will find two relevant game objects (besides the camera): The FUBI object handles the FUBI integration, handles the cursor control, GUI rendering and gesture recognition. The SampleGUI object defines a sample GUI using the functions of the FUBI object.

If you click on the FUBI object, you will find several general settings in the Inspector: You can disable FUBI completely, you can disable the tracking image shown at the right bottom of the screen, you specify whether the cursor should be disabled as long as gesture symbols are shown on the screen, you can disable the snapping feature of the cursor.
You can also change the cursor by dragging a different image on the Default Cursor property and setting its scale.
The last option defines specific gesture symbols that connect gesture recognizers with symbols to be shown on the screen.
If you want to have a look at the code, you can of course open the corresponding FUBIUnity.cs by double clicking the Script property.

When you click on the SampleGUI game object, you will find another script called SampleGUI.cs, that actually defines the current GUI. You can open it by double clicking:
There is not much code in it. The only implemented function is OnGUI(). There are two GUI elements created in the same way as you normally do in Unity3D:

    bool FubiButton(Rect r, string text, GUIStyle style):
    creates a button on the rectangle r with the given GUIStyle, containing the given text and returning whether it was pressed in the current update cycle.
    Note: If you are using a GUIStyle named "myStyle", there should also exist a GUIStyle named "myStyle-hover" that specifies the style when hovering the button. As Fubi can only change the GUIStyle but it is not possible to tell Unity3D that a GUI element is currently hovered, this is the only way for visualizing hovers.

    bool FubiGesture(Rect r, string name):
    Displays the symbol with the given name as specified in the Gesture Symbols property of the FUBI object on the rect r. In addition, Fubi calls a recognizer with the same name that should be defined in the "UnitySampleRecognizers.xml" to be found in the root folder.
    The function returns true if the recognition was successful.
    If the symbol consists of multiple images, those images will be shown one after the other as in an animated gif.

Back to the sample scene: If you now hit the play button to start the scene, you will see a button on the upper left side containing the text "Fubi Button" and a symbol on the upper right side that visualizes a waving gesture.
You can stand in front of the sensor and you should see your body shape in an image on the lower right side. Move a bit and wait until a skeleton gets rendered onto that shape, now you can control a cursor with your right hand and select items of the GUI (currently only the one button) by performing a pushing gesture with your left hand in downwards direction.
In addition you can try to perform the shown waving gesture (right hand above the shoulder!).
Both should result in a text box appearing under the corresponding GUI elements.