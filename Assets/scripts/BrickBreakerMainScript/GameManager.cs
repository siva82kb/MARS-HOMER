using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour

{
    public BallScript bscript;
    
    public int lives;
    public int score;
    public Text livesText;
    public Text scoreText;
    // public Text highScoreText;
    public bool gameOver;
    
    public GameObject gameOverPanel;
    public GameObject loadLevelPanel;


    public int numberOfBricks;
    public Transform[] levels;
    public int currentLevelIndex=0;
        // Start is called before the first frame update
    void Start()
    {

       lives = 3; // Reset lives to 3
       score = 0; // Reset score to 0
       livesText.text="Lives:"+lives; 
       scoreText.text="Score:"+score;
       numberOfBricks=GameObject.FindGameObjectsWithTag("Brick").Length;
    }

    // Update is called once per frame
    void Update()
    {
       // Check if the Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameOver();
        } 
    }
    public void UpdateLives(int changeInLives){
        lives+=changeInLives;

        // if(currentLevelIndex>0){
        //     lives=3;
        // }
        //check for no lives left and trigger the end of the game
        if(lives<=0){
            lives=0;
            GameOver();
        }
       

        livesText.text="Lives:"+lives;
    }
    public void UpdateScore(int points){
        score+=points;

        scoreText.text="Score:"+score;
    }
    public void UpdateNumberOfBricks(){
        numberOfBricks--;
        if(numberOfBricks<=0){
            if(currentLevelIndex>=levels.Length-1){
            GameOver();
            }else{
             loadLevelPanel.SetActive(true);
             loadLevelPanel.GetComponentInChildren<Text>().text="Level"+(currentLevelIndex+2);
             gameOver=true;
             Invoke("LoadLevel",3f);   
            }
        }
    }
    void LoadLevel(){
        currentLevelIndex++;
        // currentLevelIndex
        Instantiate(levels[currentLevelIndex],Vector2.zero,Quaternion.identity);
        numberOfBricks=GameObject.FindGameObjectsWithTag("Brick").Length;
        score=0;
        lives=3;
        scoreText.text="Score:"+score;
        if(loadLevelPanel.activeSelf){
          bscript.ballrb.velocity=Vector2.zero;
          bscript.inPlay=false;
           // Increment the ball's speed based on the current level
         bscript.speed += currentLevelIndex * 20; // Increases speed by 5 units per level

         // Optionally clamp the ball speed if you want to avoid it going too fast
         bscript.speed = Mathf.Clamp(bscript.speed, 200, 350); // Adjust the range as needed
        }
        gameOver=false;
        loadLevelPanel.SetActive(false);
    }
    void GameOver(){
        gameOver=true;
        gameOverPanel.SetActive(true);
        if(gameOverPanel.activeSelf){
          bscript.ballrb.velocity=Vector2.zero;
          bscript.inPlay=false;
        }
        // int highScore=PlayerPrefs.GetInt("HIGHSCORE");
        // if(score>highScore){
        //     PlayerPrefs.SetInt("HIGHSCORE",score);
        //     highScoreText.text= "New High Score:"+score;
        // }else{
        //     highScoreText.text="High Score"+highScore+"\n"+"Can You Beat it";
        // }
    }
    public void PlayAgain(){
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
    public void Quit(){
        Application.Quit();
        Debug.Log("Game quit");
    }
}
