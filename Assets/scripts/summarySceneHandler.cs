
using System;
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

    public LineChart lineChart;
    public string title;
    private ConcurrentQueue<System.Action> _actionQueue = new ConcurrentQueue<System.Action>();
    private float shutdownTimer = 5f;
    public TextMeshProUGUI ssCummulativeScoreTxt;
    public TextMeshProUGUI ppCummulativeScoreTxt;
    public TextMeshProUGUI DcCummulativeScoreTxt;
    public TextMeshProUGUI TWCummulativeScoreTxt;
    public TextMeshProUGUI TTCummulativeScoreTxt;
    public TextMeshProUGUI MCCummulativeScoreTxt;
    public TextMeshProUGUI ssCurrentScoreTxt;
    public TextMeshProUGUI ppCurrentScoreTxt;
    public TextMeshProUGUI DCCurrentScoreTxt;
    public TextMeshProUGUI TWCurrentScoreTxt;
    public TextMeshProUGUI TTCurrentScoreTxt;
    public TextMeshProUGUI MCCurrentScoreTxt;
    public GameObject SSstar;
    public GameObject PPstar;
    public GameObject DCstar;
    public GameObject TWstar;
    public GameObject TTstar;
    public GameObject MCstar;
    public GameObject TotalStar;
    public TextMeshProUGUI totalStarCount;
    public void Start()
    {
        //debug mode
        //AppData.Instance.Initialize(SceneManager.GetActiveScene().name);

        // Inialize the logger
        AppLogger.StartLogging(SceneManager.GetActiveScene().name);
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        title = "Unlock your Potential through Play";
        updateScores();
        initializeChart();

    }

    void Update()
    {
       
        shutdownTimer -= Time.deltaTime;

        if (shutdownTimer <= 0) exit();

    }
   
    public void updateScores()
    {
        int[] scores,cuScore;
        int [] CumStar = MarsGameDefs.Spaceshooter.GetStarsCount();
        AppLogger.LogInfo(" started score updateding");
       
        //SpaceShooter Game Data
        scores = MarsGameDefs.Spaceshooter.GetScores();
        cuScore = MarsGameDefs.Spaceshooter.GetCummulativeScores();
        ssCummulativeScoreTxt.text = $"{cuScore[1].ToString("D4")}";
        ssCurrentScoreTxt.text = $"{scores[1].ToString("D3")}/{scores[0].ToString("D3")}";
        if (MarsGameDefs.Spaceshooter.IsAchievedToday()) SSstar.GetComponent<Image>().color = Color.white ;
        totalStarCount.text = CumStar[0].ToString("D3");
        if (CumStar[0]>0)TotalStar.GetComponent<Image>().color = Color.white ;  

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
        AppLogger.LogInfo(" score updated succesfully");

        //TableWipping Game Data
        scores = MarsGameDefs.TableWiping.GetScores();
        cuScore = MarsGameDefs.TableWiping.GetCummulativeScores();
        TWCummulativeScoreTxt.text = $"{cuScore[1].ToString("D4")}";
        TWCurrentScoreTxt.text = $"{scores[1].ToString("D3")} / {scores[0].ToString("D3")}";
        if (MarsGameDefs.TableWiping.IsAchievedToday()) TWstar.GetComponent<Image>().color = Color.white;
        AppLogger.LogInfo(" score updated succesfully");

        //TUK-TUK Game Data
        scores = MarsGameDefs.TukTuk.GetScores();
        cuScore = MarsGameDefs.TukTuk.GetCummulativeScores();
        TTCummulativeScoreTxt.text = $"{cuScore[1].ToString("D4")}";
        TTCurrentScoreTxt.text = $"{scores[1].ToString("D3")} / {scores[0].ToString("D3")}";
        if (MarsGameDefs.TukTuk.IsAchievedToday()) TTstar.GetComponent<Image>().color = Color.white;
        AppLogger.LogInfo(" score updated succesfully");

        //Match Drop Game Data
        scores = MarsGameDefs.MatchCatch.GetScores();
        cuScore = MarsGameDefs.MatchCatch.GetCummulativeScores();
        Debug.Log(scores[0].ToString()+","+scores[1].ToString());
        Debug.Log(cuScore[1].ToString());
        MCCummulativeScoreTxt.text = $"{cuScore[1].ToString("D4")}";
        MCCurrentScoreTxt.text = $"{scores[1].ToString("D3")} / {scores[0].ToString("D3")}";
        if (MarsGameDefs.MatchCatch.IsAchievedToday()) MCstar.GetComponent<Image>().color = Color.white;
        AppLogger.LogInfo(" score updated succesfully");

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
        if (AppData.isNRSBuilt) {

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
            return;
        }


      
        AppLogger.LogInfo("Disconnected form Mars And Switch scene to DataUploading");

        SceneManager.LoadScene("DATAUPLOADING");

    }


    public void initializeChart()
    {
        SessionDataHandler.MovTimePerDay();

        lineChart = gameObject.GetComponent<LineChart>();
        if (lineChart == null)
        {
            lineChart = gameObject.AddComponent<LineChart>();
            lineChart.Init();
        }

        // Title
        var titleComp = lineChart.EnsureChartComponent<Title>();
        titleComp.show = true;
        titleComp.text = title;

        // Tooltip & Legend
        lineChart.EnsureChartComponent<Tooltip>().show = true;
        lineChart.EnsureChartComponent<Legend>().show = true;

        // Axes
        var xAxis = lineChart.EnsureChartComponent<XAxis>();
        var yAxis = lineChart.EnsureChartComponent<YAxis>();

        xAxis.show = true;
        yAxis.show = true;

        xAxis.type = Axis.AxisType.Category;
        yAxis.type = Axis.AxisType.Value;

        // Fixed Y-axis limit 0 - 100
        yAxis.min = 0;
        yAxis.max = 100;

       

        // Enable zoom
        var dataZoom = lineChart.EnsureChartComponent<DataZoom>();
        dataZoom.enable = true;
        dataZoom.supportInside = true;
        dataZoom.supportSlider = true;
        dataZoom.start = 0;
        dataZoom.end = 100;

        UpdateChartData();
    }

    public void UpdateChartData()
    {
        if (lineChart == null) return;

        AppLogger.LogInfo("linechart is not null");

        lineChart.RemoveData();
        lineChart.EnsureChartComponent<Title>().text = title;
        AppLogger.LogInfo("linechart clearing unwanted data");

        lineChart.AddSerie<Line>();

        var xAxis = lineChart.GetChartComponent<XAxis>();
        xAxis.data.Clear();

        DateTime today = DateTime.Today;

       

        for (int i = 0; i < SessionDataHandler.dateData.Length; i++)
        {
           
            string dateStr = SessionDataHandler.dateData[i];

            // Parse date
            DateTime entryDate = DateTime.Parse(dateStr);

            // Always show labels in the formate of date/Month
            xAxis.data.Add(entryDate.ToString("dd/MM"));

            if (entryDate > today)
            {
               
                // Add empty value → line breaks here
                lineChart.AddData(0, null);
            }
            else
            {

                // Add actual data
                float value = SessionDataHandler.moveTimeData[i];
                lineChart.AddData(0, value);

            }
        }
        AppLogger.LogInfo("DataUpdated successfully");
        lineChart.RefreshAllComponent();
    }


private void OnApplicationQuit()
    {

        Application.Quit();
      
    }
}
