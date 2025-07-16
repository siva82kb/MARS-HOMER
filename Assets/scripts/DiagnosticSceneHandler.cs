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

    private static string FLOAT_FORMAT = "+0.00;-0.00";
    private string fileName = "";
    private StreamWriter fileWriter = null;

    void Start()
    {
         // Connect to the robot.
        ConnectToRobot.Connect(AppData.COMPort);

        // Initialize UI
        InitializeUI();
        // Attach callbacks
        AttachControlCallbacks();

        // Get device version.
        MarsComm.getVersion();
        MarsComm.startSensorStream();
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
        // This method is called when the calibration button is released
        Debug.Log("Calibration button released.");
    }


    private void InitializeUI()
    {
        // Fill dropdown list
        ddLimbType.ClearOptions();
        ddLimbType.AddOptions(MarsComm.LIMBTEXT.ToList());
        ddControlType.AddOptions(MarsComm.CONTROLTYPETEXT.ToList());
        // List<string> _ctrlList = MarsComm.CONTROLTYPETEXT.ToList();
        // ddControlSelect.AddOptions(_ctrlList.Take(_ctrlList.Count - 1).ToList());
        // Clear panel selections.
        tglLogData.enabled = true;
        tglLogData.isOn = false;
    }

    public void AttachControlCallbacks()
    {
        // Dropdown value change.
        ddLimbType.onValueChanged.AddListener(delegate { OnLimbTypeChange(); });
        ddControlType.onValueChanged.AddListener(delegate { OnControlTypeChange(); });

        // Calibrate button click.
        btnCalibrate.onClick.AddListener(delegate {
            // Send calibration command to MARS.
            MarsComm.calibrate();
            Debug.Log("Calibration command sent.");
        });

        // Toggle button
        tglLogData.onValueChanged.AddListener(onDataLogChange);
        // tglCalibSelect.onValueChanged.AddListener(delegate { OnCalibrationChange(); });
        // tglControlSelect.onValueChanged.AddListener(delegate { OnControlChange(); });
        // tglDataLog.onValueChanged.AddListener(delegate { OnDataLogChange(); });

        // // Dropdown value change.
        // ddControlSelect.onValueChanged.AddListener(delegate { OnControlModeChange(); });

        // // Slider value change.
        // sldrTarget.onValueChanged.AddListener(delegate { OnControlTargetChange(); });
        // sldrCtrlBound.onValueChanged.AddListener(delegate { OnControlBoundChange(); });
        // sldrCtrlGain.onValueChanged.AddListener(delegate { OnControlGainChange(); });

        // // Button click.
        // btnNextRandomTarget.onClick.AddListener(delegate { OnNextRandomTarget(); });

        // // AAN Demo Button click.
        // btnAANDemo.onClick.AddListener(delegate { OnAANDemoSceneLoad(); });

        // Listen to MARS's event
        MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
        MarsComm.OnCalibButtonReleased += onCalibButtonReleased;
        MarsComm.OnNewMarsData += onNewMarsData;
    }

    public void UpdateUI()
    {
        // Check if MARS has been calibrated.
        btnCalibrate.enabled = (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB")
            && (MarsComm.LIMBTYPE[ddLimbType.value] != "NOLIMB");
        // Enable control type dropdown if limb is selected.
        ddControlType.interactable = MarsComm.CALIBRATION[MarsComm.calibration] == "YESCALIB";
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
            $"Status        : {MarsComm.OUTDATATYPE[MarsComm.status], -15} | Error : {MarsComm.errorString}",
            $"Calib         : {MarsComm.CALIBRATION[MarsComm.calibration]}",
            $"Limb          : {MarsComm.LIMBTYPE[MarsComm.limb], -15} | {MarsComm.LIMBKINPARAM[MarsComm.limbKinParam], -15} | {MarsComm.LIMBDYNPARAM[MarsComm.limbDynParam]}",
            $"Control       : {MarsComm.CONTROLTYPE[MarsComm.controlType]}",
            "",
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
            $"MARS Button   : {MarsComm.marButton}",
            $"CALIB Button  : {MarsComm.calibButton}"
        });

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

    private void OnApplicationQuit()
    {
        Application.Quit();
        JediComm.Disconnect();
    }
}
