// using System.Text.RegularExpressions;
// using TMPro;
// using Unity.VisualScripting;
// using UnityEditor.VersionControl;
// using UnityEngine;
// using UnityEngine.SceneManagement;
// using UnityEngine.UI;
// using System.IO;
// using System.ComponentModel.Design.Serialization;
// using System.Collections.Generic;


// public class UserCalibrationSceneHandler : MonoBehaviour
// {
//     //ui related variables
//     public TMP_Text instructionText;
//     public TMP_Text uaLength;
//     public TMP_Text faLength;
//     public TMP_Text epPosition;
//     private readonly string prevScene = "ROBOTCALIB";
//     private readonly string nextScene = "CHOOSEMOVE";
//     private readonly string limbWeightSetScene = "LIMBWSET";
//     private bool attachMarsButtonEvent = true;
//     private static string FLOAT_FORMAT = "+0.000;-0.000";

//     // User set up states
//     private enum UserSetupState
//     {
//         IDLE,
//         MOVING_TO_HORIZONTAL_POSITION,
//         IN_HORIZONTAL_POSITION,
//         WAIT_FOR_CALIB_BUTTON_CLICK,
//         CHECKING_HUMAN_LIMB_KIN_PARAM,
//         HUMAN_LIMB_KIN_SET
//     }
//     private UserSetupState calibState = UserSetupState.IDLE;
//     // Instruction text and color dictionary.
//     private Dictionary<UserSetupState, (string, Color)> instructionDict = new Dictionary<UserSetupState, (string, Color)>()
//     {
//         { UserSetupState.IDLE, ("Press the MARS button to move the robot to the horizontal position.", new Color32(202, 108, 0, 255)) },
//         { UserSetupState.MOVING_TO_HORIZONTAL_POSITION, ("Moving the robot to the horizontal position. Please wait.", new Color32(202, 108, 0, 255)) },
//         { UserSetupState.IN_HORIZONTAL_POSITION, ("Position the robot near the user's shoulder and press the MARS Calibration Button.", new Color32(202, 108, 0, 255)) },
//         { UserSetupState.WAIT_FOR_CALIB_BUTTON_CLICK, ("Press the MARS Calibration Button to set human limb kinematic parameters.", new Color32(202, 108, 0, 255)) },
//         { UserSetupState.CHECKING_HUMAN_LIMB_KIN_PARAM, ("Checking set human limb Kinematic Parameters.", new Color32(202, 108, 0, 255)) },
//         { UserSetupState.HUMAN_LIMB_KIN_SET, ("User kinematic limb parameter setup is complete. Press the MARS button to continue.", new Color32(202, 108, 0, 255)) },
//     };
//     private bool marsBtnPressed = false;
//     // Variables to compute the moving average of shoulder position.
//     private const int N = 100;
//     private float[] shZPosArray = new float[N];
//     private bool shZPosArrayFull = false;
//     private int shZPosIndex = 0;
//     private bool hLimbParamCheckFlag = false;
//     private enum HumanLimbSetState
//     {
//         NONE,
//         GETTING_PARAM,
//         PARAM_SET_SUCCESS,
//         PARAM_SET_ERROR
//     }
//     private HumanLimbSetState hLimbParamSetState = HumanLimbSetState.NONE;
//     private float avgShZPos;

//     void Start()
//     {
//         MarsComm.sendHeartbeat();
//         // Initialize AppData if needed
//         // Initialize if needed
//         if (AppData.Instance.userData == null)
//         {
//             AppData.Instance.Initialize(SceneManager.GetActiveScene().name);
//         }

//         // Check if the directory exists
//         if (!Directory.Exists(DataManager.basePath)) Directory.CreateDirectory(DataManager.basePath);
//         if (!File.Exists(DataManager.configFile)) SceneManager.LoadScene("CONFIG");

//         AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
//         AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

//         // Check if the device is calibrated.
//         if (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB")
//         {
//             SceneManager.LoadScene(prevScene);
//         }

//         // Reset control.
//         MarsComm.setControlType("NONE");
//         // Reset human limb kinematic parameters.
//         MarsComm.resetHumanLimbKinParams();

//         // Initialize UI
//         InitUI();
//     }

//     void Update()
//     {
//         MarsComm.sendHeartbeat();

