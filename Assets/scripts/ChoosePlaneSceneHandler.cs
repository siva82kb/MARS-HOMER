using System.Text.RegularExpressions;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;


public class ChoosePlaneSceneHandler : MonoBehaviour
{
    //ui related variables
    public TMP_Text instructionText;
    public TMP_Text trainPlaneText;
    public TMP_Text ctrlBoundText;
    public Toggle tglChangeTrainPlane;
    public Toggle tglChangeCtrlBound;
    public Slider sliderTrainPlane;
    public Slider sliderCtrlBound;
    public readonly string robotCalibScene = "ROBOTCALIB";
    public readonly string nextScene = "CHOOSEMOVE";
    private bool attachMarsButtonEvent = true;
    // Training plane choosing state
    private enum ChooseTrainingPlaneStates
    {
        WAIT_FOR_HORIZONTAL_REACH,
        WAIT_FOR_LIMB_ATTACHMENT,
        TEST_TRAINING_PLANES,
        SAVE_TRAINING_PLANE,
    }
    private ChooseTrainingPlaneStates currentState = ChooseTrainingPlaneStates.WAIT_FOR_HORIZONTAL_REACH;
    private bool marsButtonReleased = false;
    private bool calibButtonReleased = false;

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
        tglChangeTrainPlane.interactable = false;
        tglChangeTrainPlane.GameObject().SetActive(false);
        sliderTrainPlane.interactable = false;
        sliderTrainPlane.GameObject().SetActive(false);
        sliderTrainPlane.value = 0;
        sliderTrainPlane.minValue = -100f;
        sliderTrainPlane.maxValue = 0f;
        // Hide control bound controls.
        tglChangeCtrlBound.interactable = false;
        tglChangeCtrlBound.GameObject().SetActive(false);
        ctrlBoundText.GameObject().SetActive(false);
        sliderCtrlBound.interactable = false;
        sliderCtrlBound.GameObject().SetActive(false);
    }

    public void onMarsButtonReleased()
    {
        // Send the calibration command.
        MarsComm.calibrate();
    }

    public void onCalibButtonReleased()
    {
        // Send the calibration command.
        MarsComm.calibrate();
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
