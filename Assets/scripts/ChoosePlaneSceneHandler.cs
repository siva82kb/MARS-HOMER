using System.Text.RegularExpressions;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;


public class ChoosePlaneSceneHandler : MonoBehaviour
{
    //ui related variables
    public TMP_Text instructionText;
    public TMP_Text trainPlaneText;
    public TMP_Text sliderValueText;
    public TMP_Text ctrlBoundText;
    public Toggle tglChangeTrainPlane;
    public Toggle tglChangeCtrlBound;
    public Slider sliderTrainPlane;
    public Slider sliderCtrlBound;
    private static string FLOAT_FORMAT = "+0.0;-0.0";

    public readonly string robotCalibScene = "ROBOTCALIB";
    public readonly string nextScene = "CHOOSEMOVE";
    private bool attachMarsButtonEvent = true;
    // Training plane choosing state
    private enum ChooseTrainingPlaneStates
    {
        WAIT_FOR_HORIZONTAL_REACH,
        WAIT_FOR_LIMB_ATTACHMENT,
        TEST_TRAINING_PLANES,
        ALL_DONE,
    }
    private ChooseTrainingPlaneStates currentState = ChooseTrainingPlaneStates.WAIT_FOR_HORIZONTAL_REACH;
    private Dictionary<ChooseTrainingPlaneStates, (string, Color)> instructionDict = new Dictionary<ChooseTrainingPlaneStates, (string, Color)>()
    {
        { ChooseTrainingPlaneStates.WAIT_FOR_HORIZONTAL_REACH, ("Press the MARS button to move the robot to the horizontal position.", new Color32(202, 108, 0, 255)) },
        { ChooseTrainingPlaneStates.WAIT_FOR_LIMB_ATTACHMENT, ("Attach the user's limb and press the MARS button.", new Color32(202, 108, 0, 255)) },
        { ChooseTrainingPlaneStates.TEST_TRAINING_PLANES, ("Choose the training plane angle and press the Calib button.", new Color32(202, 108, 0, 255)) },
        { ChooseTrainingPlaneStates.ALL_DONE, ("All done. Press the MARS button to start training.", new Color32(202, 108, 0, 255)) },
    };
    private bool marsButtonReleased = false;
    private bool calibButtonReleased = false;
    private bool newTrainingPlaneAngle = false;
    private bool setNewTrainingPlaneAngle = false;

