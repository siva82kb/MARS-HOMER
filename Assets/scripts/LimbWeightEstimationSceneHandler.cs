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
using System.Linq;
using System.Runtime.InteropServices;


public class LimbWeightEstimationSceneHandler : MonoBehaviour
{
    //ui related variables
    public TMP_Text instructionText;
    public TMP_Text statusText;
    public TMP_Text targetLabel;
    public TMP_Text targetValue;
    public Slider sliderTargetValue;
    public Button btnSetWeightParams;
    public Button btnStartRedoEstimation;
    public Button btnSwitchControl;
    private readonly string calibScene = "ROBOTCALIB";
    private readonly string userSetupScene = "USERSETUP";
    private readonly string nextScene = "CHOOSEMOVE";
    private readonly string limbWeightEstScene = "LIMBWEST";
    private bool attachMarsButtonEvent = true;
    private static string FLOAT_FORMAT = "+0.000;-0.000";

    // User set up states
    private enum LimbWeightEstimationState
    {
        WAIT_FOR_LIMB_ATTACHMENT,
        ESTIMATING_LIMB_WEIGHT,
        ALL_SYSTEMS_GO,
    }
    private LimbWeightEstimationState limbWeightEstimationState = LimbWeightEstimationState.WAIT_FOR_LIMB_ATTACHMENT;
    // Instruction text and color dictionary.
    private Dictionary<LimbWeightEstimationState, (string, Color)> instructionDict = new Dictionary<LimbWeightEstimationState, (string, Color)>()
    {
        { LimbWeightEstimationState.WAIT_FOR_LIMB_ATTACHMENT, ("Attach the human limb to the robot and have the subject full relax. Press the Calib button to start the weight estimation.", new Color32(202, 108, 0, 255)) },
        { LimbWeightEstimationState.ESTIMATING_LIMB_WEIGHT, ("Estimating limb weight. Move the limb in the horizontal plane.", new Color32(202, 108, 0, 255)) },
        { LimbWeightEstimationState.ALL_SYSTEMS_GO, ("Estimated weight.\nPress the MARS button to continue.", new Color32(202, 108, 255, 255)) },
    };
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
    private const int nParams = 2;
    private const int maxParamEstimates = 250;
    private RecursiveLeastSquares rlsHLimbWeights = new RecursiveLeastSquares(nParams);
    private float[,] weightParams = new float[maxParamEstimates, nParams];
    private int weightParamsIndex = 0;
    private float[] weightParamsMean = new float[nParams];
    private float[] weightParamsStdev = new float[nParams];
    
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
        if ((MarsComm.limbKinParam == 0x00) || (MarsComm.CONTROLTYPE[MarsComm.controlType] != "POSITION"))
        {
            SceneManager.LoadScene(userSetupScene);
        }        

        // Reset human limb dynamic parameters.
        MarsComm.resetHumanLimbDynParams();

        // Intialize statemachine.
        limbWeightEstimationState = LimbWeightEstimationState.WAIT_FOR_LIMB_ATTACHMENT;
        calibBtnPressed = false;
        marsBtnPressed = false;
        hLimbParamSetState = HumanLimbSetState.NONE;

