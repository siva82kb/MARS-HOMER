using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;

public class GameManage4S : MonoBehaviour
{
    public BallScript bscript;

    public int lives;
    public int score;
    public Text livesText;
    public Text scoreText;
    public GameObject gameOverPanel;
    public int numberOfBricks;
    public int playerSideBricks;
    public int enemySideBricks;
    public bool gameOver;
    public GameObject NextLevelPanel;
    private float Timer;
    public float gameDuration = 60f; 
    public TextMeshProUGUI TimerText;
    public GameObject GamestartPanel;
    private int requiredScore = 200;
     private int Level;
    private string filepath=Path.Combine(Application.dataPath,"Patient_Data","BrickbreakerScore.csv");
     public Image scoreProgressBar; 


    void Start()
    {
        lives = 3; // Reset lives to 3
        // score = 0; // Reset score to 0
         string[] lines = File.ReadAllLines(filepath);
            
            string[] values = lines[0].Split(','); // Split the line by commas
            if (values.Length >= 2)
            {
                int.TryParse(values[0], out score); // Parse the first value as currentScore
                int.TryParse(values[1], out Level); // Parse the second value as currentLevel
            }
        Timer = gameDuration;
        livesText.text = "Lives: " + lives;
        scoreText.text = "Score: " + score;
        UpdateScoreProgress();
        if (FindObjectOfType<BrickType>() != null)
        {
            playerSideBricks = CountBricksWithId(1);
            enemySideBricks = CountBricksWithId(2);
        }
    }
    void UpdateScoreProgress()
    {
        if (scoreProgressBar != null)
        {
            scoreProgressBar.fillAmount = (float)score / requiredScore;
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameOver();
        }
        else{
            if(!GamestartPanel.activeInHierarchy){
             Timer -= Time.deltaTime;

            // Update the timer display
            if (TimerText != null)
            {
                TimerText.text = "Time:" + Mathf.CeilToInt(Timer).ToString() + "s"; // Show remaining time
            }
            }
        }
         if (Timer <= 0)
        {
            Timer = 0;
            GameOver();
        }
        if (FindObjectOfType<BrickType>() != null)
        {
            playerSideBricks = CountBricksWithId(1);
            enemySideBricks = CountBricksWithId(2);
            if (playerSideBricks <= 1 || enemySideBricks <= 1)
            {
                GameOver();
            }
        }
        else
        {
            numberOfBricks = GameObject.FindGameObjectsWithTag("Brick").Length;
            if (score >= requiredScore)
            {
                nextLevelPanel();
            }
        }
    }
    void GameOver()
    {
        gameOver = true;
        gameOverPanel.SetActive(true);
        if (gameOverPanel.activeSelf)
        {
            bscript.ballrb.velocity = Vector2.zero;
            bscript.inPlay = false;
        }
    }
    public void UpdateLives(int changeInLives)
    {
        lives += changeInLives;

        if (lives <= 0)
        {
            lives = 0;
            GameOver();
        }

        livesText.text = "Lives: " + lives;
    }
    public void UpdateScore(int points)
    {

        score += points;

        scoreText.text = "Score:" + score;
         if (File.Exists(filepath))
        {
            // ScoreText.text = $"Score: {currentScore}";
            string data = $"{score},{Level}";
            File.WriteAllText(filepath, data);
        }
    }
    public void PlayAgain()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void UpdateNumberOfBricks()
    {

        numberOfBricks--;
    }
    private int CountBricksWithId(int brickId)
    {
        int count = 0;

        // Count bricks with the given BrickId
        BrickType[] bricks = FindObjectsOfType<BrickType>();
        foreach (BrickType brick in bricks)
        {
            if (brick.BrickId == brickId)
            {
                count++;
            }
        }

        return count;
    }
    public void Back()
    {
        SceneManager.LoadScene("BB_Level");

    }
    void nextLevelPanel()
    {
        gameOver = true;
        NextLevelPanel.SetActive(true);
        if (NextLevelPanel.activeSelf)
        {
            bscript.ballrb.velocity = Vector2.zero;
            bscript.inPlay = false;
        }

    }
    public void go_to_secondLevel()
    {
          if (File.Exists(filepath))
        {
            // ScoreText.text = $"Score: {currentScore}";
            string data = "0,2";
            File.WriteAllText(filepath, data);
        }
        SceneManager.LoadScene("Main_2");
    }
   
}
