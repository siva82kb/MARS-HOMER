using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEngine.UI;
public class GameController : MonoBehaviour
{
  
    public static GameController Instance;
    public GameObject gameOverPanel;

   
    public TextMeshProUGUI TimerText;
    public TextMeshProUGUI scoreText;
    public GameObject targetObject;

    public Text gameSpeedTxt;
    public GameObject startImage;
    public GameObject PauseImage;
    public GameObject GameControl;
    public GameObject reminderPanel;
  
    public const float gameDuration = 60f;
    private float timer;
    private float eventDelayTimer = 0f;
    private bool runOnce = false;
    public float gameSpeed = 2f;
    private float targetSpeed;
    public float smoothFactor = 5f;

    public float xMin, xMax, yMin, yMax;
    // Game score related variables.
    public int nTargets = 0;
    public int nSuccess = 0;
    public int nFailure = 0;

    public bool isGameStarted { get; private set; } = false;
    public bool isGameFinished { get; private set; } = false;
    public bool isGamePaused { get; private set; } = false;
    public bool isSuccess { get; set; } = false;
    public bool isFailure { get; set; } = false;

    public GameObject target;
    public GameObject targerPrefeb;
    public AudioSource appleEatingSound;
    public enum GameStates
    {
        WAITING = 0,
        START,
        STOP,
        PAUSED,
        SPAWNFRUIT,
        WAITFOREAT,
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

    public bool isProcessingHit = false;
    public bool debug;
    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        if (debug)
        {
            gameState = GameStates.START;
            return;
        }
        MarsComm.sendHeartbeat();
        initUI();
        //AppData.Instance.updateSessionDetials();
        isGameStarted = false;
        gameSpeed = 5f;//default slow speed
        MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
        Debug.Log(AppData.Instance.selectedMovement.trialNumberDay + "," + AppData.Instance.userData.moveTimePrsc[AppData.Instance.selectedMovement.name] + "reminder");
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
        if (TimerText != null)
        {
            TimerText.text = "Timer:" + Mathf.CeilToInt(timer) + "s";
            scoreText.text = "Score:" + nSuccess;
        }
        if (debug)return;
        MarsComm.sendHeartbeat();
        if (isGamePaused && gameState != GameStates.PAUSED) PauseGame();
        else if (!isGamePaused && gameState == GameStates.PAUSED) ResumeGame();

       
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
        if (debug) return;
        playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
        if (target == null) return;
        targetObject = target.gameObject;
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
                gameState = GameStates.SPAWNFRUIT;
                break;
            case GameStates.SPAWNFRUIT:

                if (eventDelayTimer <= 0f && !runOnce)
                {
                    if (target != null)
                    {
                        return;
                    }
                    ResetEatFlags();
                    spawnFruit();
                    nTargets++;
                    eventDelayTimer = 0.5f;
                    runOnce = true;
                }
                else
                {
                    eventDelayTimer -= Time.deltaTime;
                    if (eventDelayTimer <= 0f)
                    {
                        
                        waitTime = gameSpeed;
                        runOnce = false;
                        gameState = GameStates.WAITFOREAT;
                    }
                }
                break;

            case GameStates.WAITFOREAT:
               if(!sheepController.instance.IsCollidingWithTarget())
                 waitTime -= Time.deltaTime;
           
                if (isSuccess) gameState = GameStates.SUCCESS;
               
                if (waitTime <= 0f || isFailure) // Add check
                {
                    nFailure++;
                    gameState = GameStates.FAILURE;
                }
                break;
            case GameStates.PAUSED:
                Debug.Log(isGamePaused);
                break;
            case GameStates.SUCCESS:
            case GameStates.FAILURE:
                if (eventDelayTimer <= 0f)
                {
                    eventDelayTimer = 0.5f;
                }
                else
                {
                    eventDelayTimer -= Time.deltaTime;
                    if (eventDelayTimer <= 0f)
                    {

                        // Wait for the gamestate to be logged.
                        isFailure = false;
                        isSuccess = false;
                        gameState = isTimeUp ? GameStates.STOP : GameStates.SPAWNFRUIT;
                        if(target!=null)Destroy(target);
                        runOnce = false;
                    }
                }
                break;
            case GameStates.STOP:
                if (debug) return;
                gameOver();
                break;
            case GameStates.DONE:
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;

        }
        Debug.Log(gameState);

    }
    public void ResetEatFlags()
    {
        isSuccess = false;
        isFailure = false;
    }

    public void spawnFruit()
    {
        float x = UnityEngine.Random.Range(xMin, xMax);
        float y = UnityEngine.Random.Range(yMin, yMax);
        target = Instantiate(targerPrefeb, new Vector3(x, y, 0), Quaternion.identity);
        target.transform.position = new Vector3(x, y, 0);
        if (x > 0)
        {
            target.GetComponent<SpriteRenderer>().flipX = true;
        }
     
    }
    public void initUI()
    {
        gameOverPanel.SetActive(false);
        //messTxt.enabled = true;
        startImage.SetActive(true);
        PauseImage.SetActive(false);
    }

    public bool IsGamePlaying()
    {
        return gameState != GameStates.WAITING
            && gameState != GameStates.PAUSED
            && gameState != GameStates.STOP;
    }

 

    public IEnumerator PlayEatingAnimation()
    {
        if (gameState != GameStates.WAITFOREAT)
            yield break;

        appleEatingSound.Play();

        Animator anim = target.GetComponent<Animator>();
        anim.Play("eating", -1, 0f);

        // Wait until animation starts
        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        while (!state.IsName("eating"))
        {
            yield return null;
            state = anim.GetCurrentAnimatorStateInfo(0);
        }

        float animationLength = state.length;
        float elapsed = 0f;

       

        while (elapsed < animationLength)
        {
            if (!sheepController.instance.IsCollidingWithTarget())
            {
                anim.Play("faild", -1, 0f);
                Debug.Log("Collision Eating failed.");
                isSuccess = false;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        sheepController.instance.AddScore();
        // Eating completed successfully
        Debug.Log("Eating success!");
        isSuccess = true;
        nSuccess++;
        Destroy(target);
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
       
        nSuccess = 0;
        nFailure = 0;
        nTargets = 0;
        isFailure = false;
        isSuccess = false;
        startImage.SetActive(false);
        timer = gameDuration; // Initialize timer 
        targetSpeed = gameSpeed;
        if (debug) return;
        //start new Trail
        AppData.Instance.StartNewTrial();
        gameSpeed = AppData.Instance.gameSpeed <= 0 ? gameSpeed : AppData.Instance.gameSpeed;
        Debug.Log(gameSpeed+"gamespeed");

    }

    //used on ui button
    public void IncreaseSpeed()
    {
        targetSpeed = Mathf.Clamp(targetSpeed - 0.2f, 2f, 5f); // step change in target
        Debug.Log("increase");
    }

    public void DecreaseSpeed()
    {
        targetSpeed = Mathf.Clamp(targetSpeed + 0.2f, 2f, 5f);
        Debug.Log("decrease");
    }

    public void onClickExit()
    {
        if (gameState != GameStates.WAITING && gameState != GameStates.STOP)
        {
            gameOver();

        }

        SceneManager.LoadScene("CHOOSEMOVE");
    }
}




