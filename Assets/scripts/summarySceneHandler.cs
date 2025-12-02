
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XCharts.Runtime;

public class summarySceneHandler : MonoBehaviour
{
    public BarChart barchart;
    public string title;
    private ConcurrentQueue<System.Action> _actionQueue = new ConcurrentQueue<System.Action>();
    private float shutdownTimer = 5f;
    public TextMeshProUGUI ssCummulativeScoreTxt;
    public TextMeshProUGUI ppCummulativeScoreTxt;
    public TextMeshProUGUI DcCummulativeScoreTxt;
    public TextMeshProUGUI ssCurrentScoreTxt;
    public TextMeshProUGUI ppCurrentScoreTxt;
    public TextMeshProUGUI DCCurrentScoreTxt;
    public GameObject SSstar;
    public GameObject PPstar;
    public GameObject DCstar;
    public void Start()
    {
        
        // Inialize the logger
        AppLogger.StartLogging(SceneManager.GetActiveScene().name);
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        title = "summary";
        initializeChart();
        updateScores();
      

    }

    void Update()
    {
       
        shutdownTimer -= Time.deltaTime;

        if (shutdownTimer <= 0) exit();

    }
   
    public void updateScores()
    {
        int[] scores,cuScore;


        //SpaceShooter Game Data
        scores = MarsGameDefs.Spaceshooter.GetScores();
        cuScore = MarsGameDefs.Spaceshooter.GetCummulativeScores();
        ssCummulativeScoreTxt.text = $"{cuScore[1].ToString("D4")}";
        ssCurrentScoreTxt.text = $"{scores[1].ToString("D3")}/{scores[0].ToString("D3")}";
        if (MarsGameDefs.Spaceshooter.IsAchievedToday()) SSstar.GetComponent<Image>().color = Color.white ;

        //PingPong Game Data
        scores = MarsGameDefs.PingPong.GetScores();
        cuScore = MarsGameDefs.PingPong.GetCummulativeScores();
        ppCummulativeScoreTxt.text = $"{cuScore[1].ToString("D4")}";
        ppCurrentScoreTxt.text = $"{scores[1].ToString("D3")}/{scores[0].ToString("D3")}";
        if(MarsGameDefs.PingPong.IsAchievedToday())PPstar.GetComponent<Image>().color = Color.white;

        //DiamondCatcher Game Data
        scores = MarsGameDefs.DiamondCatcher.GetScores();
        cuScore = MarsGameDefs.DiamondCatcher.GetCummulativeScores();
        DcCummulativeScoreTxt.text = $"{cuScore[1].ToString("D4")}";
        DCCurrentScoreTxt.text = $"{scores[1].ToString("D3")} / {scores[0].ToString("D3")}";
        if(MarsGameDefs.DiamondCatcher.IsAchievedToday())DCstar.GetComponent<Image>().color = Color.white;
       
    }
    // To load the data for a specific movement into the bar graph.
    public void selectedMovements(Button button)
    {
        title = button.gameObject.name.ToUpper();
        SessionDataHandler.SelectedMovement(button.gameObject.name.ToUpper());
        AppLogger.LogInfo($"Selected '{title}'.");
        UpdateChartData();
       
    }
    public void exit()
    {
        AppLogger.LogInfo("Disconnected form Mars And Switch scene to DataUploading");
        try
        {
            AppLogger.LogInfo("Disconnected form Mars And Application closed succesfully");
            Application.Quit();
            // Process.Start("shutdown", "/s /t 0");
            #if UNITY_EDITOR
               UnityEditor.EditorApplication.isPlaying = false;
            #endif
            // Process.Start("shutdown", "/s /t 0");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to shutdown: " + ex.Message);
        }

        //SceneManager.LoadScene("DATAUPLOADING");

    }
    
    //To initialize the barchart with whole data of moveTime per day
    public void initializeChart()
    {
  
        SessionDataHandler.MovTimePerDay();
        // Get or add the BarChart component
        barchart = gameObject.GetComponent<BarChart>();
        if (barchart == null)
        {
            barchart = gameObject.AddComponent<BarChart>();
            barchart.Init();
        }

        // Set chart title and tooltip visibility
        barchart.EnsureChartComponent<Title>().show = true;
        barchart.EnsureChartComponent<Title>().text = title;

        barchart.EnsureChartComponent<Tooltip>().show = true;
        barchart.EnsureChartComponent<Legend>().show = true;

        // Ensure x and y axes are created
        var xAxis = barchart.EnsureChartComponent<XAxis>();
        var yAxis = barchart.EnsureChartComponent<YAxis>();
        xAxis.show = true;
        yAxis.show = true;
        xAxis.type = Axis.AxisType.Category; // Set x-axis type to Category
        yAxis.type = Axis.AxisType.Value; // Set y-axis type to Value
        yAxis.min = 0; // Make sure bars start from the y=0 line
        yAxis.max = SessionDataHandler.moveTimeData.Max(); // You can adjust the maximum value as needed

        // Set zoom properties
        var dataZoom = barchart.EnsureChartComponent<DataZoom>();
        dataZoom.enable = true;
        dataZoom.supportInside = true;
        dataZoom.supportSlider = true;
        dataZoom.start = 0;
        dataZoom.end = 100;
        AppLogger.LogInfo("chart initialized successfully");
        UpdateChartData();
    }

    //To update chart with data
    public void UpdateChartData()
    {
        if (barchart == null)
        {
            
            return;
        }
        barchart.RemoveData();
        barchart.EnsureChartComponent<Title>().text = title;
        barchart.AddSerie<Bar>();
        // Update the x-axis data
        var xAxis = barchart.GetChartComponent<XAxis>();
        xAxis.data.Clear();
        foreach (string date in SessionDataHandler.dateData)
        {
            xAxis.data.Add(date); // Add x-axis labels (dates)
        }

        // Update the y-axis data (movement time)
        var yAxis = barchart.GetChartComponent<YAxis>();
        yAxis.data.Clear();
      
        for (int i = 0; i < SessionDataHandler.dateData.Length; i++)
        {
            float yValue = SessionDataHandler.moveTimeData[i];
            barchart.AddData(0, yValue);
        }
        barchart.RefreshAllComponent();
        AppLogger.LogInfo("chart updated successfully");
    }
   
    private void OnApplicationQuit()
    {

        Application.Quit();
      
    }
}
