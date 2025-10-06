
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.UI;
using System;
using UnityEditor;
using System.IO;
using Unity.Burst.Intrinsics;

public abstract class MarsAssessAROM : MonoBehaviour
{
    // Static variables related to drawing the trajectory.
    public static int OFFSET { get; private set; }
    public static readonly float SCALEX = 10f;
    public static readonly float SCALEY = 10f;
    public static readonly Vector3 SCREEN_OFFSET = new Vector3(0f, 0f, 0f);
    public static readonly float DIST_THRESHOLD = 0.01f; // meters

    // AROM raw assessment states
    protected enum AROM_RAW_ASSESS_STATES
    {
        INIT,
        ASSESSROM,
        ADJUST,
        DONE,
    }
    // AROM adjustment states 
    protected enum AROM_ADJUST_STATES
    {
        NONE,
        TOP,
        BOTTOM,
        LEFT,
        RIGHT,
    }
    protected AROM_ADJUST_STATES aromAdjustState = AROM_ADJUST_STATES.NONE;
    protected AROM_RAW_ASSESS_STATES aromRawAssessState = AROM_RAW_ASSESS_STATES.INIT;

    protected CommonUI commonUI;
    protected string movement = null;
    protected List<Vector3> endPoints;
    protected List<Vector3> unityPoints;
    protected GameObject currentPositionCircle;

    protected MarsArom oldMarsArom = null;
    protected MarsArom newMarsArom = null;

    protected virtual void Awake()
    {
        commonUI = FindFirstObjectByType<CommonUI>();

        // Add null check for safety
        if (commonUI == null)
        {
            Debug.LogError("CommonUI not found in scene!");
        }
    }

    protected virtual void Start()
    {
        // Initialize the assessment state machine
        aromRawAssessState = AROM_RAW_ASSESS_STATES.INIT;
        aromAdjustState = AROM_ADJUST_STATES.NONE;

        // Initialize the original message.
        commonUI.instructionText.text = "";

        // Initialize current position circle
        currentPositionCircle = Instantiate(commonUI.circlePrefab);
        currentPositionCircle.GetComponent<Renderer>().material.color = Color.green;
        currentPositionCircle.transform.localScale = new Vector3(0.15f, 0.15f, 1f);
        currentPositionCircle.SetActive(true); // Initially hide the circle

        // Initialize offset
        OFFSET = AppData.Instance.userData?.limb == 1 ? -1 : 1;

        // Initialize lists
        unityPoints = null;
        endPoints = null;

        // Attach callbacks.
        MarsComm.OnMarsButtonReleased += OnMarsButtonReleased;
    }

    protected virtual void Update()
    {
        // Make sure name is not null.
        if (movement == null)
        {
            Debug.LogError("Movement in MarsAssessAROM is null, cannot proceed.");
            return;
        }

        // Update the UI elements
        updateUI();

        // Check if adjustment keys are pressed.
        

        // Run the raw assessment statemachine
        runAROMRawAssessStateMachine();
    }

    protected virtual void FixedUpdate()
    {
        //     zEndPoint = MarsComm.epPosInThePlane.z;
        //     yEndPoint = MarsComm.epPosInThePlane.y;

        //     // Scaling + centering
        //     Vector3 sceneCenter = Vector3.zero;  // adjust if needed

        //     // Compute unity coordinates.
        //     unityValX = DrawParams.OFFSET * ((zEndPoint - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ)) * DrawParams.SCALEX;
        //     unityValY = ((yEndPoint - MarsDefs.EPCENTERY) / (MarsDefs.EPMAXY - MarsDefs.EPMINY)) * DrawParams.SCALEY;
        //     Vector3 toDrawValues = new Vector3((float)unityValX, (float)unityValY, 0.0f) + sceneCenter;
        //     Vector3 endPointValues = new Vector3((float)zEndPoint, (float)yEndPoint, 0.0f);
        //     // Get the last endpoint position that was added.
        //     Vector3 lastEndPoint = actualPoints.Count > 0 ? actualPoints[actualPoints.Count - 1] : Vector3.zero;

        //     // If the distance between the current and last endpoint is less than a threshold, skip adding this point.
        //     if (Vector3.Distance(endPointValues, lastEndPoint) < DrawParams.DIST_THRESHOLD) return;

        //     // Add the point and update the lists and the plot.
        //     unityPoints.Add(toDrawValues);
        //     actualPoints.Add(endPointValues);

        //     // Redraw based on the current state.
        //     switch (AssessROM.instance.aromRawAssessState)
        //     {
        //         case AssessROM.AROM_RAW_ASSESS_STATES.ASSESSROM:
        //         case AssessROM.AROM_RAW_ASSESS_STATES.INITIATECIRCLE:
        //             lineRenderer.positionCount = unityPoints.Count;
        //             lineRenderer.SetPositions(unityPoints.ToArray());
        //             lineRenderer.useWorldSpace = true;
        //             break;
        //         case AssessROM.AROM_RAW_ASSESS_STATES.WAITTOREACH:
        //         case AssessROM.AROM_RAW_ASSESS_STATES.TEST:
        //             if (AssessROM.instance.currentCircle != null)
        //             {
        //                 AssessROM.instance.currentCircle.transform.position = toDrawValues;
        //             }
        //             break;
        //     }
    }

