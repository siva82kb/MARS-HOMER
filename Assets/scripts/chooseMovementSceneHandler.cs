using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
using System;
using UnityEngine.Rendering.Universal;
using static SetUpMars;
using System.Web;
using System.Runtime.CompilerServices;


public class MovementSceneHandler : MonoBehaviour
{
    //ui related variables
    public GameObject movementSelectGroup;
    public Text message;

    public static float initialAngle;
    private string nextScene;
    //flags
    private static bool changeScene = false;
    private bool toggleSelected = false;
   
    //OTHER SCENES
    public readonly string marsSetupScene = "MARSSETUP";
    public readonly string robotCalibScene = "ROBOTCALIB";
    private string exitScene = "SUMMARY";

    private string assessmentSceneML = "AROMML";
    private string assessmentSceneAP = "AROMAP";
    private string assessmentSceneMLAP = "AROMMLAP";
    private string trainingPlaneScene = "CHOOSEPLANE";
    private string marsSetUp = "MARSSETUP";
    

    void Start()
    {
        MarsComm.sendHeartbeat();

        // Initialize AppData if needed
        if (AppData.Instance.userData == null)
        {
            AppData.Instance.Initialize(SceneManager.GetActiveScene().name);
        }

        // Check if the directory exists
        if (!Directory.Exists(DataManager.basePath)) Directory.CreateDirectory(DataManager.basePath);
        if (!File.Exists(DataManager.configFile)) SceneManager.LoadScene("CONFIG");

        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

        // If the robot is not calibrated go to the robot calib scene.
        if (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB")
        {
            SceneManager.LoadScene(robotCalibScene);
        }
        // If the robot is not in position control go to the mars setup scene.
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] != "POSITION")
            SceneManager.LoadScene(marsSetUp);

        // Attach the MARSComm callbacks.
        MarsComm.OnMarsButtonReleased += OnMarsButtonReleased;

        // Update Session Details
        AppData.Instance.updateSessionDetails();

        // Initialize GUI
        UpdateMovementToggleButtons();
        StartCoroutine(DelayedAttachListeners());

        // Clear the message text.
        message.text = "Please select the movement";
    }

    void Update()
    {
        MarsComm.sendHeartbeat();
        // Check if the magic key combination is pressed for AROM assessment 
        // or training plane selection.
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.A))
        {
            if (nextScene == "")
            {
                message.text = "Please select the movement";
                changeScene = false;
            }
            else
            {
                changeScene = true;
            }
        }
        else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
        {
            // Switch to the training plane scene.
            nextScene = trainingPlaneScene;
            changeScene = true;
        }
        //Check if a scene change is needed.
        if (changeScene == true && nextScene != "")
        {
            LoadNextScene();
            changeScene = false;
        }
    }
    public class idle
    {
        float previosAngle;
        bool istarted;
        float timer = 500;
        public void checkMarsIde()
        {
            if (istarted == false) return;
            timer -= Time.deltaTime;
         
            if (previosAngle == MarsComm.angle1 && MarsComm.force < 10)
            {

            }
            previosAngle = MarsComm.angle1;
        }
        public void reset()
        {
            istarted = false;
            timer = 500;
        }
    }

    private void UpdateMovementToggleButtons()
    {
        foreach (Transform child in movementSelectGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            bool isPrescribed = AppData.Instance.userData.moveTimePrsc[toggleComponent.name] > 0;
            // Hide the component if it has no prescribed time.
            toggleComponent.interactable = isPrescribed;
            toggleComponent.gameObject.SetActive(isPrescribed);
            // Update the time trained in the timeLeft component of toggleCompoent.
            Transform timeLeftTransform = toggleComponent.transform.Find("timeLeft");
            if (timeLeftTransform != null)
            {
                // Get the TextMeshPro component from the timeLeft GameObject
                TextMeshProUGUI timeLeftText = timeLeftTransform.GetComponent<TextMeshProUGUI>();
                if (timeLeftText != null)
                {
                    // Set the text to your desired value
                    timeLeftText.text = $"{AppData.Instance.userData.getTodayMoveTimeForMovement(toggleComponent.name)} / {AppData.Instance.userData.moveTimePrsc[toggleComponent.name]} min";
                }
                else
                {
                    Debug.LogError("TextMeshProUGUI component not found in timeLeft GameObject.");
                }
            }
            else
            {
                Debug.LogError("timeLeft GameObject not found in " + toggleComponent.name);
            }
        }
    }
  
    IEnumerator DelayedAttachListeners()
    {
        yield return new WaitForSeconds(1f);  
        AttachToggleListeners();
    }

    void AttachToggleListeners()
    {
        foreach (Transform child in movementSelectGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            if (toggleComponent != null)
            {
                toggleComponent.onValueChanged.AddListener(delegate { CheckToggleStates(); });
            }
        }
    }

    void CheckToggleStates()
    {
        foreach (Transform child in movementSelectGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            if (toggleComponent != null && toggleComponent.isOn)
            {
                // One of the toggle buttons is selected.
                toggleSelected = true;
                // Selected movement and game name.
                AppData.Instance.SetMovement(child.name);
                AppData.Instance.SetGame(AppData.MARS_GAMES[MarsDefs.getMovementIndex(child.name)]);
                // Check if assessment is done, else the next scene will be the corresponding assessment scene.
                if (AppData.Instance.selectedMovement.currentArom == null)
                {
                    // Next is an assessment scene;
                    nextScene = AppData.Instance.selectedMovement.name == "ML" ? assessmentSceneML :
                                AppData.Instance.selectedMovement.name == "AP" ? assessmentSceneAP :
                                AppData.Instance.selectedMovement.name == "MLAP" ? assessmentSceneMLAP : "";
                    message.text = "Press Mars Button to start assessment";
                }
                else
                {
                    // Next is the game scene.
                    nextScene = AppData.MARS_GAMES_SCENES[MarsDefs.getMovementIndex(child.name)];
                    message.text = "Press Mars Button to start game";
                }
                AppLogger.LogInfo($"Selected movement ({AppData.Instance.selectedMovement.name}) and game ({AppData.Instance.selectedGame})");
                break;
            }
        }
    }
    
    public void OnMarsButtonReleased()
    {
        if (toggleSelected && MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION")
        {
            changeScene = true;
            toggleSelected = false;
        }
        else
        {
            Debug.LogWarning("Select at least one toggle to proceed.");
        }
    }

    void LoadNextScene()
    {
        AppLogger.LogInfo($"Switching scene to '{nextScene}'.");
        SceneManager.LoadScene(nextScene);
    }
    
    IEnumerator LoadSummaryScene()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(exitScene);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
   
    public void OnExitButtonClicked()
    {
        Debug.Log("exitbutton");
        StartCoroutine(LoadSummaryScene());
    }

    private void OnDestroy()
    {
        MarsComm.OnMarsButtonReleased -= OnMarsButtonReleased;
    }

    private void OnApplicationQuit()
    {
        MarsComm.OnMarsButtonReleased -= OnMarsButtonReleased;
    }
}

