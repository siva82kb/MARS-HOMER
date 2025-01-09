using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.IO;

public class HalfWeightSupportROM : MonoBehaviour
{

    List<Vector3> paths;
    public GameObject messagePanel;
    float time, startTime, decayTime = 5.0f, support, supporti, startSupport, startDecay = 0, endSupport;


    void Awake()
    {
       
    }


    void Start()
    {

       messagePanel.SetActive(false);


    }

    public void Update()
    {
        time += Time.deltaTime;

        if (startDecay == 1)
        {
            if (time - startTime < decayTime)
            {
                supporti = startSupport + (time - startTime) / decayTime * (endSupport - startSupport);
                MarsComm.SUPPORT = supporti;
                AppData.dataSendToRobot = new float[] { MarsComm.SUPPORT, 0.0f, 2006, 0.0f };
               
            }
            else
            {
                startDecay = 0;
                NoWeightSupport();
            }
        }
    }
   
    public void onclick_recalibrate()
    {
        SceneManager.LoadScene("halfWeightSupportScene");

    }

    public void onclickNoweightSupport()
    {
        endSupport = 0.0f;
        startTime = time;
        startSupport = MarsComm.SUPPORT;
        startDecay = 1;
        messagePanel.SetActive(true);
    }

    public void NoWeightSupport()
    {
        MarsComm.SUPPORT = MarsComm.SUPPORT_CODE[0];
        AppData.dataSendToRobot = new float[] {MarsComm.SUPPORT, 0.0f, 2006, 0.0f };
        SceneManager.LoadScene("noWeightSupportScene");
    }

    private void OnApplicationQuit()
    {

        Application.Quit();
        JediComm.Disconnect();
    }
}
