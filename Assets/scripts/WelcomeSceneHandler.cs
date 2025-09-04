using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using System.Linq;

public class welcomeSceneHandler : MonoBehaviour
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
    public readonly string nextScene = "ROBOTCALIB";

    public bool attachMarsButtonEvent = false;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize AppData
        AppData.Instance.Initialize(SceneManager.GetActiveScene().name);

        // Check if the directory exists
        if (!Directory.Exists(DataManager.basePath)) Directory.CreateDirectory(DataManager.basePath);
        if (!File.Exists(DataManager.configFile)) SceneManager.LoadScene("CONFIG");

        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"'{SceneManager.GetActiveScene().name}' scene started.");
        daySummaries = AppData.Instance.userData.CalculateMoveTimePerDay();

        // Update summary display
        UpdateUserData();
        UpdatePieChart();
    }

    // Update is called once per frame
    void Update()
    {
        MarsComm.sendHeartbeat();
        // Attach event listener for Mars button release
        if (!attachMarsButtonEvent && Time.timeSinceLevelLoad > 1)
        {
            attachMarsButtonEvent = true;
            MarsComm.OnMarsButtonReleased += OnMarsButtonReleased;
        }
        // Check if it time to switch to the next scene
        if (changeScene == true)
        {
            LoadTargetScene();
            changeScene = false;
        }
    }

    public void OnMarsButtonReleased()
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
        userName.text = AppData.Instance.userData.hospNumber;
        int movetime = AppData.Instance.userData.totalMoveTimeRemaining;
        if (AppData.Instance.userData.isExceeded)
        {
            timeRemainingToday.text = $"Done +{movetime}[min]";
            timeRemainingToday.color = Color.green;
            Debug.Log("workinng");
        }
        else
        {
            timeRemainingToday.text = $"{movetime} min";
        }
        todaysDay.text = AppData.Instance.userData.getCurrentDayOfTraining().ToString();
        todaysDate.text = DateTime.Now.ToString("ddd, dd-MM-yyyy");
        if (!File.Exists(awsManager.filePathUploadStatus))
            awsManager.createFile(userName.text);
    }
    private void UpdatePieChart()
    {
        int N = daySummaries.Length;
        for (int i = 0; i < N; i++)
        {
            prevDays[i].text = daySummaries[i].Day;
            prevDates[i].text = daySummaries[i].Date;
            pies[i].fillAmount = daySummaries[i].MoveTime / AppData.Instance.userData.totalMoveTimePrsc;
            pies[i].color = new Color32(148, 234, 107, 255);
        }
        piChartUpdated = true;
    }

    private void OnDestroy()
    {
        MarsComm.OnMarsButtonReleased -= OnMarsButtonReleased;
    }
    private void OnApplicationQuit()
    {
        Application.Quit();
        //JediComm.Disconnect();
    }
}
