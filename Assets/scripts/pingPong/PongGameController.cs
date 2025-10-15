using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.ComponentModel;


public class pongGameController : MonoBehaviour {

    public static pongGameController Instance;

    GameObject[] pauseObjects, finishObjects;
	public BoundController rightBound;
	public BoundController leftBound;

    public Image SupportSlider;
    public TextMeshProUGUI support;
    public Text timerTxt, pointCounter;
    public Text gameSpeedTxt;
    public GameObject exitBtn;
    public GameObject gameSpeedControl;
    public bool isPaused  = false;
    public bool buttonPressed = false;
    public bool playerWon, enemyWon;
    public AudioClip[] audioClips; // winlevel loose
    public GameObject reminderPanel;

    public int enemyScore, playerScore;
    public float trialTimeLeft,
                eventDelayTimer,
                trialDuration,
                gameSpeed;
    public EnemyController enemy;
    public BallController ballSpeed;
    // Game score related variables.
    public int nTargets = 0;
    public int nSuccess = 0;
    public int nFailure = 0;

    // Target and player positions
    public Vector3? targetPosition { get; private set; }
    public Vector3 playerPosition { get; private set; }

    public GameObject targetObject;
    private bool gameSpeedChanged = false;

    //pong game events and related variables.
    public enum GameStates
    {
        WAITING = 0,
        START,
        STOP,
        PAUSED,
        SPAWNBALL,
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

    public bool isGameStarted { get; private set; } = false;
    public bool isGameFinished { get; private set; } = false;
    public bool isGamePaused { get; private set; } = false;
    public bool isBallSpawned { get; private set; } = false;
    public bool isBallHitted { get; private set; } = false;
    public bool isBallMissed { get; private set; } = false;
    public bool restartGame = false;
    public bool isGamePlaying => gameState == GameStates.MOVE
            || gameState == GameStates.SPAWNBALL
            || gameState == GameStates.SUCCESS
            || gameState == GameStates.FAILURE;

    public bool runOnce = false;
    

    public void Awake() => Instance = this;
    
    void Start ()
    {
        MarsComm.sendHeartbeat();
        // Get the objects to show/hide on pausing or finishing the game.
        pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
		finishObjects = GameObject.FindGameObjectsWithTag("ShowOnFinish");
        hideFinished();

        // Initialize game state.
        gameState = GameStates.WAITING;

        // Read session data.
        AppData.Instance.userData.readParseSessionData(DataManager.sessionFile);

         // Initialize the game speed controller.
        initializeGameSpeedController();
        gameSpeedControl.SetActive(false);
        
        // Check if the required amount of trails for the selected movement is completed.
        bool isRequiredTrialsCompleted = AppData.Instance.selectedMovement.trialNumberDay >= AppData.Instance.userData.moveTimePrsc[AppData.Instance.selectedMovement.name];
        if (isRequiredTrialsCompleted) reminderPanel.SetActive(true);
        else reminderPanel.SetActive(false);

        // Attach event handler for Mars button release.
        MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
    }

    void Update()
    {
        // Update the point counter.
        pointCounter.text = enemyScore + "\t\t\t" + playerScore;
        timerTxt.text = "Time:" + Mathf.FloorToInt(trialTimeLeft).ToString() + "s";

        // Check if the game is to be restarted.
        if (restartGame)
        {
            Reload();
            restartGame = false;
        }

        // Update game speed.
        if (gameSpeedChanged)
        {
            gameSpeedChanged = false;
            BallController.instance.speed = AppData.Instance.selectedGame.gameSpeed;
        }
        
        // Check of the game speed controller is to be shown.
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
        {
            gameSpeedControl.SetActive(!gameSpeedControl.activeSelf);
        }
    }

    public void FixedUpdate()
    {
        MarsComm.sendHeartbeat();

        // Run the state machine
        RunStateMachine();

        // Update player and target positions
        if (isGamePlaying)
        {
            playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
            targetObject = GameObject.FindGameObjectWithTag("Target");
            targetPosition = targetObject != null ? targetObject.transform.position : null;
        }
    }
    
