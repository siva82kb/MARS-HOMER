
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using TMPro;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class spaceShooterGameContoller : MonoBehaviour
{
    public static spaceShooterGameContoller Instance { get; private set; }
   
    public GameObject GameOverPanel;

    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public Text messTxt;
    public Text gameSpeedTxt;
    public GameObject startImage;
    public GameObject PauseImage;
    public GameObject GameControl;
    public float smoothFactor = 5f;
    public GameObject newSpaceshipPanel;
    public GameObject reminderPanel;

    public bool Levelunlocked = false;

    private float timer;
    public static bool changeScene = false;
    private float eventDelayTimer = 0f, 
                  gameSpeed = 1f;
    private float targetSpeed;
  
    private bool runOnce = false;

    // Game score related variables.
    public int nTargets = 0;
    public int nSuccess = 0;
    public int nFailure = 0;

    public bool isGameStarted { get; private set; } = false;
    public bool isGameFinished { get; private set; } = false;
    public bool isGamePaused { get; private set; } = false;
    public bool isSuccess { get; private set; } = false;
    public bool isFailure { get; private set; } = false;

    public float gameDuration = 60f; // Game duration in seconds


    public void setisSuccess()
    {
        isSuccess = true;
        nSuccess++;
    }
    public void setisFailure()
    {
        isFailure = true;
    }

    public enum GameStates
    {
        WAITING = 0,
        START,
        STOP,
        PAUSED,
        SPAWNASTROID,
        MOVE,
        SUCCESS,
        FAILURE,
        DONE
    }
   
    private GameStates _gameState;
    private GameStates _prevGameState = GameStates.WAITING;
    public GameStates gameState
    {
        get => _gameState;
        private set => _gameState = value;
    }
 
   
    public Vector3 playerPosition {  get; private set; }
   
    public Vector3? targetPosition { get; private set; }
    public GameObject targetObject;

  
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        MarsComm.sendHeartbeat();
        initUI();
        isGameStarted = false;
        MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
        AppData.Instance.updateSessionDetails();
        if (AppData.Instance.selectedMovement.trialNumberDay >= AppData.Instance.userData.moveTimePrsc[AppData.Instance.selectedMovement.name])
        {
            reminderPanel.SetActive(true);

        }
        else
        {
            reminderPanel.SetActive(false);

        }
    }
 
    // Update is called once per frame
    void Update()
    {
        MarsComm.sendHeartbeat();
        if (isGamePaused && gameState != GameStates.PAUSED) PauseGame();
        else if (!isGamePaused && gameState == GameStates.PAUSED) ResumeGame();

        if (timerText != null)
        {
            timerText.text = "TIMER:" + Mathf.CeilToInt(timer).ToString() + "s"; // Show remaining time
        }

        //Track Restart
        if (changeScene && gameState == GameStates.STOP)
        {
            restart();
            changeScene = false;
        }
        else
        {
            changeScene = false;
        }
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
        {
            GameControl.gameObject.SetActive(!GameControl.gameObject.activeSelf);
          
        }
        //Game Speed - for GameObject Smooth Transition
        gameSpeed = Mathf.Lerp(gameSpeed, targetSpeed, Time.deltaTime * smoothFactor);
        gameSpeedTxt.text = gameSpeed.ToString();
        scoreText.text = "SCORE:" + nSuccess.ToString();
    }

    private void FixedUpdate()
    {
        RunStateMachine();
        playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
        targetObject = GameObject.FindGameObjectWithTag("Asteroid");
        targetPosition = targetObject != null ? targetObject.transform.position : null;
    }

   public void initUI()
    {
        messTxt.enabled = true;
        startImage.SetActive(true);
        PauseImage.SetActive(false);
    }
    public bool IsGamePlaying()
    {
        return gameState != GameStates.WAITING
            && gameState != GameStates.PAUSED
            && gameState != GameStates.STOP;
    }
    public void RunStateMachine()
    {
        if(IsGamePlaying()) timer -= Time.deltaTime;
        bool isTimeUp = timer < 0; 
        switch (gameState)
        {
            case GameStates.WAITING:
                if (isGameStarted) gameState = GameStates.START;
                break;
            case GameStates.START:
                startGame();
                gameState = GameStates.SPAWNASTROID;
                break;
            case GameStates.SPAWNASTROID:
                if (AsteroidSpawner.Instance == null)
                    break;
                if (eventDelayTimer <= 0f && !runOnce)
                {
                    AsteroidSpawner.Instance.SpawnAsteroid();
                    AsteroidFall.instance.SetFallSpeed(gameSpeed);
                    nTargets++;
                    eventDelayTimer = 0.05f;
                    runOnce = true;
                }
                else
                {
                    eventDelayTimer -= Time.deltaTime;
                    if (eventDelayTimer <= 0f)
                    {
                        gameState = GameStates.MOVE;
                        runOnce = false;
                    }
                }
                break;
            case GameStates.MOVE:
                if (isSuccess) gameState = GameStates.SUCCESS;
                if(isFailure)gameState = GameStates.FAILURE;
                break;
            case GameStates.PAUSED:
                Debug.Log(isGamePaused);
                break;
            case GameStates.SUCCESS:
            case GameStates.FAILURE:
                if (eventDelayTimer <= 0f)
                {
                    eventDelayTimer = 0.05f;
                }
                else
                {
                    eventDelayTimer -= Time.deltaTime;
                    if (eventDelayTimer <= 0f)
                    {
                        // Wait for the gamestate to be logged.
                        isFailure = false;
                        isSuccess = false;
                        gameState = isTimeUp ? GameStates.STOP : GameStates.SPAWNASTROID;
                        runOnce = false;
                    }
                }
                break;
            case GameStates.STOP:
                gameOver();
                break;
        }

    }
    public void IncreaseSpeed()
    {
        targetSpeed = Mathf.Clamp(targetSpeed + 0.5f, 1f, 5f); // step change in target
    }

    public void DecreaseSpeed()
    {
        targetSpeed = Mathf.Clamp(targetSpeed - 0.5f, 1f, 5f);
    }
  
    public void onMarsButtonReleased()
    {
        //To pause and resume
        if (gameState == GameStates.WAITING) isGameStarted = true;
        else if (gameState != GameStates.STOP) isGamePaused = !isGamePaused;

        //To restart
        if (gameState == GameStates.STOP && isGameFinished)
        {
            changeScene = true;
        }

    }
    public void PauseGame()
    {
        _prevGameState = gameState;
        gameState = GameStates.PAUSED;
        PauseImage.SetActive(true);
        isGamePaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
       
        Debug.Log($"prev GS :{_prevGameState}");
        isGamePaused = false;
        gameState = _prevGameState;
        Time.timeScale = 1f;
        PauseImage.SetActive(false);
       
    }

    public void gameOver()
    {
        if (Levelunlocked == false && !isGameFinished)
        {
            GameOverPanel.SetActive(true);
            //cal gameTime
            int gametime = (int)gameDuration - (int)timer;
            AppData.Instance.gameTime = gametime< gameDuration ? gametime: gameDuration;
            AppData.Instance.gameSpeed = gameSpeed;
            //stop trail
            AppData.Instance.StopTrial(nTargets,nSuccess,nFailure);
        }
        timerText.text = "Time:0s";
        isGameFinished = true; // Set game over state 
    }
    public void onClickExit()
    {
        if (gameState != GameStates.WAITING && gameState != GameStates.STOP)
        {
            gameOver();

        }

        SceneManager.LoadScene("CHOOSEMOVE");
    }
    public void startGame()
    {
        reminderPanel.SetActive(false);
        //start new Trail
        AppData.Instance.StartNewTrial();

        nSuccess = 0;
        nFailure = 0;
        nTargets = 0;

        isFailure = false;
        isSuccess = false;

        timer = gameDuration; // Initialize timer 

        GameOverPanel.SetActive(false);
        startImage.SetActive(false);
        messTxt.enabled = false;
        
        gameSpeed = AppData.Instance.gameSpeed<=0 ? gameSpeed : AppData.Instance.gameSpeed;
        targetSpeed = gameSpeed;
       
    }

    public void restart()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        AppLogger.LogInfo($"The Game is Restarted '{currentSceneName}'.");

        SceneManager.LoadScene(currentSceneName);
    }

    
    public void Back_to_ChooseLevel()
    {
        SceneManager.LoadScene("CHOOSEMOVE");
    }
    public void UnlockShipPanel()
    {
        if (newSpaceshipPanel != null)
        {
            newSpaceshipPanel.SetActive(true);
            Levelunlocked = true;
            gameOver();

        }

    }
  
    private void OnApplicationQuit()
    {
        Application.Quit();
        ConnectToRobot.disconnect();
    }
}
