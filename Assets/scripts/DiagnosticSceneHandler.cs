using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;

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

    private static string FLOAT_FORMAT = "+0.00;-0.00";
    private string fileName = "";
    private StreamWriter fileWriter = null;
    private bool updateSliderRange = true;
    private const int MAX_ENDPOINTS = 10;
    private int endpointCount = 0;
    private Vector3[] endpointPositions = new Vector3[100];
    private float setHLimbKinUALength = 0.0f;
    private float setHLimbKinFALength = 0.0f;
    private float setHLimbKinShPosZ = 0.0f;
    // Flag to check the human limb kinematic parameters.
    private bool hLimbKinParamCheckFlag = false;
    private bool hLimbKinParamSetDone = false;
    // Dynamic parameter estimation variables.
    private enum LimbDynEstState
    {
        WAITFORSTART = 0x00,
        RECORDING = 0x01,
        HOLDTEST = 0x02,
        DONE = 0x03
    }
    private LimbDynEstState hLimbDynEstState = LimbDynEstState.WAITFORSTART;
    private List<float> hAngle1 = new List<float>();
    private List<float> hAngle2 = new List<float>();
    private List<float> hAngle3 = new List<float>();
    private List<float> torque = new List<float>();
    private float uaW, faW;

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
    }

    private void onNewMarsData()
    {
        // Write row to the file if logging is enabled.
        if (fileWriter != null)
        {
            fileWriter.WriteLine($"{MarsComm.runTime},{MarsComm.packetNumber},{MarsComm.status},{MarsComm.errorString},{MarsComm.limb},{MarsComm.calibration},{MarsComm.limbKinParam},{MarsComm.limbDynParam},{MarsComm.angle1},{MarsComm.angle2},{MarsComm.angle3},{MarsComm.angle4},{MarsComm.force},{MarsComm.xEndpoint},{MarsComm.yEndpoint},{MarsComm.zEndpoint},{MarsComm.imu1Angle},{MarsComm.imu2Angle},{MarsComm.imu3Angle},{MarsComm.marButton},{MarsComm.calibButton}");
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
            // Get the human limb ki
            MarsComm.getHumanLimbKinParams();
            // Set the check flag to verify human limb kinematic parameters are set.
            hLimbKinParamCheckFlag = true;
        }
        else if (tglHLimbDynParamSet.isOn)
        {
            // Check the current state and react.
            switch (hLimbDynEstState)
            {
                case LimbDynEstState.WAITFORSTART:
                    // Start recording human limb angles and torque.
                    hLimbDynEstState = LimbDynEstState.RECORDING;
                    hAngle1.Clear();
                    hAngle2.Clear();
                    hAngle3.Clear();
                    torque.Clear();
                    Debug.Log("Human limb dynamic parameter estimation started.");
                    break;
                case LimbDynEstState.RECORDING:
                    // Stop recording and set the parameters.
                    Debug.Log($"Estimated human limb dynamic parameters: UA Weight={uaW}, FA Weight={faW}");
                    // MarsComm.setHumanLimbDynParams(uaW, faW);
                    hLimbDynEstState = LimbDynEstState.HOLDTEST;
                    break;
                case LimbDynEstState.HOLDTEST:
                    Debug.Log($"Hold Testing with the Weight Support Control.");
                    break;
            }
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
        if (MarsComm.uaLength != setHLimbKinUALength || MarsComm.faLength != setHLimbKinFALength || MarsComm.shPosZ != setHLimbKinShPosZ)
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

        // Set target button click.
        btnSetTarget.onClick.AddListener(delegate { OnTargetSet(); });

        // Listen to MARS's event
        MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
        MarsComm.OnCalibButtonReleased += onCalibButtonReleased;
        MarsComm.OnNewMarsData += onNewMarsData;
        MarsComm.OnControlModeChange += onControlModeChange;
        MarsComm.onHumanLimbKinParamData += OnHumanLimbKinParamData;
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
                sldrTarget.minValue = 0f;
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
            ""
        });
        string sensorText = String.Join("\n", new string[] {
            $"Robot Angles  : {robotAngles}",
            $"IMU Angles    : {imuAngles}",
            $"Human Angles  : {hlimAngles}",
            $"Endpoint Pos  : {epPos}",
            $"Force         : {MarsComm.force.ToString(FLOAT_FORMAT)}",
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
            fileWriter.WriteLine("runTime,packetNumber,status,errorString,limb,calib,limbkinparam,limbdynparam,angle1,angle2,angle3,angle4,force,epx,epy,epz,imuangle1,imuangle2,imuangle3,marbtn,calibbtn");
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
        MarsComm.setControlTarget(sldrTarget.value);
        Debug.Log($"Control target set to: {sldrTarget.value}");
    }

    private void OnApplicationQuit()
    {
        Application.Quit();
        JediComm.Disconnect();
    }
}
