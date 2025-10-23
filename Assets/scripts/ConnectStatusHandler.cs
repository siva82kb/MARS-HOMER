using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class connectStatusHandler : MonoBehaviour
{
    private Image connectStatus;
    private GameObject loading;

    private TextMeshProUGUI statusText;
    private float disconnectTimer = 0f;
    private const float shutdownDelay = 3f;

    void Awake()
    {
        // Subscribe to shutdown events once per instance
        Application.quitting += CloseAppLogger; //for Exe file
        AppDomain.CurrentDomain.ProcessExit += (_, __) => CloseAppLogger(); // for external crash like OS Crash

#if UNITY_EDITOR
        EditorApplication.quitting += CloseAppLogger; //for editor
#endif
    }
    // Start is called before the first frame update
    void Start()
    {
        connectStatus = GetComponent<Image>(); // Uncomment if connectStatus is on the same GameObject
        loading = transform.Find("loading").gameObject; // Assuming loading is a child GameObject
        statusText = transform.Find("statusText").GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        // Update connection status
        if (ConnectToRobot.isMARS)
        {
            connectStatus.color = Color.green;
            loading.SetActive(false);
            statusText.text = $"{MarsComm.version}\n[{MarsComm.frameRate:F1}Hz]";
            disconnectTimer = 0f; //reset when connected

        }
        else
        {
            disconnectTimer += Time.deltaTime;

            if (disconnectTimer >= shutdownDelay)
            {
                CloseAppLogger();
            }
            connectStatus.color = Color.red;
            loading.SetActive(true);
            statusText.text = "Not connected";


        }
           
    }
    private void CloseAppLogger()
    {
        AppLogger.StopLogging();
        MarsCommLogger.StopLogging();

        Application.Quit();
            #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false; // Stop play mode if in editor
            #endif
        //Need to change
        //MarsAanLogger.StopLogging();

    }
}