        // Initialize UI
        InitUI();
    }

    void Update()
    {
        MarsComm.sendHeartbeat();

        // Wait for a second before doing anything.
        if (Time.timeSinceLevelLoad < 0.5) return;

        // Attach MARS event listeners.
        if (attachMarsButtonEvent)
        {
            attachMarsButtonEvent = false;
            // Set limb
            MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
            MarsComm.OnCalibButtonReleased += onCalibButtonReleased;
            MarsComm.onHumanLimbDynParamData += OnHumanLimbDynParamData;
        }

        // Run the state machine.
        runUserSetupStateMachine();
    }

    public void InitUI()
    {
        instructionText.text = "";
        statusText.text = "";
    }

    public void runUserSetupStateMachine()
    {
        // Udpate the instruction.
        instructionText.text = instructionDict[limbWeightEstimationState].Item1;
        instructionText.color = instructionDict[limbWeightEstimationState].Item2;
        // Run the statemachine
        switch (limbWeightEstimationState)
        {
            case LimbWeightEstimationState.WAIT_FOR_LIMB_ATTACHMENT:
                // Check the weight on the arm.
                if (calibBtnPressed)
                {
                    limbWeightEstimationState = LimbWeightEstimationState.ESTIMATING_LIMB_WEIGHT;
                }
                break;

            case LimbWeightEstimationState.ESTIMATING_LIMB_WEIGHT:
                // Update the estimtate if parameters are not yet estimated.
                rlsHLimbWeights.Update(new float[] {
                    (float) (Mathf.Sin(MarsComm.phi1 * Mathf.Deg2Rad) * Mathf.Cos(MarsComm.phi2 * Mathf.Deg2Rad)),
                    (float) (Mathf.Sin(MarsComm.phi1 * Mathf.Deg2Rad) * Mathf.Cos((MarsComm.phi2 - MarsComm.phi3) * Mathf.Deg2Rad))
                }, MarsComm.torque);
                // Check if we need to change state.
                if (computeMeanAndStdev())
                {
                    limbWeightEstimationState = LimbWeightEstimationState.ALL_SYSTEMS_GO;
                }
                break;

            case LimbWeightEstimationState.ALL_SYSTEMS_GO:
                if (marsBtnPressed)
                {
                    
                }
                break;

                // case LimbWeightEstimationState.IDLE:
                //     // instructionText.text = "Press the MARS button to move the robot to the horizontal position.";
                //     // instructionText.color = new Color32(202, 108, 0, 255);
                //     if (marsBtnPressed)
                //     {
                //         limbWeightEstimationState = LimbWeightEstimationState.MOVING_TO_HORIZONTAL_POSITION;
                //         // Set control to POSITION and set target to -90.
                //         MarsComm.setControlType("POSITION");
                //         MarsComm.setControlTarget(-90);
                //     }
                //     break;

            // case LimbWeightEstimationState.MOVING_TO_HORIZONTAL_POSITION:
                //     // instructionText.text = "Moving the robot to the horizontal position. Please wait.";
                //     // instructionText.color = new Color32(202, 108, 0, 255);
                //     if (Mathf.Abs(MarsComm.angle1 + 90) < 10)
                //     {
                //         limbWeightEstimationState = LimbWeightEstimationState.IN_HORIZONTAL_POSITION;
                //         MarsComm.resetHumanLimbKinParams();
                //         // calibBtnPressed = false;
                //         hLimbParamSetState = HumanLimbSetState.NONE;
                //         shZPosIndex = 0;
                //         shZPosArrayFull = false;
                //     }
                //     break;

                // case LimbWeightEstimationState.IN_HORIZONTAL_POSITION:
                //     // Update the shoulder position array.
                //     shZPosArray[shZPosIndex] = MarsComm.zEndpoint;
                //     shZPosIndex = (shZPosIndex + 1) % N;
                //     if (!shZPosArrayFull && shZPosIndex == 0) shZPosArrayFull = true;

                //     // Check if the human limb parameters have been set.
                //     if (hLimbParamSetState == HumanLimbSetState.NONE)
                //     {
                //         // Check the human limb kinematic parameters.
                //         if (MarsComm.limbKinParam == 0x00) hLimbParamSetState = HumanLimbSetState.NONE;
                //         else
                //         {
                //             MarsComm.getHumanLimbKinParams();
                //             limbWeightEstimationState = LimbWeightEstimationState.CHECKING_HUMAN_LIMB_KIN_PARAM;
                //             hLimbParamSetState = HumanLimbSetState.GETTING_PARAM;
                //         }
                //     }
                //     break;

                // case LimbWeightEstimationState.CHECKING_HUMAN_LIMB_KIN_PARAM:
                //     // instructionText.text = "Checking set human limb Kinematic Parameters.";
                //     // instructionText.color = new Color32(202, 108, 0, 255);
                //     // Check if the human limb parameters have been set.
                //     switch (hLimbParamSetState)
                //     {
                //         case HumanLimbSetState.GETTING_PARAM:
                //             // Check the human limb kinematic parameters.
                //             if (MarsComm.limbKinParam == 0x01) MarsComm.getHumanLimbKinParams();
                //             else
                //             {
                //                 // Go back to IN_HORIZONTAL_POSITION
                //                 limbWeightEstimationState = LimbWeightEstimationState.IN_HORIZONTAL_POSITION;
                //                 MarsComm.resetHumanLimbKinParams();
                //                 hLimbParamSetState = HumanLimbSetState.NONE;
                //                 shZPosIndex = 0;
                //                 shZPosArrayFull = false;
                //             }
                //             break;

                //         case HumanLimbSetState.PARAM_SET_SUCCESS:
                //             // Handle successful parameter setting.
                //             limbWeightEstimationState = LimbWeightEstimationState.HUMAN_LIMB_KIN_SET;
                //             break;

                //         case HumanLimbSetState.PARAM_SET_ERROR:
                //             // Handle parameter setting error.
                //             // Go back to IN_HORIZONTAL_POSITION
                //             limbWeightEstimationState = LimbWeightEstimationState.IN_HORIZONTAL_POSITION;
                //             MarsComm.resetHumanLimbKinParams();
                //             hLimbParamSetState = HumanLimbSetState.NONE;
                //             shZPosIndex = 0;
                //             shZPosArrayFull = false;
                //             break;
                //     }
                //     break;

                // case LimbWeightEstimationState.HUMAN_LIMB_KIN_SET:
                //     break;
        }
    }

    private bool computeMeanAndStdev()
    {
        bool[] _done = new bool[nParams];
        for (int _i = 0; _i < nParams; _i++)
        {
            weightParams[weightParamsIndex, _i] = rlsHLimbWeights.theta[_i];
        }
        weightParamsIndex = (weightParamsIndex + 1) % maxParamEstimates;
        // Display the parameters mean and standard deviation.
        statusText.text = "";
        for (int _i = 0; _i < nParams; _i++)
        {
            weightParamsMean[_i] = 0f;
            weightParamsStdev[_i] = 0f;
            for (int _j = 0; _j < maxParamEstimates; _j++)
            {
                weightParamsMean[_i] += weightParams[_j, _i];
            }
            weightParamsMean[_i] /= maxParamEstimates;
            for (int _j = 0; _j < maxParamEstimates; _j++)
            {
                weightParamsStdev[_i] += Mathf.Pow(weightParams[_j, _i] - weightParamsMean[_i], 2);
            }
            weightParamsStdev[_i] = Mathf.Sqrt(weightParamsStdev[_i] / maxParamEstimates);

            // Add parameter mean +/- stdev to the text.
            float _variation = 100 * weightParamsStdev[_i] / Mathf.Abs(weightParamsMean[_i]);
            statusText.text += $"{_i + 1}: {weightParamsMean[_i]:F3} +/- {weightParamsStdev[_i]:F3} [{_variation:F3}]\n";
            _done[_i] = _variation < 5f;
        }
        return _done.All(x => x);
    }

    public void onMarsButtonReleased()
    {
        // Act depending on the calibration state.
        // if (limbWeightEstimationState == LimbWeightEstimationState.IDLE)
        // {
        //     // Set control to POSITION and set target to -90.
        //     MarsComm.setControlType("POSITION");
        //     MarsComm.setControlTarget(-90);
        // }
    }

    public void onCalibButtonReleased()
    {
        // Act depending on the estimation state.
        if (limbWeightEstimationState == LimbWeightEstimationState.WAIT_FOR_LIMB_ATTACHMENT)
        {
            calibBtnPressed = true;
        }
        // Debug.Log($"{limbWeightEstimationState}, {shZPosArrayFull}");
        // if ((limbWeightEstimationState == LimbWeightEstimationState.IN_HORIZONTAL_POSITION) && shZPosArrayFull)
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
        // else if (limbWeightEstimationState == LimbWeightEstimationState.HUMAN_LIMB_KIN_SET)
        // {
        //     // Change scene to weight set scene.
        //     // SceneManager.LoadScene(limbWeightSetScene);
        // }
    }

    private void OnHumanLimbDynParamData()
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
        MarsComm.onHumanLimbKinParamData -= OnHumanLimbDynParamData;
    }
    private void OnApplicationQuit()
    {

        Application.Quit();
        JediComm.Disconnect();
    }
}
