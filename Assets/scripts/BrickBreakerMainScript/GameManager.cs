// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.SceneManagement;
// using TMPro;
// using System.IO;
// using UnityEngine.SocialPlatforms.Impl;

// public class GameManager : MonoBehaviour

// {
//     public BallScript bscript;

//     public int lives;
//     public int score;
//     public Text livesText;
//     public Text scoreText;
//     public bool gameOver;
//     public GameObject gameOverPanel;
//     public GameObject NextLevelPanel;
//     // public GameObject loadLevelPanel;
//     public int numberOfBricks;
//     public Transform[] levels;
//     private Vector2 spawnPosition;
//     public int currentLevelIndex = 0;
//     private int Level;
//     private string filepath=Path.Combine(Application.dataPath,"Patient_Data","BrickbreakerScore.csv");

//     private float Timer;
//     public float gameDuration = 60f; 
//     public TextMeshProUGUI TimerText;
//     public GameObject GamestartPanel;
//     private int requiredScore = 200;

//     void Start()
//     {

//         lives = 3; // Reset lives to 3
//         // score = 0; // Reset score to 0
//          string[] lines = File.ReadAllLines(filepath);
            
//             string[] values = lines[0].Split(','); // Split the line by commas
//             if (values.Length >= 2)
//             {
//                 int.TryParse(values[0], out score); // Parse the first value as currentScore
//                 int.TryParse(values[1], out Level); // Parse the second value as currentLevel
//             }
//         Timer = gameDuration;
//         livesText.text = "Lives:" + lives;
//         scoreText.text = "Score:" + score;
//         spawnPosition = levels[0].transform.position;
//         Debug.Log(spawnPosition);
//         numberOfBricks = GameObject.FindGameObjectsWithTag("Brick").Length;
//     }

//     void Update()
//     {
//         // Check if the Escape key is pressed
//         if (Input.GetKeyDown(KeyCode.Escape))
//         {
//             GameOver();
//         }
//         else{
//             if(!GamestartPanel.activeInHierarchy){
//              Timer -= Time.deltaTime;

//             // Update the timer display
//             if (TimerText != null)
//             {
//                 TimerText.text = "Time:" + Mathf.CeilToInt(Timer).ToString() + "s"; // Show remaining time
//             }
//             }
//         }
//          if (Timer <= 0)
//         {
//             Timer = 0;
//             GameOver();
//         }
//         if(score>=requiredScore){
//             nextLevelPanel();
//         }
//     }
//     public void UpdateLives(int changeInLives)
//     {
//         lives += changeInLives;
//         if (lives <= 0)
//         {
//             lives = 0;
//             GameOver();
//         }


//         livesText.text = "Lives:" + lives;
//     }
//     public void UpdateScore(int points)
//     {
//         score += points;
//         scoreText.text = "Score:" + score;
//           if (File.Exists(filepath))
//         {
//             // ScoreText.text = $"Score: {currentScore}";
//             string data = $"{score},{Level}";
//             File.WriteAllText(filepath, data);
//         }
//     }
//     public void UpdateNumberOfBricks()
//     {
//         numberOfBricks--;
//         if (numberOfBricks <= 0)
//         {
//             if (currentLevelIndex >= levels.Length - 1 )
//             {
                
//                 nextLevelPanel();
//             }
           
//             else
//             {
//                 //  loadLevelPanel.SetActive(true);
//                 //  loadLevelPanel.GetComponentInChildren<Text>().text="Level"+(currentLevelIndex+2);
//                 gameOver = true;
//                 Invoke("LoadLevel", 0.2f);
//             }
//         }
//     }
//     void LoadLevel()
//     {
//         currentLevelIndex++;

//         bscript.ballrb.velocity = Vector2.zero;
//         bscript.inPlay = false;
//         StartCoroutine(LoadNextLevelAfterDelay(1.5f));

//     }
//     IEnumerator LoadNextLevelAfterDelay(float delay)
//     {
//         yield return new WaitForSeconds(delay);

//         // Instantiate the new level prefab
//         Instantiate(levels[currentLevelIndex], spawnPosition, Quaternion.identity);

//         // Update the number of bricks in the scene
//         numberOfBricks = GameObject.FindGameObjectsWithTag("Brick").Length;

//         // Increment the ball's speed based on the current level
//         bscript.speed += currentLevelIndex * 10;

//         // Clamp the ball speed to avoid excessive speeds
//         bscript.speed = Mathf.Clamp(bscript.speed, 310, 350);

//         // Reset game over flag
//         gameOver = false;
//     }
//     void GameOver()
//     {
//         gameOver = true;
//         gameOverPanel.SetActive(true);
//         if (gameOverPanel.activeSelf)
//         {
//             bscript.ballrb.velocity = Vector2.zero;
//             bscript.inPlay = false;
//         }
//         // int highScore=PlayerPrefs.GetInt("HIGHSCORE");
//         // if(score>highScore){
//         //     PlayerPrefs.SetInt("HIGHSCORE",score);
//         //     highScoreText.text= "New High Score:"+score;
//         // }else{
//         //     highScoreText.text="High Score"+highScore+"\n"+"Can You Beat it";
//         // }
//     }
//     public void PlayAgain()
//     {
//         string currentSceneName = SceneManager.GetActiveScene().name;
//         SceneManager.LoadScene(currentSceneName);
//     }
//     public void Back()
//     {
//         SceneManager.LoadScene("BB_Level");

