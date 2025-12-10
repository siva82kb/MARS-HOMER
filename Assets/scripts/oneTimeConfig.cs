using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OneTimeConfig : MonoBehaviour
{
    public TMP_InputField nameField;
    public TMP_InputField ageField;
    public TMP_InputField hospitalIdField;
    public TMP_InputField startDateField;
    public TMP_InputField endDateField;

    public TMP_InputField mlDuration;
    public TMP_InputField apDuration;
    public TMP_InputField mlapDuration;
    public Button Done;
    public TMP_Dropdown affectedSideDropdown;

    public TextMeshProUGUI totalDurationText;
    public string upperArmLength = "250";
    public string foreArmLength = "150";
    
    private void Start()
    {
        // Automatically set startDateField and endDateField
        DateTime startDate = DateTime.Now;
        DateTime endDate = startDate.AddDays(30);

        startDateField.text = startDate.ToString("dd-MM-yyyy");
        endDateField.text = endDate.ToString("dd-MM-yyyy");
        mlDuration.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        apDuration.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        mlapDuration.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        Done.onClick.AddListener(delegate { saveConfig(); });
      
    }
    private void Update()
    {
       
       
    }
    private void UpdateTotalDuration()
    {
        int totalDuration = 0;

        totalDuration += ParseField(mlDuration);
        totalDuration += ParseField(apDuration);
        totalDuration += ParseField(mlapDuration);
       
        totalDurationText.text = totalDuration.ToString();
    }

    private int ParseField(TMP_InputField field)
    {
        if (int.TryParse(field.text, out int value))
        {
            return value;
        }
        return 0; 
    }
    public void onExitClicked()
    {
        SceneManager.LoadScene("LOGIN");
    }
    public void saveConfig()
    {
        if (string.IsNullOrWhiteSpace(nameField.text) ||
          string.IsNullOrWhiteSpace(ageField.text) ||
          string.IsNullOrWhiteSpace(hospitalIdField.text) ||
          string.IsNullOrWhiteSpace(startDateField.text) ||
          string.IsNullOrWhiteSpace(endDateField.text))
        {
            Debug.LogError("Name, Age, Hospital ID, Start Date, and End Date fields must not be empty.");
            return;
        }

        string date = DateTime.Now.ToString("dd-MM-yyyy");
        string name = nameField.text;
        string age = ageField.text;
        string hospitalId = hospitalIdField.text;
        string startDate = startDateField.text;
        string endDate = endDateField.text;
        AppData.Instance.setUser(hospitalId);
        // Set null to "0".
        string ML = string.IsNullOrEmpty(mlDuration.text) ? "0" : mlDuration.text;
        string AP = string.IsNullOrEmpty(this.apDuration.text) ? "0" : this.apDuration.text;
        string MLAP = string.IsNullOrEmpty(this.mlapDuration.text) ? "0" : this.mlapDuration.text;
      
        string totalDuration = totalDurationText.text;

        string trainingSide = affectedSideDropdown.options[affectedSideDropdown.value].text;
       
        string headers = "Date,name,HospitalNumber,StartDate,EndDate,age,time,ML,AP,MLAP,forearmLength,upperarmLength,TrainingSide,Location";
        string data = $"{date},{name},{hospitalId},{startDate},{endDate},{age},{totalDuration},{ML},{AP},{MLAP},{upperArmLength},{foreArmLength},{trainingSide},CMCV";
        string directoryPath = Path.Combine(Application.dataPath, "data", AppData.Instance.userID, "data");
        string datapath = Path.Combine(directoryPath, "configdata.csv");

        // Ensure directory exists
        if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

        // DataManager.CreateFileStructure();
        if (File.Exists(datapath))
        {
            Debug.Log("Configuration File Already Exists. you can't update Here");
        }
        else
        {
            if (!File.Exists(datapath))
            {
                File.WriteAllText(datapath, headers + Environment.NewLine);
                Debug.Log("Data saved to CSV: " + datapath);
            }
            File.AppendAllText(datapath, data + Environment.NewLine);


            SceneManager.LoadScene("MAIN");

        }
      
        
    }
}
