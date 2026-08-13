
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
    protected bool changeScene = false;

    // Other private variables
    private Vector3 tempLeftPos;
    private Vector3 tempRightPos;
    private Vector3 tempTopPos;
    private Vector3 tempBottomPos;
    private bool enableRecalibButton = false;
    private bool showOldArom = true;

    // Other private constants
    private const float leftRightY = 2.5f; 
    private const float topBottomX = 2.5f; 

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
        enableRecalibButton = false;
        changeScene = false;
        AppLogger.LogInfo($"AROM assessment state machine initialized | AROM State: {aromRawAssessState}, Adjust State: {aromAdjustState}");

        // Initialize the original message.
        commonUI.instructionText.text = "";

        // Initialize current position circle
        currentPositionCircle = Instantiate(commonUI.circlePrefab);
        currentPositionCircle.GetComponent<Renderer>().material.color = Color.green;
        currentPositionCircle.transform.localScale = new Vector3(0.15f, 0.15f, 1f);
        currentPositionCircle.SetActive(true); // Initially hide the circle

        // Initialize offset
        SetOffset();
        
        // Initialize lists
        unityPoints = null;
        endPoints = null;

        // Attach Mars events after a delay.
        StartCoroutine(AttachCallbacksAfterDelay(1f));

        // Reset text.
        commonUI.xRangeValueText.text = "";
        commonUI.yRangeValueText.text = "";

        // Show old AROM if available
        showOldArom = oldMarsArom != null;

        // Attach callback for the done button
        commonUI.exitButton.onClick.AddListener(() =>
        {
            AppLogger.LogWarning("Done Button clicked. Existing assessment scene.");
            // Move to the next scene, which will be the game scene.
            changeScene = true;
        });
    }

    protected virtual void Update()
    {
        MarsComm.sendHeartbeat();
        // Make sure name is not null.
        if (movement == null)
        {
            Debug.LogError("Movement in MarsAssessAROM is null, cannot proceed.");
            AppLogger.LogError("No movement set. The programm cannot proceed.");
            return;
        }

        // Update the UI elements
        updateUI();

        // Check if adjustment keys are pressed.
        aromAdjustState = getAromAdjustState(aromAdjustState);

        // Mouse left keydown in the appropriate adjust state.
        if (aromAdjustState != AROM_ADJUST_STATES.NONE && Input.GetMouseButtonUp(0))
        {
            aromAdjustState = AROM_ADJUST_STATES.NONE;
        }

        // Run the raw assessment statemachine
        runAROMRawAssessStateMachine();
    }

    private void updateUI()
    {
        // Update the endpoint position text
        commonUI.marsEPPosText.text = $"{MarsComm.epPosInThePlane.z:F2}m, {MarsComm.epPosInThePlane.y:F2}m";

        // Display the green circle for the current position. Use the circlePrefab from CommonUI.
        updateCurrentPositionCircle();

        // Draw the old AROM lines in light blue.
        if (showOldArom)
        {
            // Check the movement name
            switch(oldMarsArom.movement)
            {
                case "ML":
                    showOldAdjustAromForML();
                    break;
                case "AP":
                    showOldAdjustAromForAP();
                    break;
                case "MLAP":
                    showOldAdjustAromForMLAP();
                    break;
            }
        }

        // Enable the recalibrate button if needed.
        if (enableRecalibButton)
        {
            commonUI.EnableRecalibrateButton();
            enableRecalibButton = false;
            // Attach the onclick event
            commonUI.recalibrateButton.onClick.RemoveAllListeners();
            commonUI.recalibrateButton.onClick.AddListener(() =>
            {
                resetAssessment();
            });
        }
    }

    private void resetAssessment()
    {
        // Reset the state machine
        aromRawAssessState = AROM_RAW_ASSESS_STATES.INIT;
        aromAdjustState = AROM_ADJUST_STATES.NONE;
        newAdjustedValue = false;
        changeScene = false;

        // Clear the trajectory line
        commonUI.ClearLineRenderers();

        // Clear the range text
        commonUI.xRangeValueText.text = "";
        commonUI.yRangeValueText.text = "";

        // Clear lists
        unityPoints = null;
        endPoints = null;

        // Reset temporary positions
        tempLeftPos = Vector3.zero;
        tempRightPos = Vector3.zero;
        tempTopPos = Vector3.zero;
        tempBottomPos = Vector3.zero;

        // Reset newMarsArom
        newMarsArom = new MarsArom(movement, readFromFile: false);

        // Show old AROM again.
        showOldArom = oldMarsArom != null;

        AppLogger.LogInfo($"Resetting AROM assessment. | AROM State: {aromRawAssessState}, Adjust State: {aromAdjustState}");
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
                // showRawAromBoxLines();
                // Show adjusted AROM box
                showAdjustedAromBoxLines();
                // Updat range text.
                updateAromRangeText();
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

    // Some useful conversion functions.
    private void SetOffset()
    {
        OFFSET = AppData.Instance.userData?.limb == 1 ? -1 : 1;
    }
    private float robotToUnityX(float robotX) => OFFSET * SCALEX * ((robotX - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
    private float robotToUnityY(float robotY) => SCALEY * ((robotY - MarsDefs.EPCENTERY) / (MarsDefs.EPMAXY - MarsDefs.EPMINY));

    private void showAdjustedAromBoxLines()
    {
        // What we show depends on the movement.
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        // Update temporary positions — clamped to the robot's valid workspace.
        float _adjustedX = Mathf.Clamp(
            MarsDefs.EPCENTERZ + mouseWorldPos.x * (MarsDefs.EPMAXZ - MarsDefs.EPMINZ) / (OFFSET * SCALEX),
            MarsDefs.EPMINZ, MarsDefs.EPMAXZ);
        float _adjustedY = Mathf.Clamp(
            MarsDefs.EPCENTERY + mouseWorldPos.y * (MarsDefs.EPMAXY - MarsDefs.EPMINY) / SCALEY,
            MarsDefs.EPMINY, MarsDefs.EPMAXY);
        switch (aromAdjustState)
        {
            case AROM_ADJUST_STATES.LEFT:
                tempLeftPos.x = _adjustedX;
                tempLeftPos.y = _adjustedY;
                break;
            case AROM_ADJUST_STATES.RIGHT:
                tempRightPos.x = _adjustedX;
                tempRightPos.y = _adjustedY;
                break;
            case AROM_ADJUST_STATES.TOP:
                tempTopPos.x = _adjustedX;
                tempTopPos.y = _adjustedY;
                break;
            case AROM_ADJUST_STATES.BOTTOM:
                tempBottomPos.x = _adjustedX;
                tempBottomPos.y = _adjustedY;
                break;
            default:
                // Optional: handle unexpected states or do nothing
                break;
        }
        // Update display based on the current movement.
        switch (movement)
        {
            case "ML":
                // Show the raw AROM lines
                showAdjustAromForML();
                break;
            case "AP":
                showAdjustAromForAP();
                break;
            case "MLAP":
                showAdjustAromForMLAP();
                break;
        }
    }

    private void updateAromRangeText()
    {
        // Update the AROM range text based on movement
        switch (movement)
        {
            case "ML":
                float mlRange = tempRightPos.x - tempLeftPos.x;
                commonUI.xRangeValueText.text = $"{100 * mlRange:F2}cm";
                commonUI.yRangeValueText.text = "";
                break;
            case "AP":
                float apRange = tempTopPos.y - tempBottomPos.y;
                commonUI.xRangeValueText.text = "";
                commonUI.yRangeValueText.text = $"{100 * apRange:F2}cm";
                break;
            case "MLAP":
                float mlapRangeX = tempRightPos.x - tempLeftPos.x;
                float mlapRangeY = tempTopPos.y - tempBottomPos.y;
                commonUI.xRangeValueText.text = $"{100 * mlapRangeX:F2}cm (ML)";
                commonUI.yRangeValueText.text = $"{100 * mlapRangeY:F2}cm (AP)";
                break;
            default:
                break;
        }
    }
    
    private void showAdjustAromForML()
    {
        // Convert screen coordinates to world coordinates
        // Update the adjusted AROM line renderers
        float leftX = OFFSET * SCALEX * ((tempLeftPos.x - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
        float rightX = OFFSET * SCALEX * ((tempRightPos.x - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
        commonUI.aromLine1Renderer.positionCount = 2;
        commonUI.aromLine1Renderer.SetPositions(new Vector3[]
        {
            new Vector3(leftX, leftRightY, 0),
            new Vector3(leftX, -leftRightY, 0)
        });
        commonUI.aromLine2Renderer.positionCount = 2;
        commonUI.aromLine2Renderer.SetPositions(new Vector3[]
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
        Color boxColor = new Color(1f, 0f, 0f, 0.5f);
        commonUI.aromAreaBox.GetComponent<Renderer>().material.color = boxColor;
    }
    
    private void showAdjustAromForAP()
    {
        // Convert screen coordinates to world coordinates
        // Update the adjusted AROM line renderers
        float topY = SCALEY * ((tempTopPos.y - MarsDefs.EPCENTERY) / (MarsDefs.EPMAXY - MarsDefs.EPMINY));
        float bottomY = SCALEY * ((tempBottomPos.y - MarsDefs.EPCENTERY) / (MarsDefs.EPMAXY - MarsDefs.EPMINY));
        commonUI.aromLine1Renderer.positionCount = 2;
        commonUI.aromLine1Renderer.SetPositions(new Vector3[]
        {
            new Vector3(topBottomX, topY, 0),
            new Vector3(-topBottomX, topY, 0)
        });
        commonUI.aromLine2Renderer.positionCount = 2;
        commonUI.aromLine2Renderer.SetPositions(new Vector3[]
        {
            new Vector3(topBottomX, bottomY, 0),
            new Vector3(-topBottomX, bottomY, 0)
        });
        // Fill the area between the adjusted lines with a semi-transparent box.
        Vector3 boxCenter = new Vector3(0, (topY + bottomY) / 2, 0);
        Vector3 boxSize = new Vector3(2 * topBottomX, Mathf.Abs(bottomY - topY), 0.1f);
        commonUI.aromAreaBox.transform.position = boxCenter;
        commonUI.aromAreaBox.transform.localScale = boxSize;
        commonUI.aromAreaBox.SetActive(true);
        // Transparent red color
        Color boxColor = new Color(1f, 0f, 0f, 0.5f);
        commonUI.aromAreaBox.GetComponent<Renderer>().material.color = boxColor;
    }
    
    private void showAdjustAromForMLAP()
    {
        // Verticex.
        Vector3[] vertices = new Vector3[]
        {
            // Left point
            new Vector3(robotToUnityX(tempLeftPos.x), robotToUnityY(tempLeftPos.y), 0),
            // Bottom point
            new Vector3(robotToUnityX(tempBottomPos.x), robotToUnityY(tempBottomPos.y), 0),
            // Right point
            new Vector3(robotToUnityX(tempRightPos.x), robotToUnityY(tempRightPos.y), 0),
            // Top point
            new Vector3(robotToUnityX(tempTopPos.x), robotToUnityY(tempTopPos.y), 0)
        };
        // Show the adjusted AROM Box.
        commonUI.aromBoxLineRenderer.positionCount = 5;
        commonUI.aromBoxLineRenderer.SetPositions(new Vector3[]
        {
            // Left point
            vertices[0],
            // Bottom point
            vertices[1],
            // Right point
            vertices[2],
            // Top point
            vertices[3],
            // Closing the box (back to Left point)
            vertices[0]
        });
    }

    private void showOldAdjustAromForML()
    {
        // Show the parallel vertical lines
        float leftX = OFFSET * SCALEX * ((oldMarsArom.leftAdjusted.x - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
        float rightX = OFFSET * SCALEX * ((oldMarsArom.rightAdjusted.x - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
        commonUI.aromLine1RendererOld.positionCount = 2;
        commonUI.aromLine1RendererOld.SetPositions(new Vector3[]
        {
            new Vector3(leftX, leftRightY, 0),
            new Vector3(leftX, -leftRightY, 0)
        });
        commonUI.aromLine2RendererOld.positionCount = 2;
        commonUI.aromLine2RendererOld.SetPositions(new Vector3[]
        {
            new Vector3(rightX, leftRightY, 0),
            new Vector3(rightX, -leftRightY, 0)
        });
        // Display old area
        // Fill the area between the adjusted lines with a semi-transparent box.
        Vector3 boxCenter = new Vector3((leftX + rightX) / 2, 0, 0);
        Vector3 boxSize = new Vector3(Mathf.Abs(rightX - leftX), leftRightY * 2, 0.1f);
        commonUI.aromAreaBoxOld.transform.position = boxCenter;
        commonUI.aromAreaBoxOld.transform.localScale = boxSize;
        commonUI.aromAreaBoxOld.SetActive(true);
        // Transparent blue color
        Color boxColor = new Color(0f, 0f, 1f, 0.3f);
        commonUI.aromAreaBoxOld.GetComponent<Renderer>().material.color = boxColor;
        showOldArom = false; // Show only once
    }

    private void showOldAdjustAromForAP()
    {
        // Show the parallel vertical lines
        float topY = SCALEY * ((oldMarsArom.topAdjusted.y - MarsDefs.EPCENTERY) / (MarsDefs.EPMAXY - MarsDefs.EPMINY));
        float bottomY = SCALEY * ((oldMarsArom.bottomAdjusted.y - MarsDefs.EPCENTERY) / (MarsDefs.EPMAXY - MarsDefs.EPMINY));
        commonUI.aromLine1RendererOld.positionCount = 2;
        commonUI.aromLine1RendererOld.SetPositions(new Vector3[]
        {
            new Vector3(topBottomX, topY, 0),
            new Vector3(-topBottomX, topY, 0)
        });
        commonUI.aromLine2RendererOld.positionCount = 2;
        commonUI.aromLine2RendererOld.SetPositions(new Vector3[]
        {
            new Vector3(topBottomX, bottomY, 0),
            new Vector3(-topBottomX, bottomY, 0)
        });
        // Display old area
        // Fill the area between the adjusted lines with a semi-transparent box.
        Vector3 boxCenter = new Vector3(0, (topY + bottomY) / 2, 0);
        Vector3 boxSize = new Vector3(2 * topBottomX, Mathf.Abs(bottomY - topY), 0.1f);
        commonUI.aromAreaBoxOld.transform.position = boxCenter;
        commonUI.aromAreaBoxOld.transform.localScale = boxSize;
        commonUI.aromAreaBoxOld.SetActive(true);
        // Transparent blue color
        Color boxColor = new Color(0f, 0f, 1f, 0.3f);
        commonUI.aromAreaBoxOld.GetComponent<Renderer>().material.color = boxColor;
        showOldArom = false; // Show only once
    }

    private void showOldAdjustAromForMLAP()
    {
        // Vertices
        Vector3[] vertices = new Vector3[]
        {
            // Left point
            new Vector3(robotToUnityX(oldMarsArom.leftAdjusted.x), robotToUnityY(oldMarsArom.leftAdjusted.y), 0),
            // Bottom point
            new Vector3(robotToUnityX(oldMarsArom.bottomAdjusted.x), robotToUnityY(oldMarsArom.bottomAdjusted.y), 0),
            // Right point
            new Vector3(robotToUnityX(oldMarsArom.rightAdjusted.x), robotToUnityY(oldMarsArom.rightAdjusted.y), 0),
            // Top point
            new Vector3(robotToUnityX(oldMarsArom.topAdjusted.x), robotToUnityY(oldMarsArom.topAdjusted.y), 0)
        };
        commonUI.aromBoxLineRendererOld.positionCount = 5;
        commonUI.aromBoxLineRendererOld.SetPositions(new Vector3[]
        {
            // Left point
            vertices[0],
            // Bottom point
            vertices[1],
            // Right point
            vertices[2],
            // Top point
            vertices[3],
            // Closing the box (back to Left point)
            vertices[0]
        });
        showOldArom = false; // Show only once
    }

    private AROM_ADJUST_STATES getAromAdjustState(AROM_ADJUST_STATES currState)
    {
        AROM_ADJUST_STATES _newAdujustState = currState;
        // This returns a valid code only if in ADJUST state
        if (aromRawAssessState != AROM_RAW_ASSESS_STATES.ADJUST)
        {
            _newAdujustState = AROM_ADJUST_STATES.NONE;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                tempLeftPos = newMarsArom.leftAdjusted;
                _newAdujustState = AROM_ADJUST_STATES.LEFT;
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                tempRightPos = newMarsArom.rightAdjusted;
                _newAdujustState = AROM_ADJUST_STATES.RIGHT;
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                tempTopPos = newMarsArom.topAdjusted;
                _newAdujustState = AROM_ADJUST_STATES.TOP;
            }
            else if (Input.GetKeyDown(KeyCode.B))
            {
                tempBottomPos = newMarsArom.bottomAdjusted;
                _newAdujustState = AROM_ADJUST_STATES.BOTTOM;
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                switch (aromAdjustState)
                {
                    case AROM_ADJUST_STATES.LEFT:
                        tempLeftPos = newMarsArom.leftAdjusted;
                        break;
                    case AROM_ADJUST_STATES.RIGHT:
                        tempRightPos = newMarsArom.rightAdjusted;
                        break;
                    case AROM_ADJUST_STATES.TOP:
                        tempTopPos = newMarsArom.topAdjusted;
                        break;
                    case AROM_ADJUST_STATES.BOTTOM:
                        tempBottomPos = newMarsArom.bottomAdjusted;
                        break;
                }
                _newAdujustState = AROM_ADJUST_STATES.NONE;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                // Validate boundary ordering before committing: left must be < right, bottom < top.
                bool _valid = movement switch
                {
                    "ML"   => tempLeftPos.x < tempRightPos.x,
                    "AP"   => tempBottomPos.y < tempTopPos.y,
                    "MLAP" => tempLeftPos.x < tempRightPos.x && tempBottomPos.y < tempTopPos.y,
                    _      => true
                };

                if (_valid)
                {
                    newMarsArom.setAdjustedAromLeft(tempLeftPos.x, tempLeftPos.y);
                    newMarsArom.setAdjustedAromRight(tempRightPos.x, tempRightPos.y);
                    newMarsArom.setAdjustedAromTop(tempTopPos.x, tempTopPos.y);
                    newMarsArom.setAdjustedAromBottom(tempBottomPos.x, tempBottomPos.y);
                }
                else
                {
                    // Snap temp positions back to last valid adjusted values.
                    tempLeftPos   = newMarsArom.leftAdjusted;
                    tempRightPos  = newMarsArom.rightAdjusted;
                    tempTopPos    = newMarsArom.topAdjusted;
                    tempBottomPos = newMarsArom.bottomAdjusted;
                    AppLogger.LogWarning("AROM boundary rejected: left >= right or bottom >= top. Reverted.");
                }
                _newAdujustState = AROM_ADJUST_STATES.NONE;
            }
        }
        if (currState != _newAdujustState)
        {
            AppLogger.LogInfo($"Changing adjust state from {currState} to {_newAdujustState}.");
        }
        return _newAdujustState;
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
            commonUI.aromLine2Renderer.SetPositions(new Vector3[]
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
                AppLogger.LogInfo($"Chaning Asessment State | AROM State: {aromRawAssessState}, Adjust State: {aromAdjustState}");
                break;
            case AROM_RAW_ASSESS_STATES.ASSESSROM:
                aromRawAssessState = AROM_RAW_ASSESS_STATES.ADJUST;
                // Assessment data collection done. Do the computations.
                newMarsArom.stopAromAssessment();
                // Set the temporary adjusted positions to the raw positions.
                tempLeftPos = newMarsArom.leftAdjusted;
                tempRightPos = newMarsArom.rightAdjusted;
                tempTopPos = newMarsArom.topAdjusted;
                tempBottomPos = newMarsArom.bottomAdjusted;
                Debug.Log("RAW:" + newMarsArom.leftRaw + " " + newMarsArom.rightRaw + " " + newMarsArom.topRaw + " " + newMarsArom.bottomRaw);
                Debug.Log("Adjusted:" + newMarsArom.leftAdjusted + " " + newMarsArom.rightAdjusted + " " + newMarsArom.topAdjusted + " " + newMarsArom.bottomAdjusted);
                Debug.Log("Temp:" + tempLeftPos + " " + tempRightPos + " " + tempTopPos + " " + tempBottomPos);
                enableRecalibButton = true;
                AppLogger.LogInfo($"Chaning Asessment State. | AROM State: {aromRawAssessState}, Adjust State: {aromAdjustState}");
                AppLogger.LogInfo($"Initialize variables for AROM adjustment.");
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
                // Set change scene flag to true.
                changeScene = true;
                AppLogger.LogInfo($"Chaning Asessment State | AROM State: {aromRawAssessState}, Adjust State: {aromAdjustState}");
                AppLogger.LogInfo($"Saving AROM data to file.");
                AppLogger.LogInfo($"Change scene flag set.");
                break;
        }
    }
    
    protected virtual void OnDestroy()
    {
        MarsComm.OnMarsButtonReleased -= OnMarsButtonReleased;
    }
}