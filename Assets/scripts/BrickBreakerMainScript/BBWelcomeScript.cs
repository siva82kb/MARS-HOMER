using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class BBWelcomeScript : MonoBehaviour
{
    public Button[] levelButtons; // Array of level buttons
    private string file_path=Path.Combine(Application.dataPath, "Patient_Data","BrickBreakerScore.csv");
    private int currentLevel = 1;

    private int currentScore = 0;
    public void Start()
    {
        string scene=SceneManager.GetActiveScene().name;
        if (scene=="BB_Level"){
        InitializeLevelButtons();
        Checkscore();
        }
    }
      private void InitializeLevelButtons()
    {
        // Set all level buttons to non-interactable initially
        foreach (Button button in levelButtons)
        {
            button.interactable = false;
        }
    }
    private void Checkscore()
    {
        if (!File.Exists(file_path))
        {
            File.WriteAllText(file_path, "0,1");
        }
        // Read score from CSV file
        if (File.Exists(file_path))
        {
            string[] lines = File.ReadAllLines(file_path);
            
            string[] values = lines[0].Split(','); // Split the line by commas
            if (values.Length >= 2)
            {
                int.TryParse(values[0], out currentScore); // Parse the first value as currentScore
                int.TryParse(values[1], out currentLevel); // Parse the second value as currentLevel
            }
        }
        else
        {
            currentScore = 0;
            currentLevel = 1;
        }

        // Unlock levels based on the current score
        UnlockLevelsBasedOnScore();
    }
        private void UnlockLevelsBasedOnScore()
    {

        int levelsToUnlock = currentLevel - 1;
        for (int i = 0; i <= levelsToUnlock; i++)
        {
            levelButtons[i].interactable = true;
        }
    }
    public void onclick_start_ChooseLevel()
    {
        SceneManager.LoadScene("BB_Level");
    }
    public void onclick_Back()
    {
        // SceneManager.LoadScene("#anything"); can add additional scene or just quit the application
        Application.Quit();
    }
    public void onClick_lvl_1()
    {
        SceneManager.LoadScene("Main_1");
    }
    public void onClick_lvl_2()
    {
        SceneManager.LoadScene("Main_2");
    }
    public void onClick_lvl_3()
    {
        SceneManager.LoadScene("Main_3");
    }
    public void onClick_lvl_4()
    {
        SceneManager.LoadScene("Main_4");
    }
    public void onClick_lvl_5()
    {
        SceneManager.LoadScene("Main_5");
    }
    public void onclick_back_to_welcomeScene()
    {
        SceneManager.LoadScene("BB_Welcome");
    }
}
