using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class connectStatusHandler : MonoBehaviour
{
    private Image connectStatus;
    private GameObject loading;
    public Button closePanel;
    public GameObject errorPanel;
    public TextMeshProUGUI errorTxt;
    private TextMeshProUGUI statusText;

    //BatteryLevelCheck
    BatteryStatus status ;
    float level;

    //Idle check
    float previousAngle2;
    float previousAngle3;
    float previousAngle4;
    bool istarted;
    float timer = 60;

    void Awake()
    {
        // Subscribe to shutdown events once per instance
        Application.quitting += CloseApploggQuit; //for Exe file
        AppDomain.CurrentDomain.ProcessExit += (_, __) => closeApploggCrash(); // for external crash like OS Crash
  
        #if UNITY_EDITOR
                EditorApplication.quitting += CloseApploggQuit; //for editor
        #endif
    }
    // Start is called before the first frame update
    void Start()
    {
        connectStatus = GetComponent<Image>(); // Uncomment if connectStatus is on the same GameObject
        loading = transform.Find("loading").gameObject; // Assuming loading is a child GameObject

        statusText = transform.Find("statusText").GetComponent<TextMeshProUGUI>();
        closePanel.onClick.AddListener(delegate { closeApploggErrorPanelCloseBtn(); });
        if (AppData.Instance != null)return;
        if (AppData.Instance.userData.isErrorOccurred())
        {
            if (SceneManager.GetActiveScene().name == "DIAGNOSTICS") return;
            errorPanel.SetActive(true);
        }
        AppLogger.LogInfo($"Starting Device with a Battery level of  | level : {SystemInfo.batteryLevel*100}%");
       
    }

 

    // Update is called once per frame
    void Update()
    {
        level = SystemInfo.batteryLevel;      // 0.0 � 1.0   OR -1 if unsupported
        status = SystemInfo.batteryStatus;

        //if level below 30% it show the indication to connect charger
        if (level <= 0.3
            && !errorPanel.gameObject.activeSelf
            && status != BatteryStatus.Charging 
            && !AppData.NeedToDeacitaveMars)// 30% Battery Level Threshold
        {
            errorPanel.SetActive(true);
            AppLogger.LogInfo($"Error Below BatteryLevel   | level : {SystemInfo.batteryLevel * 100}%");
            errorTxt.text = $"Battery Low{level * 100}%Please Connect the Charger\nor click Close ,To Deactivate Device";
        }

        //if Battery connected after the indication shown, Indication disappear Dynamically
        if(status == BatteryStatus.Charging 
            && level <= 0.3
            && errorPanel.gameObject.activeSelf 
            && MarsComm.errorStatus<=1
            &&!AppData.NeedToDeacitaveMars)
        {
            AppLogger.LogInfo($"Error Panel closed dynamically when device connect with charger | status : {status}");
            errorPanel.SetActive(!errorPanel.gameObject.activeSelf);
        }

        //Check if Device is in use
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION")checkMarsIdle();

        // Update connection status
        if (ConnectToRobot.isMARS)
        {
            connectStatus.color = Color.green;
            loading.SetActive(false);
            statusText.text = $"{MarsComm.version}\n[{MarsComm.frameRate:F1}Hz]";

        }
        else
        {
            connectStatus.color = Color.red;
            loading.SetActive(true);
            statusText.text = "Not connected";

        }
        if (MarsComm.errorStatus != 0 && MarsComm.errorStatus != 1)
        {
            if (SceneManager.GetActiveScene().name == "DIAGNOSTICS") return;
            errorTxt.text = "Device has issue. Call the Engineers.";
            errorPanel.SetActive(true);
        }
           
    }
    //check Device Idle by Angle 2 and Force values
    public void checkMarsIdle()
    {
        //countDown
        if (istarted&&!errorPanel.gameObject.activeSelf) timer -= Time.deltaTime;


        if (previousAngle2 == MarsComm.angle2 &&
            previousAngle3 == MarsComm.angle3 &&
            previousAngle4 == MarsComm.angle4 &&
            MarsComm.force < 10 )
        {

            if (!istarted && !errorPanel.gameObject.activeSelf) istarted = true;


        }
        else
        {
            if (istarted)
            {
                istarted = false;
                timer = 60;
                if (errorPanel.gameObject.activeSelf) errorPanel.SetActive(false);
            }
           
        }
        previousAngle2 = MarsComm.angle2;
        previousAngle3 = MarsComm.angle3;
        previousAngle4 = MarsComm.angle4;

        if (timer < 0 && !errorPanel.gameObject.activeSelf)
        {
            AppLogger.LogInfo("Device is in Idle");
            errorTxt.text = "Device is in Idle !.. Please Deactivate Device";
            errorPanel.SetActive(true);
        }
    }
    private void CloseApploggQuit()
    {
        AppLogger.LogInfo($"CloseAppLogger Trigger on Quit function");
        CloseAppLogger();
    }
    private void closeApploggCrash()
    {
        AppLogger.LogInfo($"CloseAppLogger Trigger on ApplicationCrash");
        CloseAppLogger();
    }
    private void closeApploggErrorPanelCloseBtn()
    {
        AppLogger.LogInfo($"CloseAppLogger Trigger on Error Panel Close Button");
        CloseAppLogger();
    }
    private void CloseAppLogger()
    {
        AppLogger.LogInfo($"closingApplogger SceneName : {SceneManager.GetActiveScene().name}");
        //Ensure while running Game ,the log file should closed Properly
        if (SpaceShooterGameContoller.Instance != null)
        {
            if (SpaceShooterGameContoller.Instance.isGameStarted)
            {
               
                SpaceShooterGameContoller.Instance.onClickExit();

            }
        }
        if (pongGameController.Instance != null)
        {
            if (pongGameController.Instance.isGameStarted)
            {
                pongGameController.Instance.ExitGame();

            }
        }
        if (FlappyGameControl.Instance != null)
        {
            if (FlappyGameControl.Instance.isGameStarted)
            {
                FlappyGameControl.Instance.ExitGame();
            }
        }
        if (DCGameController.Instance != null)
        {
            if (DCGameController.Instance.isGameStarted)
            {

                DCGameController.Instance.onClickExit();

            }
        }
        //To close the battery power Indication, if there is no power ,we immediatly deactivate the device or Incase of Idle also we deactivate device
        if ( errorPanel.gameObject.activeSelf && MarsComm.errorStatus <= 1)
        {
            AppLogger.LogInfo($"Error Panel Closing by pressing close Button  | status : {status}");
            errorPanel.SetActive(false);
            
            if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION")
            {
                AppData.NeedToDeacitaveMars = true;
                SceneManager.LoadScene("MARSSETUP");
                return;
            }
           
        }
       
        JediComm.Disconnect();
        AppLogger.StopLogging();
        MarsCommLogger.StopLogging();

        Application.Quit();
            #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false; // Stop play mode if in editor
            #endif
      
    }
   
       

}
