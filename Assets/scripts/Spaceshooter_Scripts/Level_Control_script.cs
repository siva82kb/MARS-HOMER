using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
public class Level_Control_script : MonoBehaviour
{
    // Start is called before the first frame update
    public Button[] levelButtons; // Array of level buttons
    private string file_path; 
    private int currentLevel = 1;
   
    private int currentScore = 0;
    public TextMeshProUGUI currentScoreText; // Text for displaying current score
    public TextMeshProUGUI LevelsUnlockedText; // Text for displaying score needed for next level

    void Start()
    {
        InitializeLevelButtons();
        Checkscore();
        UpdateScoreDisplay();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
     private void InitializeLevelButtons()
    {
        // Set all level buttons to non-interactable initially
        foreach (Button button in levelButtons)
        {
            button.interactable = false;
        }
    }
     private  void Checkscore(){
        string path=Path.Combine(Application.dataPath,"Patient_Data");
        if(!Directory.Exists(path)){
            Directory.CreateDirectory(path);
        }
        file_path=Path.Combine(path,"ScoreManager.csv");
        if(!File.Exists(file_path)){
            File.WriteAllText(file_path, "0,1");
        }
        // Read score from CSV file
        if (File.Exists(file_path))
        {
            string[] lines = File.ReadAllLines(file_path);
            // if (lines.Length > 0)
            // {
            //     int.TryParse(lines[0], out currentScore,out currentlevel); // Parse score from the first line
            // }
        string[] values = lines[0].Split(','); // Split the line by commas
        if (values.Length >= 2)
        {
            int.TryParse(values[0], out currentScore); // Parse the first value as currentScore
            int.TryParse(values[1], out currentLevel); // Parse the second value as currentLevel
        }
        }
        else
        {
            // If the file doesn't exist, assume a score of 0
            currentScore = 0;
            currentLevel=1;
        }

        // Unlock levels based on the current score
        UnlockLevelsBasedOnScore();
    }
    
     private void UnlockLevelsBasedOnScore()
    {
        // Calculate how many levels should be unlocked
        int levelsToUnlock=currentLevel-1;
        // Make calculated number of levels interactable
        for (int i = 0; i <= levelsToUnlock; i++)
        {

            levelButtons[i].interactable = true;
        }
    }
    private void UpdateScoreDisplay()
    {
        // Display the current score
        currentScoreText.text = $"Score: {currentScore}";
        LevelsUnlockedText.text=$"Levels Unlocked:{currentLevel}";

        // // Calculate and display the score required to unlock the next level
        // int nextLevelScore = ((currentScore / requiredScore) + 1) * requiredScore;
        
        // // If max level is reached, no further level to unlock
        // if (nextLevelScore >= requiredScore * levelButtons.Length)
        // {
        //     nextLevelScoreText.text = "Max Level Reached";
        // }
        // else
        // {
        //     nextLevelScoreText.text = $"Next Level at: {nextLevelScore}";
        // }
    }
   
    public void onclcik_back(){
        SceneManager.LoadScene("space_shooter_home");
    }
    public void onclick_Level1(){
        SceneManager.LoadScene("SpaceShooter_Level1");
    }
    public void onclick_Level2(){
        SceneManager.LoadScene("SpaceShooter_Level2");
    }
    public void onclick_Level3(){
        SceneManager.LoadScene("SpaceShooter_Level3");
    }
    public void onclick_Level4(){
        SceneManager.LoadScene("SpaceShooter_Level4");
    }
    public void onclick_Level5(){
        SceneManager.LoadScene("SpaceShooter_Level5");
    }
    public void onclick_Level6(){
        SceneManager.LoadScene("SpaceShooter_Level6");
    }
    public void onclick_Level7(){
        SceneManager.LoadScene("SpaceShooter_Level7");
    }
    public void onclick_Level8(){
        SceneManager.LoadScene("SpaceShooter_Level8");
    }
    public void onclick_Level9(){
        SceneManager.LoadScene("SpaceShooter_Level9");
    }
    public void onclick_Level10(){
        SceneManager.LoadScene("SpaceShooter_Level10");
    }
 }