//         // Debug.Log(MarsComm.xEndpoint);
//         epPosition.text = $"{(100 * MarsComm.xEndpoint).ToString(FLOAT_FORMAT)}cm, {(100 * MarsComm.yEndpoint).ToString(FLOAT_FORMAT)}cm, {(100 * MarsComm.zEndpoint).ToString(FLOAT_FORMAT)}cm";

//         // Wait for a second before doing anything.
//         if (Time.timeSinceLevelLoad < 1) return;

//         // Attach MARS event listeners.
//         if (attachMarsButtonEvent)
//         {
//             attachMarsButtonEvent = false;
//             // Set limb
//             MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
//             MarsComm.OnCalibButtonReleased += onCalibButtonReleased;
//             MarsComm.onHumanLimbKinParamData += OnHumanLimbKinParamData;
//         }

//         // Run the state machine.
//         runUserSetupStateMachine();
//     }

//     public void InitUI()
//     {
//         instructionText.text = "";
//         uaLength.text = (100 * AppData.Instance.userData.uaLength).ToString() + "cm";
//         faLength.text = (100 * AppData.Instance.userData.faLength).ToString() + "cm";
//         epPosition.text = $"{(100 * MarsComm.xEndpoint).ToString(FLOAT_FORMAT)}cm, {(100 * MarsComm.yEndpoint).ToString(FLOAT_FORMAT)}cm, {(100 * MarsComm.zEndpoint).ToString(FLOAT_FORMAT)}cm";
//     }

//     public void runUserSetupStateMachine()
//     {
//         // Udpate the instruction.
//         instructionText.text = instructionDict[calibState].Item1;
//         instructionText.color = instructionDict[calibState].Item2;        
//         // Run the statemachine
//         switch (calibState)
//         {
//             case UserSetupState.IDLE:
//                 if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION")
//                 {
//                     // Check if the target is -90
//                     if (MarsComm.target == -90) calibState = UserSetupState.MOVING_TO_HORIZONTAL_POSITION;
//                     else MarsComm.setControlTarget(-90);
//                 }
//                 break;

//             case UserSetupState.MOVING_TO_HORIZONTAL_POSITION:
//                 // instructionText.text = "Moving the robot to the horizontal position. Please wait.";
//                 // instructionText.color = new Color32(202, 108, 0, 255);
//                 if (Mathf.Abs(MarsComm.angle1 + 90) < 10)
//                 {
//                     calibState = UserSetupState.IN_HORIZONTAL_POSITION;
//                     MarsComm.resetHumanLimbKinParams();
//                     // marsBtnPressed = false;
//                     hLimbParamSetState = HumanLimbSetState.NONE;
//                     shZPosIndex = 0;
//                     shZPosArrayFull = false;
//                 }
//                 break;

//             case UserSetupState.IN_HORIZONTAL_POSITION:
//                 // Update the shoulder position array.
//                 shZPosArray[shZPosIndex] = MarsComm.zEndpoint;
//                 shZPosIndex = (shZPosIndex + 1) % N;
//                 if (!shZPosArrayFull && shZPosIndex == 0) shZPosArrayFull = true;

//                 // Check if the human limb parameters have been set.
//                 if (hLimbParamSetState == HumanLimbSetState.NONE)
//                 {
//                     // Check the human limb kinematic parameters.
//                     if (MarsComm.limbKinParam == 0x00) hLimbParamSetState = HumanLimbSetState.NONE;
//                     else
//                     {
//                         MarsComm.getHumanLimbKinParams();
//                         calibState = UserSetupState.CHECKING_HUMAN_LIMB_KIN_PARAM;
//                         hLimbParamSetState = HumanLimbSetState.GETTING_PARAM;
//                     }
//                 }
//                 break;

//             case UserSetupState.CHECKING_HUMAN_LIMB_KIN_PARAM:
//                 // instructionText.text = "Checking set human limb Kinematic Parameters.";
//                 // instructionText.color = new Color32(202, 108, 0, 255);
//                 // Check if the human limb parameters have been set.
//                 switch (hLimbParamSetState)
//                 {
//                     case HumanLimbSetState.GETTING_PARAM:
//                         // Check the human limb kinematic parameters.
//                         if (MarsComm.limbKinParam == 0x01) MarsComm.getHumanLimbKinParams();
//                         else
//                         {
//                             // Go back to IN_HORIZONTAL_POSITION
//                             calibState = UserSetupState.IN_HORIZONTAL_POSITION;
//                             MarsComm.resetHumanLimbKinParams();
//                             hLimbParamSetState = HumanLimbSetState.NONE;
//                             shZPosIndex = 0;
//                             shZPosArrayFull = false;
//                         }
//                         break;

