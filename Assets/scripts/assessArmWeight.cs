
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.UI;
using System;
using UnityEditor;
using System.IO;
using JetBrains.Annotations;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Collections;
using Unity.Burst.Intrinsics;

public class AssessArmWeight : MonoBehaviour
{
    // Static variables related to drawing on the scren.
    public static int OFFSET { get; private set; }
    public static readonly float SCALEX = 1000f;
    public static readonly float SCALEY = 1000f;
    public static readonly Vector3 SCREEN_OFFSET = new Vector3(0f, 0f, 0f);

    // Scenes to change to.
    private readonly string preScene = "CHOOSEMOVE";
    private readonly string robotCalibScene = "ROBOTCALIB";
    private readonly string marsSetUp = "MARSSETUP";
    private readonly string mlapAromAssess = "AROMMLAP";

    public GameObject epPos;
    public GameObject leftTarget;
    public GameObject rightTarget;
    public GameObject  topTarget;
    public GameObject bottomTarget;
    public GameObject centerTarget;
    public LineRenderer aromBoxLineRenderer;
    public Text instructionText;
    private MarsArom mlapArom;

    private bool changeScene = false;

    // Arm weight assessment statemachine variables
    private enum ARMWEIGHT_ASSESS_STATE
    {
        INIT,
        WAIT_FOR_TARGET_SELECTION,
        MOVING_TO_TARGET,
        IN_TARGET,
        IN_TARGET_RECORDING,
        TARGET_RECORDED,
        ALL_DONE
    }
    private ARMWEIGHT_ASSESS_STATE currentState = ARMWEIGHT_ASSESS_STATE.INIT;
    private ArmWeight.ARMWEIGHT_TARGET currentTarget
    {
        get
        {
            return currentTarget;
        }
        set
        {
            currentTarget = value;
            AppData.Instance.annotation = (int)currentTarget;
        }
    }
    private GameObject currentTargetObject = null;
    private Vector3 currentTargetPosition = Vector3.zero;
    private ArmWeight armWeight;
    private float stateStartTime = 0f;
    private const float IN_TARGET_RECORDING_TIME = 2f;
    private bool updateTargetsDisplayFlag = false;

    // Some constants
    public const float TARGET_SIZE = 0.05f;  // cm
    public const float TARGET_REACH_SCALE = 2.0f;
    public const float TARGET_COMPLETE_SCALE = 0.6f;

