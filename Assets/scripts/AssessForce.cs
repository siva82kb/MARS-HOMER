using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static AssessROM;
using static UnityEditor.Rendering.InspectorCurveEditor;

public class AssessForce : MonoBehaviour
{
    public static AssessForce instance;
    public LineRenderer romQuad;
    public GameObject circlePrefab;
    public GameObject currentCircle;
    private GameObject topCircle;
    private GameObject bottomCircle;
    private GameObject leftCircle;
    private GameObject rightCircle;
    private GameObject testCircle;
    public readonly string preScene = "CHOOSEMOVE";
    public readonly string robotCalibScene = "ROBOTCALIB";
    private string marsSetUp = "MARSSETUP";
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        MarsComm.sendHeartbeat();
        // Initialize AppData if needed
        if (AppData.Instance.userData == null)
        {
            AppData.Instance.Initialize(SceneManager.GetActiveScene().name);
        }

        // Check if the directory exists
        if (!Directory.Exists(DataManager.basePath)) Directory.CreateDirectory(DataManager.basePath);
        if (!File.Exists(DataManager.configFile)) SceneManager.LoadScene("CONFIG");

        //logging about the scene
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

        // IF the robot is not calibrated go to the robot calib scene.
        if (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB")
        {
            SceneManager.LoadScene(robotCalibScene);
        }
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] != "POSITION")
            SceneManager.LoadScene(marsSetUp);

      
        MarsComm.OnMarsButtonReleased += OnMarsButtonReleased;
        float minx, maxx, miny, maxy, meanx, meany;
        //minx = AssessROM.instance.minxpres;
        //miny = AssessROM.instance.minypres;
        //maxy = AssessROM.instance.maxypres;
        //maxx = AssessROM.instance.maxxpres;
        //meanx = AssessROM.instance.meanZpre;
        //meany = AssessROM.instance.meanYpre;
        //AssessROM.instance.DrawQuad(minx, maxx, miny, maxy, meanx, meany, romQuad, new Color(137 / 255f, 175 / 255f, 253 / 255f));
        if (currentCircle == null)
        {
            GameObject circle = Instantiate(circlePrefab, circlePrefab.transform.position, Quaternion.identity);
            //GameObject circle1 = Instantiate(circlePrefab, new Vector3(meanx, miny, 0), Quaternion.identity);
            //GameObject circle2 = Instantiate(circlePrefab, new Vector3(maxx, meany, 0), Quaternion.identity);
            //GameObject circle3 = Instantiate(circlePrefab, new Vector3(meanx, maxy, 0), Quaternion.identity);
            //GameObject circle4 = Instantiate(circlePrefab, new Vector3(minx, meany, 0), Quaternion.identity);
            currentCircle = circle;
            currentCircle.GetComponent<SpriteRenderer>().color = Color.green;
            //bottomCircle = circle1;
            //rightCircle = circle2;
            //topCircle = circle3;
            //leftCircle = circle4;
        }
    }

    // Update is called once per frame
    void Update()
    {
        MarsComm.sendHeartbeat();
    }
    public void OnMarsButtonReleased()
    {

      

    }
}
