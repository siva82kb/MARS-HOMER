using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;
using System.Collections;

public class PlanModeSceneHandler : MonoBehaviour
{
    public GameObject movementSelectGroup;
    public Button startAssessmentButton;
    public Button setTimeButton;
    public Button backButton;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI feedBackTxt;

    private readonly string assessmentSceneML = "AROMML";
    private readonly string assessmentSceneAP = "AROMAP";
    private readonly string assessmentSceneMLAP = "AROMMLAP";
    private readonly string setTimeScene = "SETTIME";
    private readonly string chooseMoveScene = "CHOOSEMOVE";
    private readonly string robotCalibScene = "ROBOTCALIB";
    private readonly string marsSetupScene = "MARSSETUP";

    private string nextScene = "";
    private bool changeScene = false;
    private string selectedMovement = "";

    void Start()
    {
        MarsComm.sendHeartbeat();

        if (AppData.Instance.userData == null)
        {
            AppData.Instance.Initialize(SceneManager.GetActiveScene().name);
        }

        if (!Directory.Exists(DataManager.basePath)) Directory.CreateDirectory(DataManager.basePath);
        if (!File.Exists(DataManager.configFile)) SceneManager.LoadScene("CONFIG");

        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

        if (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB")
        {
            SceneManager.LoadScene(robotCalibScene);
        }

        if (MarsComm.CONTROLTYPE[MarsComm.controlType] != "POSITION")
        {
            SceneManager.LoadScene(marsSetupScene);
        }

        setTimeButton.onClick.AddListener(OnSetTime);
        backButton.onClick.AddListener(OnBack);

        MarsComm.OnMarsButtonReleased += OnMarsButtonReleased;

        ReloadUserDataAndShowFeedback();
        UpdateMovementTimeDisplay();
        StartCoroutine(DelayedAttachListeners());
    }

    void Update()
    {
        MarsComm.sendHeartbeat();

        if (changeScene)
        {
            SceneManager.LoadScene(nextScene);
        }
    }

    IEnumerator DelayedAttachListeners()
    {
        yield return new WaitForSeconds(0.1f);
        AttachToggleListeners();
    }

    void AttachToggleListeners()
    {
        foreach (Transform child in movementSelectGroup.transform)
        {
            Toggle toggle = child.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(x => CheckToggleStates());
            }
        }
    }

    void CheckToggleStates()
    {
        foreach (Transform child in movementSelectGroup.transform)
        {
            Toggle toggle = child.GetComponent<Toggle>();
            if (toggle != null && toggle.isOn)
            {
                selectedMovement = child.name.ToUpper();
                AppData.Instance.SetMovement(selectedMovement);
                messageText.text = $"SELECTED MOVEMENT : {selectedMovement}";
                return;
            }
        }
        selectedMovement = "";
        messageText.text = "No movement selected";
    }

    void UpdateMovementTimeDisplay()
    {
        foreach (Transform child in movementSelectGroup.transform)
        {
            Toggle toggle = child.GetComponent<Toggle>();
            if (toggle != null)
            {
                Transform timeLeftTransform = toggle.transform.Find("timeLeft");
                if (timeLeftTransform != null)
                {
                    TextMeshProUGUI timeLeftText = timeLeftTransform.GetComponent<TextMeshProUGUI>();
                    if (timeLeftText != null)
                    {
                        string movementName = toggle.name.ToUpper();
                        int todayTime = AppData.Instance.userData.getTodayMoveTimeForMovement(movementName);
                        float  prescribedTime = AppData.Instance.userData.moveTimePrsc[movementName];
                        timeLeftText.text = $"{prescribedTime} min";
                    }
                }
            }
        }
    }

