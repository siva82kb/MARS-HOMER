using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Policy;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using static UnityEngine.GraphicsBuffer;

public class TWGameController : MonoBehaviour
{
    public static TWGameController Instance;
    // Game start/pause variables.
    public GameObject startImage;
    public GameObject PauseImage;

    // Timer and score related UI elements.
    public TextMeshProUGUI scoreTxt;
    public TextMeshProUGUI timerTxt;

    // Game over panel UI
    public GameObject gameOverPanel;

    // Game time completion reminder panel.
    public GameObject reminderPanel;

    // Start is called before the first frame update
    private GameObject targetObject;
    public GameObject targetPrefabB;
    public GameObject targetPrefabS;
    public Sprite[] stainSprites;
    public GameObject[] bigStains;
    private GameObject target;
    public ParticleSystem cleaningParticalPrefeb;
    private ParticleSystem cleaningPartical;
    public AudioSource audioSource;
    public AudioSource audioSourcewipping;
    public GameObject moneySprite;
    private GameObject money;
    public AudioClip playerWinAudio;
   
    public TextMeshProUGUI cummulativeScoreTxt;
    public GameObject celebrationPanle;
    public TextMeshProUGUI scoreComparisonTxt;
    public TextMeshProUGUI yesterdayScoreTxt;
    public TextMeshProUGUI todayScoreTxt;
    public TextMeshProUGUI starCount;
    public GameObject GameOverStar;
    // Other game logic variables.
    private float gameTimeLeft;
    private float gameDuration = 60;
    private float eventDelayTimer = 1f;
    private bool runOnce = false;
    private float insideTargetTimer = 0f;
    public int[] scores;
    private bool restart = false;

    public Vector3 playerGamePosition { get; private set; }
    public Vector3? targetGamePosition { get; private set; }
    public Vector3? targetEndPointPosition { get; private set; }
   
    public bool debug;

    // Game score related variables.
    public int nTargets = 0;
    public int nSuccess = 0;
    public int nFailure = 0;