    public void RunStateMachine()
    {
        // Check if the game is paused or to be paused/resumed.
        if (isGamePaused) pauseGame();
        else if (gameState == GameStates.PAUSED) resumeGame();

        // Update the trial timer if the game is playing.
        if (isGamePlaying) trialTimeLeft -= Time.deltaTime;
        // Check if the trial time is up.
        bool isTimeUp = trialTimeLeft < 0;
        switch (gameState)
        {
            case GameStates.WAITING:
                showPaused();
                if (isGameStarted) gameState = GameStates.START;
                break;
            case GameStates.START:
                hidePaused();
                startGame();
                break;
            case GameStates.SPAWNBALL:
                nTargets++;
                gameState = GameStates.MOVE;
                break;
            case GameStates.MOVE:
                if (isBallHitted) gameState = GameStates.SUCCESS;
                if (isBallMissed) gameState = GameStates.FAILURE;
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
                        isBallHitted = false;
                        isBallMissed = false;
                        gameState = isTimeUp ? GameStates.STOP : GameStates.SPAWNBALL;
                        runOnce = false;
                    }
                }
                break;
            case GameStates.STOP:
                gameOver();
                break;
        }

    }

    public void startGame()
    {
        // Disable the reminder panel if it is showing.
        if (reminderPanel.activeSelf) reminderPanel.SetActive(false);

        // Start the new trial.
        AppData.Instance.StartNewTrial();
        
        // Set the trial duration
        trialDuration = MarsGameDefs.GAMEDURATION[AppData.Instance.selectedGame.name];
        nTargets = 0;
        nSuccess = 0;
        nFailure = 0;

        // Trial Time
        trialTimeLeft = trialDuration;
        
        // Spawing the ball.
        gameState = GameStates.SPAWNBALL;
        gameSpeed = AppData.Instance.selectedGame.gameSpeed;
    }

    public void gameOver()
    {
        if (!isGameFinished)
        {
            showFinished();
            float gameTime = trialDuration - trialTimeLeft;
            AppData.Instance.gameTime = (gameTime < trialDuration) ? (int)gameTime : trialDuration;
            // AppData.Instance.selectedGame.gameSpeed = gameSpeed;
            AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
        }
        isGameFinished = true;
    }
    
    private void pauseGame()
    {
        _prevGameState = gameState;
        gameState = GameStates.PAUSED;
        Time.timeScale = 0;
        isGamePaused = true;
        isPaused = true;
        showPaused();
        exitBtn.SetActive(false);
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
        gameSpeedChanged = true;
    }

    public void ExitGame()
    {
        if (gameState != GameStates.WAITING && gameState != GameStates.STOP)
        {
            gameOver();
        }

        SceneManager.LoadScene("CHOOSEMOVE");
    }
    
    private void resumeGame()
    {
        gameState = _prevGameState;
        Time.timeScale = 1;
        isGamePaused = false;
        isPaused = false;
        hidePaused();
        exitBtn.SetActive(true);

    }
    
    public void BallHitted()
    {
        isBallHitted = true;
        isBallMissed = false;
        nSuccess++;
    }
    public void BallMissed()
    {
        isBallHitted = false;
        isBallMissed = true;
        nFailure++;
    }

    public void onMarsButtonReleased()
    {
        // Act according to the current game state.
        if (gameState == GameStates.WAITING)
        {
            // Start the game.
            isGameStarted = true;
            isGamePaused = false;
            return;
        }
        else if (isGamePlaying)
        {
            // Pause the game if it is currently playing.
            isGamePaused = !isGamePaused;
            return;
        }
        // Check if game is done. Then this is a request to restart the game.
        else if (gameState == GameStates.STOP)
        {
            // Restart the game.
            restartGame = true;
            return;
        }
    }
    
    public void Reload()
    {
        playerScore = enemyScore = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void showPaused(){
		foreach(GameObject g in pauseObjects){
			g.SetActive(true);
		}
	}

	public void hidePaused(){
		foreach(GameObject g in pauseObjects){
			g.SetActive(false);
		}
	}

	public void showFinished(){

        foreach (GameObject g in finishObjects){
			g.SetActive(true);
		}
	}
	
	public void hideFinished(){
		foreach(GameObject g in finishObjects){
			g.SetActive(false);
		}
	}
    private void OnDestroy()
    {
        MarsComm.OnMarsButtonReleased -= onMarsButtonReleased;
    }


}
