using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;

public class DiagnosticSceneHandler : MonoBehaviour
{
    public TMP_Text UpdateTextBox;

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
        // This method is called when new data is received from MARS
        // You can handle the new data here if needed
        Debug.Log("New data received from MARS.");
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
        //Construct the display text while ensuring values are safely converted to string
        // string displayText = "Theta1        : " + MarsComm.angle1.ToString() + "\n" +
        //                      "Theta2        : " + MarsComm.angle2.ToString() + "\n" +
        //                      "Theta3        : " + MarsComm.angle3.ToString() + "\n" +
        //                      "Theta4        : " + MarsComm.angle4.ToString() + "\n" +
        //                      "IMUAng1       : " + MarsComm.imuAng1.ToString() + "\n" +
        //                      "IMUAng2       : " + MarsComm.imuAng2.ToString() + "\n" +
        //                      "IMUAng3       : " + MarsComm.imuAng3.ToString() + "\n" +
        //                      "IMUAng4       : " + MarsComm.imuAng4.ToString() + "\n" +
        //                      "Force1        : " + MarsComm.force.ToString() + "\n" +
        //                      "calibBtnState : " + MarsComm.calibButton.ToString() + "\n" +

        //                      "des1          : " + MarsComm.desOne.ToString() + "\n" +
        //                      "des2          : " + MarsComm.desTwo.ToString() + "\n" +
        //                      "des3          : " + MarsComm.desThree.ToString() + "\n" +
        //                      "dataSndToRobot: " + MarsComm.pcParameter.ToString() + "\n" +
        //                      "shoulderPosX  : " + MarsComm.shPosX.ToString() + "\n" +
        //                      "shoulderPosy  : " + MarsComm.shPosY.ToString() + "\n" +
        //                      "shoulderPosz  : " + MarsComm.shPosZ.ToString() + "\n" +
        //                      "lenUpperArm   : " + MarsComm.lenUpperArm.ToString() + "\n" +
        //                      "lenLowerArm   : " + MarsComm.lenUpperArm.ToString() + "\n" +
        //                      "weight1       : " + MarsComm.lenUpperArm.ToString() + "\n" +
        //                      "weight2       : " + MarsComm.lenUpperArm.ToString() + "\n" +

        //                      //$"IMU1         :  aX - {MarsComm.imu1aX.ToString()}    aY - {MarsComm.imu1aY.ToString()}   aZ - {MarsComm.imu1aZ}" + "\n" +
        //                      //$"IMU2         :  aX - {MarsComm.imu2aX.ToString()}    aY - {MarsComm.imu2aY.ToString()}   aZ - {MarsComm.imu2aZ}" + "\n" +
        //                      //$"IMU3         :  aX - {MarsComm.imu3aX.ToString()}    aY - {MarsComm.imu3aY.ToString()}   aZ - {MarsComm.imu3aZ}" + "\n" +
        //                      "Btn_st        : " + MarsComm.buttonState.ToString();

        // UpdateTextBox.text = displayText;
    }

    private void OnApplicationQuit()
    {
        Application.Quit();
        JediComm.Disconnect();
    }
}
