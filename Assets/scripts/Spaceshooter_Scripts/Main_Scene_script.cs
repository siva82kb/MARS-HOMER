using NeuroRehabLibrary;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Main_Scene_script : MonoBehaviour
{
    private GameSession currentGameSession;

    // Start is called before the first frame update
    void Start()
    {
        // Attach PLUTO button event
        //MarsComm.OnButtonReleased += OnPlutoButtonReleased;
        StartNewGameSession();
    }

    // Update is called once per frame
    void Update()
    {

    }
    void StartNewGameSession()
    {
        currentGameSession = new GameSession
        {
            GameName = "Tuk-Tuk",
            Assessment = 0 // Example assessment value, 
                           //If this script for calibration and assessment then Assessment=1. 
        };
        SessionManager.Instance.StartGameSession(currentGameSession);
        SetSessionDetails();
    }

    public void SetSessionDetails()
    {
        string device = "MARS"; // Set the device name
        string assistMode = "Null"; // Set the assist mode
        string assistModeParameters = "Null"; // Set the assist mode parameters
        string deviceSetupLocation = DataManager.filePathConfigData;
        SessionManager.Instance.SetDevice(device, currentGameSession);
        SessionManager.Instance.SetAssistMode(assistMode, assistModeParameters, currentGameSession);
        SessionManager.Instance.SetDeviceSetupLocation(deviceSetupLocation, currentGameSession);
    }
    void EndCurrentGameSession()
    {
        if (currentGameSession != null)
        {
            string trialDataFileLocation = "pass_trial_data_location";
            string gameParameter = "pass_game_parameter";
            SessionManager.Instance.SetGameParameter(gameParameter, currentGameSession);
            SessionManager.Instance.SetTrialDataFileLocation(trialDataFileLocation, currentGameSession);
            SessionManager.Instance.EndGameSession(currentGameSession);
        }
    }

    public void onClick_gotoLevel()
    {
        SceneManager.LoadScene("space_shooter_level");
    }
    public void onclick_gotochoose()
    {
        SceneManager.LoadScene("chooseMovementScene");
    }
}
