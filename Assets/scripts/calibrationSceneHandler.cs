using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class calibrationSceneHandler : MonoBehaviour
{
    public static bool changeScene = false;
    public readonly string nextScene = "chooseMovementScene";
    void Start()
    {
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        MarsComm.OnButtonReleased += onMarsButtonReleased;
    }

    // Update is called once per frame
    void Update()
    {
       
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
        Debug.Log("pressed");
    }

    private void LoadTargetScene()
    {
        AppLogger.LogInfo($"Switching to the next scene '{nextScene}'.");
        SceneManager.LoadScene(nextScene);
    }
}
