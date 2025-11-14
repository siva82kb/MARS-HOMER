using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DCGameController : MonoBehaviour
{
    public static DCGameController Instance;
    
    // Game start/pause variables.
    public GameObject startImage;
    public GameObject PauseImage;
    
    // Timer and score related UI elements.
    public TextMeshProUGUI TimerText;
    public TextMeshProUGUI scoreText;
    
    // Game over panel UI
    public GameObject gameOverPanel;

    // Game speed control prefab.
    public GameObject gameSpeedControl;

    // Game time completion reminder panel.
    public GameObject reminderPanel;
    
    // Game target related variables.
    public GameObject targetObject;
    public GameObject targetBubblePrefab;
    public GameObject successTimerPrefab;
    public GameObject targetTimerPrefab;
    private GameObject targetBubble;
    private GameObject successTimer;
    private GameObject targetTimer;
    Animator targetAnim;
    public GameObject targetPrefab;
    public GameObject target;
    public ParticleSystem targetGlitterPrefab;
    private ParticleSystem targetGlitter;
    public ParticleSystem catchGlitterPrefeb;
    private ParticleSystem catchGlitter;
    public AudioSource audioSource;
    public AudioClip playerIn;
    public AudioClip playerOut;
    public AudioClip TargetFailed;
    public TextMeshProUGUI cummulativeScoreTxt;


    // UI Canvas
    public Canvas uiCanvas;
    
    // Other game logic variables.
    private float gameTimeLeft;
    private float eventDelayTimer = 0f;
    private bool runOnce = false;
    private float insideTargetTimer = 0f;

    // Game score related variables.
    public int nTargets = 0;
    public int nSuccess = 0;
    public int nFailure = 0;

    public bool isGameStarted { get; private set; } = false;
    public bool isGameFinished { get; private set; } = false;
    public bool isGamePaused { get; private set; } = false;
    public bool isSuccess { get; set; } = false;
    public bool isFailure { get; set; } = false;
    public bool isGamePlaying => gameState == GameStates.WAITFORCATCH
            || gameState == GameStates.SPAWNDIAMOND
            || gameState == GameStates.PLAYERIN
            || gameState == GameStates.PLAYEREXIT
            || gameState == GameStates.SUCCESS
            || gameState == GameStates.FAILURE;
    public enum GameStates
    {
        WAITING = 0,
        START,
        STOP,
        PAUSED,
        SPAWNDIAMOND,
        WAITFORCATCH,
        PLAYERIN,
        PLAYEREXIT,
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

    private float reachTimeLeft;
    public Vector3 playerGamePosition { get; private set; }
    public Vector3? targetGamePosition { get; private set; }
    public Vector3? targetEndPointPosition { get; private set; }
    public float gameDuration = MarsGameDefs.GAMEDURATION["DC"];
    public bool gameSpeedChanged { get; private set; } = false;
    private float reachDuration;
    public bool debug;
    private void Awake() => Instance = this;
    
    void Start()
    {
        MarsComm.sendHeartbeat();
        initUI();
        isGameStarted = false;

        // Initialize the game speed controller.
        initializeGameSpeedController();
        gameSpeedControl.SetActive(false);
        // Compute reach duration.
        reachDuration = MarsGameDefs.GetReachDurationForGame("DC", AppData.Instance.selectedGame.reachSpeed, AppData.Instance.selectedGame.arom);
        AppLogger.LogInfo($"Reach duration for game 'DC' with reach speed {AppData.Instance.selectedGame.reachSpeed} m/s is {reachDuration} seconds.");

        // Check if the required amount fo trials for the selected movement has been completed today.
        bool isRequiredTrialsCompleted = AppData.Instance.selectedMovement.trialNumberDay >= AppData.Instance.userData.moveTimePrsc[AppData.Instance.selectedMovement.name];
        if (isRequiredTrialsCompleted) reminderPanel.SetActive(true);
        else reminderPanel.SetActive(false);
        
        // Attach event handler to Mars button release event.
        MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
    }

    void Update()
    {
        MarsComm.sendHeartbeat();
        // Update the gameTimeLeft and score text.
        if (isGamePlaying)
        {
            TimerText.text = "Timer:" + Mathf.CeilToInt(gameTimeLeft) + "s";
            scoreText.text = "Score:" + nSuccess;
        }
        if (isGamePaused && gameState != GameStates.PAUSED) PauseGame();
        else if (!isGamePaused && gameState == GameStates.PAUSED) ResumeGame();

        // Check of the game speed controller is to be shown.
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
        {
            gameSpeedControl.SetActive(!gameSpeedControl.activeSelf);
        }
    }

    private void FixedUpdate()
    {
        MarsComm.sendHeartbeat();

        // Run the statemachine
        RunStateMachine();
        
        // Update player and target positions.
        if (isGamePlaying)
        {
            // Player game position.
            playerGamePosition = GameObject.FindGameObjectWithTag("Player").transform.position;
            if (target == null)
            {
                targetEndPointPosition = null;
                targetGamePosition = null;
            }
            else
            {
                // Target game position.
                targetObject = target.gameObject;
                targetGamePosition = targetObject != null ? targetObject.transform.position : null;
                // Target endpoint position.
                targetEndPointPosition = targetObject != null ? targetEndPointPosition : null;
            }
        }
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
    
    public void RunStateMachine()
    {
        // Decrement the gameTimeLeft if game is playing.
        if (isGamePlaying) gameTimeLeft -= Time.deltaTime;

        // Check if time is up.
        bool isTimeUp = gameTimeLeft < 0;
        switch (gameState)
        {
            case GameStates.WAITING:
                if (isGameStarted) gameState = GameStates.START;
                break;
            case GameStates.START:
                startGame();
                gameState = GameStates.SPAWNDIAMOND;
                eventDelayTimer = 0.05f;
                runOnce = false;
                break;
            case GameStates.SPAWNDIAMOND:
                if (!runOnce)
                {
                    if (target != null) return;
                    // Spawn the new target.
                    clearObjects();
                    SpawnDiamond();
                    nTargets++;
                    eventDelayTimer = 0.5f;
                    runOnce = true;
                }
                else
                {
                    eventDelayTimer -= Time.deltaTime;
                    if (eventDelayTimer <= 0f)
                    {
                        reachTimeLeft = reachDuration;
                        //Debug.Log(reachDuration + "reach");
                        runOnce = false;
                        gameState = GameStates.WAITFORCATCH;
                    }
                }
                break;
            case GameStates.WAITFORCATCH:
                reachTimeLeft -= Time.deltaTime;
                targetTimer.GetComponent<Image>().fillAmount = reachTimeLeft / reachDuration;
                if (reachTimeLeft <= 0f) 
                {
                    nFailure++;
                    audioSource.PlayOneShot(TargetFailed);
                    gameState = GameStates.FAILURE;
                }
                break;
            case GameStates.PLAYERIN:
                // Increment time.               
                insideTargetTimer += Time.deltaTime;
                // Update in target animation.
                successTimer.GetComponent<Image>().fillAmount = insideTargetTimer / MarsGameDefs.DiamondCatcher.TARGET_IN_TIME;
                if (insideTargetTimer >= MarsGameDefs.DiamondCatcher.TARGET_IN_TIME)
                {
                    targetAnim.Play("disappear", -1, 0f);
                    gameState = GameStates.SUCCESS;
                    insideTargetTimer = 0f;
                    eventDelayTimer = 0.5f;
                    nSuccess++;
                    DCPlayer.instance.AddScore();
                }
                break;
            case GameStates.PLAYEREXIT:
                gameState = GameStates.WAITFORCATCH;
                insideTargetTimer = 0f;
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
                    gameState = isTimeUp ? GameStates.STOP : GameStates.SPAWNDIAMOND;
                    // Clean up the target and related objects.
                    //if (target != null) Destroy(target);
                    //if (targetGlitter != null) Destroy(targetGlitter);
                    //if (targetBubble != null) Destroy(targetBubble);
                    //if (catchGlitter != null) Destroy(catchGlitter);
                    //if (successTimer != null) Destroy(successTimer);
                    //if (targetTimer != null) Destroy(targetTimer);
                    clearObjects();
                    runOnce = false;
                }
                break;
            case GameStates.STOP:
                gameOver();
                break;
            case GameStates.DONE:
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
        }
    }
  
    public void SetPlayerIn()
    {
        // Player is inside the target
        gameState = GameStates.PLAYERIN;
        // Show the animation for the successful reach into the target.
        if (successTimer != null) Destroy(successTimer);
        successTimer = Instantiate(successTimerPrefab, uiCanvas.transform);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(target.transform.position);
        successTimer.transform.position = new Vector3(screenPos.x, screenPos.y - 25f, screenPos.z);
        targetAnim.Play("idle", -1, 0f);
        catchGlitter = Instantiate(catchGlitterPrefeb, target.transform.position, Quaternion.identity);
        catchGlitter.Play();
        audioSource.PlayOneShot(playerIn);
        // Initialize the gameTimeLeft for being inside the target.
        insideTargetTimer = 0;
    }

    public void SetPlayerOut()
    {
        gameState = GameStates.PLAYEREXIT;
        if (successTimer != null)
        {
            Destroy(successTimer);
            targetAnim.Play("TargetHighlight", -1, 0f);
            audioSource.PlayOneShot(playerOut);
        }
        if (catchGlitter != null) catchGlitter.Stop(true);
    }
    public void clearObjects()
    {
        if (target != null) Destroy(target);
        if (targetGlitter != null) Destroy(targetGlitter);
        if (targetBubble != null) Destroy(targetBubble);
        if (catchGlitter != null) Destroy(catchGlitter);
        if (successTimer != null) Destroy(successTimer);
        if (targetTimer != null) Destroy(targetTimer);
    }
    public void SpawnDiamond()
    {
        // Generate the new target
        (Vector2 epTarget, Vector2 gTarget) = DCPlayer.instance.GenerateNextRandomTarget();
        
        // Update the target game and endpoint positions.
        targetGamePosition = new Vector3(gTarget.x, gTarget.y, 0);
        targetEndPointPosition = new Vector3(0, epTarget.y, epTarget.x);

        // Update the game target.
        Vector3 spawnPos = new Vector3(gTarget.x, gTarget.y, 0);

        // Instantiate the target, target glitter, target bubble and target timer.
        target = Instantiate(targetPrefab, spawnPos, Quaternion.identity);
        targetGlitter = Instantiate(targetGlitterPrefab, spawnPos, Quaternion.identity);
        targetBubble = Instantiate(targetBubblePrefab, new Vector3(spawnPos.x, spawnPos.y - 0.25f, spawnPos.z), Quaternion.identity);
        targetTimer = Instantiate(targetTimerPrefab, uiCanvas.transform);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(target.transform.position);
        targetTimer.transform.position = new Vector3(screenPos.x + 20f, screenPos.y + 50f, screenPos.z);
        if (target.transform.position.x > 0) target.GetComponent<SpriteRenderer>().flipX = true;
        targetAnim = target.GetComponentInChildren<Animator>();
        targetAnim.Play("TargetHighlight", -1, 0f);
    }
    
    public void initUI()
    {
        gameOverPanel.SetActive(false);
        startImage.SetActive(true);
        PauseImage.SetActive(false);
    }

    public void onMarsButtonReleased()
    {
        //To pause and resume
        if (gameState == GameStates.WAITING) isGameStarted = true;
        else if (gameState != GameStates.STOP) isGamePaused = !isGamePaused;

        //To restart
        if (gameState == GameStates.STOP && isGameFinished)
        {
            gameState = GameStates.DONE;
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
            //cal gameTime
            int gametime = (int)gameDuration - (int)gameTimeLeft;
            AppData.Instance.gameTime = gametime < gameDuration ? gametime : gameDuration;
          
            //stop trail
            AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
            cummulativeScoreTxt.text = $"{AppData.Instance.selectedGame.cummulativeHits:D4}";


        }
        isGameFinished = true; // Set game over state 
    }

    public void startGame()
    {
        // Hide the reminder panel.
        reminderPanel.SetActive(false);

        // Initialize the targets, hits and misses.
        nSuccess = 0;
        nFailure = 0;
        nTargets = 0;

        // Reset success and failure flags.
        isFailure = false;
        isSuccess = false;

        // Hide the start image.
        startImage.SetActive(false);
        gameDuration = MarsGameDefs.GAMEDURATION["DC"];
        gameTimeLeft = gameDuration;

        // Start the next new Trail
        AppData.Instance.StartNewTrial();
    }

    public void onClickExit()
    {
        if (gameState != GameStates.WAITING && gameState != GameStates.STOP)
        {
            gameOver();
        }
        isGamePaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("CHOOSEMOVE");
    }
}