    private void updateUI()
    {
        // Update the endpoint position text
        commonUI.marsEPPosText.text = $"{MarsComm.epPosInThePlane.z:F2}m, {MarsComm.epPosInThePlane.y:F2}m";

        // Display the green circle for the current position. Use the circlePrefab from CommonUI.
        updateCurrentPositionCircle();

        // Draw the old AROM lines in light blue.
        if (oldMarsArom != null)
        {
            // Check the movement name
            if (oldMarsArom.movement == "ML")
            {
                // Show the parallel vertical lines
                float leftX = OFFSET * SCALEX * ((oldMarsArom.leftRaw.x - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
                float leftY = -1f;
                float rightX = OFFSET * SCALEX * ((oldMarsArom.rightRaw.x - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
                float rightY = 1f;
                commonUI.rawAromLine1RendererOld.positionCount = 2;
                commonUI.rawAromLine1RendererOld.SetPositions(new Vector3[]
                {
                    new Vector3(leftX, leftY, 0),
                    new Vector3(leftX, rightY, 0)
                });
            }
        }
    }

    private void updateCurrentPositionCircle()
    {
        // Scaling + centering
        Vector3 sceneCenter = SCREEN_OFFSET;

        // Compute unity coordinates.
        double zEndPoint = MarsComm.epPosInThePlane.z;
        double yEndPoint = MarsComm.epPosInThePlane.y;

        double unityValX = SCALEX * OFFSET * ((zEndPoint - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
        double unityValY = SCALEY * ((yEndPoint - MarsDefs.EPCENTERY) / (MarsDefs.EPMAXY - MarsDefs.EPMINY));

        // Update the position of the circle
        currentPositionCircle.transform.position = new Vector3((float)unityValX, (float)unityValY, 0.0f) + sceneCenter;
    }

    protected void runAROMRawAssessStateMachine()
    {
        // Implement the state machine logic here
        switch (aromRawAssessState)
        {
            case AROM_RAW_ASSESS_STATES.INIT:
                // Initialization logic
                commonUI.instructionText.text = "Press the MARS button to start assessment.";
                // Initialize the new MarsAROM
                newMarsArom = new MarsArom(movement, readFromFile: false);
                newMarsArom.startAromAssessment();
                break;
            case AROM_RAW_ASSESS_STATES.ASSESSROM:
                // Logic for assessing range of motion
                commonUI.instructionText.text = "Maximize the separate between the lines. Press the MARS button when done.";
                // Add points to the trajectory
                endPoints.Add(MarsComm.epPosInThePlane);
                // Add point to assessment.
                newMarsArom.addAromDataPoint(MarsComm.epPosInThePlane.z, MarsComm.epPosInThePlane.y);
                // Add the display point.
                unityPoints.Add(currentPositionCircle.transform.position);
                // Render the trajectory line
                commonUI.trajectoryLineRenderer.positionCount = unityPoints.Count;
                commonUI.trajectoryLineRenderer.SetPositions(unityPoints.ToArray());
                break;
            case AROM_RAW_ASSESS_STATES.ADJUST:
                // Logic for waiting to reach position
                commonUI.instructionText.text = "Adjust AROM if needed. Press the REDO button to redo assessment.";
                // Show the raw AROM box
                showRawAromBoxLines();
                break;
            case AROM_RAW_ASSESS_STATES.DONE:
                // Completion logic
                break;
            default:
                Debug.LogError("Unknown state in AROM raw assessment state machine.");
                break;
        }
    }

    private void showRawAromBoxLines()
    {
        // What we show depends on the movement.
        switch (movement)
        {
            case "ML":
                // Show the raw AROM lines
                float leftX = OFFSET * SCALEX * ((newMarsArom.leftRaw.x - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
                float rightX = OFFSET * SCALEX * ((newMarsArom.rightRaw.x - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
                float leftRightY = 2.5f;
                // Update the raw AROM line renderers
                commonUI.rawAromLine1Renderer.positionCount = 2;
                commonUI.rawAromLine1Renderer.SetPositions(new Vector3[]
                {
                    new Vector3(leftX, leftRightY, 0),
                    new Vector3(leftX, -leftRightY, 0)
                });
                commonUI.rawAromLine2Renderer.positionCount = 2;
                commonUI.rawAromLine2Renderer.SetPositions(new Vector3[]
                {
                    new Vector3(rightX, leftRightY, 0),
                    new Vector3(rightX, -leftRightY, 0)
                });
                // Update the adjusted AROM line renderers
                commonUI.adjustedAromLine1Renderer.positionCount = 2;
                commonUI.adjustedAromLine1Renderer.SetPositions(new Vector3[]
                {
                    new Vector3(leftX, leftRightY, 0),
                    new Vector3(leftX, -leftRightY, 0)
                });
                commonUI.adjustedAromLine2Renderer.positionCount = 2;
                commonUI.adjustedAromLine2Renderer.SetPositions(new Vector3[]
                {
                    new Vector3(rightX, leftRightY, 0),
                    new Vector3(rightX, -leftRightY, 0)
                });
                break;
            case "AP":
                break;
            case "MLAP":
                break;
        }
    }
    
    public void OnMarsButtonReleased()
    {
        // React differently based on the current assessment state
        switch (aromRawAssessState)
        {
            case AROM_RAW_ASSESS_STATES.INIT:
                aromRawAssessState = AROM_RAW_ASSESS_STATES.ASSESSROM;
                // Initialize lists
                unityPoints = new List<Vector3>();
                endPoints = new List<Vector3>();
                break;
            case AROM_RAW_ASSESS_STATES.ASSESSROM:
                aromRawAssessState = AROM_RAW_ASSESS_STATES.ADJUST;
                // Assessment data collection done. Do the computations.
                newMarsArom.stopAromAssessment();
                break;
        }

    }
    //Assigned to REDO Button
    // public void onclick_recalibrate()
    // {
    //     SceneManager.LoadScene("ASSESSROM");

    // }
    protected virtual void OnDestroy()
    {
        MarsComm.OnMarsButtonReleased -= OnMarsButtonReleased;
    }
}