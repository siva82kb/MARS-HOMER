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
        JediComm.ConnectToRobot("COM7");
    }

    private void Update()
    {
        updateTextBox();
        byte[] _data = new byte[16];
        Buffer.BlockCopy(AppData.PCParam, 0, _data, 0, _data.Length);
        JediComm.SendMessage(_data);
    }
    public void updateTextBox()
    {
        //Construct the display text while ensuring values are safely converted to string
        string displayText = "Theta1: " + MarsComm.thetaOne.ToString() + "\n" +
                             "Theta2: " + MarsComm.thetatwo.ToString() + "\n" +
                             "Theta3: " + MarsComm.thetaThree.ToString() + "\n" +
                             "Theta4: " + MarsComm.thetafour.ToString() + "\n" +
                             "Force1: " + MarsComm.forceOne.ToString() + "\n" +
                             "Force2: " + MarsComm.forceTwo.ToString() + "\n" +
                             "des1: " + MarsComm.descOne.ToString() + "\n" +
                             "des2: " + MarsComm.descTwo.ToString() + "\n" +
                             "des3: " + MarsComm.descThree.ToString() + "\n" +
                             "PCParam: " + MarsComm.pcParameter.ToString() + "\n" +
                             "Btn_st: " + MarsComm.buttonState.ToString();
        
        UpdateTextBox.text = displayText;
    }

    private void OnApplicationQuit()
    {

        Application.Quit();
        JediComm.Disconnect();
    }
}
