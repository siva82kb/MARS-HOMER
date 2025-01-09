using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using UnityEngine.UI;
using UnityEditor;

public class Drawpath : MonoBehaviour
{
    public static Drawpath instance;
    public float[] Encoder = new float[6];
    public float[] ELValue = new float[6];
    public float max_x;
    public float min_x;
    public float max_y;
    public float min_y;
    public GameObject message;
    public static int inclusion;
    public static string currentDate;
    public Slider force;
    public GameObject messagePanel;
    List<Vector3> paths;

    float time, startTime, decayTime = 5.0f, support, supporti, startSupport, startDecay = 0, endSupport;
    const int SUPPORT_MODE = 2006;
    void Awake()
    {
        instance = this;
        Time.timeScale = 1;
    }

    void Start()
    {

        //AppData.InitializeRobot();
        AppLogger.StartLogging(SceneManager.GetActiveScene().name);
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        messagePanel.SetActive(false);
        paths = new List<Vector3>();
        message.SetActive(false);
        currentDate = (DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss"));
    }

    public void Update()
    {
        time += Time.deltaTime;
        updateforce();
        if (startDecay == 1)
        {
            if (time - startTime < decayTime)
            {
                supporti = startSupport + (time - startTime) / decayTime * (endSupport - startSupport);
                MarsComm.SUPPORT = supporti;
                AppData.dataSendToRobot = new float[] { MarsComm.SUPPORT, 0.0f, SUPPORT_MODE, 0.0f };
            }
            else
            {
                startDecay = 0;
                halfWeightSupport();
            }
        }
    }

    public void onclick_recalibrate()
    {
        SceneManager.LoadScene("fullWeightSupportScene");

    }
    public void onclickHalfweightSupport()
    {

        endSupport = 0.5f;
        startTime = time;
        startSupport = MarsComm.SUPPORT;
        startDecay = 1;
        paths = Drawlines.paths_pass;
        max_x = paths.Max(v => v.x);
        min_x = paths.Min(v => v.x);
        max_y = paths.Max(v => v.y);
        min_y = paths.Min(v => v.y);
        //Debug.LogError(max_x + ";" + min_x + ";" + max_y + ";" + min_y);
        if (Mathf.Abs(max_x-min_x) > 100|| Mathf.Abs(max_y - min_y) > 100)
        {
            inclusion = 1;
            message.SetActive(true);
            messagePanel.SetActive(true);
            //To write assessment data
            writeAssessmentData();
        }
        else
        {
            inclusion = 0;
            message.SetActive(false);
        }
    }
    public void writeAssessmentData()
    {
        if (!Directory.Exists(DataManager.directoryAssessmentData))
        {
            Directory.CreateDirectory(DataManager.directoryAssessmentData);
        }
        if (!File.Exists(DataManager.filePathAssessmentData))
        {
            File.Create(DataManager.filePathAssessmentData).Dispose();
        }
        try
        {
   
            if (File.Exists(DataManager.filePathAssessmentData) && File.ReadAllLines(DataManager.filePathAssessmentData).Length == 0)
            {
                string headerData = "startdate,Max_x,Min_x,Max_y,Min_y";
                File.WriteAllText(DataManager.filePathAssessmentData, headerData + "\n"); // Add a new line after the header
                Debug.Log("Header written successfully.");
                DateTime time = DateTime.Now;
                string data = time.ToString() + "," + max_x + "," + min_x + "," + max_y + "," + min_y;
                File.AppendAllText(DataManager.filePathAssessmentData, data + "\n");
                AppLogger.LogInfo("Assessment data writtern successfully");
            }
            else
            {
                DateTime time = DateTime.Now;
                string data = time.ToString()+","+max_x+","+min_x+","+max_y+","+min_y;
                File.AppendAllText(DataManager.filePathAssessmentData, data + "\n");
                AppLogger.LogInfo("Assessment data writtern successfully");
            }
        }
        catch (Exception ex)
        {
            // Catch any other generic exceptions
            Debug.LogError("An error occurred while writing the data: " + ex.Message);
            AppLogger.LogError("An error occurred while writing the data: " + ex.Message);
        }
    }
    public void updateforce()
    {
        force.value = 0.005f * MarsComm.forceTwo;
        //Debug.Log(force.value);
    }
    public void halfWeightSupport()
    {
        MarsComm.SUPPORT = MarsComm.SUPPORT_CODE[2];
        AppData.dataSendToRobot = new float[] { MarsComm.SUPPORT, 0.0f, SUPPORT_MODE, 0.0f };
        Debug.Log("Half weight support initiated");
        AppLogger.LogInfo("half weight support initiated");
        SceneManager.LoadScene("halfWeightSupportScene");
    }
    private void OnApplicationQuit()
    {
        Application.Quit();
        JediComm.Disconnect();
    }
}