    public bool isGameStarted { get; private set; } = false;
    public bool isGameFinished { get; private set; } = false;
    public bool isGamePaused { get; private set; } = false;
    public bool isSuccess { get; set; } = false;
    public bool isFailure { get; set; } = false;
    public bool isGamePlaying => gameState == GameStates.WAITFORCLEAN
            || gameState == GameStates.SPAWSTAIN
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
        SPAWSTAIN,
        WAITFORCLEAN,
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
           
        }
    }
    public enum StainSize
    {
        Small,
        Big
    }

    public LineRenderer lr;
    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        if (debug){
            AppData.Instance.Initialize(SceneManager.GetActiveScene().name);
            AppData.Instance.SetMovement("MLAP");
        }

        initUI();
   
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 5;

        //read Rom File
        // Check if the required amount fo trials for the selected movement has been completed today.
        bool isRequiredTrialsCompleted = AppData.Instance.selectedMovement.trialNumberDay >= AppData.Instance.userData.moveTimePrsc[AppData.Instance.selectedMovement.name];
        if (isRequiredTrialsCompleted) reminderPanel.SetActive(true);
        else reminderPanel.SetActive(false);

        // Attach event handler to Mars button release event.
        MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
        updateStarCount();
        scores = MarsGameDefs.TableWiping.GetScores();
        Debug.Log($"{scores[0]}/{scores[1]}");
        AppLogger.LogInfo($"scores - yesterDayScore:{scores[1]} | TodayScore{scores[0]}");
    }
    public void updateStarCount()
    {
        starCount.text = $"{AppData.Instance.selectedGame.cummulativeStars.ToString("D2")}";
    }

    public void Update()
    {
        TWPlayer.instance.OnDrawGizmos(lr);
       

        // Check of the game speed controller is to be shown.
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
        {
            isGameStarted=true;
        }
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
        {
            restart = true;
        }
        if (restart)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            restart = false;
        }
        if (debug) return;
        MarsComm.sendHeartbeat();
        if (isGamePaused && gameState != GameStates.PAUSED) PauseGame();
        else if (!isGamePaused && gameState == GameStates.PAUSED) ResumeGame();
       
    }

    // Update is called once per frame

    private void FixedUpdate()
    {

        // Run the statemachine
        RunStateMachine();
        if (debug) return;
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
            //gameState = GameStates.DONE;
            restart = true;
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

            //cal gameTime
            int gametime = (int)gameDuration - (int)gameTimeLeft;
            AppData.Instance.gameTime = gametime < gameDuration ? gametime : gameDuration;
            //Stop the current game trial
            if ((scores[0] + nSuccess) > scores[1] && !AppData.Instance.selectedGame.isAchievedToday())
            {
                AppData.Instance.selectedGame.updateCummulativeStars();
                celebrationPanle.SetActive(true);
            }

            gameOverPanel.SetActive(!celebrationPanle.gameObject.activeSelf);
            AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);

            if (gameOverPanel.gameObject.activeSelf)
            {
                GameOverStar.SetActive(AppData.Instance.selectedGame.isAchievedToday());
                yesterdayScoreTxt.text = $"{scores[1]:D4}";
                todayScoreTxt.text = $"{(scores[0] + nSuccess):D4}";
            }
            if (celebrationPanle.gameObject.activeSelf)
            {
                updateStarCount();
                scoreComparisonTxt.text = $"{(scores[0] + nSuccess):D3}";
            }

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
        gameDuration = MarsGameDefs.GAMEDURATION["TW"];
        gameTimeLeft = gameDuration;
        isGameStarted = true;
        if (debug) return;
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
public void RunStateMachine()
    {
        // Decrement the gameTimeLeft if game is playing.
        if (isGamePlaying) gameTimeLeft -= Time.deltaTime;
        timerTxt.text = $"TIME : {(int)gameTimeLeft}";
        // Check if time is up.
        bool isTimeUp = gameTimeLeft < 0;
        switch (gameState)
        {
            case GameStates.WAITING:
                if (isGameStarted) gameState = GameStates.START;
              
               
                break;
            case GameStates.START:
                startGame();
                gameState = GameStates.SPAWSTAIN;
                eventDelayTimer = 0.05f;
                runOnce = false;
                break;
            case GameStates.SPAWSTAIN:
                if (!runOnce)
                {
                    if (target != null) return;
                    // Spawn the new target.
                    spawnStain();
                    nTargets++;
                    eventDelayTimer = 0.5f;
                    runOnce = true;
                }
                else
                {
                    eventDelayTimer -= Time.deltaTime;
                    if (eventDelayTimer <= 0f)
                    {
  
                        runOnce = false;
                        gameState = GameStates.WAITFORCLEAN;
                    }
                }
                break;
            case GameStates.WAITFORCLEAN:
                if (StainWipe2D.Instance != null)
                {
                    //Debug.Log($"{StainWipe2D.Instance.erasedPixels}/{StainWipe2D.Instance.totalStainPixels}===={StainWipe2D.Instance.erasedPixels / StainWipe2D.Instance.totalStainPixels}");
                    if ((StainWipe2D.Instance.erasedPixels / StainWipe2D.Instance.totalStainPixels > 0.95f) && !runOnce) {
                        money = Instantiate(moneySprite,target.transform.position, Quaternion.identity);
                      
                        audioSource.PlayOneShot(playerWinAudio);
                        runOnce = true;
                        eventDelayTimer = 0.5f;
                        return;
                    }
                    else if((StainWipe2D.Instance.erasedPixels / StainWipe2D.Instance.totalStainPixels > 0.95f))
                    {
                        eventDelayTimer -= Time.deltaTime;
                        if (eventDelayTimer <= 0f)
                        {
                            nSuccess++;
                            scoreTxt.text = $"SCORE : {nSuccess}";
                            runOnce = false;
                            gameState = GameStates.SUCCESS;
                        }
                    }
                    //gameState = GameStates.FAILURE;
                }
                break;
            case GameStates.PLAYERIN:
                // Increment time.               
               
                //Update in target animation.
              
                break;
            case GameStates.PLAYEREXIT:
                gameState = GameStates.WAITFORCLEAN;
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
                    gameState = isTimeUp ? GameStates.STOP : GameStates.SPAWSTAIN;
                    clearObject();
                    runOnce = false;
                }
                break;
            case GameStates.STOP:
                gameOver();
                break;
            case GameStates.DONE:

                break;
        }
    }
   
    public void spawnStain()
    {
        // Generate the new target
        (Vector2 epTarget, Vector2 gTarget) = TWPlayer.instance.GenerateNextRandomTarget();
        Debug.Log(gTarget);
        StainSize size = TWPlayer.instance.DecideStainSize(gTarget);

        Vector3 spawnPos = new Vector3(gTarget.x, gTarget.y, 0);


        // Instantiate the target
        target = Instantiate(size == StainSize.Big ? bigStains[Random.Range(0,bigStains.Length)] :targetPrefabS, spawnPos, Quaternion.identity);
       
        //money.SetActive(false);
        // Get SpriteRenderer
        SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        // Assign random sprite
        if ( size == StainSize.Small && stainSprites.Length > 0)
        {
            sr.sprite = stainSprites[Random.Range(0, stainSprites.Length)];
        }

    }
    public void SetPlayerIn()
    {
       audioSourcewipping.Play();   

        if (cleaningPartical == null)
        {
            cleaningPartical = Instantiate(
                cleaningParticalPrefeb,
                target.transform.position,
                Quaternion.identity
            );

            cleaningPartical.Play();
            return;
        }

        if (!cleaningPartical.isPlaying)
            cleaningPartical.Play();

    }


    public void SetPlayerOut()
    {
        audioSourcewipping.Stop();
        if (cleaningPartical != null && cleaningPartical.isPlaying)
        {
            cleaningPartical.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void clearObject()
    {
        if (cleaningPartical != null)
        {
            Destroy(cleaningPartical.gameObject);
            cleaningPartical = null; 
        }

        if (target != null)
        {
            Destroy(target);
            target = null;
        }

        Destroy(money.gameObject);
    }

}
