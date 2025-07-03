using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;

public class DiagnosticSceneHandler : MonoBehaviour
{
    public TMP_Text UpdateTextBox;
    private static string FLOAT_FORMAT = "+0.00;-0.00";

    void Start()
    {
         // Connect to the robot.
        ConnectToRobot.Connect(AppData.COMPort);

        // Listen to MARS's event
        MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
        MarsComm.OnCalibButtonReleased += onCalibButtonReleased;
        MarsComm.OnNewMarsData += onNewMarsData;
    }

    private void Update()
    {
        updateTextBox();
    }

    private void onNewMarsData()
    {
        Debug.Log("New MARS data received.");
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

    public void updateTextBox()
    {
        // State text
        string runT = MarsComm.runTime.ToString("0.000").PadRight(10);
        string packNo = MarsComm.packetNumber.ToString().PadRight(8);
        string stateText = string.Join("\n", new string[] {
            $"Device Time   : {runT} | Packet Numnber: {packNo}",
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
        string imuAngles = string.Join(" ", new string[] {
            MarsComm.imu1Angle.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.imu2Angle.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.imu3Angle.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.imu4Angle.ToString(FLOAT_FORMAT).PadRight(8),
        });
        string sensorText = String.Join("\n", new string[] {
            $"Robot Angles  : {robotAngles}",
            $"IMU Angles    : {imuAngles}",
            $"Force         : {MarsComm.force.ToString(FLOAT_FORMAT)}",
            $"MARS Button   : {MarsComm.marButton}",
            $"CALIB Button  : {MarsComm.calibButton}"
        });

        UpdateTextBox.text = stateText + sensorText;
    }

    private void OnApplicationQuit()
    {
        Application.Quit();
        JediComm.Disconnect();
    }
}
