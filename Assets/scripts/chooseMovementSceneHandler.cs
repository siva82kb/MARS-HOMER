using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using JetBrains.Annotations;



public class MovementSceneHandler : MonoBehaviour
{
    //ui related variables
    public GameObject movementSelectGroup;
    public GameObject ChooseGamePanel;
    public Text message;
    public Text additionalMessage;
    public Text mlAromText;
    public Text apAromText;
    public Text mlapAromText;
    public Text armWeightText;
    public GameObject gameDC;
    public GameObject gameTW;
    public GameObject gameTT;
    public GameObject gamePP;
    public GameObject gameSS;
    public GameObject gameMD;
 
    public static float initialAngle;
    private string nextScene = "";
    //flags
    private static bool changeScene = false;
    private bool toggleSelected = false;
   
    //OTHER SCENES
    public readonly string marsSetupScene = "MARSSETUP";
    public readonly string robotCalibScene = "ROBOTCALIB";
    private readonly string trainingPlaneScene = "CHOOSEPLANE";
    private readonly string armWeightScene = "ARMWEIGHT";
    private readonly string marsSetUp = "MARSSETUP";
    private readonly string exitScene = "SUMMARY";
    private readonly string assessAp = "ASSESSAP";

    private readonly string assessmentSceneML = "AROMML";
    private readonly string assessmentSceneAP = "AROMAP";
    private readonly string assessmentSceneMLAP = "AROMMLAP";
    private string aromAssessmentScene = "";

    // Define dark red and green colors
    private Color darkRed = new Color(0.85f, 0f, 0f);
    private Color darkGreen = new Color(0f, 0.60f, 0f);

    void Start()
    {
        

        MarsComm.sendHeartbeat();
    
        // Initialize AppData if needed
        if (AppData.Instance.userData == null)
        {
            AppData.Instance.Initialize(SceneManager.GetActiveScene().name);
          
        }

        //Check if the directory exists
        if (!Directory.Exists(DataManager.basePath)) Directory.CreateDirectory(DataManager.basePath);
        if (!File.Exists(DataManager.configFile)) SceneManager.LoadScene("CONFIG");

        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

        AppLogger.SetCurrentMovement("");
        AppLogger.SetCurrentGame("");

        // If the robot is not calibrated go to the robot calib scene.
        if (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB")
        {
            SceneManager.LoadScene(robotCalibScene);
        }
        // If the robot is not in position control go to the mars setup scene.
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] != "POSITION")
        {
            SceneManager.LoadScene(marsSetUp);
        }

        // Attach the MARSComm callbacks.
        MarsComm.OnMarsButtonReleased += OnMarsButtonReleased;

        // Update Session Details
        AppData.Instance.userData.readParseSessionData(DataManager.sessionFile);

        // Initialize GUI
        UpdateMovementToggleButtons();
        StartCoroutine(DelayedAttachListeners());

       
        additionalMessage.text = "";

