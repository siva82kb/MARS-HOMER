using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
    public Image button;
    public Image arrow;
    public Image rotationmovement;
    public Image countdownBar;
    //BatteryLevelCheck
    BatteryStatus status ;
    float level;

    // Connection timeout — quit if device never connects within this window
    private const float CONNECTION_TIMEOUT = 30f;
    private float connectionTimer = CONNECTION_TIMEOUT;
    private bool hasEverConnected = false;
    private bool connectionTimedOut = false;

    //Idle check
    float previousAngle2;
    float previousAngle3;
    float previousAngle4;
    bool istarted;
    float timer = 60;
    private bool isMarsButtonDisabled = false;
    private bool _previousIsMARS = false;
    private float disconnectDebounce = 0f;
    private const float DISCONNECT_DEBOUNCE = 3f;

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
        // Watchdog: if the reader thread died (IOException), isMARS can stay true
        // with no data arriving. Force it false after MARS_WATCHDOG_TIMEOUT seconds.
        if (ConnectToRobot.isMARS
            && MarsComm.currentTime != default(DateTime)
            && (DateTime.Now - MarsComm.currentTime).TotalSeconds > AppData.MARS_WATCHDOG_TIMEOUT)
        {
            ConnectToRobot.isMARS = false;
            AppLogger.LogWarning($"Watchdog: no packet for {AppData.MARS_WATCHDOG_TIMEOUT}s — marking disconnected.");
        }

        // Debounced disconnect detection — isMARS must be false for DISCONNECT_DEBOUNCE
        // seconds continuously before treating it as a real disconnect.
        // This prevents the initial-connection flicker from falsely triggering SUMMARY.
        if (hasEverConnected)
        {
            if (!ConnectToRobot.isMARS)
            {
                disconnectDebounce += Time.deltaTime;
                if (disconnectDebounce >= DISCONNECT_DEBOUNCE)
                {
                    AppLogger.LogWarning("Bluetooth disconnected.");

                    if (AppData.Instance.trialRawDataFile != null)
                    {
                        AppData.Instance.StopTrialOnDisconnect();
                        AppLogger.LogWarning("Trial stopped on disconnect — data saved.");
                    }

                    AppLogger.LogWarning("Navigating to SUMMARY on disconnect.");
                    UnityEngine.SceneManagement.SceneManager.LoadScene("SUMMARY");
                }
            }
            else
            {
                disconnectDebounce = 0f;
            }
        }

        //to stop marsButton usage
        AppData.isErrorPanelActive = errorPanel.gameObject.activeSelf;
        level = SystemInfo.batteryLevel;      // 0.0 � 1.0   OR -1 if unsupported
        status = SystemInfo.batteryStatus;

    //if level below 30% it show the indication to connect charger
        if (level <= 0.3
            && !errorPanel.gameObject.activeSelf
            && status != BatteryStatus.Charging 
            && !AppData.NeedToDeacitaveMars
            && hasEverConnected)// 30% Battery Level Threshold
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
            &&!AppData.NeedToDeacitaveMars
            && hasEverConnected)
        {
            AppLogger.LogInfo($"Error Panel closed dynamically when device connect with charger | status : {status}");
            errorPanel.SetActive(!errorPanel.gameObject.activeSelf);
            
          
        }

        //Check if Device is in use
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION")checkMarsIdle();

        // Track first successful connection — dismiss connection error panel on connect.
        if (ConnectToRobot.isMARS)
        {
            if (!hasEverConnected && errorPanel.activeSelf)
                errorPanel.SetActive(false);
            hasEverConnected = true;
        }

        // If device never connected, count down and quit.
        string _scene = SceneManager.GetActiveScene().name;
        bool _inSetupScene = _scene == "CONFIG" || _scene == "LOGIN";
        bool _showingConnectionError = !hasEverConnected && !connectionTimedOut && !_inSetupScene;

        if (_showingConnectionError)
        {
            connectionTimer -= Time.deltaTime;
            float _ratio = Mathf.Clamp01(connectionTimer / CONNECTION_TIMEOUT);
            errorPanel.SetActive(true);

            errorTxt.text = "Please switch on the device\nor enable Bluetooth on the laptop.";

            // Progress bar: drains left-to-right, green → yellow → red.
            if (countdownBar != null)
            {
                countdownBar.gameObject.SetActive(true);
                countdownBar.fillAmount = _ratio;
                countdownBar.color = Color.Lerp(Color.red, Color.green, _ratio);
            }

            // Show switch visuals and animate.
            button.gameObject.SetActive(true);
            arrow.gameObject.SetActive(true);
            rotationmovement.gameObject.SetActive(true);

            // Blink arrow at 2 Hz.
            arrow.enabled = Mathf.Sin(Time.time * Mathf.PI * 2f) > 0f;

            // Continuously rotate the rotationmovement image.
            rotationmovement.transform.Rotate(0f, 0f, -90f * Time.deltaTime);

            if (connectionTimer <= 0)
            {
                connectionTimedOut = true;
                CloseAppLogger();
            }
        }
        else
        {
            // Hide switch visuals for all other error states.
            if (countdownBar != null) countdownBar.gameObject.SetActive(false);
            button.gameObject.SetActive(false);
            arrow.gameObject.SetActive(false);
            rotationmovement.gameObject.SetActive(false);
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
            errorTxt.text = "Device is in Idle !.. Please Deactivate Device Or use Device";
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
        if (MCGameController.Instance != null)
        {
            if (MCGameController.Instance.isGameStarted) { MCGameController.Instance.onClickExit(); }
        }
        if(TWGameController.Instance!= null)
        {
            if (TWGameController.Instance.isGameStarted) {  TWGameController.Instance.onClickExit(); }
        }
        //To close the battery power Indication, if there is no power ,we immediatly deactivate the device or Incase of Idle also we deactivate device
        if ( errorPanel.gameObject.activeSelf && MarsComm.errorStatus <= 1&&!hasEverConnected)
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
        #else
                Process.Start("shutdown", "/s /t 0");
        #endif

    }
    


}
