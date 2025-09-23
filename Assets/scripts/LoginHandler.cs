using System.IO;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;

public class LoginHandler : MonoBehaviour
{
    public TMP_Dropdown userDropdown;
    public TMP_InputField[] configInputs; // Assign in inspector
    public Button saveButton, createButton, editButton;
    private string configFileName = "configdata.csv";
    private bool isEditing = false;

    private string baseDataPath;
    private string currentUserPath;
    private string[] headers;
    private string[] originalValues;
    private string hospitalID;

    public TMP_InputField userSearchInput;
    public GameObject resultItemPrefab; // Prefab with Button + Text
    public Transform resultContentParent; // Parent under the ScrollView's Content

    private List<string> allUserFolders = new List<string>();
    private string selectedUser = "";


    void Start()
    {
        baseDataPath = Path.Combine(Application.dataPath, "data");
        if (!Directory.Exists(baseDataPath))
        {
            Debug.LogWarning("Data folder not found.");
            SceneManager.LoadScene("CONFIG");
        }
        //LoadUserFolders();
        // LoadUserFoldersForSearch();
        
    LoadUserFoldersForSearch(); // <-- Load folders into allUserFolders
    userSearchInput.onValueChanged.AddListener(UpdateSearchResults); // <-- Hook the search


        //userDropdown.onValueChanged.AddListener(OnUserSelected);
        saveButton.onClick.AddListener(SaveIfChanged);
        createButton.onClick.AddListener(createConfig);
        editButton.onClick.AddListener(EnableEditing);

        foreach (var input in configInputs)
        {
            input.interactable = false; 
        }
         if (userDropdown.options.Count > 0)
        {
           // OnUserSelected(userDropdown.value); 
        }

    }


    void LoadUserFoldersForSearch()
{
    if (!Directory.Exists(baseDataPath))
    {
        Debug.LogWarning("Data folder not found");
        SceneManager.LoadScene("CONFIG");
        return;
    }

    allUserFolders = Directory.GetDirectories(baseDataPath)
                              .Select(Path.GetFileName)
                              .ToList();

    if (allUserFolders.Count == 0)
    {
        Debug.LogWarning("No user folders found inside data folder");
        SceneManager.LoadScene("CONFIG");
        return;
    }

    UpdateSearchResults(""); // Show all users initially
}


    void UpdateSearchResults(string input)
    {
        foreach (Transform child in resultContentParent)
        {
            Destroy(child.gameObject);
        }

        var filtered = allUserFolders
            .Where(folder => folder.ToLower().Contains(input.ToLower()))
            .ToList();

  
    // Filter and take only first 4
    var resultsToShow = allUserFolders
        .Where(folder => folder.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
        .Take(5); // ✅ Only take 4 suggestions

        // Hide scroll view if no match
        //resultContentParent.parent.parent.gameObject.SetActive(filtered.Count > 0);

        // Hide scroll view if no match
    resultContentParent.parent.parent.gameObject.SetActive(filtered.Count > 0);


        foreach (var folder in resultsToShow)
        {
            GameObject item = Instantiate(resultItemPrefab, resultContentParent);
            Debug.Log(folder);
            item.GetComponentInChildren<TextMeshProUGUI>().text = folder;

            item.GetComponent<Button>().onClick.AddListener(() =>
            {
                userSearchInput.text = folder;
                selectedUser = folder;
                OnUserSelected(folder);
                ClearResults();

                
    // 👇 Hide the ScrollView explicitly
    resultContentParent.parent.parent.gameObject.SetActive(false);
            });
        }
    if (filtered.Count == 1)
    {
        userSearchInput.text = filtered[0];
        selectedUser = filtered[0];
        OnUserSelected(filtered[0]);
        ClearResults();
    }

}

    void ClearResults()
    {
        foreach (Transform child in resultContentParent)
        {
            Destroy(child.gameObject);
        }
    
    // 👇 Hide the ScrollView explicitly
    resultContentParent.parent.parent.gameObject.SetActive(false);
}

   void OnUserSelected(string folderName)
{
    hospitalID = folderName;
    currentUserPath = Path.Combine(baseDataPath, folderName, "data");
    string csvPath = Path.Combine(currentUserPath, configFileName);

    if (!File.Exists(csvPath)) return;

    string[] requiredFields = { "WFE", "WURD", "FPS", "HOC", "FME1", "FME2" };
    string[] lines = File.ReadAllLines(csvPath);
    if (lines.Length < 2) return;

    headers = lines[0].Split(',');
    string[] lastLine = lines[lines.Length - 1].Split(',');
    originalValues = lines[lines.Length - 1].Split(',');

    Dictionary<string, string> fieldValueMap = new Dictionary<string, string>();
    for (int i = 0; i < headers.Length; i++)
    {
        if (requiredFields.Contains(headers[i]))
        {
            fieldValueMap[headers[i]] = i < lastLine.Length ? lastLine[i] : "";
        }
    }

    for (int i = 0; i < configInputs.Length; i++)
    {
        string key = requiredFields[i];
        configInputs[i].text = fieldValueMap.ContainsKey(key) ? fieldValueMap[key] : "";
    }
}

    void SaveIfChanged()
    {
        if (originalValues != null)
        {
            string[] updatedValues = (string[])originalValues.Clone();
            bool anyChanges = false;
            string[] editableFields = new string[] { "WFE", "WURD", "FPS", "HOC", "FME1", "FME2" };

            for (int i = 0; i < editableFields.Length; i++)
            {
                string field = editableFields[i];
                int index = Array.IndexOf(headers, field);

                if (index >= 0 && index < updatedValues.Length)
                {
                    string newValue = configInputs[i].text;

                    if (updatedValues[index] != newValue)
                    {
                        updatedValues[index] = newValue;
                        anyChanges = true;
                    }
                }
            }

            if (anyChanges)
            {
                string newLine = string.Join(",", updatedValues);
                string csvPath = Path.Combine(currentUserPath, configFileName);
                File.AppendAllText(csvPath, newLine + Environment.NewLine);
            }
            else
            {
                Debug.Log("No changes detected.");
            }
        }
        else
        {
            Debug.LogError("originalValues is null");
        }
        Debug.Log(hospitalID);
        AppData.Instance.setUser(hospitalID);
        
        SceneManager.LoadScene("MAIN");
    }


    void createConfig(){
        SceneManager.LoadScene("CONFIG");
    }
    public void EnableEditing()
    {
        isEditing = true;

        foreach (var input in configInputs)
        {
            input.interactable = true;
        }
    }

}