//     }
//     void nextLevelPanel()
//     {
//         gameOver = true;
//         NextLevelPanel.SetActive(true);
//         if (NextLevelPanel.activeSelf)
//         {
//             bscript.ballrb.velocity = Vector2.zero;
//             bscript.inPlay = false;
//         }

//     }
//     public void go_to_fifthLevel()
//     {
//          if (File.Exists(filepath))
//         {
//             // ScoreText.text = $"Score: {currentScore}";
//             string data = "0,5";
//             File.WriteAllText(filepath, data);
//         }
//         SceneManager.LoadScene("Main_5");
//     }
//     public void go_to_ThirdLevel()
//     {
//           if (File.Exists(filepath))
//         {
//             // ScoreText.text = $"Score: {currentScore}";
//             string data = "0,3";
//             File.WriteAllText(filepath, data);
//         }
//         SceneManager.LoadScene("Main_3");
//     }
//     public void go_to_fourthLevel()
//     {
//           if (File.Exists(filepath))
//         {
//             // ScoreText.text = $"Score: {currentScore}";
//             string data = "0,4";
//             File.WriteAllText(filepath, data);
//         }
//         SceneManager.LoadScene("Main_4");
//     }
    
// }
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;

public class GameManager : MonoBehaviour
{
    public BallScript bscript;

    public int lives;
    public int score;
    public Text livesText;
    public Text scoreText;
    public bool gameOver;
    public GameObject gameOverPanel;
    public GameObject NextLevelPanel;

    public int numberOfBricks;
    public Transform[] levels;  // Array of brick formations
    private Vector2 spawnPosition;
    public int currentLevelIndex = 0;
    private int Level;
    private string filepath = Path.Combine(Application.dataPath, "Patient_Data", "BrickbreakerScore.csv");

    private float Timer;
    public float gameDuration = 60f; 
    public TextMeshProUGUI TimerText;
    public GameObject GamestartPanel;
    private int requiredScore = 200;
     public Image scoreProgressBar; 

    void Start()
    {
        lives = 3; // Reset lives to 3

        // Read score & level from file
        if (File.Exists(filepath))
        {
            string[] lines = File.ReadAllLines(filepath);
            string[] values = lines[0].Split(',');
            if (values.Length >= 2)
            {
                int.TryParse(values[0], out score);
                int.TryParse(values[1], out Level);
            }
        }

        Timer = gameDuration;
        livesText.text = "Lives: " + lives;
        scoreText.text = "Score: " + score;
        UpdateScoreProgress();
        // Choose a random formation at the start
        currentLevelIndex = Random.Range(0, levels.Length);
        spawnPosition = levels[0].transform.position;

        // Instantiate the chosen brick formation
        Instantiate(levels[currentLevelIndex], spawnPosition, Quaternion.identity);

        // Get the number of bricks in the formation
        numberOfBricks = GameObject.FindGameObjectsWithTag("Brick").Length;
    }
    void UpdateScoreProgress()
    {
        if (scoreProgressBar != null)
        {
            scoreProgressBar.fillAmount = (float)score / requiredScore;
        }
    }

    void Update()
    {
        // Escape key triggers Game Over
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameOver();
        }
        else
        {
            if (!GamestartPanel.activeInHierarchy)
            {
                Timer -= Time.deltaTime;
                if (TimerText != null)
                {
                    TimerText.text = "Time: " + Mathf.CeilToInt(Timer) + "s";
                }
            }
        }

        // If timer reaches zero, game over
        if (Timer <= 0)
        {
            Timer = 0;
            GameOver();
        }

        // If score meets requirement, show next level panel
        if (score >= requiredScore)
        {
            nextLevelPanel();
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
        scoreText.text = "Score: " + score;

        // Save score to file
        if (File.Exists(filepath))
        {
            string data = $"{score},{Level}";
            File.WriteAllText(filepath, data);
        }
    }

    public void UpdateNumberOfBricks()
    {
        numberOfBricks--;

        if (numberOfBricks <= 0)
        {
            // Destroy any remaining bricks (just in case)
            GameObject[] remainingBricks = GameObject.FindGameObjectsWithTag("Brick");
            foreach (GameObject brick in remainingBricks)
            {
                Destroy(brick);
            }

            // Choose a new random formation (different from the previous one)
            int newRandomIndex;
            if (levels.Length > 1)
            {
                do
                {
                    newRandomIndex = Random.Range(0, levels.Length);
                } while (newRandomIndex == currentLevelIndex); // Ensure it's different
            }
            else
            {
                newRandomIndex = currentLevelIndex; // If only one formation, use it
            }

            currentLevelIndex = newRandomIndex;

            // Spawn the new formation
            Instantiate(levels[currentLevelIndex], spawnPosition, Quaternion.identity);

            // Update brick count
            numberOfBricks = GameObject.FindGameObjectsWithTag("Brick").Length;

            Debug.Log("New randomly selected level: " + currentLevelIndex);
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

    public void PlayAgain()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
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

    public void go_to_fifthLevel()
    {
        if (File.Exists(filepath))
        {
            string data = "0,5";
            File.WriteAllText(filepath, data);
        }
        SceneManager.LoadScene("Main_5");
    }

    public void go_to_ThirdLevel()
    {
        if (File.Exists(filepath))
        {
            string data = "0,3";
            File.WriteAllText(filepath, data);
        }
        SceneManager.LoadScene("Main_3");
    }

    public void go_to_fourthLevel()
    {
        if (File.Exists(filepath))
        {
            string data = "0,4";
            File.WriteAllText(filepath, data);
        }
        SceneManager.LoadScene("Main_4");
    }
}
