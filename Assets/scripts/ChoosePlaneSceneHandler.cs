using System.Text.RegularExpressions;
using TMPro;
using Unity.VisualScripting;

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
    public Button btnDone;
    private static string FLOAT_FORMAT = "+0.0;-0.0"; 
    private static string FLOAT_FORMAT_D = "0";
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
        { ChooseTrainingPlaneStates.WAIT_FOR_HORIZONTAL_REACH, ("Moving the robot to the horizontal position. Please wait…", new Color32(202, 108, 0, 255)) },
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
        if (!File.Exists(DataManager.configFile)) SceneManager.LoadSceneAsync("CONFIG");

        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

        // IF the robot is not calibrated go to the robot calib scene.
        if (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB")
        {
            SceneManager.LoadSceneAsync(robotCalibScene);
        }

        // Initialize UI
        InitUI();
        currentState = MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION"
            ? ChooseTrainingPlaneStates.WAIT_FOR_LIMB_ATTACHMENT
            : ChooseTrainingPlaneStates.WAIT_FOR_HORIZONTAL_REACH;
        AppLogger.LogInfo($"Starting Choose Training Plane state: {currentState}");
        
        // Reset flags
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
        }

        // Run the statemachine
        runStateMachine();
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
        // Attach call back for the done button.
        btnDone.onClick.AddListener(() => {
            if (currentState == ChooseTrainingPlaneStates.ALL_DONE) return;
            AppLogger.LogInfo($"Done button pressed. Leaving abruptly.");
            SceneManager.LoadSceneAsync(nextScene); 
        });
        //hide exit button for, first time to set plane
        btnDone.gameObject.SetActive(AppData.Instance.userData.trainingPlaneAngle != 0);
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
                            AppLogger.LogInfo($"Changing state to {currentState} | Target: {MarsComm.target} | Actual: {MarsComm.angle1}.");
                        }
                    }
                    else
                    {
                        MarsComm.setControlTarget(-90);
                    }
                }
                else
                {
                    // Set control to POSITION and set target to -90.
                    MarsComm.setControlType("POSITION");
                    MarsComm.setControlTarget(-90);
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
                    AppLogger.LogInfo($"Changing state to {currentState}.");
                }
                break;

            case ChooseTrainingPlaneStates.TEST_TRAINING_PLANES:
                // Update the current and set training angles.
                trainPlaneText.text = $"Set: {Mathf.Abs(MarsComm.desired).ToString(FLOAT_FORMAT_D)} deg | ";
                trainPlaneText.text += $"Actual: {Mathf.Abs(MarsComm.angle1).ToString(FLOAT_FORMAT_D)} deg";
                sliderValueText.text = $"{Mathf.Abs(sliderTrainPlane.value).ToString(FLOAT_FORMAT_D)} deg";
                // Check of new training plane angle has been set.
                if (newTrainingPlaneAngle)
                {
                    // New training plane angle is ready to be set. It will be set when the calib button is pressed.
                    instructionText.text = "Press Set Plane to set the new training plane angle.";
                    // Check if the calib buttons has been pressed.
                    if (setNewTrainingPlaneAngle)
                    {
                        MarsComm.setControlTarget(sliderTrainPlane.value);
                        AppLogger.LogInfo($"Setting target to {sliderTrainPlane.value}.");
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
                        // Disable the slider.
                        sliderTrainPlane.interactable = false;
                        AppLogger.LogInfo($"Changing state to {currentState}.");
                    }
                }
                break;

            case ChooseTrainingPlaneStates.ALL_DONE:
                if (marsButtonReleased)
                {
                    marsButtonReleased = false;
                    // Go to the next scene.
                    AppLogger.LogInfo($"Changing state to scene to {nextScene}.");
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
                        AppLogger.LogInfo($"Changing state to {currentState} | Redoing training plane angle.");
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
            if (MarsComm.force > 10)
            {
                marsButtonReleased = true;
            }
           
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
    public void onClickSetPlane()
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
        // Reload the training plane angle from the config file.
        AppData.Instance.userData.reloadTrainingPlaneAngle();
        // Detach MARS event listeners.
        MarsComm.OnMarsButtonReleased -= onMarsButtonReleased;
    }

    private void OnApplicationQuit()
    {
        Application.Quit();
        JediComm.Disconnect();
    }
}
