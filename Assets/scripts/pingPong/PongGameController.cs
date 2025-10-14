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

    public bool runOnce = false;
    
    public bool isPlayingState(GameStates state)
    {
        return state == GameStates.MOVE
            || state == GameStates.SPAWNBALL
            || state == GameStates.SUCCESS
            || state == GameStates.FAILURE;
    }

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

        // Pause/Resume the game
        if (isGameStarted && isGamePaused) pauseGame();
        else if (isGameStarted && !isGamePaused) resumeGame();

        // Check if the game is to be restarted.
        if (restartGame)
        {
            Reload();
            restartGame = false;
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
        RunStateMachine();
        if (IsGamePlaying())
        {
            
            playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
            targetObject = GameObject.FindGameObjectWithTag("Target");
            targetPosition = targetObject != null ? targetObject.transform.position : null;
          
        }
       
    }
    public void RunStateMachine()
    {
        if (isGamePaused) pauseGame();
        else if (gameState == GameStates.PAUSED) resumeGame();
        if (IsGamePlaying())trialTimeLeft -= Time.deltaTime;
      
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
        reminderPanel.SetActive(false);
        AppData.Instance.StartNewTrial();
        trialDuration = 60f;
        nTargets = 0;
        nSuccess = 0;
        nFailure = 0;
        //Trial Time
        trialTimeLeft = trialDuration;
        gameState = GameStates.SPAWNBALL;
        gameSpeed = AppData.Instance.selectedGame.gameSpeed;
     
    }
    public void gameOver()
    {
        if (!isGameFinished)
        {
            showFinished();
            float gameTime = trialDuration - trialTimeLeft;
            AppData.Instance.gameTime = (gameTime < trialDuration) ?(int) gameTime : trialDuration;
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
    public bool IsGamePlaying()
    {
        return gameState != GameStates.WAITING
            && gameState != GameStates.PAUSED
            && gameState != GameStates.STOP;
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
    public void increaseGameSpeed()
    {
        if (gameSpeed >= 5.0f) return;
        gameSpeed += 0.2f;
        UpdateGameSpeeds();
    }
    public void decreaseGameSpeed()
    {

      
        if (gameSpeed <= 1.5f) return;
        gameSpeed -= 0.2f;
        UpdateGameSpeeds();

    }
    private void UpdateGameSpeeds()
    {
        gameSpeed = Mathf.Clamp(gameSpeed, 1.5f, 5.0f);
        BallController.instance.speed = gameSpeed;
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
        else if (isPlayingState(gameState))
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
