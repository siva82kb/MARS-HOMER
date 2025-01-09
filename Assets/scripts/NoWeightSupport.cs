using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System;

public class NoWeightSupport : MonoBehaviour
{
  
    public float time;
    List<Vector3> paths;

    void Awake()
    {
       
    }

    void Start()
    {
        AppLogger.StartLogging(SceneManager.GetActiveScene().name);
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

    }

    public void Update()
    {
        time += Time.deltaTime;
        System.DateTime currentTime = System.DateTime.Now;
    }
    
    public void onclick_recalibrate()
    {
        SceneManager.LoadScene("noWeightSupportScene");

    }
    public void onclickMenu()
    {
        AppLogger.LogInfo("NoWeightSupport Done");
        SceneManager.LoadScene("chooseMovementScene");
    }

  
    private void OnApplicationQuit()
    {

        Application.Quit();
        JediComm.Disconnect();
    }
}
