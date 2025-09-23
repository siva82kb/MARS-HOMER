using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
using System;
using UnityEngine.Rendering.Universal;


public class MovementSceneHandler : MonoBehaviour
{
    //ui related variables
    public GameObject movementSelectGroup;
    public GameObject marsActivation;
    public GameObject shortCutKeys;
    public Button exit;
    public static float initialAngle;
    private string nextScene;
    private string exitScene = "SUMMARY";
    private string assessmentScene = "ASSESSROM";
    private string chooseTPscene = "CHOOSEPLANE";
    public static float shAng;
    //flags
    private static bool changeScene = false;
    private bool toggleSelected = false;
    public Text message;
    public Text instructionTxt;
    public GameObject marsActivationGIF;
    //public GameObject AttachArmGIF;
    //public GameObject setTrainigPlaneGIF;
    public readonly string robotCalibScene = "ROBOTCALIB";

    public enum SETUPMARS
    {
        IDLE,
        ACTIVATE,
        ATTACHARM,
        SETTRAININGPLANEANGLE,
        DONE,
    }
    public SETUPMARS currentState = SETUPMARS.IDLE;
    //Game names
    public static string[] selectGame = { "space_shooter_home", "pong_game", "Whack_WelcomeScene" };
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
        //if Already TrainingPlane was set no need to setupMars again
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] != "POSITION" || MarsComm.angle1 < MarsComm.target)
        {
            currentState = SETUPMARS.IDLE;
        }
        else
        {
            currentState = SETUPMARS.DONE;
        }

        AttachCallbacks();
        //update SessionDetials
        AppData.Instance.updateSessionDetials();

        UpdateMovementToggleButtons();

        StartCoroutine(DelayedAttachListeners());
      
        message.text = "";

    }

    void Update()
    {
        MarsComm.sendHeartbeat();
        updateGUI();
       


        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.A))
        {
            if (AppData.Instance.selectedMovement == null)
            {
                if (currentState == SETUPMARS.DONE)
                {
                    message.text = "Please Select the Movement !!..";
                    return;

                }
            }
            if (currentState == SETUPMARS.DONE && AppData.Instance.selectedMovement != null) SceneManager.LoadScene(assessmentScene);

        }
        if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
        {
            if (currentState == SETUPMARS.DONE) SceneManager.LoadScene(chooseTPscene);
        }
        //Check if a scene change is needed.
        if (changeScene == true)
        {
            shAng = MarsComm.angle1;
            LoadNextScene();
            changeScene = false;
        }

        runStateMachine();
      

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
   
    public void runStateMachine()
    {
        if (currentState == SETUPMARS.DONE) return;
 
        switch (currentState)
        {
            case SETUPMARS.IDLE:
        
                instructionTxt.text = "Press Mars Button To Activate Mars";
                if(MarsComm.CONTROLTYPE[MarsComm.controlType] != "POSITION")
                    MarsComm.setControlType("POSITION");
            break;
            case SETUPMARS.ACTIVATE:

                instructionTxt.text = "Mars getting Ready...";
                if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION")
                {
                    if (MarsComm.target == -90)
                    {
                        // Check if the target has been reached.
                        if (Mathf.Abs(MarsComm.angle1 - MarsComm.target) < 10)
                        {
                            currentState = SETUPMARS.ATTACHARM;
                        }
                    }
                    else
                    {
                        MarsComm.setControlTarget(-90);
                    }

                }
            break;
                
            case SETUPMARS.ATTACHARM:
                if (MarsComm.force > 10)
                {
                    instructionTxt.text = "Press Mars Button To Set TrainigPlane Angle";
                }
                else
                {
                    instructionTxt.text = "Please Attach your Limb with Mars";
                }
               break;
               
            case SETUPMARS.SETTRAININGPLANEANGLE:

                if (MarsComm.target != AppData.Instance.userData.trainingPlaneAngle)
                    MarsComm.setControlTarget(AppData.Instance.userData.trainingPlaneAngle);
                if (MarsComm.target == AppData.Instance.userData.trainingPlaneAngle)
                {
                    // Check if the target has been reached.
                    if (Mathf.Abs(MarsComm.angle1 - MarsComm.target) < 2)
                    {
                        currentState = SETUPMARS.DONE;
                        instructionTxt.text = "";
                        message.text = "Please Select the Movement !!..";
                    }
                }
                break;
        }
    }
    public void updateGUI()
    {
        movementSelectGroup.SetActive(currentState == SETUPMARS.DONE);
        instructionTxt.gameObject.SetActive(currentState != SETUPMARS.DONE);
        marsActivation.SetActive(currentState == SETUPMARS.ATTACHARM || currentState == SETUPMARS.SETTRAININGPLANEANGLE || currentState == SETUPMARS.DONE);
        marsActivationGIF.SetActive(currentState == SETUPMARS.IDLE|| currentState == SETUPMARS.ACTIVATE);
        shortCutKeys.SetActive(currentState == SETUPMARS.DONE);
    }
    public void AttachCallbacks()
    {
        // Attach PLUTO button event
        MarsComm.OnMarsButtonReleased += OnMarsButtonReleased;
        //exit.onClick.AddListener(OnExitButtonClicked);
       
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
        switch (currentState)
        {
            case SETUPMARS.IDLE:
                currentState = SETUPMARS.ACTIVATE;
                break;

            case SETUPMARS.ATTACHARM:
                if (MarsComm.force > 10)
                {
                    currentState = SETUPMARS.SETTRAININGPLANEANGLE;
                }
                break;

            case SETUPMARS.DONE:
                HandleSetupComplete();
                break;

            default:
                // Other states don't need handling on button release
                return;
        }
    }


    /// Handles actions once setup is marked as DONE
    private void HandleSetupComplete()
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

