using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using NeuroRehabLibrary;

public class welcomSceneHandler : MonoBehaviour
{
    //public GameObject loading;
    public TextMeshProUGUI userName;
    public TextMeshProUGUI timeRemainingToday;
    public TextMeshProUGUI todaysDay;
    public TextMeshProUGUI todaysDate;
    public int daysPassed;
    public TextMeshProUGUI[] prevDays = new TextMeshProUGUI[7];
    public TextMeshProUGUI[] prevDates = new TextMeshProUGUI[7];
    public Image[] pies = new Image[7];
    public bool piChartUpdated = false;
    private DaySummary[] daySummaries;
    public static bool changeScene = false;
    public readonly string nextScene = "calibrationScene";
    
    // Private variables
    private bool attachPlutoButtonEvent = false;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize.
        AppData.InitializeRobot();
        daySummaries = SessionDataHandler.CalculateMoveTimePerDay();
        SessionManager.Initialize(DataManager.directoryPathSession);
        SessionManager.Instance.Login();

        // Inialize the logger
        AppLogger.StartLogging(SceneManager.GetActiveScene().name);
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

        // Update summary display
        if (!piChartUpdated)
        {
            UpdateUserData();
            UpdatePieChart();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Attach PlutoButton release event after 2 seconds if it is not attached already.
        if (!attachPlutoButtonEvent && Time.timeSinceLevelLoad > 2)
        {
            attachPlutoButtonEvent = true;
            MarsComm.OnButtonReleased += onMarsButtonReleased;
        }

        // Check if it time to switch to the next scene
        if (changeScene == true)
        {
            LoadTargetScene();
            changeScene = false;
        }
    }

    public void onMarsButtonReleased()
    {
        AppLogger.LogInfo("Mars button released.");
        changeScene = true;
       
    }

    private void LoadTargetScene()
    {
        AppLogger.LogInfo($"Switching to the next scene '{nextScene}'.");
        SceneManager.LoadScene(nextScene);
    }

    private void UpdateUserData()
    {
        userName.text = AppData.UserData.hospNumber;
        timeRemainingToday.text = $"{AppData.UserData.totalMoveTimeRemaining} min";
        todaysDay.text = AppData.UserData.getCurrentDayOfTraining().ToString();
        todaysDate.text = DateTime.Now.ToString("ddd, dd-MM-yyyy");
    }

    private void UpdatePieChart()
    {
        int N = daySummaries.Length;
        for (int i = 0; i < N; i++)
        {
            prevDays[i].text = daySummaries[i].Day;
            prevDates[i].text = daySummaries[i].Date;
            pies[i].fillAmount = daySummaries[i].MoveTime / AppData.UserData.totalMoveTimePrsc;
            pies[i].color = new Color32(148, 234, 107, 255);
        }
        piChartUpdated = true;
    }

    private void OnDestroy()
    {
        MarsComm.OnButtonReleased -= onMarsButtonReleased;
    }
    private void OnApplicationQuit()
    {
        Application.Quit();
        JediComm.Disconnect();
    }
}