//                     case HumanLimbSetState.PARAM_SET_SUCCESS:
//                         // Handle successful parameter setting.
//                         calibState = UserSetupState.HUMAN_LIMB_KIN_SET;
//                         marsBtnPressed = false;
//                         break;

//                     case HumanLimbSetState.PARAM_SET_ERROR:
//                         // Handle parameter setting error.
//                         // Go back to IN_HORIZONTAL_POSITION
//                         calibState = UserSetupState.IN_HORIZONTAL_POSITION;
//                         MarsComm.resetHumanLimbKinParams();
//                         hLimbParamSetState = HumanLimbSetState.NONE;
//                         shZPosIndex = 0;
//                         shZPosArrayFull = false;
//                         break;
//                 }
//                 break;

//             case UserSetupState.HUMAN_LIMB_KIN_SET:
//                 // Change scene if the MARS button is pressed.
//                 if (marsBtnPressed) SceneManager.LoadScene(limbWeightSetScene);
//                 break;
//         }
//     }

//     public void onMarsButtonReleased()
//     {
//         // Act depending on the calibration state.
//         if (calibState == UserSetupState.IDLE)
//         {
//             // Set control to POSITION and set target to -90.
//             MarsComm.setControlType("POSITION");
//             MarsComm.setControlTarget(-90);
//         }
//         else if (calibState == UserSetupState.HUMAN_LIMB_KIN_SET)
//         {
//             marsBtnPressed = true;
//         }
//     }

//     public void onCalibButtonReleased()
//     {
//         // Act depending on the calibration state.
//         Debug.Log($"{calibState}, {shZPosArrayFull}");
//         if ((calibState == UserSetupState.IN_HORIZONTAL_POSITION) && shZPosArrayFull)
//         {
//             avgShZPos = 0;
//             for (int i = 0; i < shZPosArray.Length; i++)
//             {
//                 avgShZPos += shZPosArray[i];
//             }
//             avgShZPos /= shZPosArray.Length;
//             // Set the human limb kinematic parameters.
//             MarsComm.setHumanLimbKinParams(AppData.Instance.userData.uaLength, AppData.Instance.userData.faLength, avgShZPos);
//             hLimbParamCheckFlag = true;
//         }
//     }

//     private void OnHumanLimbKinParamData()
//     {
//         // Check the param set state first.
//         if (hLimbParamSetState != HumanLimbSetState.GETTING_PARAM) return;
//         // Check if the human limb kinematics parameters match the set values.
//         Debug.Log($"Limb Kin Param: {MarsComm.limbKinParam}, UA Length: [{MarsComm.uaLength}, {AppData.Instance.userData.uaLength}], FA Length: [{MarsComm.faLength}, {AppData.Instance.userData.faLength}], SH Z Pos: [{MarsComm.shPosZ}, {avgShZPos}]");
//         if (MarsComm.limbKinParam != 0x01 || MarsComm.uaLength != AppData.Instance.userData.uaLength || MarsComm.faLength != AppData.Instance.userData.faLength || MarsComm.shPosZ != avgShZPos)
//         {
//             Debug.LogWarning("Human limb kinematic parameters are not set correctly.");
//             hLimbParamSetState = HumanLimbSetState.PARAM_SET_ERROR;
//             MarsComm.resetHumanLimbKinParams();
//         }
//         else
//         {
//             hLimbParamSetState = HumanLimbSetState.PARAM_SET_SUCCESS;
//         }
//     }

//     private void OnDestroy()
//     {
//         MarsComm.OnMarsButtonReleased -= onMarsButtonReleased;
//         MarsComm.OnCalibButtonReleased -= onCalibButtonReleased;
//         MarsComm.onHumanLimbKinParamData -= OnHumanLimbKinParamData;
//     }
//     private void OnApplicationQuit()
//     {

//         Application.Quit();
//         JediComm.Disconnect();
//     }
// }
