
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.UI;
using System;
using UnityEditor;
using System.IO;
using JetBrains.Annotations;
using System.Runtime.Remoting.Messaging;

public class AssessROMML : MarsAssessAROM
{
    // Scenes to change to.
    private readonly string preScene = "CHOOSEMOVE";
    private readonly string robotCalibScene = "ROBOTCALIB";
    private readonly string marsSetUp = "MARSSETUP";
    private readonly string gameScene = "SSHOME";

    protected override void Awake()
    {
        base.Awake();
        MarsComm.sendHeartbeat();
        MarsComm.setControlType("POSITION");
    }

    protected override void Start()
    {
        // Initialize AppData if needed
        if (AppData.Instance.userData == null)
        {
            AppData.Instance.Initialize(SceneManager.GetActiveScene().name);
        }

        // Check if the directory exists
        if (!Directory.Exists(DataManager.basePath)) Directory.CreateDirectory(DataManager.basePath);
        if (!File.Exists(DataManager.configFile)) SceneManager.LoadScene("CONFIG");

        // Logging the scene
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

        // If the robot is not calibrated go to the robot calib scene.
        if (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB") SceneManager.LoadScene(robotCalibScene);

        // If the robot is not in position control go to the mars setup scene.
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] != "POSITION") SceneManager.LoadScene(marsSetUp);

        // Set the movement.
        movement = "ML";
        oldMarsArom = AppData.Instance.selectedMovement?.currentArom;
        base.Start();
    }
    protected override void Update()
    {
        MarsComm.sendHeartbeat();
        base.Update();

        // Check if its time to change scene.
        if (changeScene)
        {
            SceneManager.LoadScene(gameScene);
        }
    }

    void getRandomTargt()
    {
        // Vector2 t;
        // Vector2 target;
      
        // //rx+ry<=1
        // float rx = UnityEngine.Random.Range(0, 1f);
        // float ry = UnityEngine.Random.Range(0, (1f - rx));

        // //find left or right
        // int random = UnityEngine.Random.value < 0.5f ? -1 : 1;
     
        // if (random == 1)
        // {
           
        //     t = (rx * x1) + (ry * y1);
        //     target = t + left;
            
        // }
        // else
        // {
        //     t = (rx * x2) +( ry * y2);
        //     target = t + right;
        // }

        // GameObject circle6 = Instantiate(circlePrefab, target, Quaternion.identity);
        //testCircle = circle6;
        // if (testCircle != null)
        //     Destroy(testCircle);
        // testCircle = circle6;

    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        // MarsComm.OnMarsButtonReleased -= OnMarsButtonReleased;
    }
}
