using System.Text.RegularExpressions;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.ComponentModel.Design.Serialization;
using System.Collections.Generic;


public class LimbWeightSetSceneHandler : MonoBehaviour
{
    //ui related variables
    public TMP_Text instructionText;
    public TMP_Text status;
    public TMP_Text limbDetails;
    private readonly string calibScene = "ROBOTCALIB";
    private readonly string userSetupScene = "USERSETUP";
    private readonly string nextScene = "CHOOSEMOVE";
    private readonly string limbWeightEstScene = "LIMBWEST";
    private bool attachMarsButtonEvent = true;
    private static string FLOAT_FORMAT = "+0.000;-0.000";

    // User set up states
    private enum UserSetupState
    {
        IDLE,
        WAIT_FOR_LIMB_ATTACHMENT,
        USER_LIMB_ATTACHED,
        WAIT_FOR_CONTROL_SWITCH_TEST,
        IMPROPER_WEIGHT_SUPPORT,
        ALL_SYSTEMS_GO,
    }
    private UserSetupState calibState = UserSetupState.IDLE;
    // Instruction text and color dictionary.
    // private Dictionary<UserSetupState, (string, Color)> instructionDict = new Dictionary<UserSetupState, (string, Color)>()
    // {
    //     { UserSetupState.IDLE, ("Press the MARS button to move the robot to the horizontal position.", new Color32(202, 108, 0, 255)) },
    //     { UserSetupState.MOVING_TO_HORIZONTAL_POSITION, ("Moving the robot to the horizontal position. Please wait.", new Color32(202, 108, 0, 255)) },
    //     { UserSetupState.IN_HORIZONTAL_POSITION, ("Position the robot near the user's shoulder and press the MARS Calibration Button.", new Color32(202, 108, 0, 255)) },
    //     { UserSetupState.WAIT_FOR_CALIB_BUTTON_CLICK, ("Press the MARS Calibration Button to set human limb kinematic parameters.", new Color32(202, 108, 0, 255)) },
    //     { UserSetupState.CHECKING_HUMAN_LIMB_KIN_PARAM, ("Checking set human limb Kinematic Parameters.", new Color32(202, 108, 0, 255)) },
    //     { UserSetupState.HUMAN_LIMB_KIN_SET, ("User kinematic limb parameter setup is complete. Press the MARS button to continue.", new Color32(202, 108, 0, 255)) },
    // };
    private bool marsBtnPressed = false;
    private bool calibBtnPressed = false;
    private bool hLimbParamCheckFlag = false;
    private enum HumanLimbSetState
    {
        NONE,
        GETTING_PARAM,
        PARAM_SET_SUCCESS,
        PARAM_SET_ERROR
    }
    private HumanLimbSetState hLimbParamSetState = HumanLimbSetState.NONE;
    // Variables to keep track of time and the number of times limb weights are set.
    private float startTimeFlag = 0;
    private int limbWeightSetCount = 0;
    // Time thresholds for setting limb weight parameter.
    private const float LIMB_WEIGHT_PARAM_SET_TIME_THRESHOLD = 0.5f;
    private const int LIMB_WEIGHT_PARAM_SET_COUNT_THRESHOLD = 5;

