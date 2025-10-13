

using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class spaceShooterGameContoller : MonoBehaviour
{
    public static spaceShooterGameContoller Instance { get; private set; }

    // List of scenes to change to.
    private readonly string robotCalibScene = "ROBOTCALIB";
    private readonly string marsSetUp = "MARSSETUP";
    public readonly string moveSelect = "CHOOSEMOVE";

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
    private float eventDelayTimer = 0f; 
    private float gameSpeed = 1f;
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
    public bool isInitialized { get; private set; } = false;


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
        if (Instance == null)
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
        
        // Initialize AppData if needed
        if (AppData.Instance.userData == null)
        {
            AppData.Instance.Initialize(SceneManager.GetActiveScene().name);
        }

        // Check if the directory exists
        if (!Directory.Exists(DataManager.basePath)) Directory.CreateDirectory(DataManager.basePath);
        if (!File.Exists(DataManager.configFile)) SceneManager.LoadScene("CONFIG");

        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

        // If the robot is not calibrated go to the robot calib scene.
        if (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB")
        {
            SceneManager.LoadScene(robotCalibScene);
        }
        // If the robot is not in position control go to the mars setup scene.
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] != "POSITION")
        {
            SceneManager.LoadScene(marsSetUp);
        }
        // Check if movement is selected.
        if (AppData.Instance.selectedMovement == null)
        {
            SceneManager.LoadScene(moveSelect);
        }
        else
        {
            // Update session details
            AppData.Instance.updateSessionDetails();
            if (AppData.Instance.selectedMovement.trialNumberDay >= AppData.Instance.userData.moveTimePrsc[AppData.Instance.selectedMovement.name])
            {
                reminderPanel.SetActive(true);
            }
            else
            {
                reminderPanel.SetActive(false);
            }
            
            // Initialize the game GUI.
            initUI();

            // Game start flag.
            isGameStarted = false;

            // Attach the MARS button callback.
            MarsComm.OnMarsButtonReleased += onMarsButtonReleased;

            // Initialize the player.
            GameObject.FindGameObjectWithTag("Player").GetComponent<SSPlayerController>().Initialize();

            // Game ready to start.
            isInitialized = true;
        }
    }
 
    // Update is called once per frame
    void Update()
    {
        MarsComm.sendHeartbeat();

        // Nothing to do if not initialized
        if (!isInitialized) return;

        // Pause and Resume the game
        if (isGamePaused && gameState != GameStates.PAUSED) PauseGame();
        else if (!isGamePaused && gameState == GameStates.PAUSED) ResumeGame();

        // Update Timer
        if (timerText != null)
        {
            timerText.text = "TIMER:" + Mathf.FloorToInt(timer).ToString() + "s"; // Show remaining time
        }

        // Track Restart
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
      
    }

    private void FixedUpdate()
    {
        // Nothing to do if not initialized
        if (!isInitialized) return;

        // Initialization is done.
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
        if (IsGamePlaying())
        {
            gameSpeed = Mathf.Lerp(gameSpeed, targetSpeed, Time.deltaTime * smoothFactor);
            gameSpeedTxt.text = gameSpeed.ToString();
            scoreText.text = "SCORE:" + nSuccess.ToString();
            timer -= Time.deltaTime;
        }
            
           
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
                    AsteroidSpawner.Instance.SpawnAsteroid(
                        xMin: SSPlayerController.xScreenMin,
                        xMax: SSPlayerController.xScreenMax
                    );
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
            AppData.Instance.gameTime = gametime < gameDuration ? gametime : gameDuration;
            AppData.Instance.gameSpeed = gameSpeed;
            //stop trail
            AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
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


        gameSpeed = AppData.Instance.gameSpeed <= 1f ? gameSpeed : AppData.Instance.gameSpeed;

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
