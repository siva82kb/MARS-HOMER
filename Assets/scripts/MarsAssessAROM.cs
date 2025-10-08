
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.UI;
using System;
using UnityEditor;
using System.IO;
using Unity.Burst.Intrinsics;
using System.Collections;

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
    protected bool newAdjustedValue = false;

    protected CommonUI commonUI;
    protected string movement = null;
    protected List<Vector3> endPoints;
    protected List<Vector3> unityPoints;
    protected GameObject currentPositionCircle;

    protected MarsArom oldMarsArom = null;
    protected MarsArom newMarsArom = null;

    // Other private variables
    private float elapsedTime = 0f;

    // Other private constants
    private const float leftRightY = 2.5f;

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

        // Attach Mars events after a delay.
        StartCoroutine(AttachCallbacksAfterDelay(1f));
    }

    protected virtual void Update()
    {
        MarsComm.sendHeartbeat();
        // Make sure name is not null.
            if (movement == null)
            {
                Debug.LogError("Movement in MarsAssessAROM is null, cannot proceed.");
                return;
            }

        // Update the UI elements
        updateUI();

        // Check if adjustment keys are pressed.
        aromAdjustState = getAromAdjustState(aromAdjustState);

        // Mouse left keydown in the appropriate adjust state.
        newAdjustedValue = aromAdjustState != AROM_ADJUST_STATES.NONE && Input.GetMouseButtonUp(0);

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
                float rightX = OFFSET * SCALEX * ((oldMarsArom.rightRaw.x - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
                commonUI.rawAromLine1RendererOld.positionCount = 2;
                commonUI.rawAromLine1RendererOld.SetPositions(new Vector3[]
                {
                    new Vector3(leftX, leftRightY, 0),
                    new Vector3(leftX, -leftRightY, 0)
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
                // Update AROM box/lines depending on the adjustment state
                adjustAromBoxLines();
                break;
            case AROM_RAW_ASSESS_STATES.DONE:
                // Completion logic
                // Move to the next scene, which will be the game scene.

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
                leftX = OFFSET * SCALEX * ((newMarsArom.leftAdjusted.x - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
                rightX = OFFSET * SCALEX * ((newMarsArom.rightAdjusted.x - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
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
                // Fill the area between the adjusted lines with a semi-transparent box.
                Vector3 boxCenter = new Vector3((leftX + rightX) / 2, 0, 0);
                Vector3 boxSize = new Vector3(Mathf.Abs(rightX - leftX), leftRightY * 2, 0.1f);
                commonUI.aromAreaBox.transform.position = boxCenter;
                commonUI.aromAreaBox.transform.localScale = boxSize;
                commonUI.aromAreaBox.SetActive(true);
                // Transparent red color
                Color boxColor = new Color(1f, 0f, 0f, 0.3f);
                commonUI.aromAreaBox.GetComponent<Renderer>().material.color = boxColor;
                break;
            case "AP":
                break;
            case "MLAP":
                break;
        }
    }

    private void adjustAromBoxLines()
    {
        // What we show depends on the movement.
        // Convert screen coordinates to world coordinates
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        switch (aromAdjustState)
        {
            case AROM_ADJUST_STATES.LEFT:
                // Connect the left line to the position of the mouse in the scene.
                commonUI.adjustedAromLine1Renderer.SetPositions(new Vector3[]
                {
                    new Vector3(mouseWorldPos.x, leftRightY, 0),
                    new Vector3(mouseWorldPos.x, -leftRightY, 0)
                });
                if (movement == "ML")
                {
                    aromAdjustState = setNewAdjustedAromForML(mouseWorldPos, aromAdjustState);
                }
                newAdjustedValue = aromAdjustState == AROM_ADJUST_STATES.NONE ? false : newAdjustedValue;
                break;
            case AROM_ADJUST_STATES.RIGHT:
                // Connect the right line to the position of the mouse in the scene.
                commonUI.adjustedAromLine2Renderer.SetPositions(new Vector3[]
                {
                    new Vector3(mouseWorldPos.x, leftRightY, 0),
                    new Vector3(mouseWorldPos.x, -leftRightY, 0)
                });
                if (movement == "ML")
                {
                    aromAdjustState = setNewAdjustedAromForML(mouseWorldPos, aromAdjustState);
                }
                newAdjustedValue = aromAdjustState == AROM_ADJUST_STATES.NONE ? false : newAdjustedValue;
                break;
            case AROM_ADJUST_STATES.TOP:
                // newMarsArom.adjustTopAdjusted(0.01f);
                break;
            case AROM_ADJUST_STATES.BOTTOM:
                // newMarsArom.adjustBottomAdjusted(-0.01f);
                break;
            case AROM_ADJUST_STATES.NONE:
                // No adjustment
                break;
        }
    }

    private AROM_ADJUST_STATES getAromAdjustState(AROM_ADJUST_STATES currState)
    {
        // This returns a valid code only if in ADJUST state
        if (aromRawAssessState != AROM_RAW_ASSESS_STATES.ADJUST) return AROM_ADJUST_STATES.NONE;
        if (Input.GetKeyDown(KeyCode.L))
        {
            return (movement == "ML" || movement == "MLAP") ? AROM_ADJUST_STATES.LEFT : AROM_ADJUST_STATES.NONE;
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            return (movement == "ML" || movement == "MLAP") ? AROM_ADJUST_STATES.RIGHT : AROM_ADJUST_STATES.NONE;
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            return (movement == "AP" || movement == "MLAP") ? AROM_ADJUST_STATES.TOP : AROM_ADJUST_STATES.NONE;
        }
        else if (Input.GetKeyDown(KeyCode.B))
        {
            return (movement == "AP" || movement == "MLAP") ? AROM_ADJUST_STATES.BOTTOM : AROM_ADJUST_STATES.NONE;
        }
        return currState;
    }

    private AROM_ADJUST_STATES setNewAdjustedAromForML(Vector3 mouseWorldPos, AROM_ADJUST_STATES state)
    {
        float _newx = MarsDefs.EPCENTERZ + mouseWorldPos.x * (MarsDefs.EPMAXZ - MarsDefs.EPMINZ) / (OFFSET * SCALEX);
        // Set the new adjusted value.
        if (!newAdjustedValue) return state;

        // Convert from world coordinates to robot coordinates.
        if (state == AROM_ADJUST_STATES.LEFT)
        {
            newMarsArom.setAdjustedAromLeft(_newx, newMarsArom.leftRaw.y);
            newMarsArom.setAdjustedAromTop(_newx, newMarsArom.leftRaw.y);
        }
        else if (state == AROM_ADJUST_STATES.RIGHT)
        {
            commonUI.adjustedAromLine2Renderer.SetPositions(new Vector3[]
            {
                new Vector3(mouseWorldPos.x, leftRightY, 0),
                new Vector3(mouseWorldPos.x, -leftRightY, 0)
            });
            newMarsArom.setAdjustedAromRight(_newx, newMarsArom.rightRaw.y);
            newMarsArom.setAdjustedAromBottom(_newx, newMarsArom.rightRaw.y);
        }
        return AROM_ADJUST_STATES.NONE;
    }

    private IEnumerator AttachCallbacksAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        MarsComm.OnMarsButtonReleased += OnMarsButtonReleased;
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
                // Start AROM assessment raw data logging.
                AppData.Instance.StartRawDataAromDataLogging(newMarsArom.movement, newMarsArom.datetime);
                break;
            case AROM_RAW_ASSESS_STATES.ASSESSROM:
                aromRawAssessState = AROM_RAW_ASSESS_STATES.ADJUST;
                // Assessment data collection done. Do the computations.
                newMarsArom.stopAromAssessment();
                break;
            case AROM_RAW_ASSESS_STATES.ADJUST:
                aromRawAssessState = AROM_RAW_ASSESS_STATES.DONE;
                // Stop AROM assessment raw data logging.
                AppData.Instance.StopRawDataAromDataLogging();
                // Update AROM for the current movement.
                AppData.Instance.selectedMovement.newArom = newMarsArom;
                // Save the AROM to file.
                AppData.Instance.selectedMovement.newArom.WriteToAssessmentFile();
                // Reload movement data.
                AppData.Instance.selectedMovement.ReloadMovementData();
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