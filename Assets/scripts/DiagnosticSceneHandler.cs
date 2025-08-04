using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using UnityEditor.ShaderKeywordFilter;
using Unity.Mathematics;
using Unity.VisualScripting;

public class DiagnosticSceneHandler : MonoBehaviour
{
    public TMP_Text dataDisplayText;
    public Dropdown ddLimbType;
    public Dropdown ddControlType;
    public Button btnCalibrate;
    public Toggle tglLogData;
    public Slider sldrTarget;
    public TMP_Text targetValueText;
    public Button btnSetTarget;
    public Toggle tglHLimbKinParamSet;
    public Slider sldrHLimbUALength;
    public Slider sldrHLimbFALength;
    public TMP_Text hlbUALengthText;
    public TMP_Text hlbFALengthText;
    public TMP_Text hlbKinParamSetText;
    public Toggle tglHLimbDynParamSet;
    public TMP_Text hlbDynParamSetText;

    private static string FLOAT_FORMAT = "+0.000;-0.000";
    private string fileName = "";
    private StreamWriter fileWriter = null;
    private bool updateSliderRange = true;
    private const int MAX_ENDPOINTS = 10;
    private int endpointCount = 0;
    private Vector3[] endpointPositions = new Vector3[100];
    private float setHLimbKinUALength = 0.0f;
    private float setHLimbKinFALength = 0.0f;
    private float setHLimbKinShPosZ = 0.0f;
    private float setHLimbDynUAWeight = 0.0f;
    private float setHLimbDynFAWeight = 0.0f;
    // Flag to check the human limb kinematic parameters.
    private bool hLimbKinParamCheckFlag = false;
    private bool hLimbKinParamSetDone = false;
    // Flag to check the human limb dynamic parameters.
    // private bool hLimbDynParamSetFlag = false;
    private bool hLimbDynParamCheckFlag = false;
    private bool hLimbDynParamSetDone = false;
    // Dynamic parameter estimation variables.
    private enum LimbDynEstState
    {
        WAITFORSTART = 0x00,
        SETUPFORESTIMATION = 0x01,
        ESTIMATE = 0x02,
        ESTIMATIONDONE = 0x03,
        HOLDTEST = 0x04,
        TEARDOWNSETUP = 0x05,
        DONE = 0x06
    }
    private LimbDynEstState hLimbDynEstState = LimbDynEstState.WAITFORSTART;
    private bool readyForStateChange = false;
    private const int nParams = 2;
    private const int maxParamEstimates = 250;
    private RecursiveLeastSquares rlsHLimbWeights = new RecursiveLeastSquares(nParams);
    private float[,] weightParams = new float[maxParamEstimates, nParams];
    private int weightParamsIndex = 0;
    private float[] weightParamsMean = new float[nParams];
    private float[] weightParamsStdev = new float[nParams];

    void Start()
    {
        // Connect to the robot.
        ConnectToRobot.Connect(AppData.COMPort);

        // Initialize UI
        InitializeUI();
        // Attach callbacks
        AttachControlCallbacks();
        // Invoke some callbacks to update the UI.
        sldrHLimbUALength.onValueChanged.Invoke(sldrHLimbUALength.value);
        sldrHLimbFALength.onValueChanged.Invoke(sldrHLimbFALength.value);

        // Get device version.
        MarsComm.getVersion();
        MarsComm.startSensorStream();
        MarsComm.setDiagnosticMode();
    }

    private void Update()
    {
        // MARS heartbeat
        MarsComm.sendHeartbeat();

        // Display device data
        DisplayDeviceData();

        // Update UI.
        UpdateUI();

        // Run the human limb dynamic parameter estimation state machine.
        RunHumanLimbDynParamEstimation();
    }

