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
    private string assessmentScene = "ASSESSROM 1";
    private string chooseTPscene = "CHOOSEPLANE";
    private string marsSetUp = "MARSSETUP";


    //Game names
    public static string[] selectGame = { "space_shooter_home", "pong_game", "FLYINGSHEEP" };
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

        // IF the robot is not calibrated go to the robot calib scene.
        if (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB")
        {
            SceneManager.LoadScene(robotCalibScene);
        }
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] != "POSITION")
            SceneManager.LoadScene(marsSetUp);

        // Attach the MARSComm callbacks.
        MarsComm.OnMarsButtonReleased += OnMarsButtonReleased;
     
        // Update Session Detials
        AppData.Instance.updateSessionDetials();

        UpdateMovementToggleButtons();
        StartCoroutine(DelayedAttachListeners());

        // Clear the message text.
        message.text = "Please Select the Movement !!..";
    }

    void Update()
    {
        MarsComm.sendHeartbeat();
      

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.A))
        {
            if (AppData.Instance.selectedMovement == null)
            {
                message.text = "Please Select the Movement !!..";
                return;
            }
            AppLogger.LogInfo($"Switching scene to '{assessmentScene}'.");
            SceneManager.LoadScene(assessmentScene);

        }
        if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
        {
            AppLogger.LogInfo($"Switching scene to '{chooseTPscene}'.");
            SceneManager.LoadScene(chooseTPscene);
        }
        //Check if a scene change is needed.
        if (changeScene == true)
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
         
            if (previosAngle == MarsComm.angle1 && MarsComm.force > 10)
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
                toggleSelected = true;
                AppData.Instance.SetMovement(child.name);
                nextScene = selectGame[MarsDefs.getMovementIndex(AppData.Instance.selectedMovement.name)];
                AppData.Instance.SetGame(nextScene);
                if (AppData.Instance.selectedMovement.CurrentArom == null)nextScene = assessmentScene;
                message.text = "Press Mars Button to move Next Scene";
                Debug.Log(nextScene);
                AppLogger.LogInfo($"Selected '{AppData.Instance.selectedMovement.name}'.");
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

