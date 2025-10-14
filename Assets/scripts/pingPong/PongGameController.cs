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
    public GameObject GameControl;
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
    public GameStates gameState
    {
        get => _gameState;
        private set => _gameState = value;
    }
    private GameStates _prevGameState = GameStates.WAITING;

    public bool isGameStarted { get; private set; } = false;
    public bool isGameFinished { get; private set; } = false;
    public bool isGamePaused { get; private set; } = false;
    public bool isBallSpawned { get; private set; } = false;
    public bool isBallHitted { get; private set; } = false;
    public bool isBallMissed { get; private set; } = false;
  
    public bool runOnce = false;

    public void Awake()
    {
     
        Instance = this;
        

    }
    void Start () {
       
        MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
        pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
		finishObjects = GameObject.FindGameObjectsWithTag("ShowOnFinish");
		hideFinished();
        gameState = GameStates.WAITING;
        AppData.Instance.userData.readParseSessionData(DataManager.sessionFile);
        if (AppData.Instance.selectedMovement.trialNumberDay >= AppData.Instance.userData.moveTimePrsc[AppData.Instance.selectedMovement.name])
        {
            reminderPanel.SetActive(true);

        }
        else
        {
            reminderPanel.SetActive(false);

        }

    }
	
	void Update () {


        pointCounter.text = enemyScore + "\t\t\t" +playerScore;
        if (timerTxt != null)
        {
            timerTxt.text = "Time:" + Mathf.CeilToInt(trialTimeLeft).ToString() + "s"; // Show remaining time
        }

        if ((Input.GetKeyDown(KeyCode.P) && !isGameFinished) || (buttonPressed && !isGameFinished))
        {
            if(gameState == GameStates.WAITING && buttonPressed)
            {
                isGameStarted = true;
                buttonPressed = false;
                return;
            }
            if (!isPaused)
            {
                pauseGame();
            }
            else
            {
                resumeGame();
               
            }
            buttonPressed = false;
        }
        if ((isGameFinished && Input.GetKeyDown(KeyCode.P)) || (isGameFinished && buttonPressed && gameState == GameStates.STOP))
        {
            Reload();
            buttonPressed = false;
        }
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
        {
            GameControl.gameObject.SetActive(!GameControl.gameObject.activeSelf);

        }

        gameSpeedTxt.text = gameSpeed.ToString();
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
        AppLogger.LogInfo("Mars button released.");
        buttonPressed = true;
      
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