    private void RunHumanLimbDynParamEstimation()
    {
        if (hLimbDynEstState == LimbDynEstState.WAITFORSTART) return;
        switch (hLimbDynEstState)
        {
            case LimbDynEstState.SETUPFORESTIMATION:
                // Wait for the start of the estimation.
                handleSetupForEstimation();
                // Send the weight parameters to MARS.
                // setHLimbDynUAWeight = -5.686f;
                // setHLimbDynFAWeight = -0.241f;
                // MarsComm.setHumanLimbDynParams(setHLimbDynUAWeight, setHLimbDynFAWeight);
                break;
            case LimbDynEstState.ESTIMATE:
                // Update the estimtate if parameters are not yet estimated.
                rlsHLimbWeights.Update(new float[] {
                    (float) (Math.Sin(MarsComm.phi1 * Mathf.Deg2Rad) * Math.Cos(MarsComm.phi2 * Mathf.Deg2Rad)),
                    (float) (Math.Sin(MarsComm.phi1 * Mathf.Deg2Rad) * Math.Cos((MarsComm.phi2 - MarsComm.phi3) * Mathf.Deg2Rad))
                }, MarsComm.torque);
                // Check if we need to change state.
                if (computeMeanAndStdev())
                {
                    hLimbDynEstState = LimbDynEstState.ESTIMATIONDONE;
                    readyForStateChange = false;
                }
                break;
            case LimbDynEstState.ESTIMATIONDONE:
                if (readyForStateChange) break;
                // Not ready for change. Set the parameters in the firmware.
                // Send the weight parameters to MARS.
                setHLimbDynUAWeight = weightParamsMean[0];
                setHLimbDynFAWeight = weightParamsMean[1];
                MarsComm.setHumanLimbDynParams(setHLimbDynUAWeight, setHLimbDynFAWeight);
                // Get the human limb dynamic parameters from MARS.
                MarsComm.getHumanLimbDynParams();
                // Set the check flag to verify human limb dynamic parameters are set.
                hLimbDynParamCheckFlag = true;
                break;
            case LimbDynEstState.HOLDTEST:
                // Perform hold testing with the estimated weights.
                // Set the control type to AWS.
                break;
            case LimbDynEstState.TEARDOWNSETUP:
                // Teardown the setup for estimation.
                if (handleTearDownSetup())
                {
                    hLimbDynEstState = LimbDynEstState.DONE;
                }
                break;
            case LimbDynEstState.DONE:
                // Reset the state to WAITFORSTART after completion.
                hLimbDynEstState = LimbDynEstState.WAITFORSTART;
                break;
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
        hlbDynParamSetText.text = "";
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
            float _variation = 100 * weightParamsStdev[_i] / Math.Abs(weightParamsMean[_i]);
            hlbDynParamSetText.text += $"{_i + 1}: {weightParamsMean[_i]:F3} +/- {weightParamsStdev[_i]:F3} [{_variation:F3}]\n";
            _done[_i] = _variation < 5f;
        }
        return _done.All(x => x);
    }