    public static void SetOffset()
    {
        OFFSET = AppData.Instance.userData?.limb == 1 ? -1 : 1;
    }
    public static float robotToUnityX(float robotX) => OFFSET * SCALEX * ((robotX - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
    public static float robotToUnityY(float robotY) => SCALEY * ((robotY - MarsDefs.EPCENTERY) / (MarsDefs.EPMAXY - MarsDefs.EPMINY));


    void Awake()
    {
        MarsComm.sendHeartbeat();
        MarsComm.setControlType("POSITION");
    }

    void Start()
    {
        // Initialize AppData if needed
        if (AppData.Instance.userData == null)
        {
            AppData.Instance.Initialize(SceneManager.GetActiveScene().name);
        }

        // Check if the directory exists
        if (!Directory.Exists(DataManager.basePath)) Directory.CreateDirectory(DataManager.basePath);
        if (!File.Exists(DataManager.configFile)) SceneManager.LoadScene("CONFIG");

        // Logging the scene
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

        // If the robot is not calibrated go to the robot calib scene.
        if (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB") SceneManager.LoadScene(robotCalibScene);

        // If the robot is not in position control go to the mars setup scene.
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] != "POSITION") SceneManager.LoadScene(marsSetUp);

        // First check of MLAP AROM assessment is available.
        SetOffset();
        aromBoxLineRenderer.positionCount = 0;
        if (AppData.Instance.userData.IsAromAssessmentAvailableForTrainingAngle("MLAP"))
        {
            leftTarget.SetActive(true);
            rightTarget.SetActive(true);
            topTarget.SetActive(true);
            bottomTarget.SetActive(true);
            centerTarget.SetActive(true);
            // Draw the MLAP AROM box.
            mlapArom = new MarsArom("MLAP", readFromFile: true);
            drawMLAPAromBox();
        }
        else
        {
            SceneManager.LoadScene(mlapAromAssess);
        }

        // Initialize the state machine.
        currentState = ARMWEIGHT_ASSESS_STATE.INIT;
        currentTarget = ArmWeight.ARMWEIGHT_TARGET.NONE;
        stateStartTime = 0f;
        armWeight = new ArmWeight(false);
        updateTargetsDisplayFlag = true;

        // Attach Mars events after a delay.
        StartCoroutine(AttachCallbacksAfterDelay(1f));
    }

    void Update()
    {
        MarsComm.sendHeartbeat();

        // Update the current position of the end effector.
        updateCurrentEpPosition();

        // Read and handle keyboard input.
        handleKeyboardInput();

        // Run the statemachine.
        runArmWeightAssessStateMachine();

        // Update target display.
        if (updateTargetsDisplayFlag)
        {
            updateTargetDisplay();
            updateTargetsDisplayFlag = false;
        }

        // Check if its time to change scene.
        if (changeScene)
        {
            SceneManager.LoadScene(preScene);
        }
    }

    private void handleKeyboardInput()
    {
        // Respond only when the state is WAIT_FOR_TARGET_SELECTION
        if (currentState != ARMWEIGHT_ASSESS_STATE.WAIT_FOR_TARGET_SELECTION) return;
        if (Input.GetKeyDown(KeyCode.L))
        {
            currentTarget = ArmWeight.ARMWEIGHT_TARGET.LEFT;
            currentTargetObject = leftTarget;
            currentTargetPosition = new Vector3(
                0,
                mlapArom.leftAdjusted.y,
                mlapArom.leftAdjusted.x
            );
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            currentTarget = ArmWeight.ARMWEIGHT_TARGET.RIGHT;
            currentTargetObject = rightTarget;
            currentTargetPosition = new Vector3(
                0,
                mlapArom.rightAdjusted.y,
                mlapArom.rightAdjusted.x
            );
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            currentTarget = ArmWeight.ARMWEIGHT_TARGET.TOP;
            currentTargetObject = topTarget;
            currentTargetPosition = new Vector3(
                0,
                mlapArom.topAdjusted.y,
                mlapArom.topAdjusted.x
            );
        }
        else if (Input.GetKeyDown(KeyCode.B))
        {
            currentTarget = ArmWeight.ARMWEIGHT_TARGET.BOTTOM;
            currentTargetObject = bottomTarget;
            currentTargetPosition = new Vector3(
                0,
                mlapArom.bottomAdjusted.y,
                mlapArom.bottomAdjusted.x
            );
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            currentTarget = ArmWeight.ARMWEIGHT_TARGET.CENTER;
            currentTargetObject = centerTarget;
            currentTargetPosition = new Vector3(
                0,
                0.25f * (mlapArom.leftAdjusted.y + mlapArom.rightAdjusted.y + mlapArom.topAdjusted.y + mlapArom.bottomAdjusted.y),
                0.25f * (mlapArom.leftAdjusted.x + mlapArom.rightAdjusted.x + mlapArom.topAdjusted.x + mlapArom.bottomAdjusted.x)
            );
        }
        else
        {
            currentTarget = ArmWeight.ARMWEIGHT_TARGET.NONE;
            currentTargetObject = null;
            currentTargetPosition = Vector3.zero;
            return;
        }
        AppLogger.LogInfo($"Target selected for arm weight assessment. | {currentTarget}: {currentTargetPosition}.");
        // Change state to MOVING_TO_TARGET
        currentState = ARMWEIGHT_ASSESS_STATE.MOVING_TO_TARGET;
        stateStartTime = Time.time;
        armWeight.startArmWeightAssessment(currentTarget);
        // Upate raw data annotation
        AppData.Instance.annotation = (int)currentTarget;
        updateTargetsDisplayFlag = true;
        AppLogger.LogInfo($"State changed to {currentState}.");
    }

    private void runArmWeightAssessStateMachine()
    {
        switch (currentState)
        {
            case ARMWEIGHT_ASSESS_STATE.INIT:
                instructionText.text = "Press the MARS button to start the assessment.";
                break;
            case ARMWEIGHT_ASSESS_STATE.WAIT_FOR_TARGET_SELECTION:
                instructionText.text = "Select a target by pressing L, R, T, B, or C.";
                break;
            case ARMWEIGHT_ASSESS_STATE.MOVING_TO_TARGET:
                instructionText.text = $"Moving to {currentTarget} target.";
                // Check if the robot has reached the target.
                if (Mathf.Abs(MarsComm.epPosInThePlane.x - currentTargetPosition.x) < 0.5 * TARGET_SIZE &&
                    Mathf.Abs(MarsComm.epPosInThePlane.y - currentTargetPosition.y) < 0.5 * TARGET_SIZE &&
                    Mathf.Abs(MarsComm.epPosInThePlane.z - currentTargetPosition.z) < 0.5 * TARGET_SIZE)
                {
                    currentState = ARMWEIGHT_ASSESS_STATE.IN_TARGET;
                    stateStartTime = Time.time;
                    updateTargetsDisplayFlag = true;
                    AppLogger.LogInfo($"State changed to {currentState}.");
                }
                break;
            case ARMWEIGHT_ASSESS_STATE.IN_TARGET:
                instructionText.text = $"In {currentTarget} target. Hold still.";
                if (Mathf.Abs(MarsComm.epPosInThePlane.x - currentTargetPosition.x) >= 0.5 * TARGET_SIZE ||
                    Mathf.Abs(MarsComm.epPosInThePlane.y - currentTargetPosition.y) >= 0.5 * TARGET_SIZE ||
                    Mathf.Abs(MarsComm.epPosInThePlane.z - currentTargetPosition.z) >= 0.5 * TARGET_SIZE)
                {
                    // Moved out of target, go back to moving to target.
                    currentState = ARMWEIGHT_ASSESS_STATE.MOVING_TO_TARGET;
                    stateStartTime = Time.time;
                    updateTargetsDisplayFlag = true;
                    AppLogger.LogInfo($"State changed to {currentState}.");
                }
                else
                {
                    // Mars button is pressed
                    if (MarsComm.buttonState == 1)
                    {
                        // Button not pressed.
                        stateStartTime = Time.time;
                    }
                    else
                    {
                        // Button released, start recording.
                        currentState = ARMWEIGHT_ASSESS_STATE.IN_TARGET_RECORDING;
                        stateStartTime = Time.time;
                        AppLogger.LogInfo($"State changed to {currentState}.");
                    }
                }
                break;
            case ARMWEIGHT_ASSESS_STATE.IN_TARGET_RECORDING:
                instructionText.text = $"Recording data for {currentTarget} target. Hold still.";
                if (Mathf.Abs(MarsComm.epPosInThePlane.x - currentTargetPosition.x) >= 0.5 * TARGET_SIZE ||
                    Mathf.Abs(MarsComm.epPosInThePlane.y - currentTargetPosition.y) >= 0.5 * TARGET_SIZE ||
                    Mathf.Abs(MarsComm.epPosInThePlane.z - currentTargetPosition.z) >= 0.5 * TARGET_SIZE)
                {
                    // Moved out of target, go back to moving to target.
                    currentState = ARMWEIGHT_ASSESS_STATE.MOVING_TO_TARGET;
                    stateStartTime = Time.time;
                    updateTargetsDisplayFlag = true;
                    AppLogger.LogInfo($"State changed to {currentState}.");
                }
                else if (MarsComm.buttonState == 1)
                {
                    // Button pressed, go back to in target.
                    currentState = ARMWEIGHT_ASSESS_STATE.IN_TARGET;
                    stateStartTime = Time.time;
                    updateTargetsDisplayFlag = true;
                    AppLogger.LogInfo($"State changed to {currentState}.");
                }
                else
                {
                    // Add data to the assessment.
                    armWeight.addArmWeightDataPoint(MarsComm.epPosInThePlane.z, MarsComm.epPosInThePlane.y, MarsComm.force);
                    // Completed recording?
                    if (Time.time - stateStartTime < IN_TARGET_RECORDING_TIME) break;
                    // Mark the current target as completed.
                    currentState = ARMWEIGHT_ASSESS_STATE.TARGET_RECORDED;
                    stateStartTime = Time.time;
                    AppLogger.LogInfo($"State changed to {currentState}.");
                }
                break;
            case ARMWEIGHT_ASSESS_STATE.TARGET_RECORDED:
                instructionText.text = $"{currentTarget} target recorded.";
                armWeight.stopArmWeightAssessment();
                // Check if all targets are done.
                if (armWeight.isAssessmentComplete)
                {
                    currentState = ARMWEIGHT_ASSESS_STATE.ALL_DONE;
                    stateStartTime = Time.time;
                    updateTargetsDisplayFlag = true;
                }
                else
                {
                    currentState = ARMWEIGHT_ASSESS_STATE.WAIT_FOR_TARGET_SELECTION;
                    currentTarget = ArmWeight.ARMWEIGHT_TARGET.NONE;
                    currentTargetObject = null;
                    currentTargetPosition = Vector3.zero;
                    stateStartTime = Time.time;
                    updateTargetsDisplayFlag = true;
                    AppLogger.LogInfo($"State changed to {currentState}.");
                }
                break;
            case ARMWEIGHT_ASSESS_STATE.ALL_DONE:
                instructionText.text = "Assessment complete. Press the MARS button to save and exit the scene.";
                break;
        }
    }

    void getRandomTargt()
    {
        // Vector2 t;
        // Vector2 target;
      
        // //rx+ry<=1
        // float rx = UnityEngine.Random.Range(0, 1f);
        // float ry = UnityEngine.Random.Range(0, (1f - rx));

        // //find left or right
        // int random = UnityEngine.Random.value < 0.5f ? -1 : 1;
     
        // if (random == 1)
        // {
           
        //     t = (rx * x1) + (ry * y1);
        //     target = t + left;
            
        // }
        // else
        // {
        //     t = (rx * x2) +( ry * y2);
        //     target = t + right;
        // }

        // GameObject circle6 = Instantiate(circlePrefab, target, Quaternion.identity);
        //testCircle = circle6;
        // if (testCircle != null)
        //     Destroy(testCircle);
        // testCircle = circle6;

    }

    void OnDestroy()
    {
        // MarsComm.OnMarsButtonReleased -= OnMarsButtonReleased;
    }

    private IEnumerator AttachCallbacksAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        MarsComm.OnMarsButtonReleased += OnMarsButtonReleased;
    }

