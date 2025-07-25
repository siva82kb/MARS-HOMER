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

    private static string FLOAT_FORMAT = "+0.00;-0.00";
    private string fileName = "";
    private StreamWriter fileWriter = null;
    private bool updateSliderRange = true;

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
        // Do something only when the human limb kinematic parameters toggle is enabled.
        if (!tglHLimbKinParamSet.isOn)
        {
            // If the toggle is not enabled, return early.
            return;
        }
        // Get the current endpoint position from MARS robot.
        Debug.Log(MarsKinDynamics.ForwardKinematicsExtended(MarsComm.angle1, MarsComm.angle2, MarsComm.angle3, MarsComm.angle4));
    }

    private void onControlModeChange()
    {
        // This method is called when the control mode is changed
        Debug.Log($"Control mode changed to: {MarsComm.CONTROLTYPE[MarsComm.controlType]}");
        // Set the update slider flag for the main thread to update the UI.
        updateSliderRange = true;
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
        // tglCalibSelect.onValueChanged.AddListener(delegate { OnCalibrationChange(); });
        // tglControlSelect.onValueChanged.AddListener(delegate { OnControlChange(); });
        // tglDataLog.onValueChanged.AddListener(delegate { OnDataLogChange(); });

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
        sldrHLimbUALength.onValueChanged.AddListener(delegate {
            hlbUALengthText.text = "UA: " + sldrHLimbUALength.value.ToString(FLOAT_FORMAT) + " m";
        });
        sldrHLimbFALength.onValueChanged.AddListener(delegate {
            hlbFALengthText.text = "FA: " + sldrHLimbFALength.value.ToString(FLOAT_FORMAT) + " m";
        });

        // Set target button click.
        btnSetTarget.onClick.AddListener(delegate { OnTargetSet(); });

        // Human limb kin parameters button click.
        // btnSetHLimbKinParams.onClick.AddListener(delegate {
        //     // Set human limb kinematic parameters.
        //     // MarsComm.setHumanLimbKinParams(sldrHLimbUALength.value, sldrHLimbFALength.value);
        //     Debug.Log($"Human limb kinematic parameters set: UALength={sldrHLimbUALength.value}, FALength={sldrHLimbFALength.value}");
        // });

        // Listen to MARS's event
        MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
        MarsComm.OnCalibButtonReleased += onCalibButtonReleased;
        MarsComm.OnNewMarsData += onNewMarsData;
        MarsComm.OnControlModeChange += onControlModeChange;
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
            $"Time          : {MarsComm.currentTime}",
            $"Dev ID        : {MarsComm.deviceId}",
            $"F/W Version   : {MarsComm.version}",
            $"Compile Date  : {MarsComm.compileDate}",
            "",
            $"Device Time   : {runT} | Packet Number: {packNo}",
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
        string sensorText = String.Join("\n", new string[] {
            $"Robot Angles  : {robotAngles}",
            $"IMU Angles    : {imuAngles}",
            $"Endpoint Pos  : {epPos}",
            $"Force         : {MarsComm.force.ToString(FLOAT_FORMAT)}",
            $"Target        : {MarsComm.target.ToString(FLOAT_FORMAT)}",
            $"Desired       : {MarsComm.desired.ToString(FLOAT_FORMAT)}",
            $"Control       : {MarsComm.control.ToString(FLOAT_FORMAT)}",
            $"MARS Button   : {MarsComm.marButton}",
            $"CALIB Button  : {MarsComm.calibButton}"
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
        dataDisplayText.text = stateText + sensorText;
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