    private void handleSetupForEstimation()
    {
        // Check if the control mode is position, else set and leave.
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] != "POSITION")
        {
            MarsComm.setControlType("POSITION");
            return;
        }
        // Check if the target position is set to -90 degrees.
        if (MarsComm.target != -90f)
        {
            MarsComm.setControlTarget(-90f);
            return;
        }
        // Check if the robot has reached the target position.
        if (Mathf.Abs(MarsComm.angle1 - MarsComm.target) < 2.5)
        {
            readyForStateChange = true;
            rlsHLimbWeights.ResetEstimator();
        }
    }

    private bool handleTearDownSetup()
    {
        // Check if the target position is set to -90 degrees.
        if (MarsComm.target != 0f)
        {
            MarsComm.setControlTarget(0f);
            return false;
        }
        // Check if the robot has reached the target position.
        if (Mathf.Abs(MarsComm.angle1 - MarsComm.target) > 2.5)
        {
            return false;
        }
        // Check if the control mode is position, else set and leave.
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] != "NONE")
        {
            MarsComm.setControlType("NONE");
            return false;
        }
        return true;
    }

    private void onNewMarsData()
    {
        // Write row to the file if logging is enabled.
        if (fileWriter != null)
        {
            fileWriter.WriteLine($"{MarsComm.runTime},{MarsComm.packetNumber},{MarsComm.status},{MarsComm.errorString},{MarsComm.limb},{MarsComm.calibration},{MarsComm.limbKinParam},{MarsComm.limbDynParam},{MarsComm.angle1},{MarsComm.angle2},{MarsComm.angle3},{MarsComm.angle4},{MarsComm.force},{MarsComm.torque},{MarsComm.xEndpoint},{MarsComm.yEndpoint},{MarsComm.zEndpoint},{MarsComm.phi1},{MarsComm.phi2},{MarsComm.phi3},{MarsComm.imu1Angle},{MarsComm.imu2Angle},{MarsComm.imu3Angle},{MarsComm.marButton},{MarsComm.calibButton},{MarsComm.target},{MarsComm.desired},{MarsComm.control}");
            fileWriter.Flush();
        }
    }

    private void onMarsButtonReleased()
    {
        // This method is called when the MARS button is released
        Debug.Log("MARS button released.");
    }

    private void onCalibButtonReleased()
    {
        if (tglHLimbKinParamSet.isOn)
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
            setHLimbKinUALength = sldrHLimbUALength.value;
            setHLimbKinFALength = sldrHLimbFALength.value;
            setHLimbKinShPosZ = averagePosition.z;
            Debug.Log($"Setting human limb kinematic parameters: UALength={setHLimbKinUALength}, FALength={setHLimbKinFALength}, Average Position Z={setHLimbKinShPosZ}");
            MarsComm.setHumanLimbKinParams(setHLimbKinUALength, setHLimbKinFALength, setHLimbKinShPosZ);
            // Get the human limb kinematic parameters from MARS.
            MarsComm.getHumanLimbKinParams();
            // Set the check flag to verify human limb kinematic parameters are set.
            hLimbKinParamCheckFlag = true;
        }
        else if (tglHLimbDynParamSet.isOn && readyForStateChange)
        {
            // Check the current state and react.
            switch (hLimbDynEstState)
            {
                case LimbDynEstState.SETUPFORESTIMATION:
                    // Start recording human limb angles and torque.
                    hLimbDynEstState = LimbDynEstState.ESTIMATE;
                    rlsHLimbWeights.ResetEstimator();
                    break;
                case LimbDynEstState.ESTIMATIONDONE:
                    hLimbDynEstState = LimbDynEstState.HOLDTEST;
                    break;
                case LimbDynEstState.HOLDTEST:
                    break;
            }
            readyForStateChange = false;
        }
    }

    private void onControlModeChange()
    {
        // This method is called when the control mode is changed
        Debug.Log($"Control mode changed to: {MarsComm.CONTROLTYPE[MarsComm.controlType]}");
        // Set the update slider flag for the main thread to update the UI.
        updateSliderRange = true;
    }

    private void OnHumanLimbKinParamData()
    {
        // Check if the human limb kinematics parameters match the set values.
        if (MarsComm.limbKinParam != 0x01 || MarsComm.uaLength != setHLimbKinUALength || MarsComm.faLength != setHLimbKinFALength || MarsComm.shPosZ != setHLimbKinShPosZ)
        {
            Debug.LogWarning("Human limb kinematic parameters are not set correctly.");
            hLimbKinParamCheckFlag = false;
            MarsComm.resetHumanLimbKinParams();
        }
        else
        {
            Debug.Log("Human limb kinematic parameters are set correctly.");
            hLimbKinParamCheckFlag = false;
            hLimbKinParamSetDone = true;
        }
    }

    private void OnHumanLimbDynParamData()
    {
        // Check if the human limb dynamics parameters match the set values.
        if (MarsComm.limbDynParam != 0x01 || MarsComm.uaWeight != setHLimbDynUAWeight || MarsComm.faWeight != setHLimbDynFAWeight)
        {
            Debug.LogWarning("Human limb dynamic parameters are not set correctly.");
            hLimbDynParamCheckFlag = false;
            hLimbDynParamSetDone = false;
            MarsComm.resetHumanLimbDynParams();
            readyForStateChange = false;
        }
        else
        {
            Debug.Log("Human limb dynamic parameters are set correctly.");
            hLimbDynParamCheckFlag = false;
            hLimbDynParamSetDone = true;
            // Ready to change state.
            readyForStateChange = true;
        }
    }

    private void InitializeUI()
    {
        // Fill dropdown list
        ddLimbType.ClearOptions();
        ddLimbType.AddOptions(MarsComm.LIMBTEXT.ToList());
        ddControlType.AddOptions(MarsComm.CONTROLTYPETEXT.ToList());
        // Disable set target button.
        btnSetTarget.interactable = false;
        // Clear panel selections.
        tglLogData.enabled = true;
        tglLogData.isOn = false;
        // Disable slider for human limb kinematic parameters.
        sldrHLimbUALength.interactable = false;
        sldrHLimbFALength.interactable = false;
        // Disable the human limb kinematic and dynamic parameters toggle.
        tglHLimbKinParamSet.isOn = false;
        tglHLimbDynParamSet.isOn = false;
        tglHLimbKinParamSet.interactable = false;
        tglHLimbDynParamSet.interactable = false;
    }

    public void AttachControlCallbacks()
    {
        // Dropdown value change.
        ddLimbType.onValueChanged.AddListener(delegate { OnLimbTypeChange(); });
        ddControlType.onValueChanged.AddListener(delegate { OnControlTypeChange(); });

        // Calibrate button click.
        btnCalibrate.onClick.AddListener(delegate
        {
            // Send calibration command to MARS.
            MarsComm.calibrate();
            Debug.Log("Calibration command sent.");
        });

        // Toggle button
        tglLogData.onValueChanged.AddListener(onDataLogChange);

        // Human limb kinematic parameters toggle.
        tglHLimbKinParamSet.onValueChanged.AddListener(delegate
        {
            // Enable or disable the sliders based on the toggle state.
            bool isEnabled = tglHLimbKinParamSet.isOn;
            sldrHLimbUALength.interactable = isEnabled;
            sldrHLimbFALength.interactable = isEnabled;
            hlbUALengthText.text = isEnabled ? "UA: " + sldrHLimbUALength.value.ToString(FLOAT_FORMAT) + " m" : "";
            hlbFALengthText.text = isEnabled ? "FA: " + sldrHLimbFALength.value.ToString(FLOAT_FORMAT) + " m" : "";
            hlbKinParamSetText.text = "Press the Calib button to set the parameters.\nMake sure the UA and FA values are set correctly.";
        });
        // Slider value change.
        sldrTarget.onValueChanged.AddListener(delegate { OnControlTargetChange(); });
        sldrHLimbUALength.onValueChanged.AddListener(delegate
        {
            hlbUALengthText.text = "UA: " + sldrHLimbUALength.value.ToString(FLOAT_FORMAT) + " m";
        });
        sldrHLimbFALength.onValueChanged.AddListener(delegate
        {
            hlbFALengthText.text = "FA: " + sldrHLimbFALength.value.ToString(FLOAT_FORMAT) + " m";
        });

        // Human limb dynamic parameters toggle.
        tglHLimbDynParamSet.onValueChanged.AddListener(delegate { OnHLimbDynParamEstimate(); });

        // Set target button click.
        btnSetTarget.onClick.AddListener(delegate { OnTargetSet(); });

        // Listen to MARS's event
        MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
        MarsComm.OnCalibButtonReleased += onCalibButtonReleased;
        MarsComm.OnNewMarsData += onNewMarsData;
        MarsComm.OnControlModeChange += onControlModeChange;
        MarsComm.onHumanLimbKinParamData += OnHumanLimbKinParamData;
        MarsComm.onHumanLimbDynParamData += OnHumanLimbDynParamData;
    }

    public void UpdateUI()
    {
        // Check if MARS has been calibrated.
        btnCalibrate.enabled = (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB")
            && (MarsComm.LIMBTYPE[ddLimbType.value] != "NOLIMB");
        // Enable control type dropdown if limb is selected.
        ddControlType.interactable = MarsComm.CALIBRATION[MarsComm.calibration] == "YESCALIB";
        // Enable the set target button only if the control is not NONE.
        btnSetTarget.interactable = MarsComm.CONTROLTYPE[MarsComm.controlType] != "NONE";

        // Human limb kinematic and dynamic parameters toggle.
        tglHLimbKinParamSet.interactable = (MarsComm.CALIBRATION[MarsComm.calibration] == "YESCALIB")
            && tglHLimbDynParamSet.isOn == false;
        tglHLimbDynParamSet.interactable = (MarsComm.limbKinParam == 0x01)
            && tglHLimbKinParamSet.isOn == false;

        // Check of the slider range needs to be updated.
        if (updateSliderRange)
        {
            // Change slider range based on control type.
            if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION")
            {
                sldrTarget.minValue = -120f;
                sldrTarget.maxValue = 0f;
                sldrTarget.value = MarsComm.angle1;
                targetValueText.text = sldrTarget.value.ToString(FLOAT_FORMAT) + " deg";
            }
            else if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "TORQUE")
            {
                sldrTarget.minValue = -10f;
                sldrTarget.maxValue = 10f;
                sldrTarget.value = MarsComm.torque;
                targetValueText.text = sldrTarget.value.ToString(FLOAT_FORMAT) + " Nm";
            }
            else
            {
                sldrTarget.minValue = 0f;
                sldrTarget.maxValue = 1f;
                sldrTarget.value = 0;
                targetValueText.text = "";
            }
            updateSliderRange = false;
        }

        // Update the human limb kinematic parameters text.
        hlbKinParamSetText.text = MarsKinDynamics.ForwardKinematicsExtended(MarsComm.angle1, MarsComm.angle2, MarsComm.angle3, MarsComm.angle4).ToString("F3");
        // Human limb kinematic parameters calculation.
        if (tglHLimbKinParamSet.isOn && MarsComm.calibButton == 0)
        {
            if (endpointCount < MAX_ENDPOINTS)
            {
                endpointPositions[endpointCount] = MarsKinDynamics.ForwardKinematicsExtended(MarsComm.angle1, MarsComm.angle2, MarsComm.angle3, MarsComm.angle4);
                endpointCount++;
            }
        }
        else
        {
            endpointCount = 0;
        }

        // Check the set human limb kinematic parameters.
        if (hLimbKinParamCheckFlag)
        {
            // Check the human limb kinematic parameters.
            if (MarsComm.limbKinParam == 0x01) MarsComm.getHumanLimbKinParams();
        }
        if (hLimbKinParamSetDone)
        {
            tglHLimbKinParamSet.isOn = false;
            hLimbKinParamSetDone = false;
        }

        // Check the set human limb dynamic parameters.
        if (hLimbDynParamCheckFlag)
        {
            // Check the human limb dynamic parameters.
            if (MarsComm.limbDynParam == 0x01) MarsComm.getHumanLimbDynParams();
        }
        if (hLimbDynParamSetDone)
        {
            // tglHLimbDynParamSet.isOn = false;
            hLimbDynParamSetDone = false;
        }
    }

    public void DisplayDeviceData()
    {
        // State text
        string runT = MarsComm.runTime.ToString("0.000").PadRight(15);
        string packNo = MarsComm.packetNumber.ToString().PadRight(8);
        string stateText = string.Join("\n", new string[] {
            $"Time          : {MarsComm.currentTime, -15}",
            $"Dev ID        : {MarsComm.deviceId}",
            $"F/W Version   : {MarsComm.version}",
            $"Compile Date  : {MarsComm.compileDate}",
            "",
            $"Device Time   : {runT} | Packet Number: {packNo}",
            $"Frame Rate    : {MarsComm.frameRate.ToString(FLOAT_FORMAT)} Hz",
            $"Status        : {MarsComm.OUTDATATYPE[MarsComm.dataType], -15} | Error : {MarsComm.errorString}",
            $"Calib         : {MarsComm.CALIBRATION[MarsComm.calibration], -15} | Cmd Status: {MarsComm.COMMAND_STATUS[MarsComm.recentCommandStatus]}",
            $"Limb          : {MarsComm.LIMBTYPE[MarsComm.limb], -15} | {MarsComm.LIMBKINPARAM[MarsComm.limbKinParam], -15} | {MarsComm.LIMBDYNPARAM[MarsComm.limbDynParam]}",
            $"Control       : {MarsComm.CONTROLTYPE[MarsComm.controlType]}",
            ""
        });
        //Construct the display text while ensuring values are safely converted to string
        string robotAngles = string.Join(" ", new string[] {
            MarsComm.angle1.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.angle2.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.angle3.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.angle4.ToString(FLOAT_FORMAT).PadRight(8),
        });
        string hlimAngles = string.Join(" ", new string[] {
            MarsComm.phi1.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.phi2.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.phi3.ToString(FLOAT_FORMAT).PadRight(8)
        });
        string epPos = string.Join(" ", new string[] {
            MarsComm.xEndpoint.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.yEndpoint.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.zEndpoint.ToString(FLOAT_FORMAT).PadRight(8),
        });
        string imuAngles = string.Join(" ", new string[] {
            MarsComm.imu1Angle.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.imu2Angle.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.imu3Angle.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.imu4Angle.ToString(FLOAT_FORMAT).PadRight(8),
        });
        // Human limb parameters text
        string hlbParamText = string.Join("\n", new string[] {
            $"Kin Param     : (UA) {MarsComm.uaLength.ToString(FLOAT_FORMAT)}, (FA) {MarsComm.faLength.ToString(FLOAT_FORMAT)}, (Z) {MarsComm.shPosZ.ToString(FLOAT_FORMAT)}",
            $"Dyn Param     : (UA) {MarsComm.uaWeight.ToString(FLOAT_FORMAT)}, (FA) {MarsComm.faWeight.ToString(FLOAT_FORMAT)}",
            "",
            ""
        });
        string sensorText = String.Join("\n", new string[] {
            $"Robot Angles  : {robotAngles}",
            $"IMU Angles    : {imuAngles}",
            $"Human Angles  : {hlimAngles}",
            $"Endpoint Pos  : {epPos}",
            $"Force         : {MarsComm.force.ToString(FLOAT_FORMAT), -15} | Torque : {MarsComm.torque.ToString(FLOAT_FORMAT)}",
            $"Target        : {MarsComm.target.ToString(FLOAT_FORMAT), -15} | Desired : {MarsComm.desired.ToString(FLOAT_FORMAT)}",
            $"Control       : {MarsComm.control.ToString(FLOAT_FORMAT)}",
            $"MARS Button   : {MarsComm.marButton, -15} | Calib Button : {MarsComm.calibButton}"
        });
        // If DIAGNOSTICS is enabled, append diagnostics data
        if (MarsComm.OUTDATATYPE[MarsComm.dataType] == "DIAGNOSTICS")
        {
            sensorText += String.Join("\n", new string[] {
                "",
                $"ErrorP        : {MarsComm.errP.ToString(FLOAT_FORMAT)}",
                $"ErrorD        : {MarsComm.errD.ToString(FLOAT_FORMAT)}",
                $"ErrorI        : {MarsComm.errI.ToString(FLOAT_FORMAT)}",
                $"Grav. Comp.   : {MarsComm.gravityCompensationTorque.ToString(FLOAT_FORMAT)}",
                $"Ang. Vel.     : {MarsComm.angularVelocity1.ToString(FLOAT_FORMAT)}",
                $"Torque Change : {MarsComm.dTorque.ToString(FLOAT_FORMAT)}"
            });
        }
        dataDisplayText.text = stateText + hlbParamText + sensorText;
    }

    /*
     * Callback functions.
     */
    private void OnLimbTypeChange()
    {
        // Set MARS limb type.
        MarsComm.setLimb(MarsComm.LIMBTYPE[ddLimbType.value]);
    }
    private void OnControlTypeChange()
    {
        // Set MARS control type.
        MarsComm.setControlType(MarsComm.CONTROLTYPE[ddControlType.value]);
    }
    private void onDataLogChange(bool isOn)
    {
        if (isOn)
        {
            // Enable data logging.
            fileName = $"MARS_Data_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            fileWriter = new StreamWriter(fileName, true);
            fileWriter.WriteLine($"DeviceID: {MarsComm.deviceId}");
            fileWriter.WriteLine("runTime,packetNumber,status,errorString,limb,calib,limbkinparam,limbdynparam,angle1,angle2,angle3,angle4,force,torque,epx,epy,epz,phi1,phi2,phi3,imuangle1,imuangle2,imuangle3,marbtn,calibbtn,target,desired,control");
            Debug.Log($"Data logging started. File: {fileName}");
        }
        else
        {
            // Disable data logging.
            if (fileWriter != null)
            {
                fileWriter.Close();
                fileWriter = null;
                Debug.Log($"Data logging stopped. File saved: {fileName}");
            }
        }
    }
    private void OnControlTargetChange()
    {
        targetValueText.text = sldrTarget.value.ToString(FLOAT_FORMAT)
            + (MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION" ? " deg" : " Nm");
    }
    private void OnTargetSet()
    {
        // Set target value for control.
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION")
        {
            // Set the target position.
            MarsComm.setControlTarget(sldrTarget.value);
            Debug.Log($"Control target set to: {sldrTarget.value} deg");
        }
        else if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "TORQUE")
        {
            // Set the target torque.
            MarsComm.setControlTarget(sldrTarget.value, dur: 3f);
            Debug.Log($"Control target set to: {sldrTarget.value} Nm");
        }
    }
    private void OnHLimbDynParamEstimate()
    {
        // Set the robot to position control mode, with target postion as -90deg.
        if (tglHLimbDynParamSet.isOn)
        {
            // Enable the human limb dynamic parameter estimation.
            hLimbDynEstState = LimbDynEstState.SETUPFORESTIMATION;
        }
        else
        {
            // Get out of the human limb dynamic parameter estimation mode.
            hLimbDynEstState = LimbDynEstState.TEARDOWNSETUP;
        }
    }
    private void OnApplicationQuit()
    {
        Application.Quit();
        JediComm.Disconnect();
    }
}