        // Update the assessment status text.
        updateAssessmentStatusText();
       
    }

    void Update()
    {
        MarsComm.sendHeartbeat();
        
        // // Check if the magic key combination is pressed for AROM assessment 
        // // or training plane selection.
        // if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.A))
        // {
        //     if (aromAssessmentScene != "")
        //     {
              
        //         // Go the next assessment scene based on the selected movement.
        //         nextScene = aromAssessmentScene;
        //         changeScene = true;
        //     }
           

        // }
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.T))
        {
            // Switch to the training plane scene.
            nextScene = trainingPlaneScene;
            changeScene = true;
        }
        // if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.W))
        // {
        //     if (AppData.Instance.selectedMovement == null || AppData.Instance.selectedMovement.name != "MLAP")
        //     {
        //         message.text = "Weight assessment available only for MLAP movement.";
        //         additionalMessage.text = "";
        //         changeScene = false;
        //         return;
        //     }

        //     if (AppData.Instance.userData.IsAromAssessmentAvailableForTrainingAngle("MLAP"))
        //     {
        //         // Switch to the training plane scene.
        //         nextScene = armWeightScene;
        //         changeScene = true;
        //     }
        //     else
        //     {
        //         additionalMessage.text = "MLAP assessment needs to be completed first.";
        //         // Switch to the MLAP AROM assessment scene.
        //         AppData.Instance.SetMovement("MLAP");
        //         nextScene = assessmentSceneMLAP;
        //         changeScene = true;
        //     }
            
        // }
        // else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Y))
        // {
        //     nextScene = assessAp;
        //     changeScene = true;

        // }
        if (Input.GetKey(KeyCode.LeftControl)&& Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.X))
        {
            SceneManager.LoadScene("PLANMODE");
        }
        if(  !AppData.Instance.userData.IsAromAssessmentAvailableForTrainingAngle("ML")&&
             !AppData.Instance.userData.IsAromAssessmentAvailableForTrainingAngle("AP")&&
             !AppData.Instance.userData.IsAromAssessmentAvailableForTrainingAngle("MLAP"))
        {
            SceneManager.LoadScene("PLANMODE");
        }
            
        
        //Check if a scene change is needed.
        if (changeScene == true && nextScene != "")
        {
            LoadNextScene();
            changeScene = false;
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
            // Clear the message text.

            message.text = isPrescribed?"Please select the movement":"";
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
                Debug.Log($"Selected movement: {child.name}");
               
               
                // Check if assessment is done or if the correct assessment is available, 
                // else the next scene will be the corresponding assessment scene.
                bool noAssessAvailable = AppData.Instance.selectedMovement.currentArom == null;
                bool trainingPlaneMismatch = AppData.Instance.userData.trainingPlaneAngle != AppData.Instance.selectedMovement.currentArom?.trainingPlaneAngle;
                Debug.Log($"Angles: {AppData.Instance.userData.trainingPlaneAngle} vs {AppData.Instance.selectedMovement.currentArom?.trainingPlaneAngle}");
                if (noAssessAvailable || trainingPlaneMismatch)
                {
                    // Next is an assessment scene;
                    nextScene = AppData.Instance.selectedMovement.name == "ML" ? assessmentSceneML :
                                AppData.Instance.selectedMovement.name == "AP" ? assessmentSceneAP :
                                AppData.Instance.selectedMovement.name == "MLAP" ? assessmentSceneMLAP : "";
                    message.text = "Press Mars Button to start assessment";
                    additionalMessage.text = noAssessAvailable ? "No previous assessment found. Assessment will be done first." :
                                             trainingPlaneMismatch ? "Training plane angle mismatch. Reassesment will be done first." : "";

                }
                else
                {
                    if ((trainingPlaneMismatch || AppData.Instance.userData.IsArmWeightAssessmentAvailableForTrainingAngle()== false) && AppData.Instance.selectedMovement.name == "MLAP")
                    {
                     message.text = "Press Mars Button to start weight assessment";
                    additionalMessage.text = !AppData.Instance.userData.IsArmWeightAssessmentAvailableForTrainingAngle() ? "No previous weight assessment found. Assessment will be done first." : "";
                    nextScene = armWeightScene;


                    }
                    else
                    {
                         // Set the AROM assessment scene.
                    aromAssessmentScene = AppData.Instance.selectedMovement.name == "ML" ? assessmentSceneML :
                                          AppData.Instance.selectedMovement.name == "AP" ? assessmentSceneAP :
                                          AppData.Instance.selectedMovement.name == "MLAP" ? assessmentSceneMLAP : "";
                    // Next is the game scene.
                  
                    
                    //open chooseGamePanel
                    createChooseGamePanel();
                    message.text = "Please choose the game";
                    additionalMessage.text = "";
                   
                    }
                   

                }

                AppLogger.LogInfo($"Selected movement ({AppData.Instance.selectedMovement.name}) and game ({AppData.Instance.selectedGame})");
                break;
            }
        }
    }
    public void createChooseGamePanel()
    {
        ChooseGamePanel.SetActive(AppData.Instance.selectedMovement!=null);
        //GAME ICON FOR MLAP
        gameDC.SetActive(AppData.Instance.selectedMovement.name == "MLAP");
        gameTW.SetActive(AppData.Instance.selectedMovement.name == "MLAP");
        //GAME ICON FOR AP
        gamePP.SetActive(AppData.Instance.selectedMovement.name == "AP");
        gameTT.SetActive(AppData.Instance.selectedMovement.name == "AP");
        //GAME ICON FOR ML
        gameSS.SetActive(AppData.Instance.selectedMovement.name == "ML");
        gameMD.SetActive(AppData.Instance.selectedMovement.name == "ML");

    }
    public void onClickTW()
    {
        AppData.Instance.SetGame(MarsGameDefs.GAMES[3]);
        AppLogger.LogInfo($"Selected movement ({AppData.Instance.selectedMovement.name}) and game ({AppData.Instance.selectedGame})");
        nextScene = MarsGameDefs.GAME_SCENES[3];
        changeScene = true;
    }
    public void onClickDC()
    {
        AppData.Instance.SetGame(MarsGameDefs.GAMES[2]);
        AppLogger.LogInfo($"Selected movement ({AppData.Instance.selectedMovement.name}) and game ({AppData.Instance.selectedGame})");
        nextScene = MarsGameDefs.GAME_SCENES[2];
        changeScene = true;

    }
    public void onClickTT()
    {
        AppData.Instance.SetGame(MarsGameDefs.GAMES[4]);
        AppLogger.LogInfo($"Selected movement ({AppData.Instance.selectedMovement.name}) and game ({AppData.Instance.selectedGame})");
        nextScene = MarsGameDefs.GAME_SCENES[4];
        changeScene = true;
    }
    public void onClickPP()
    {
        AppData.Instance.SetGame(MarsGameDefs.GAMES[1]);
        AppLogger.LogInfo($"Selected movement ({AppData.Instance.selectedMovement.name}) and game ({AppData.Instance.selectedGame})");
        nextScene = MarsGameDefs.GAME_SCENES[1];
        changeScene = true;
    }
    public void onClickSS()
    {
        AppData.Instance.SetGame(MarsGameDefs.GAMES[0]);
        AppLogger.LogInfo($"Selected movement ({AppData.Instance.selectedMovement.name}) and game ({AppData.Instance.selectedGame})");
        nextScene = MarsGameDefs.GAME_SCENES[0];
        changeScene = true;
    }
    public void onClickMC()
    {
        AppData.Instance.SetGame(MarsGameDefs.GAMES[5]);
        AppLogger.LogInfo($"Selected movement ({AppData.Instance.selectedMovement.name}) and game ({AppData.Instance.selectedGame})");
        nextScene = MarsGameDefs.GAME_SCENES[5];
        changeScene = true;
    }
    public void onClickClose()
    {
        if(ChooseGamePanel.activeSelf)ChooseGamePanel.SetActive(false);
        AppLogger.SetCurrentMovement("");
        AppLogger.SetCurrentGame("");
    }

    private void updateAssessmentStatusText()
    {
        int days;
        // Check if the different assessments are available and update the assessment status text.
        // ML AROm
        if (!AppData.Instance.userData.IsAromAssessmentAvailableForTrainingAngle("ML"))
        {
            mlAromText.text = "ML AROM   : N/A\n";
            mlAromText.color = darkRed;
        }
        else
        {
            days = AppData.Instance.userData.DaysSinceAromAssessmentForTrainingAngle("ML");
            mlAromText.text = $"ML AROM   : {days} days ago\n";
            mlAromText.color = days < 13 ? darkGreen : darkRed;
        }
        // AP AROM
        if (!AppData.Instance.userData.IsAromAssessmentAvailableForTrainingAngle("AP"))
        {
            apAromText.text = "AP AROM   : N/A\n";
            apAromText.color = darkRed;
        }
        else
        {
            days = AppData.Instance.userData.DaysSinceAromAssessmentForTrainingAngle("AP");
            apAromText.text = $"AP AROM   : {days} days ago\n";
            apAromText.color = days < 13 ? darkGreen : darkRed;
        }
        // MLAP AROM
        if (!AppData.Instance.userData.IsAromAssessmentAvailableForTrainingAngle("MLAP"))
        {
            mlapAromText.text = "MLAP AROM : N/A\n";
            mlapAromText.color = darkRed;
        }
        else
        {
            days = AppData.Instance.userData.DaysSinceAromAssessmentForTrainingAngle("MLAP");
            mlapAromText.text = $"MLAP AROM : {days} days ago\n";
            mlapAromText.color = days < 13 ? darkGreen : darkRed;
        }
        // Arm Weight
        if (!AppData.Instance.userData.IsArmWeightAssessmentAvailableForTrainingAngle())
        {
            armWeightText.text = "Arm Weight: N/A\n";
            armWeightText.color = darkRed;
        }
        else
        {
            days = AppData.Instance.userData.DaysSinceArmWeightAssessmentForTrainingAngle();
            armWeightText.text = $"Arm Weight: {days} days ago\n";
            armWeightText.color = days < 13 ? darkGreen : darkRed;
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
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(marsSetUp);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
   
    public void OnExitButtonClicked()
    {
      
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