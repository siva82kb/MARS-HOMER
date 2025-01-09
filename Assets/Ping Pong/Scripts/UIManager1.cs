using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using NeuroRehabLibrary;

public class UIManager1 : MonoBehaviour {

	GameObject[] pauseObjects, finishObjects;
	public BoundController rightBound;
	public BoundController leftBound;
	public bool isFinished;
	public bool playerWon, enemyWon;
    public AudioClip[] audioClips; // winlevel loose
    public int winScore = 7;
    public int win;
    private GameSession currentGameSession;
    // Use this for initialization
    void Start () {
		//AppData.InitializeRobot();
		//pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
		finishObjects = GameObject.FindGameObjectsWithTag("ShowOnFinish");
		hideFinished();
		StartNewGameSession();
	}
	// Update is called once per frame
	void Update () {

		//Debug.Log(AppData.inputPressed());



        if (Input.GetKeyDown(KeyCode.Return))
        {

         

            LoadLevel("pong_game");
        }


 
	}
    
    void StartNewGameSession()
    {
        currentGameSession = new GameSession
        {
            GameName = "PING-PONG",
            Assessment = 0 // Example assessment value, 
                           //If this script for calibration and assessment then Assessment=1. 
        };
        SessionManager.Instance.StartGameSession(currentGameSession);
        SetSessionDetails();
    }

    public void SetSessionDetails()
    {
        string device = "MARS"; // Set the device name
        string assistMode = "Null"; // Set the assist mode
        string assistModeParameters = "Null"; // Set the assist mode parameters
        string deviceSetupLocation = DataManager.filePathConfigData;
        SessionManager.Instance.SetDevice(device, currentGameSession);
        SessionManager.Instance.SetAssistMode(assistMode, assistModeParameters, currentGameSession);
        SessionManager.Instance.SetDeviceSetupLocation(deviceSetupLocation, currentGameSession);
    }
    void EndCurrentGameSession()
    {
        if (currentGameSession != null)
        {
            string trialDataFileLocation = "pass_trial_data_location";
            string gameParameter = "pass_game_parameter";
            SessionManager.Instance.SetGameParameter(gameParameter, currentGameSession);
            SessionManager.Instance.SetTrialDataFileLocation(trialDataFileLocation, currentGameSession);
            SessionManager.Instance.EndGameSession(currentGameSession);
        }
    }


    //Reloads the Level
    public void Reload(){
		Application.LoadLevel(Application.loadedLevel);
	}
    void playAudio(int clipNumber)
    {
        AudioSource audio = GetComponent<AudioSource>();
        audio.clip = audioClips[clipNumber];
        audio.Play();

    }
    //controls the pausing of the scene
    public void pauseControl(){
		if(Time.timeScale == 1)
		{
			Time.timeScale = 0;
			showPaused();
		} else if (Time.timeScale == 0){
			Time.timeScale = 1;
			hidePaused();
		}
	}
	
	//shows objects with ShowOnPause tag
	public void showPaused(){
		foreach(GameObject g in pauseObjects){
			g.SetActive(true);
		}
	}

	//hides objects with ShowOnPause tag
	public void hidePaused(){
		foreach(GameObject g in pauseObjects){
			g.SetActive(false);
		}
	}

	//shows objects with ShowOnFinish tag
	public void showFinished(){
      

        foreach (GameObject g in finishObjects){
			g.SetActive(true);
		}
	}
	
	//hides objects with ShowOnFinish tag
	public void hideFinished(){
		foreach(GameObject g in finishObjects){
			g.SetActive(false);
		}
	}
	

	//loads inputted level
	public void LoadLevel(string level){

		SceneManager.LoadSceneAsync(level);
	}

	public void Exit()
    {
		SceneManager.LoadScene("chooseMovementScene");
	}
}
