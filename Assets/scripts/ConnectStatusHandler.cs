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
    BatteryStatus status ;
    float level;
    void Awake()
    {
        // Subscribe to shutdown events once per instance
        Application.quitting += CloseAppLogger; //for Exe file
        AppDomain.CurrentDomain.ProcessExit += (_, __) => CloseAppLogger(); // for external crash like OS Crash

        #if UNITY_EDITOR
                EditorApplication.quitting += CloseAppLogger; //for editor
        #endif
    }
    // Start is called before the first frame update
    void Start()
    {

        connectStatus = GetComponent<Image>(); // Uncomment if connectStatus is on the same GameObject
        loading = transform.Find("loading").gameObject; // Assuming loading is a child GameObject

        statusText = transform.Find("statusText").GetComponent<TextMeshProUGUI>();
        closePanel.onClick.AddListener(delegate { CloseAppLogger(); });
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
        level = SystemInfo.batteryLevel;      // 0.0 – 1.0   OR -1 if unsupported
        status = SystemInfo.batteryStatus;

        //if level below 30% it show the indication to connect charger
        if (level < 0.3 && !errorPanel.gameObject.activeSelf && status != BatteryStatus.Charging)// 30% Battery Level Threshold
        {
            errorPanel.SetActive(true);
            AppLogger.LogInfo($"Error Below BatteryLevel   | level : {SystemInfo.batteryLevel * 100}%");
            errorTxt.text = $"Battery Low{level * 100}%Please Connect the Charger";
        }
        //if Battery connected after the indication shown, Indication disappear Dynamically
        if(status == BatteryStatus.Charging && level <= 0.3 && errorPanel.gameObject.activeSelf && MarsComm.errorStatus != 0 && MarsComm.errorStatus != 1)
        {
            AppLogger.LogInfo($"closed dynamically when device connect with charger | status : {status}");
            errorPanel.SetActive(!errorPanel.gameObject.activeSelf);
        }

        
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
            //if (SceneManager.GetActiveScene().name == "DIAGNOSTICS") return;
            errorTxt.text = "Device has issue. Call the Engineers.";
            errorPanel.SetActive(true);
        }
           
    }
   
    private void CloseAppLogger()
    {
        //Optional To close the Connect charger Indication
        if( status == BatteryStatus.Charging && errorPanel.gameObject.activeSelf && MarsComm.errorStatus != 0 && MarsComm.errorStatus != 1)
        {
            AppLogger.LogInfo($"Closing by pressing close Button  | status : {status}");
            errorPanel.SetActive(false);
            return;
        }
        //Ensure while running Game ,the log file should closed Properly
        if (SpaceShooterGameContoller.Instance != null)
        {
            if (SpaceShooterGameContoller.Instance.IsGamePlaying())
            {
                SpaceShooterGameContoller.Instance.onClickExit();

            }
        }
        if(pongGameController.Instance != null)
        {
            if (pongGameController.Instance.isGamePlaying)
            {
                pongGameController.Instance.ExitGame();
               
            }
        }
        if(DCGameController.Instance != null)
        {
            if (DCGameController.Instance.isGamePlaying)
            {

                DCGameController.Instance.onClickExit();
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
