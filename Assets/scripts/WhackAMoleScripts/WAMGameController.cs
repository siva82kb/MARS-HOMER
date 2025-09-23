
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WAMGameController : MonoBehaviour
{
    public static WAMGameController Instance;
    public GameObject gameOverPanel;
    public GameObject NextLevelPanel;
  
    public MoleController[] moles;
    public TextMeshProUGUI TimerText;
    private List<int> recentHoles = new List<int>();
    public HoleSelector hs;
    public GameObject targetObject;
    private MoleController currentMole;
    public Text messTxt;
    public Text gameSpeedTxt;
    public GameObject startImage;
    public GameObject PauseImage;
    public GameObject GameControl;
    public GameObject reminderPanel;

    public const float gameDuration = 60f;
    private float timer;
    private float eventDelayTimer = 0f;      
    private bool runOnce = false;
    public float gameSpeed = 1.5f;
    private float targetSpeed;
    public float smoothFactor = 5f;


    // Game score related variables.
    public int nTargets = 0;
    public int nSuccess = 0;
    public int nFailure = 0;

    public bool isGameStarted { get; private set; } = false;
    public bool isGameFinished { get; private set; } = false;
    public bool isGamePaused { get; private set; } = false;
    public bool isSuccess { get; private set; } = false;
    public bool isFailure { get; private set; } = false;

    public enum GameStates
    {
        WAITING = 0,
        START,
        STOP,
        PAUSED,
        POPUPMOLE,
        WAITFORHIT,
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

    public float waitTime;
    public Vector3 playerPosition { get; private set; }
    public Vector3? targetPosition { get; private set; }


    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        MarsComm.sendHeartbeat();
        initUI();
        AppData.Instance.updateSessionDetials();
        isGameStarted = false;
        gameSpeed = 5f;//default slow speed
        MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
        Debug.Log(AppData.Instance.selectedMovement.trialNumberDay + "," + AppData.Instance.userData.moveTimePrsc[AppData.Instance.selectedMovement.name]+"reminder");
        if (AppData.Instance.selectedMovement.trialNumberDay >= AppData.Instance.userData.moveTimePrsc[AppData.Instance.selectedMovement.name])
        {
            reminderPanel.SetActive(true);

        }
        else
        {
            reminderPanel.SetActive(false);

        }

    }
    void Update()
    { 
        MarsComm.sendHeartbeat();
        if (isGamePaused && gameState != GameStates.PAUSED) PauseGame();
        else if (!isGamePaused && gameState == GameStates.PAUSED) ResumeGame();

        if (TimerText != null)
        {
            TimerText.text = "Timer:" + Mathf.CeilToInt(timer) + "s";
        }
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
        {
            GameControl.gameObject.SetActive(!GameControl.gameObject.activeSelf);

        }
       
        gameSpeed = Mathf.Lerp(gameSpeed, targetSpeed, Time.deltaTime * smoothFactor);
        gameSpeedTxt.text = gameSpeed.ToString();
    }

    private void FixedUpdate()
    {
        RunStateMachine();
        playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
        if (currentMole == null) return;
        targetObject = currentMole.gameObject;
        targetPosition = targetObject != null ? targetObject.transform.position : null;

    }
    public void RunStateMachine()
    {
        if (IsGamePlaying()) timer -= Time.deltaTime;
        bool isTimeUp = timer < 0;
        switch (gameState)
        {
            case GameStates.WAITING:
                if (isGameStarted) gameState = GameStates.START;
                break;
            case GameStates.START:
                startGame();
                gameState = GameStates.POPUPMOLE;
                break;
            case GameStates.POPUPMOLE:
               
                if (eventDelayTimer <= 0f && !runOnce)
                {
                    spawnMole();
                    nTargets++;
                    eventDelayTimer = 0.05f;
                    runOnce = true;
                }
                else
                {
                    eventDelayTimer -= Time.deltaTime;
                    if (eventDelayTimer <= 0f)
                    {
                        gameState = GameStates.WAITFORHIT;
                        waitTime = gameSpeed;
                        runOnce = false;
                    }
                }
                break;
            case GameStates.WAITFORHIT:
                waitTime -= Time.deltaTime;
                if (waitTime <= 0f)
                {
                    gameState = GameStates.FAILURE;
                    nFailure++;
                }
                if (currentMole.HasBeenHit()) gameState = GameStates.SUCCESS;
                
               
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
                        if(gameState == GameStates.FAILURE) hideMole();
                        // Wait for the gamestate to be logged.
                        isFailure = false;
                        isSuccess = false;
                        gameState = isTimeUp ? GameStates.STOP : GameStates.POPUPMOLE;
                      
                        runOnce = false;
                    }
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
    public void initUI()
    {
        gameOverPanel.SetActive(false);
        messTxt.enabled = true;
        startImage.SetActive(true);
        PauseImage.SetActive(false);
    }
    public void spawnMole()
    {
        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, moles.Length);
        } while (recentHoles.Contains(randomIndex) && moles.Length > 2);

        recentHoles.Add(randomIndex);
        if (recentHoles.Count > 2) recentHoles.RemoveAt(0);
        currentMole = moles[randomIndex];
        currentMole.PopUPMole(true);
    }
    public void hideMole()
    {
        if (currentMole != null)
            currentMole.failDownMole();
    }
    public bool IsGamePlaying()
    {
        return gameState != GameStates.WAITING
            && gameState != GameStates.PAUSED
            && gameState != GameStates.STOP;
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
            //cal gameTime
            int gametime = (int)gameDuration - (int)timer;
            AppData.Instance.gameTime = gametime < gameDuration ? gametime : gameDuration;
            AppData.Instance.gameSpeed = gameSpeed;
            //stop trail
            AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
            
        }
        isGameFinished = true; // Set game over state 
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
        startImage.SetActive(false);
        messTxt.enabled = false;
        timer = gameDuration; // Initialize timer 
        gameSpeed = AppData.Instance.gameSpeed <= 0 ? gameSpeed : AppData.Instance.gameSpeed;
        targetSpeed = gameSpeed;
    }

    //used on ui button
    public void IncreaseSpeed()
    {
        targetSpeed = Mathf.Clamp(targetSpeed - 0.2f, 1f, 5f); // step change in target
    }

    public void DecreaseSpeed()
    {
        targetSpeed = Mathf.Clamp(targetSpeed + 0.2f, 1f, 5f);
    }

    public void onClickExit()
    {
        if (gameState!=GameStates.WAITING && gameState!=GameStates.STOP)
        {
            gameOver();

        }
      
        SceneManager.LoadScene("CHOOSEMOVE");
    }
}


