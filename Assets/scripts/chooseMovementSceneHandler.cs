using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MechanismSceneHandler : MonoBehaviour
{
    public GameObject mehcanismSelectGroup;
    public Button nextButton;
    public Button exit;
    private static bool changeScene = false;
    
    private bool toggleSelected = false;  
    private string nextScene = "calibration";
    private string exitScene = "Summary";

    void Start()
    {
        // Initialize if needed
        if (AppData.UserData.dTableConfig == null)
        {
            // Inialize the logger
            AppLogger.StartLogging(SceneManager.GetActiveScene().name);
            // Initialize.
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
        UpdateMechanismToggleButtons();
        StartCoroutine(DelayedAttachListeners());
    }

    void Update()
    {
        // Check if a scene change is needed.
        if (changeScene == true)
        {
            LoadNextScene();
            changeScene = false;
        }
    }

    private void UpdateMechanismToggleButtons()
    {
        foreach (Transform child in mehcanismSelectGroup.transform)
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
        foreach (Transform child in mehcanismSelectGroup.transform)
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
        foreach (Transform child in mehcanismSelectGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            if (toggleComponent != null && toggleComponent.isOn)
            {
                toggleSelected = true;
                AppData.selectedMovement = child.name;
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
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("summaryScene");

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
}