    private void OnMarsButtonReleased()
    {
        // Act according to the current state.
        switch(currentState)
        {
            case ARMWEIGHT_ASSESS_STATE.INIT:
                // Move to the next state.
                currentState = ARMWEIGHT_ASSESS_STATE.WAIT_FOR_TARGET_SELECTION;
                armWeight.initializeArmWeightAssessment();
                // Initialize raw data annotation and logging.
                AppData.Instance.annotation = (int) currentTarget;
                AppData.Instance.StartRawDataArmWeightDataLogging(armWeight.datetime.Replace(" ", "_").Replace(":", "-"));
                AppLogger.LogInfo($"State changed to {currentState}.");
                break;
            case ARMWEIGHT_ASSESS_STATE.ALL_DONE:
                // Save the assessment and exit the scene.
                armWeight.WriteToArmWeightFile();
                AppData.Instance.StopRawDataArmWeightDataLogging();
                AppLogger.LogInfo("Arm weight assessment completed and saved.");
                changeScene = true;
                break;
        }
    }

    private void drawMLAPAromBox()
    {
        // Draw the MLAP AROM box.
        Vector3[] vertices = new Vector3[]
        {
            // Left
            new Vector3(robotToUnityX(mlapArom.leftAdjusted.x),
                        robotToUnityY(mlapArom.leftAdjusted.y),
                        1),
            // Top
            new Vector3(robotToUnityX(mlapArom.topAdjusted.x),
                        robotToUnityY(mlapArom.topAdjusted.y),
                        1),
            // Right
            new Vector3(robotToUnityX(mlapArom.rightAdjusted.x),
                        robotToUnityY(mlapArom.rightAdjusted.y),
                        1),
            // Bottom
            new Vector3(robotToUnityX(mlapArom.bottomAdjusted.x),
                        robotToUnityY(mlapArom.bottomAdjusted.y),
                        1),
        };
        Debug.Log($"Actual Vertices: {mlapArom.leftAdjusted}, {mlapArom.rightAdjusted}, {mlapArom.topAdjusted}, {mlapArom.bottomAdjusted}");
        Debug.Log($"Unity Vertices: {string.Join(", ", vertices.Select(v => v.ToString()).ToArray())}");
        // Draw the box using LineRenderer
        aromBoxLineRenderer.startWidth = 3f;
        aromBoxLineRenderer.endWidth = 3f;
        aromBoxLineRenderer.startColor = CommonUI.LIGHTER_BROWN;
        aromBoxLineRenderer.endColor = CommonUI.LIGHTER_BROWN;
        aromBoxLineRenderer.positionCount = 5;
        aromBoxLineRenderer.SetPositions(new Vector3[]
        {
            // Left point
            vertices[0],
            // Top point
            vertices[1],
            // Right point
            vertices[2],
            // Bottom point
            vertices[3],
            // Closing the box (back to Left point)
            vertices[0]
        });
        // Draw the targets.
        leftTarget.transform.localPosition = new Vector3(vertices[0].x, vertices[0].y, 0);
        topTarget.transform.localPosition = new Vector3(vertices[1].x, vertices[1].y, 0);
        rightTarget.transform.localPosition = new Vector3(vertices[2].x, vertices[2].y, 0);
        bottomTarget.transform.localPosition = new Vector3(vertices[3].x, vertices[3].y, 0);
        centerTarget.transform.localPosition = new Vector3(
            0.25f * (vertices[0].x + vertices[1].x + vertices[2].x + vertices[3].x),
            0.25f * (vertices[0].y + vertices[1].y + vertices[2].y + vertices[3].y),
            0
        );
        // Target sizes.
        float tgtSizeX = 2 * SCALEX * TARGET_SIZE;
        float tgtSizeY = 2 * SCALEY * TARGET_SIZE;
        leftTarget.transform.localScale = new Vector3(tgtSizeX, tgtSizeY, 1);
        rightTarget.transform.localScale = new Vector3(tgtSizeX, tgtSizeY, 1);
        topTarget.transform.localScale = new Vector3(tgtSizeX, tgtSizeY, 1);
        bottomTarget.transform.localScale = new Vector3(tgtSizeX, tgtSizeY, 1);
        centerTarget.transform.localScale = new Vector3(tgtSizeX, tgtSizeY, 1);
    }