    void Start()
    {
        MarsComm.sendHeartbeat();

        // Initialize AppData if needed
        // Initialize if needed
        if (AppData.Instance.userData == null)
        {
            AppData.Instance.Initialize(SceneManager.GetActiveScene().name);
        }

        // Check if the directory exists
        if (!Directory.Exists(DataManager.basePath)) Directory.CreateDirectory(DataManager.basePath);
        if (!File.Exists(DataManager.configFile)) SceneManager.LoadScene("CONFIG");

        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

        // Check if the device is calibrated.
        if (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB")
        {
            SceneManager.LoadScene(calibScene);
        }
        if (MarsComm.limbKinParam == 0x00)
        {
            SceneManager.LoadScene(userSetupScene);
        }
        if (!AppData.Instance.userData.isLimbWeightAvailable)
        {
            SceneManager.LoadScene(limbWeightEstScene);
        }

        // Reset human limb dynamic parameters.
        MarsComm.resetHumanLimbDynParams();

        // Initialize UI
        InitUI();
    }

    void Update()
    {
        MarsComm.sendHeartbeat();

        // // Debug.Log(MarsComm.xEndpoint);
        // epPosition.text = $"{(100 * MarsComm.xEndpoint).ToString(FLOAT_FORMAT)}cm, {(100 * MarsComm.yEndpoint).ToString(FLOAT_FORMAT)}cm, {(100 * MarsComm.zEndpoint).ToString(FLOAT_FORMAT)}cm";

        // // Wait for a second before doing anything.
        // if (Time.timeSinceLevelLoad < 1) return;

        // // Attach MARS event listeners.
        // if (attachMarsButtonEvent)
        // {
        //     attachMarsButtonEvent = false;
        //     // Set limb
        //     MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
        //     MarsComm.OnCalibButtonReleased += onCalibButtonReleased;
        //     MarsComm.onHumanLimbKinParamData += OnHumanLimbKinParamData;
        // }

        // // Run the state machine.
        // runUserSetupStateMachine();
    }

    public void InitUI()
    {
        instructionText.text = "";
        // uaLength.text = (100 * AppData.Instance.userData.uaLength).ToString() + "cm";
        // faLength.text = (100 * AppData.Instance.userData.faLength).ToString() + "cm";
        // epPosition.text = $"{(100 * MarsComm.xEndpoint).ToString(FLOAT_FORMAT)}cm, {(100 * MarsComm.yEndpoint).ToString(FLOAT_FORMAT)}cm, {(100 * MarsComm.zEndpoint).ToString(FLOAT_FORMAT)}cm";
    }

    public void runUserSetupStateMachine()
    {
        // // Udpate the instruction.
        // instructionText.text = instructionDict[UserSetupState.IDLE].Item1;
        // instructionText.color = instructionDict[UserSetupState.IDLE].Item2;        
        // // Run the statemachine
        // switch (calibState)
        // {
        //     case UserSetupState.IDLE:
        //         if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION")
        //         {
        //             // Check if the target is -90
        //             if (MarsComm.target == -90) calibState = UserSetupState.MOVING_TO_HORIZONTAL_POSITION;
        //             else MarsComm.setControlTarget(-90);
        //         }
        //         break;

        //     case UserSetupState.MOVING_TO_HORIZONTAL_POSITION:
        //         // instructionText.text = "Moving the robot to the horizontal position. Please wait.";
        //         // instructionText.color = new Color32(202, 108, 0, 255);
        //         if (Mathf.Abs(MarsComm.angle1 + 90) < 10)
        //         {
        //             calibState = UserSetupState.IN_HORIZONTAL_POSITION;
        //             MarsComm.resetHumanLimbKinParams();
        //             // calibBtnPressed = false;
        //             hLimbParamSetState = HumanLimbSetState.NONE;
        //             shZPosIndex = 0;
        //             shZPosArrayFull = false;
        //         }
        //         break;

        //     case UserSetupState.IN_HORIZONTAL_POSITION:
        //         // Update the shoulder position array.
        //         shZPosArray[shZPosIndex] = MarsComm.zEndpoint;
        //         shZPosIndex = (shZPosIndex + 1) % N;
        //         if (!shZPosArrayFull && shZPosIndex == 0) shZPosArrayFull = true;

        //         // Check if the human limb parameters have been set.
        //         if (hLimbParamSetState == HumanLimbSetState.NONE)
        //         {
        //             // Check the human limb kinematic parameters.
        //             if (MarsComm.limbKinParam == 0x00) hLimbParamSetState = HumanLimbSetState.NONE;
        //             else
        //             {
        //                 MarsComm.getHumanLimbKinParams();
        //                 calibState = UserSetupState.CHECKING_HUMAN_LIMB_KIN_PARAM;
        //                 hLimbParamSetState = HumanLimbSetState.GETTING_PARAM;
        //             }
        //         }
        //         break;

        //     case UserSetupState.CHECKING_HUMAN_LIMB_KIN_PARAM:
        //         // instructionText.text = "Checking set human limb Kinematic Parameters.";
        //         // instructionText.color = new Color32(202, 108, 0, 255);
        //         // Check if the human limb parameters have been set.
        //         switch (hLimbParamSetState)
        //         {
        //             case HumanLimbSetState.GETTING_PARAM:
        //                 // Check the human limb kinematic parameters.
        //                 if (MarsComm.limbKinParam == 0x01) MarsComm.getHumanLimbKinParams();
        //                 else
        //                 {
        //                     // Go back to IN_HORIZONTAL_POSITION
        //                     calibState = UserSetupState.IN_HORIZONTAL_POSITION;
        //                     MarsComm.resetHumanLimbKinParams();
        //                     hLimbParamSetState = HumanLimbSetState.NONE;
        //                     shZPosIndex = 0;
        //                     shZPosArrayFull = false;
        //                 }
        //                 break;

        //             case HumanLimbSetState.PARAM_SET_SUCCESS:
        //                 // Handle successful parameter setting.
        //                 calibState = UserSetupState.HUMAN_LIMB_KIN_SET;
        //                 break;

        //             case HumanLimbSetState.PARAM_SET_ERROR:
        //                 // Handle parameter setting error.
        //                 // Go back to IN_HORIZONTAL_POSITION
        //                 calibState = UserSetupState.IN_HORIZONTAL_POSITION;
        //                 MarsComm.resetHumanLimbKinParams();
        //                 hLimbParamSetState = HumanLimbSetState.NONE;
        //                 shZPosIndex = 0;
        //                 shZPosArrayFull = false;
        //                 break;
        //         }
        //         break;

        //     case UserSetupState.HUMAN_LIMB_KIN_SET:
        //         break;
        // }
    }

    public void onMarsButtonReleased()
    {
        // Act depending on the calibration state.
        if (calibState == UserSetupState.IDLE)
        {
            // Set control to POSITION and set target to -90.
            MarsComm.setControlType("POSITION");
            MarsComm.setControlTarget(-90);
        }
    }

    public void onCalibButtonReleased()
    {
        // Act depending on the calibration state.
        // Debug.Log($"{calibState}, {shZPosArrayFull}");
        // if ((calibState == UserSetupState.IN_HORIZONTAL_POSITION) && shZPosArrayFull)
        // {
        //     avgShZPos = 0;
        //     for (int i = 0; i < shZPosArray.Length; i++)
        //     {
        //         avgShZPos += shZPosArray[i];
        //     }
        //     avgShZPos /= shZPosArray.Length;
        //     // Set the human limb kinematic parameters.
        //     MarsComm.setHumanLimbKinParams(AppData.Instance.userData.uaLength, AppData.Instance.userData.faLength, avgShZPos);
        //     hLimbParamCheckFlag = true;
        // }
        // else if (calibState == UserSetupState.HUMAN_LIMB_KIN_SET)
        // {
        //     // Change scene to weight set scene.
        //     // SceneManager.LoadScene(limbWeightSetScene);
        // }
    }

    private void OnHumanLimbKinParamData()
    {
        // Check the param set state first.
        if (hLimbParamSetState != HumanLimbSetState.GETTING_PARAM) return;
        // Check if the human limb kinematics parameters match the set values.
        // Debug.Log($"Limb Kin Param: {MarsComm.limbKinParam}, UA Length: [{MarsComm.uaLength}, {AppData.Instance.userData.uaLength}], FA Length: [{MarsComm.faLength}, {AppData.Instance.userData.faLength}], SH Z Pos: [{MarsComm.shPosZ}, {avgShZPos}]");
        // if (MarsComm.limbKinParam != 0x01 || MarsComm.uaLength != AppData.Instance.userData.uaLength || MarsComm.faLength != AppData.Instance.userData.faLength || MarsComm.shPosZ != avgShZPos)
        // {
        //     Debug.LogWarning("Human limb kinematic parameters are not set correctly.");
        //     hLimbParamSetState = HumanLimbSetState.PARAM_SET_ERROR;
        //     MarsComm.resetHumanLimbKinParams();
        // }
        // else
        // {
        //     hLimbParamSetState = HumanLimbSetState.PARAM_SET_SUCCESS;
        // }
    }

    private void OnDestroy()
    {
        MarsComm.OnMarsButtonReleased -= onMarsButtonReleased;
        MarsComm.OnCalibButtonReleased -= onCalibButtonReleased;
        MarsComm.onHumanLimbKinParamData -= OnHumanLimbKinParamData;
    }
    private void OnApplicationQuit()
    {

        Application.Quit();
        JediComm.Disconnect();
    }
}
