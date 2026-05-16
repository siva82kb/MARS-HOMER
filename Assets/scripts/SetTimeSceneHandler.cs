using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.IO;
using System.Collections;

public class SetTimeSceneHandler : MonoBehaviour
{
    public Toggle mlToggle;
    public Toggle apToggle;
    public Toggle mlapToggle;
    public Slider mlSlider;
    public Slider apSlider;
    public Slider mlapSlider;
    public TextMeshProUGUI mlValueText;
    public TextMeshProUGUI apValueText;
    public TextMeshProUGUI mlapValueText;
    public TextMeshProUGUI TotalText;
    public Button saveButton;
    public Button backButton;
    public TextMeshProUGUI messageText;

    private readonly string chooseMoveScene = "CHOOSEMOVE";
    private string[] headers;
    private string[] originalValues;

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

        saveButton.onClick.AddListener(OnSave);
        backButton.onClick.AddListener(OnBack);

        LoadCurrentValues();
    }

    private void Update()
    {
        MarsComm.sendHeartbeat();
    }

    void LoadCurrentValues()
    {
        try
        {
            string[] lines = File.ReadAllLines(DataManager.configFile);
            if (lines.Length < 2)
            {
                messageText.text = "Config file is empty";
                return;
            }

            headers = lines[0].Split(',');
            originalValues = lines[lines.Length - 1].Split(',');

            int mlIndex = System.Array.IndexOf(headers, "ML");
            int apIndex = System.Array.IndexOf(headers, "AP");
            int mlapIndex = System.Array.IndexOf(headers, "MLAP");

            // Set all sliders to 10-30 range (only active when toggle is ON)
            mlSlider.minValue = 10;
            mlSlider.maxValue = 30;
            apSlider.minValue = 10;
            apSlider.maxValue = 30;
            mlapSlider.minValue = 10;
            mlapSlider.maxValue = 30;

            // Check which movements have assessment data
            bool mlAssessmentDone = AppData.Instance.userData.IsAromAssessmentAvailableForTrainingAngle("ML");
            bool apAssessmentDone = AppData.Instance.userData.IsAromAssessmentAvailableForTrainingAngle("AP");
            bool mlapAssessmentDone = AppData.Instance.userData.IsAromAssessmentAvailableForTrainingAngle("MLAP");

            // Load previous values from config
            if (mlIndex >= 0 && mlIndex < originalValues.Length)
            {
                if (int.TryParse(originalValues[mlIndex], out int mlVal))
                {
                    // Toggle ON only if assessment is done
                    mlToggle.isOn = mlAssessmentDone;
                    mlToggle.interactable = mlAssessmentDone;
                    mlSlider.interactable = mlAssessmentDone;
                    mlSlider.value = mlAssessmentDone ? (mlVal > 0 ? mlVal : 10) : 10;
                    mlValueText.text = mlAssessmentDone ? (mlVal > 0 ? $"{mlVal} (min)" : "10 (min)") : "0";
                }
            }

            if (apIndex >= 0 && apIndex < originalValues.Length)
            {
                if (int.TryParse(originalValues[apIndex], out int apVal))
                {
                    // Toggle ON only if assessment is done
                    apToggle.isOn = apAssessmentDone;
                    apToggle.interactable = apAssessmentDone;
                    apSlider.interactable = apAssessmentDone;
                    apSlider.value = apAssessmentDone ? (apVal > 0 ? apVal : 10) : 10;
                    apValueText.text = apAssessmentDone ? (apVal > 0 ? $"{apVal} (min)" : "10 (min)") : "0";
                }
            }

            if (mlapIndex >= 0 && mlapIndex < originalValues.Length)
            {
                if (int.TryParse(originalValues[mlapIndex], out int mlapVal))
                {
                    // Toggle ON only if assessment is done
                    mlapToggle.isOn = mlapAssessmentDone;
                    mlapToggle.interactable = mlapAssessmentDone;
                    mlapSlider.interactable = mlapAssessmentDone;
                    mlapSlider.value = mlapAssessmentDone ? (mlapVal > 0 ? mlapVal : 10) : 10;
                    mlapValueText.text = mlapAssessmentDone ? (mlapVal > 0 ? $"{mlapVal} (min)" : "10 (min)") : "0";
                }
            }

            // Add toggle listeners
            mlToggle.onValueChanged.AddListener(isOn =>
            {
                mlSlider.interactable = isOn;
                mlSlider.value = isOn ? 10 : 10;
                mlValueText.text = isOn ? "10 (min)" : "0";
                UpdateTotalDisplay();
            });

            apToggle.onValueChanged.AddListener(isOn =>
            {
                apSlider.interactable = isOn;
                apSlider.value = isOn ? 10 : 10;
                apValueText.text = isOn ? "10 (min)" : "0";
                UpdateTotalDisplay();
            });

            mlapToggle.onValueChanged.AddListener(isOn =>
            {
                mlapSlider.interactable = isOn;
                mlapSlider.value = isOn ? 10 : 10;
                mlapValueText.text = isOn ? "10 (min)" : "0";
                UpdateTotalDisplay();
            });

            // Add slider listeners AFTER loading values
            mlSlider.onValueChanged.AddListener(val =>
            {
                mlValueText.text = $"{(int)val} (min)";
                UpdateTotalDisplay();
            });
            apSlider.onValueChanged.AddListener(val =>
            {
                apValueText.text = $"{(int)val} (min)";
                UpdateTotalDisplay();
            });
            mlapSlider.onValueChanged.AddListener(val =>
            {
                mlapValueText.text = $"{(int)val} (min)";
                UpdateTotalDisplay();
            });

            // Update total display with loaded values
            UpdateTotalDisplay();

            if (TotalText != null)
            {
                messageText.text = $"Previous total: {TotalText.text}. Adjust to equal 30 (min).";
            }
            else
            {
                messageText.text = "Values loaded (Total must equal 30 min)";
            }
        }
        catch (System.Exception ex)
        {
            messageText.text = $"Error loading config: {ex.Message}";
            Debug.LogError($"LoadCurrentValues error: {ex}");
        }
    }

    void UpdateTotalDisplay()
    {
        int mlVal = mlToggle.isOn ? (int)mlSlider.value : 0;
        int apVal = apToggle.isOn ? (int)apSlider.value : 0;
        int mlapVal = mlapToggle.isOn ? (int)mlapSlider.value : 0;
        int total = mlVal + apVal + mlapVal;

        if (TotalText != null)
        {
            TotalText.text = $"{total}";

            if (total == 30)
            {
                TotalText.color = new Color(0f, 0.6f, 0f);
                messageText.text = "Total is correct (30 min). Ready to save.";
            }
            else if (total < 30)
            {
                TotalText.color = new Color(1f, 0.6f, 0f);
                messageText.text = $"Total is {total} (min). Need {30 - total} more minutes.";
            }
            else
            {
                TotalText.color = new Color(0.85f, 0f, 0f);
                messageText.text = $"Total is {total} (min). Remove {total - 30} minutes.";
            }
        }
    }

    void OnSave()
    {
        if (originalValues == null)
        {
            messageText.text = "No config data loaded";
            return;
        }

        try
        {
            int mlVal = mlToggle.isOn ? (int)mlSlider.value : 0;
            int apVal = apToggle.isOn ? (int)apSlider.value : 0;
            int mlapVal = mlapToggle.isOn ? (int)mlapSlider.value : 0;
            int total = mlVal + apVal + mlapVal;

            if (total != 30)
            {
                messageText.text = $"Total must be exactly 30 minutes. Current total: {total} (min)";
                return;
            }

            string[] updatedValues = (string[])originalValues.Clone();

            int mlIndex = System.Array.IndexOf(headers, "ML");
            int apIndex = System.Array.IndexOf(headers, "AP");
            int mlapIndex = System.Array.IndexOf(headers, "MLAP");
            int startDateIndex = System.Array.IndexOf(headers, "StartDate");

            // Update movement times
            if (mlIndex >= 0 && mlIndex < updatedValues.Length)
                updatedValues[mlIndex] = mlVal.ToString();
            if (apIndex >= 0 && apIndex < updatedValues.Length)
                updatedValues[apIndex] = apVal.ToString();
            if (mlapIndex >= 0 && mlapIndex < updatedValues.Length)
                updatedValues[mlapIndex] = mlapVal.ToString();

            // Update start date to today (keep end date from original config)
            DateTime today = DateTime.Now;
            string todayDateString = today.ToString("dd-MM-yyyy HH:mm:ss");

            if (startDateIndex >= 0 && startDateIndex < updatedValues.Length)
                updatedValues[startDateIndex] = todayDateString;

            string newLine = string.Join(",", updatedValues);
            File.AppendAllText(DataManager.configFile, newLine + Environment.NewLine);

            AppData.Instance.userData = new MarsUserData(
                DataManager.configFile,
                DataManager.sessionFile,
                AppData.Instance.userID
            );

            messageText.text = "Saved! Returning...";
            StartCoroutine(DelayedLoadScene());
        }
        catch (System.Exception ex)
        {
            messageText.text = $"Save error: {ex.Message}";
            Debug.LogError($"OnSave error: {ex}");
        }
    }

    IEnumerator DelayedLoadScene()
    {
        yield return new WaitForSeconds(1f);
        SceneTransitionManager.ResetAssessmentReturnScene();
        SceneManager.LoadScene(chooseMoveScene);
    }

    void OnBack()
    {
        SceneTransitionManager.ResetAssessmentReturnScene();
        SceneManager.LoadScene(chooseMoveScene);
    }
}