    void OnStartAssessment()
    {
        if (string.IsNullOrEmpty(selectedMovement))
        {
            messageText.text = "Please select a movement first";
            return;
        }

        SceneTransitionManager.SetAssessmentReturnScene("PLANMODE");

        switch (selectedMovement)
        {
            case "ML":
                nextScene = assessmentSceneML;
                break;
            case "AP":
                nextScene = assessmentSceneAP;
                break;
            case "MLAP":
                nextScene = assessmentSceneMLAP;
                break;
            default:
                messageText.text = "Unknown movement";
                return;
        }
        changeScene = true;
    }

    void OnSetTime()
    {
        nextScene = setTimeScene;
        changeScene = true;
    }

    void OnBack()
    {
        SceneTransitionManager.ResetAssessmentReturnScene();
        nextScene = chooseMoveScene;
        changeScene = true;
    }

    void OnMarsButtonReleased()
    {
        if (!string.IsNullOrEmpty(selectedMovement) && MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION")
        {
            OnStartAssessment();
        }
    }

    void OnDestroy()
    {
        MarsComm.OnMarsButtonReleased -= OnMarsButtonReleased;
    }

    void OnEnable()
    {
        ReloadUserDataAndShowFeedback();
    }

    void ReloadUserDataAndShowFeedback()
    {
        try
        {
            AppData.Instance.userData = new MarsUserData(
                DataManager.configFile,
                DataManager.sessionFile,
                AppData.Instance.userID
            );
            Debug.Log("[PLANMODE] UserData reloaded successfully");
            ShowAssessmentStatusFeedback();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PLANMODE] Error reloading user data: {ex}");
            feedBackTxt.text = "Error loading assessment data";
        }
    }

    void ShowAssessmentStatusFeedback()
    {
        string feedback = "<b>==== ASSESSMENT STATUS ====</b>\n\n";

        // ML AROM
        if (AppData.Instance.userData.IsAromAssessmentAvailableForTrainingAngle("ML"))
        {
            int days = AppData.Instance.userData.DaysSinceAromAssessmentForTrainingAngle("ML");
            feedback += $"<color=#00cc00> ML AROM</color>\n";        //{days} days ago\n";
        }
        else
        {
            feedback += $"<color=#ff0000> ML AROM</color>\n";//       Not completed\n";
        }

        // AP AROM
        if (AppData.Instance.userData.IsAromAssessmentAvailableForTrainingAngle("AP"))
        {
            int days = AppData.Instance.userData.DaysSinceAromAssessmentForTrainingAngle("AP");
            feedback += $"<color=#00cc00> AP AROM</color>\n";//        {days} days ago\n";
        }
        else
        {
            feedback += $"<color=#ff0000> AP AROM</color>\n";//      Not completed\n";
        }

        // MLAP AROM
        if (AppData.Instance.userData.IsAromAssessmentAvailableForTrainingAngle("MLAP"))
        {
            int days = AppData.Instance.userData.DaysSinceAromAssessmentForTrainingAngle("MLAP");
            feedback += $"<color=#00cc00> MLAP AROM</color>\n";//        {days} days ago\n";
        }
        else
        {
            feedback += $"<color=#ff0000> MLAP AROM</color>\n";//    Not completed\n";
        }

        // Arm Weight Assessment
        if (AppData.Instance.userData.IsArmWeightAssessmentAvailableForTrainingAngle())
        {
            int days = AppData.Instance.userData.DaysSinceArmWeightAssessmentForTrainingAngle();
            feedback += $"<color=#00cc00> Arm Weight</color>\n";//      {days} days ago";
        }
        else
        {
            feedback += $"<color=#ff0000> Arm Weight</color>";//      Not completed";
        }

        feedback += "\n\n<b>═════════════════════════</b>";

        if (feedBackTxt != null)
        {
            feedBackTxt.text = feedback;
            Debug.Log($"[PLANMODE] Feedback updated:\n{feedback}");
        }
        else
        {
            Debug.LogError("[PLANMODE] feedBackTxt is not assigned!");
        }
    }

    public void RefreshAssessmentFeedback()
    {
        Debug.Log("[PLANMODE] RefreshAssessmentFeedback called");
        ReloadUserDataAndShowFeedback();
    }
}
