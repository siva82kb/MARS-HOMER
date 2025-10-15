

using System.Diagnostics.Eventing.Reader;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class SpaceShooterGameContoller : MonoBehaviour
{
    public static SpaceShooterGameContoller Instance { get; private set; }

    // List of scenes to change to.
    private readonly string robotCalibScene = "ROBOTCALIB";
    private readonly string marsSetUp = "MARSSETUP";
    public readonly string moveSelect = "CHOOSEMOVE";

    public GameObject gameOverPanel;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public GameObject gameSpeedControl;
    public bool gameSpeedChanged = false;
    public GameObject startImage;
    public GameObject PauseImage;
    public float smoothFactor = 5f;
    public GameObject newSpaceshipPanel;
    public GameObject reminderPanel;

    public bool Levelunlocked = false;

    private float timer;
    public static bool changeScene = false;
    private float eventDelayTimer = 0f; 
    // private float gameSpeed = 1f;
    private float targetSpeed;
  
    private bool runOnce = false;

    // Game score related variables.
    public int nTargets = 0;
    public int nSuccess = 0;
    public int nFailure = 0;

    public bool isGameStarted { get; private set; } = false;
    public bool isGameFinished { get; private set; } = false;
    public bool isSuccess { get; private set; } = false;
    public bool isGamePaused { get; private set; } = false;
    public bool isFailure { get; private set; } = false;

    public float gameDuration = MarsGameDefs.GAMEDURATION["SS"];
    public bool isInitialized { get; private set; } = false;

    public void setIsSuccess()
    {
        isSuccess = true;
        nSuccess++;
    }

    public void setIsFailure()
    {
        isFailure = true;
        nFailure++;
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
        private set
        {
            _prevGameState = _gameState;
            _gameState = value;
            AppLogger.LogInfo($"Game state changed from {_prevGameState} to {_gameState}.");
        }
    }
 
    public Vector3 playerPosition {  get; private set; }   
    public Vector3? targetPosition { get; private set; }
    public GameObject targetObject;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
            // Reload session details to get the latest data.
            AppData.Instance.userData.readParseSessionData(DataManager.sessionFile);

            // Check of the required amount of trials for the selected movement is completed.
            bool isRequiredTrialsCompleted = AppData.Instance.selectedMovement.trialNumberDay >= AppData.Instance.userData.moveTimePrsc[AppData.Instance.selectedMovement.name];
            if (isRequiredTrialsCompleted) reminderPanel.SetActive(true);
            else reminderPanel.SetActive(false);

            // Get game duration
            gameDuration = MarsGameDefs.GAMEDURATION["SS"];

            // Initialize the game speed controller.
            initializeGameSpeedController();
            gameSpeedControl.SetActive(false);
            
            // Initialize the game GUI.
            startImage.SetActive(true);
            PauseImage.SetActive(false);

            // Game start flag.
            isGameStarted = false;

            // Attach the MARS button callback.
            MarsComm.OnMarsButtonReleased += onMarsButtonReleased;

            // Initialize the player.
            GameObject.FindGameObjectWithTag("Player").GetComponent<SSPlayerController>().Initialize();
            GameObject.FindGameObjectWithTag("Player").GetComponent<SSPlayerFiringController>().Initialize();

            // Game ready to start.
            isInitialized = true;

            AppLogger.LogInfo("Space Shooter Game initialized.");
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

        // Update Timer (show remaining time)
        timerText.text = $"Time Left: {Mathf.FloorToInt(timer)}s";

        // Track Restart
        if (changeScene && gameState == GameStates.STOP)
        {
            restartGame();
            changeScene = false;
        }
        else
        {
            changeScene = false;
        }

        // Check of the game speed controller is to be shown.
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
        {
            gameSpeedControl.SetActive(!gameSpeedControl.activeSelf);
        }
    }

    private void FixedUpdate()
    {
        // Nothing to do if not initialized
        if (!isInitialized) return;

        // Initialization is done.
        RunStateMachine();
        
        // Update the player and target positions.
        playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
        targetObject = GameObject.FindGameObjectWithTag("Asteroid");
        targetPosition = targetObject != null ? targetObject.transform.position : null;
    }

    public bool IsGamePlaying()
    {
        return gameState != GameStates.WAITING
            && gameState != GameStates.PAUSED
            && gameState != GameStates.STOP;
    }

    public void RunStateMachine()
    {
        bool isGamePlaying = gameState != GameStates.WAITING && gameState != GameStates.PAUSED && gameState != GameStates.STOP;
        if (isGamePlaying)
        {
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
                eventDelayTimer = 0.05f;
                runOnce = false;
                break;
            case GameStates.SPAWNASTROID:
                if (AsteroidSpawner.Instance == null) break;
                if (!runOnce)
                {
                    AsteroidSpawner.Instance.SpawnAsteroid(
                        xMin: SSPlayerController.xScreenMin,
                        xMax: SSPlayerController.xScreenMax
                    );
                    AsteroidFall.instance.SetFallSpeed(AppData.Instance.selectedGame.gameSpeed);
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
                if (isSuccess)
                {
                    gameState = GameStates.SUCCESS;
                    eventDelayTimer = 0.05f;
                }
                if (isFailure)
                {
                    gameState = GameStates.FAILURE;
                    eventDelayTimer = 0.05f;
                }
                break;
            case GameStates.PAUSED:
                break;
            case GameStates.SUCCESS:
            case GameStates.FAILURE:
                eventDelayTimer -= Time.deltaTime;
                if (eventDelayTimer <= 0f)
                {
                    isFailure = false;
                    isSuccess = false;
                    gameState = isTimeUp ? GameStates.STOP : GameStates.SPAWNASTROID;
                    eventDelayTimer = 0.05f;
                    runOnce = false;
                }
                break;
            case GameStates.STOP:
                gameOver();
                break;
        }
    }

    public void onMarsButtonReleased()
    {
        // To pause, resume or restart the game.
        if (gameState == GameStates.WAITING) isGameStarted = true;
        else if (gameState != GameStates.STOP) isGamePaused = !isGamePaused;
        else if (gameState == GameStates.STOP && isGameFinished) changeScene = true;
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
        if (!isGameFinished)
        {
            gameOverPanel.SetActive(true);
            // Compute game time
            int gametime = (int)(gameDuration - timer);
            AppData.Instance.gameTime = gametime;
            // Stop the current game trial
            AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
            AppLogger.LogInfo($"Space Shooter Game Over. Time: {gametime}s | Targets: {nTargets} | Hits: {nSuccess} | Misses: {nFailure}");
        }
        timerText.text = "Time: 0s";
        // Set game over state
        isGameFinished = true; 
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
        // Hide reminder panel if visible
        reminderPanel.SetActive(false);

        // Start a new Trail
        AppData.Instance.StartNewTrial();

        // Initialize targets, hits, and misses
        nSuccess = 0;
        nFailure = 0;
        nTargets = 0;

        // Trial success/failure.
        isFailure = false;
        isSuccess = false;

        // Set game duration.
        timer = gameDuration;
        AppLogger.LogInfo($"Space Shooter Game started for movement '{AppData.Instance.selectedMovement.name}'. Game Speed: {AppData.Instance.selectedGame.gameSpeed} | Duration: {gameDuration}s");

        // Remove game over and start panel.
        gameOverPanel.SetActive(false);
        startImage.SetActive(false);
    }

    public void restartGame()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        AppLogger.LogInfo($"The Game is Restarted '{currentSceneName}'.");
        SceneManager.LoadScene(currentSceneName);
    }

    private void initializeGameSpeedController()
    {
        // Hide game speed control initially
        gameSpeedControl.SetActive(false);

        GameSpeedController gsc = gameSpeedControl.GetComponent<GameSpeedController>();
        if (gsc == null) return;

        // Attach the buttons
        if (gsc.decreaseButton != null)
            gsc.decreaseButton.onClick.AddListener(() => changeGameSpeed(false));
        if (gsc.increaseButton != null)
            gsc.increaseButton.onClick.AddListener(() => changeGameSpeed(true));

        // Set the initial game speed
        gsc.gameSpeedText.text = $"{AppData.Instance.selectedGame.gameSpeed:F2}";
    }
    
    public void changeGameSpeed(bool increase)
    {
        float _rs = AppData.Instance.selectedGame.reachSpeed;
        AppData.Instance.selectedGame.reachSpeed = _rs + (increase ? MarsGameDefs.REACH_SPEED_DELTA : -MarsGameDefs.REACH_SPEED_DELTA);
        AppData.Instance.annotation = $"RS:{AppData.Instance.selectedGame.reachSpeed:F3},GS:{AppData.Instance.selectedGame.gameSpeed:F3}";
    }
  
    private void OnApplicationQuit()
    {
        Application.Quit();
        ConnectToRobot.disconnect();
    }
}