    private void updateCurrentEpPosition()
    {
        Vector3 ep = new Vector3(
            robotToUnityX(MarsComm.epPosInThePlane.z),
            robotToUnityY(MarsComm.epPosInThePlane.y),
            -2
        );
        epPos.transform.localPosition = ep;
        epPos.transform.localScale = new Vector3(0.02f * SCALEX, 0.02f * SCALEY, 1);
    }
    
    private void updateTargetDisplay()
    {
        // Display depending on the current state.
        AppLogger.LogInfo($"Updating target display for state {currentState}.");
        switch(currentState)
        {
            case ARMWEIGHT_ASSESS_STATE.INIT:
                leftTarget.SetActive(false);
                rightTarget.SetActive(false);
                topTarget.SetActive(false);
                bottomTarget.SetActive(false);
                centerTarget.SetActive(false);
                break;
            case ARMWEIGHT_ASSESS_STATE.WAIT_FOR_TARGET_SELECTION:
            case ARMWEIGHT_ASSESS_STATE.ALL_DONE:
                // Check which targets have been completed.
                updateIndividualTarget(leftTarget, armWeight.targetAssessmentStatus[(int)ArmWeight.ARMWEIGHT_TARGET.LEFT]);
                updateIndividualTarget(rightTarget, armWeight.targetAssessmentStatus[(int)ArmWeight.ARMWEIGHT_TARGET.RIGHT]);
                updateIndividualTarget(topTarget, armWeight.targetAssessmentStatus[(int)ArmWeight.ARMWEIGHT_TARGET.TOP]);
                updateIndividualTarget(bottomTarget, armWeight.targetAssessmentStatus[(int)ArmWeight.ARMWEIGHT_TARGET.BOTTOM]);
                updateIndividualTarget(centerTarget, armWeight.targetAssessmentStatus[(int)ArmWeight.ARMWEIGHT_TARGET.CENTER]);
                break;
            case ARMWEIGHT_ASSESS_STATE.MOVING_TO_TARGET:
                currentTargetObject.SetActive(true);
                currentTargetObject.transform.localScale = new Vector3(SCALEX * TARGET_SIZE, SCALEY * TARGET_SIZE, 1);
                break;
            case ARMWEIGHT_ASSESS_STATE.IN_TARGET:
                currentTargetObject.transform.localScale = new Vector3(TARGET_REACH_SCALE * SCALEX * TARGET_SIZE, TARGET_REACH_SCALE * SCALEY * TARGET_SIZE, 1);
                break;
        }
    }

    private void updateIndividualTarget(GameObject target, bool completed)
    {
        target.SetActive(completed);
        float _scale = completed ? TARGET_COMPLETE_SCALE : 1.0f;
        target.transform.localScale = new Vector3(_scale * SCALEX * TARGET_SIZE, _scale * SCALEY * TARGET_SIZE, 1);
        if (completed) target.GetComponent<SpriteRenderer>().color = Color.black;
    }
}
