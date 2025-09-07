using System.Text.RegularExpressions;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class calibrationSceneHandler : MonoBehaviour
{
    //ui related variables
    public TMP_Text messageBox;
    public readonly string nextScene = "CHOOSEMOVEMENT";
    public GameObject panel;
   
  
    public Image calibTick;
    public Text calibTxt;
    public Text kinParaTxt;
    public Image kinTick;
    public Text messageTxt;
    public GameObject CalibSetupImg;
    public GameObject kinSetupImg;
    public TMP_InputField faLength;
    public TMP_InputField uaLength;
    public Button setLenght;
  
    private const int MAX_ENDPOINTS = 10;
    private int endpointCount = 0;
    private Vector3[] endpointPositions = new Vector3[100];

    private float setHLimbKinUALength = 0.0f;
    private float setHLimbKinFALength = 0.0f;
    private float setHLimbKinShPosZ = 0.0f;
    private enum CALIBSTATES
    {
        WAITFORSTART = 0x00,
        START = 0x01,
        SETUPLIMB = 0x02,
        CALIBRATE= 0x03,
        SETPOSITION = 0X04,
        SETLIMBKINPARA = 0x05,
        ALLDONE = 0x06,
        CHANGESCENE = 0x07
        
    }
    private CALIBSTATES calibState ;
    void Start()
    {
        messageTxt.text = "Press the MARS button to start calibration. Before proceeding, ensure the MARS device orientation matches the image shown";
        
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
        MarsComm.OnCalibButtonReleased += onCalibButtonReleased;
        // MarsComm.onHumanLimbKinParamData += OnHumanLimbKinParamData;
        calibState = CALIBSTATES.SETUPLIMB;
        initUI()
;    }

    void Update()
    {
        //To open Weight Estimation Scene
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.A))
        {
             if(calibState == CALIBSTATES.ALLDONE)
                SceneManager.LoadScene("WEIGHTEST");
        }

        //change length of Arm
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L))
        {
            setLenght.gameObject.SetActive(!setLenght.IsActive());
        }

        MarsComm.sendHeartbeat();
        RunCalibStateMachine();
       
    }
   
    public void onMarsButtonReleased()
    {
        switch (calibState)
        {
            
            case CALIBSTATES.SETUPLIMB:
                calibState = CALIBSTATES.CALIBRATE;
                break;
            case CALIBSTATES.CALIBRATE:
                
                break;
            case CALIBSTATES.ALLDONE:
                calibState = CALIBSTATES.CHANGESCENE;
                //change the scene
                break;
        }
    }
    

    private void onCalibButtonReleased()
    {
        if (calibState == CALIBSTATES.SETLIMBKINPARA)
        {
            // Get the current endpoint position from MARS robot.
            // Compute the average position of the endpoints.
            Vector3 averagePosition = Vector3.zero;
            for (int i = 0; i < MAX_ENDPOINTS; i++)
            {
                averagePosition += endpointPositions[i];
            }
            averagePosition /= MAX_ENDPOINTS;
            // Set the human limb kinematic parameters based on the average position.
            setHLimbKinUALength = (float)AppData.Instance.userData.uaLength;
            setHLimbKinFALength = (float)AppData.Instance.userData.faLength;
            setHLimbKinShPosZ = averagePosition.z;
            Debug.Log($"Setting human limb kinematic parameters: UALength={setHLimbKinUALength}, FALength={setHLimbKinFALength}, Average Position Z={setHLimbKinShPosZ}");
            // MarsComm.setHumanLimbKinParams(setHLimbKinUALength, setHLimbKinFALength, setHLimbKinShPosZ);
            // Get the human limb kinematic parameters from MARS.
            // MarsComm.getHumanLimbKinParams();
           
        }
    }
    
    private void RunCalibStateMachine()
    {
        if (calibState == CALIBSTATES.WAITFORSTART) return;
        updataUI();
        switch (calibState)
        {
            case CALIBSTATES.SETUPLIMB:
                if (MarsComm.LIMBTYPE[MarsComm.limb] == "NOLIMB")
                {
                    MarsComm.setLimb(MarsComm.LIMBTYPE[AppData.Instance.userData.rightArm ? 1 : 2]);
                    return;
                }
                break;
            case CALIBSTATES.CALIBRATE:
                if (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB")
                {
                    if (MarsComm.COMMAND_STATUS[MarsComm.recentCommandStatus] == "FAIL")
                    {
                        messageTxt.text = "Check that the MARS device looks exactly like the image (straight and properly aligned)";
                    }
                    MarsComm.calibrate();
                    return;
                }
                calibState = CALIBSTATES.SETLIMBKINPARA;
                break;
            case CALIBSTATES.SETLIMBKINPARA:
                if (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB")
                    calibState = CALIBSTATES.CALIBRATE;
                Debug.Log(MarsKinDynamics.ForwardKinematicsExtended(MarsComm.angle1, MarsComm.angle2, MarsComm.angle3, MarsComm.angle4));
                messageTxt.text = "Perform the procedure as illustrated, then press the calibration button in the MARS device.";
                if (endpointCount < MAX_ENDPOINTS && MarsComm.calibButton == 0)
                {
                    endpointPositions[endpointCount] = MarsKinDynamics.ForwardKinematicsExtended(MarsComm.angle1, MarsComm.angle2, MarsComm.angle3, MarsComm.angle4);
                    endpointCount++;
                }
                else
                {
                    endpointCount = 0;
                }
                break;
            case CALIBSTATES.ALLDONE:
                messageTxt.text = "You can redo the parameter setup, or press the MARS button to move to the next step.";
                kinTick.enabled = true;
                break;
            case CALIBSTATES.CHANGESCENE:

                //check need to calibrater or not
                // if (AppData.Instance.transitionControl.isDynLimbParamExist)
                // {
                //     SceneManager.LoadScene("CHOOSEMOVEMENT");
                // }
                // else
                // {
                //     SceneManager.LoadScene("WEIGHTEST");

                // }

                break;
        }

    }
    private void initUI()
    {
      
        calibTick.enabled = false;
        kinTick.enabled = false;
        CalibSetupImg.gameObject.SetActive(false);
        kinSetupImg.gameObject.SetActive(false);
        panel.gameObject.SetActive(false);
        MarsComm.setLimb("NOLIMB");
        setLenght.gameObject.SetActive(false);

        //change Lenght of Arm Dynamically
        setLenght.onClick.AddListener(delegate
        {
            AppData.Instance.userData.setFALength(float.Parse(faLength.text));
            AppData.Instance.userData.setUALength(float.Parse(uaLength.text));
            setLenght.gameObject.SetActive(!setLenght.IsActive());
        });

    }
    private void updataUI()
    {
        string calibStatus = MarsComm.calibration == 1 ? "DONE" : "CALIBRATE";
        calibTxt.text = $"STATUS : {calibStatus}";
        // kinParaTxt.text = $"KIN-PARAM: \n\t{MarsComm.uaLength}m\n\t{MarsComm.faLength}m\n\t{MarsComm.shPosZ}";
        calibTick.enabled = MarsComm.CALIBRATION[MarsComm.calibration] != "NOCALIB";
        CalibSetupImg.SetActive(calibState == CALIBSTATES.CALIBRATE || calibState == CALIBSTATES.SETUPLIMB);
        kinSetupImg.SetActive(calibState == CALIBSTATES.SETLIMBKINPARA);
        panel.SetActive( calibState == CALIBSTATES.ALLDONE);

    }
    public void redoKinParamSet()
    {
        if (calibState != CALIBSTATES.ALLDONE)
            return;
        calibState = CALIBSTATES.SETLIMBKINPARA;
        kinTick.enabled = false;
    }
    private void OnDestroy()
    {
        MarsComm.OnMarsButtonReleased -= onMarsButtonReleased;
    }
    private void OnApplicationQuit()
    {

        Application.Quit();
        JediComm.Disconnect();
    }
}


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
//     private readonly string limbWeightScene = "LIMBWEIGHT";
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
//         HUMAN_LIMB_KIN_SET,
//         CHECKING_HUMAN_LIMB_DYN_PARAM,
//         HUMAN_LIMB_DYN_PARAM_SET_ERROR,
//         ALL_DONE,
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
//         { UserSetupState.HUMAN_LIMB_KIN_SET, ("User kinematic limb parameter setup is complete.", new Color32(202, 108, 0, 255)) },
//         { UserSetupState.CHECKING_HUMAN_LIMB_DYN_PARAM, ("Setting and checking limb weight parameter.", new Color32(202, 108, 0, 255)) },
//         { UserSetupState.HUMAN_LIMB_DYN_PARAM_SET_ERROR, ("Error setting limb weight parameter. Please check the limb weight parameters and try again.", Color.red) },
//         { UserSetupState.ALL_DONE, ("User set up is complete. Press the MARS button to continue to start therapy.", new Color32(0, 153, 51, 255)) },
//     };
//     private bool calibBtnPressed = false;
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
//     // Variables to keep track of time and the number of times limb weights are set.
//     private float startTimeFlag = 0;
//     private int limbWeightSetCount = 0;
//     // Time thresholds for setting limb weight parameter.
//     private const float LIMB_WEIGHT_PARAM_SET_TIME_THRESHOLD = 0.5f;
//     private const int LIMB_WEIGHT_PARAM_SET_COUNT_THRESHOLD = 5;

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
//         instructionText.text = instructionDict[UserSetupState.IDLE].Item1;
//         instructionText.color = instructionDict[UserSetupState.IDLE].Item2;        
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
//                     // calibBtnPressed = false;
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
//                 instructionText.text = "User kinematic limb parameter setup is complete.";
//                 instructionText.color = new Color32(202, 108, 0, 255);
//                 // Check if the limb weight parameters are available.
//                 if (AppData.Instance.userData.uaWeight > 0 && AppData.Instance.userData.faWeight > 0)
//                 {
//                     // Set the human limb weight parameters.
//                     MarsComm.resetHumanLimbDynParams();
//                     calibState = UserSetupState.CHECKING_HUMAN_LIMB_DYN_PARAM;
//                     hLimbParamSetState = HumanLimbSetState.GETTING_PARAM;
//                     MarsComm.setHumanLimbDynParams(AppData.Instance.userData.uaWeight, AppData.Instance.userData.faWeight);
//                     startTimeFlag = Time.time;
//                     limbWeightSetCount = 1;
//                 }
//                 else
//                 {
//                     // Go to limb weight scene.
//                     // SceneManager.LoadScene(limbWeightScene);
//                 }
//                 break;

//             case UserSetupState.CHECKING_HUMAN_LIMB_DYN_PARAM:
//                 instructionText.text = "Setting and checking limb weight parameter.";
//                 instructionText.color = new Color32(202, 108, 0, 255);
//                 // Check if the human limb parameters have been set.
//                 switch (hLimbParamSetState)
//                 {
//                     case HumanLimbSetState.GETTING_PARAM:
//                         // Check the human limb dynamic parameters.
//                         if (MarsComm.limbDynParam == 0x01)
//                         {
//                             MarsComm.getHumanLimbDynParams();
//                         }
//                         else
//                         {
//                             if ((Time.time - startTimeFlag) > LIMB_WEIGHT_PARAM_SET_TIME_THRESHOLD)
//                             {
//                                 if (limbWeightSetCount < LIMB_WEIGHT_PARAM_SET_COUNT_THRESHOLD)
//                                 {
//                                     MarsComm.setHumanLimbDynParams(AppData.Instance.userData.uaWeight, AppData.Instance.userData.faWeight);
//                                     limbWeightSetCount++;
//                                     startTimeFlag = Time.time;
//                                 }
//                                 else
//                                 {
//                                     calibState = UserSetupState.HUMAN_LIMB_DYN_PARAM_SET_ERROR;
//                                 }
//                             }
//                         }
//                         break;

//                     case HumanLimbSetState.PARAM_SET_SUCCESS:
//                         // Handle successful parameter setting.
//                         calibState = UserSetupState.ALL_DONE;
//                         break;

//                     case HumanLimbSetState.PARAM_SET_ERROR:
//                         // Handle parameter setting error.
//                         MarsComm.resetHumanLimbDynParams();
//                         hLimbParamSetState = HumanLimbSetState.NONE;
//                         break;
//                 }
//                 break;

//             case UserSetupState.HUMAN_LIMB_DYN_PARAM_SET_ERROR:
//                 instructionText.text = "Error setting limb weight parameter. Please check the limb weight parameters and try again.";
//                 instructionText.color = Color.red;
//                 break;

//             case UserSetupState.ALL_DONE:
//                 instructionText.text = "User set up is complete. Press the MARS button to continue to start therapy.";
//                 instructionText.color = new Color32(0, 153, 51, 255);
//                 if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION")
//                 {
//                     // Check if the target is 0
//                     if (MarsComm.target == 0)
//                     {
//                         SceneManager.LoadScene(nextScene);
//                     }
//                     else
//                     {
//                         MarsComm.setControlTarget(0);
//                     }
//                 }
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
