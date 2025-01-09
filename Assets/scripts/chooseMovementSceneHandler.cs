using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
using System;

public class MovementSceneHandler : MonoBehaviour
{
    //ui related variables
    public GameObject movementSelectGroup;
    public Button nextButton;
    public Button exit;
    public static float initialAngle;
    private string nextScene = "calibratioScene";
    private string exitScene = "SummaryScene";
    private string assessmentScene = "weightCalibrationScene";
    public static float shAng;
    //flags
    private static bool changeScene = false;
    private bool toggleSelected = false;  
    

    void Start()
    {

        // Initialize if needed
        if (AppData.UserData.dTableConfig == null)
        {
            // Inialize the logger
            AppLogger.StartLogging(SceneManager.GetActiveScene().name);
            // Initialize.
            Debug.Log("calling");
            AppData.InitializeRobot();
        }
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
       
        // Attach PLUTO button event
        MarsComm.OnButtonReleased += OnPlutoButtonReleased;

        //checking time scale 
        if (Time.timeScale == 0)
        {
            Time.timeScale = 1;
        }
        exit.onClick.AddListener(OnExitButtonClicked);
        nextButton.onClick.AddListener(OnNextButtonClicked);
        UpdateMovementToggleButtons();
        StartCoroutine(DelayedAttachListeners());
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.A))
        {
            SceneManager.LoadScene(assessmentScene);

            //if (!File.Exists(DataManager.filePathAssessmentData))
            //{
            //    AppLogger.LogInfo("Assessment Mode Activated");
            //    SceneManager.LoadScene(assessmentScene);
            //}
            //else if (AppData.returnLastAssesment() > 7)
            //{
            //    AppLogger.LogInfo("Assessment Mode Activated");
            //    SceneManager.LoadScene(assessmentScene);
            //}
            //else
            //{
            //    AppLogger.LogWarning("Assessment mode cannot be open often");
            //}
           
            
        }
        // Check if a scene change is needed.
        if (changeScene == true)
        {
            shAng = MarsComm.angleOne;
            LoadNextScene();
            changeScene = false;
        }
    }

    private void UpdateMovementToggleButtons()
    {
        foreach (Transform child in movementSelectGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            bool isPrescribed = AppData.UserData.movementMoveTimePrsc[toggleComponent.name] > 0;
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
                    timeLeftText.text = $"{AppData.UserData.getTodayMoveTimeForMovement(toggleComponent.name)} / {AppData.UserData.movementMoveTimePrsc[toggleComponent.name]} min";
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
                //for tuk-tuk only
                initialAngle = MarsComm.angleOne;
                toggleSelected = true;
                AppData.selectedMovement = child.name;
                nextScene = AppData.selectedGame[AppData.MarsDefs.getMovementIndex(AppData.selectedMovement)];
                Debug.Log(nextScene);
                AppLogger.LogInfo($"Selected '{AppData.selectedMovement}'.");
                break;
            }
        }
    }

    public void OnPlutoButtonReleased()
    {
        if (toggleSelected)
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

    private void OnExitButtonClicked()
    {
        StartCoroutine(LoadSummaryScene());
    }
    private void OnNextButtonClicked()
    {
        if (toggleSelected)
        {
            LoadNextScene();
            toggleSelected = false;
        }
    }
    private void OnDestroy()
    {
        if (JediComm.isMars) 
        {
            MarsComm.OnButtonReleased -= OnPlutoButtonReleased;
        }
    }
    private void OnApplicationQuit()
    {
        //MarsComm.OnButtonReleased -= onMarsButtonReleased;
    }
}