    void Start()
    {
        MarsComm.sendHeartbeat();

        // Initialize AppData if needed
        if (AppData.Instance.userData == null)
        {
            AppData.Instance.Initialize(SceneManager.GetActiveScene().name);
        }

        // Check if the directory exists
        if (!Directory.Exists(DataManager.basePath)) Directory.CreateDirectory(DataManager.basePath);
        if (!File.Exists(DataManager.configFile)) SceneManager.LoadScene("CONFIG");

        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

        // IF the robot is not calibrated go to the robot calib scene.
        if (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB")
        {
            SceneManager.LoadScene(robotCalibScene);
        }

        // Initialize UI
        InitUI();

        // Initialize state
        currentState = ChooseTrainingPlaneStates.WAIT_FOR_HORIZONTAL_REACH;
        marsButtonReleased = false;
        calibButtonReleased = false;
    }

    void Update()
    {
        MarsComm.sendHeartbeat();

        // Wait for a second before doing anything.
        if (Time.timeSinceLevelLoad < 0.25) return;

        // Attach MARS event listeners.
        if (attachMarsButtonEvent)
        {
            attachMarsButtonEvent = false;
            MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
            MarsComm.OnCalibButtonReleased += onCalibButtonReleased;
        }

        // Run the statemachine
        runStateMachine();

        // // Update status text.
        // string _status = $"User Limb: {_limb} | {MarsComm.CALIBRATION[MarsComm.calibration]}\n{MarsComm.imu1Angle}deg, {MarsComm.imu2Angle}deg, {MarsComm.imu3Angle}deg, {MarsComm.imu4Angle}deg";
        // // statusText.text = _status;

        // // Check if scene is to be changed.
        // if (MarsComm.CALIBRATION[MarsComm.calibration] == "YESCALIB")
        // {
        //     instructionText.text = "MARS calibration successful.";
        //     SceneManager.LoadScene(nextScene);
        // }
        // else
        // {
        //     // Check if all angles are within 20deg.
        //     if (Mathf.Abs(MarsComm.imu1Angle) > 20 || Mathf.Abs(MarsComm.imu2Angle) > 20 || Mathf.Abs(MarsComm.imu3Angle) > 20 || Mathf.Abs(MarsComm.imu4Angle) > 20)
        //     {
        //         instructionText.text = "Make sure all angles are within 20 degrees.";
        //         instructionText.color = new Color32(202, 0, 0, 255);
        //     }
        //     else
        //     {
        //         instructionText.text = "Press the MARS Button when ready.";
        //         instructionText.color = new Color32(202, 108, 0, 255);
        //     }
        // }
    }

    private void InitUI()
    {
        instructionText.text = "";
        trainPlaneText.text = "";
        sliderValueText.text = "";
        tglChangeTrainPlane.interactable = false;
        tglChangeTrainPlane.GameObject().SetActive(false);
        sliderTrainPlane.interactable = false;
        sliderTrainPlane.GameObject().SetActive(false);
        sliderTrainPlane.value = 0;
        sliderTrainPlane.minValue = -100f;
        sliderTrainPlane.maxValue = 0f;
        // Attach some UI callbacks.
        sliderTrainPlane.onValueChanged.AddListener(delegate { OnTrainPlaneSliderValueChanged(); });

        // Hide control bound controls.
        tglChangeCtrlBound.interactable = false;
        tglChangeCtrlBound.GameObject().SetActive(false);
        ctrlBoundText.GameObject().SetActive(false);
        sliderCtrlBound.interactable = false;
        sliderCtrlBound.GameObject().SetActive(false);
    }

    private void runStateMachine()
    {
        // Update instruction.
        (string, Color) instr = instructionDict[currentState];
        instructionText.text = instr.Item1;
        instructionText.color = instr.Item2;
        // Act according to the state.
        switch (currentState)
        {
            case ChooseTrainingPlaneStates.WAIT_FOR_HORIZONTAL_REACH:
                // Check if position control is set.
                if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION")
                {
                    // Check if the target is -90
                    if (MarsComm.target == -90)
                    {
                        // Check if the target has been reached.
                        if (Mathf.Abs(MarsComm.angle1 - MarsComm.target) < 10)
                        {
                            currentState = ChooseTrainingPlaneStates.WAIT_FOR_LIMB_ATTACHMENT;
                            marsButtonReleased = false;
                            calibButtonReleased = false;
                        }
                    }
                    else
                    {
                        MarsComm.setControlTarget(-90);
                    }
                }
                break;

            case ChooseTrainingPlaneStates.WAIT_FOR_LIMB_ATTACHMENT:
                // Check if the MARS button has been pressed.
                if (marsButtonReleased)
                {
                    currentState = ChooseTrainingPlaneStates.TEST_TRAINING_PLANES;
                    marsButtonReleased = false;
                    // Enable controls.
                    sliderTrainPlane.GameObject().SetActive(true);
                    sliderTrainPlane.value = MarsComm.target;
                    sliderTrainPlane.interactable = true;
                    newTrainingPlaneAngle = false;
                    setNewTrainingPlaneAngle = false;
                    calibButtonReleased = false;
                }
                break;

            case ChooseTrainingPlaneStates.TEST_TRAINING_PLANES:
                // Update the current and set training angles.
                trainPlaneText.text = $"Set: {MarsComm.desired.ToString(FLOAT_FORMAT)} deg | ";
                trainPlaneText.text += $"Actual: {MarsComm.angle1.ToString(FLOAT_FORMAT)} deg";
                sliderValueText.text = $"{sliderTrainPlane.value.ToString(FLOAT_FORMAT)} deg";
                // Check of new training plane angle has been set.
                if (newTrainingPlaneAngle)
                {
                    // New training plane angle is ready to be set. It will be set when the calib button is pressed.
                    instructionText.text = "Press Calib button to set the new training plane angle.";
                    // Check if the calib buttons has been pressed.
                    if (setNewTrainingPlaneAngle)
                    {
                        MarsComm.setControlTarget(sliderTrainPlane.value);
                        // Reset the new training plane angle flag if the target on MARS is same as the slider value.
                        setNewTrainingPlaneAngle = !(MarsComm.target == sliderTrainPlane.value);
                    }
                    // Disable the slider till the robot is still changing to the new target angle.
                    sliderTrainPlane.interactable = MarsComm.target == MarsComm.desired;
                    // Check if the target has been reached.
                    newTrainingPlaneAngle = !(MarsComm.desired == sliderTrainPlane.value);
                }
                else
                {
                    instructionText.text = "Press MARS button to save the training plane angle.";
                    if (marsButtonReleased)
                    {
                        // Save the training plane angle in the config file.
                        AppData.Instance.userData.writeUpdateTrainingPlaneData(MarsComm.target);
                        marsButtonReleased = false;
                        currentState = ChooseTrainingPlaneStates.ALL_DONE;
                    }
                }
                break;

            case ChooseTrainingPlaneStates.ALL_DONE:
                if (marsButtonReleased)
                {
                    marsButtonReleased = false;
                    // Go to the next scene.
                    SceneManager.LoadScene(nextScene);
                }
                else
                {
                    // Check if Control + R is pressed to redo the training plane angle.
                    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
                    {
                        currentState = ChooseTrainingPlaneStates.TEST_TRAINING_PLANES;
                        // Enable controls.
                        sliderTrainPlane.GameObject().SetActive(true);
                        sliderTrainPlane.value = MarsComm.target;
                        sliderTrainPlane.interactable = true;
                        newTrainingPlaneAngle = false;
                        setNewTrainingPlaneAngle = false;
                        calibButtonReleased = false;
                    }
                }
                break;
        }
    }

    public void onMarsButtonReleased()
    {
        // Act depending on the calibration state.
        if (currentState == ChooseTrainingPlaneStates.WAIT_FOR_HORIZONTAL_REACH)
        {
            // Set control to POSITION and set target to -90.
            MarsComm.setControlType("POSITION");
            MarsComm.setControlTarget(-90);
        }
        else if (currentState == ChooseTrainingPlaneStates.WAIT_FOR_LIMB_ATTACHMENT)
        {
            marsButtonReleased = true;
        }
        else if (currentState == ChooseTrainingPlaneStates.TEST_TRAINING_PLANES)
        {
            marsButtonReleased = !newTrainingPlaneAngle;
        }
        else if (currentState == ChooseTrainingPlaneStates.ALL_DONE)
        {
            marsButtonReleased = true;
        }
    }

    public void onCalibButtonReleased()
    {
        // Check if new training plane angle is to be set.
        if ((currentState == ChooseTrainingPlaneStates.TEST_TRAINING_PLANES) && newTrainingPlaneAngle)
        {
            // Send the training plane angle.
            setNewTrainingPlaneAngle = true;
        }
    }

    private void OnTrainPlaneSliderValueChanged()
    {
        // Check the state and act accordingly.
        newTrainingPlaneAngle = false;
        if (currentState != ChooseTrainingPlaneStates.TEST_TRAINING_PLANES) return;
        newTrainingPlaneAngle = (sliderTrainPlane.value != MarsComm.desired);
        setNewTrainingPlaneAngle = false;
    }

    private void OnDestroy()
    {
        MarsComm.OnMarsButtonReleased -= onMarsButtonReleased;
        MarsComm.OnCalibButtonReleased -= onCalibButtonReleased;
    }

    private void OnApplicationQuit()
    {

        Application.Quit();
        JediComm.Disconnect();
    }
}
