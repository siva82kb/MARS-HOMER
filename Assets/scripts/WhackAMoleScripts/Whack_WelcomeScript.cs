using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Whack_WelcomeScript : MonoBehaviour
{
     private string filepath = Path.Combine(Application.dataPath, "Patient_Data", "Whack_Score.csv");
      public Button[] levelButtons;
      private int currentLevel = 1;

    private int currentScore = 0;
    // Start is called before the first frame update
       public void Start()
    {
        InitializeLevelButtons();
        Checkscore();
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
        if (!File.Exists(filepath))
        {
            File.WriteAllText(filepath, "0,1");
        }
        // Read score from CSV file
        if (File.Exists(filepath))
        {
            string[] lines = File.ReadAllLines(filepath);
            
            string[] values = lines[0].Split(',');
            if (values.Length >= 2)
            {
                int.TryParse(values[0], out currentScore); 
                int.TryParse(values[1], out currentLevel); 
            }
        }
        else
        {
            currentScore = 0;
            currentLevel = 1;
        }
        string scene=SceneManager.GetActiveScene().name;
        if(scene=="Whack_levels"){
        UnlockLevelsBasedOnScore();}
    }
        private void UnlockLevelsBasedOnScore()
    {

        int levelsToUnlock = currentLevel - 1;
        for (int i = 0; i <= levelsToUnlock; i++)
        {
            levelButtons[i].interactable = true;
        }
    }
    public void onclick_start(){
        SceneManager.LoadScene("Whack_levels");
    }
    public void Back(){
        // SceneManager.LoadScene("Back");
        // Application.Quit();
    }
    public void lvl_1(){
        SceneManager.LoadScene("Whack_Lvl1");

    }
     public void lvl_2(){
        SceneManager.LoadScene("Whack_Lvl2");
    }
     public void lvl_3(){
        SceneManager.LoadScene("Whack_Lvl3");
    }
     public void lvl_4(){
        SceneManager.LoadScene("Whack_Lvl4");
    }
    public void onClick_WelcomeScene(){
        SceneManager.LoadScene("Whack_WelcomeScene");
    }
}
