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
        
        AppData.InitializeRobot();
    }

    private void Update()
    {
        updateTextBox();
      
    }
    public void updateTextBox()
    {
        //Construct the display text while ensuring values are safely converted to string
        string displayText = "Theta1: " + MarsComm.angleOne.ToString() + "\n" +
                             "Theta2: " + MarsComm.angleTwo.ToString() + "\n" +
                             "Theta3: " + MarsComm.angleThree.ToString() + "\n" +
                             "Theta4: " + MarsComm.angleFour.ToString() + "\n" +
                             "Force1: " + MarsComm.forceOne.ToString() + "\n" +
                             "Force2: " + MarsComm.forceTwo.ToString() + "\n" +
                             "des1: " + MarsComm.desOne.ToString() + "\n" +
                             "des2: " + MarsComm.desTwo.ToString() + "\n" +
                             "des3: " + MarsComm.desThree.ToString() + "\n" +
                             "dataSendToRobot: " + MarsComm.pcParameter.ToString() + "\n" +
                             "Btn_st: " + MarsComm.buttonState.ToString();
        
        UpdateTextBox.text = displayText;
    }

    private void OnApplicationQuit()
    {

        Application.Quit();
        JediComm.Disconnect();
    }
}
