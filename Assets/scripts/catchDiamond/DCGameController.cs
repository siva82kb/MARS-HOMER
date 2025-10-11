using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DCGameController : MonoBehaviour
{
   

    public static DCGameController Instance;
    public GameObject gameOverPanel;


    public TextMeshProUGUI TimerText;
    public TextMeshProUGUI scoreText;
    public GameObject targetObject;

    public Text gameSpeedTxt;
    public GameObject startImage;
    public GameObject PauseImage;
    public GameObject GameControl;
    public GameObject reminderPanel;
    public GameObject targetBubblePrefeb;
    public GameObject targetTimerPrefeb;
    public GameObject targetBubble;
    public GameObject targetTimer;  
    public Canvas uiCanvas;
    Animator targetAnim;
    public const float gameDuration = 60f;
    private float timer;
    private float eventDelayTimer = 0f;
    private bool runOnce = false;
    public float gameSpeed = 5f;
    private float targetSpeed;
    public float smoothFactor = 5f;
    public float InsideTarget = 0f;

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
    public AudioSource failSound;

    //catchDiamond
    public ParticleSystem hightlightsprefeb;
    public ParticleSystem hightlights;
    public ParticleSystem moveingupstars;
    public ParticleSystem moveingupstarsobj;
    public enum GameStates
    {
        WAITING = 0,
        START,
        STOP,
        PAUSED,
        SPAWNFRUIT,
        WAITFOREAT,
        PLAYERIN,
        PLAYEROUT,
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
        if (debug) return;
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
                    //ResetEatFlags();
                    spawnDiamond();
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
                if (isSuccess)
                {
                    gameState = GameStates.SUCCESS;
                    break;
                }
                if (!player.instance.IsCollidingWithTarget() && !isSuccess) waitTime -= Time.deltaTime;
             
                if ((waitTime / gameSpeed) <= 0.5f && target != null && !player.instance.IsCollidingWithTarget())
                {
                    //targetAnim.Play("end");
                }

                if (waitTime <= 0f) // Add check
                {
                    //failSound.Play();
                    //if (isSuccess) gameState = GameStates.SUCCESS;
                    Debug.Log(waitTime + "waittime" + isFailure + isSuccess);
                    nFailure++;
                    gameState = GameStates.FAILURE;
                }
                break;
            case GameStates.PLAYERIN:
                if (targetTimer == null)
                {
                    targetTimer = Instantiate(targetTimerPrefeb, uiCanvas.transform);
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(target.transform.position);//just above the sheep
                    targetTimer.transform.position = new Vector3(screenPos.x, screenPos.y - 25f, screenPos.z);
                    targetAnim.Play("idle", -1, 0f);
                    moveingupstarsobj = Instantiate(moveingupstars, DCGameController.Instance.target.transform.position, Quaternion.identity);
                    moveingupstarsobj.Play();
                }
               

                InsideTarget += Time.deltaTime;
                targetTimer.GetComponent<Image>().fillAmount = InsideTarget;
                if (InsideTarget >= 1f)
                {
                    targetAnim.Play("disappear", -1, 0f);
                    gameState = GameStates.SUCCESS;
                    InsideTarget = 0f;
                    player.instance.AddScore();
                }
                break;
            case GameStates.PLAYEROUT:
                if (targetTimer != null)
                {
                    Destroy(targetTimer);
                    targetAnim.Play("TargetHighlight", -1, 0f);
                }
                if(moveingupstarsobj!=null)moveingupstarsobj.Stop(true);
                InsideTarget = 0f;
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
                        if (target != null) Destroy(target);
                        if (hightlights!=null)Destroy(hightlights);
                        if (targetBubble != null) Destroy(targetBubble);
                        if(moveingupstarsobj!=null) Destroy(moveingupstarsobj);
                        if(targetTimer!=null) Destroy(targetTimer);
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
    public void setPlayerIn()
    {
        gameState = GameStates.PLAYERIN;
    }
    public void setPlayerOut()
    {
        gameState = GameStates.PLAYEROUT;
    }
    public void spawnDiamond()
    {
        Vector3 spawnPos;

        // Loop until we find a position far enough from the player
        do
        {
            float x = UnityEngine.Random.Range(xMin, xMax);
            float y = UnityEngine.Random.Range(yMin, yMax);
            spawnPos = new Vector3(x, y, 0);
        }
        while (Vector3.Distance(spawnPos, GameObject.FindGameObjectWithTag("Player").transform.position) < 5f);

        // Instantiate the target
        //target = Instantiate(targerPrefeb, spawnPos, Quaternion.identity);

       
     
        target = Instantiate(targerPrefeb, spawnPos, Quaternion.identity);
        targetBubble = Instantiate(targetBubblePrefeb, new Vector3(spawnPos.x,spawnPos.y-0.25f,spawnPos.z), Quaternion.identity);
        //target.transform.position = Vector3.Lerp(target.transform.position, spawnPos, Time.deltaTime);
        hightlights = Instantiate(hightlightsprefeb, spawnPos, Quaternion.identity);
        hightlights.Play();
        if (target.transform.position.x > 0)
        {
            target.GetComponent<SpriteRenderer>().flipX = true;
        }
        targetAnim = target.GetComponentInChildren<Animator>();
        targetAnim.Play("TargetHighlight", -1, 0f);

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

        //appleEatingSound.Play();
        

        //targetAnim.Play("eating", -1, 0f);

        // Wait until animation starts
        AnimatorStateInfo state = targetAnim.GetCurrentAnimatorStateInfo(0);
        while (!state.IsName("eating"))
        {
            yield return null;
            state = targetAnim.GetCurrentAnimatorStateInfo(0);
        }

        float animationLength = state.length;
        float elapsed = 0f;



        while (elapsed < animationLength)
        {
            if (!sheepController.instance.IsCollidingWithTarget())
            {
              
                Debug.Log("Collision Eating failed.");
                isSuccess = false;

                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        //player.instance.AddScore();
        //player.instance.highligtsobj.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        player.instance.moveingupstarsobj.Stop(true);
        //sheepController.instance.StopAllCoroutines();

        // Eating completed successfully
        Debug.Log("Eating success!");
        isSuccess = true;
        nSuccess++;
        if (targetBubble != null) Destroy(targetBubble);
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

        if (debug) return;
        //start new Trail
        AppData.Instance.StartNewTrial();
        gameSpeed = AppData.Instance.gameSpeed <= 0 ? gameSpeed : AppData.Instance.gameSpeed;
        targetSpeed = gameSpeed;
        Debug.Log(gameSpeed + "gamespeed");

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






