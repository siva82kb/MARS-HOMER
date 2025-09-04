using System.Text.RegularExpressions;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;


public class UserCalibrationSceneHandler : MonoBehaviour
{
    //ui related variables
    public TMP_Text instructionText;
    public TMP_Text statusText;
    public TMP_Text uaLength;
    public TMP_Text faLength;
    public TMP_Text epPosition;
    private readonly string prevScene = "ROBOTCALIB";
    private readonly string nextScene = "USERCALIB";
    private bool attachMarsButtonEvent = true;
    private string _limb;
    private bool buttonPressed;
    private static string FLOAT_FORMAT = "+0.000;-0.000";


    void Start()
    {
        // Initialize AppData
        AppData.Instance.Initialize(SceneManager.GetActiveScene().name);

        // Check if the directory exists
        if (!Directory.Exists(DataManager.basePath)) Directory.CreateDirectory(DataManager.basePath);
        if (!File.Exists(DataManager.configFile)) SceneManager.LoadScene("CONFIG");

        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

        // Check if the device is calibrated.
        if (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB")
        {
           SceneManager.LoadScene(prevScene);
        }

        // Initialize UI
        InitUI();
    }

    void Update()
    {
        MarsComm.sendHeartbeat();
        
        // Debug.Log(MarsComm.xEndpoint);
        epPosition.text = $"{(100 * MarsComm.xEndpoint).ToString(FLOAT_FORMAT)}cm, {(100 * MarsComm.yEndpoint).ToString(FLOAT_FORMAT)}cm, {(100 * MarsComm.zEndpoint).ToString(FLOAT_FORMAT)}cm";

        // Wait for a second before doing anything.
        if (Time.timeSinceLevelLoad < 1) return;

        // Attach MARS event listeners.
        if (attachMarsButtonEvent)
        {
            attachMarsButtonEvent = false;
            // Set limb
            MarsComm.setLimb(_limb);
            MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
        }

        // Update status text.
        string _status = $"User Limb: {_limb} | {MarsComm.CALIBRATION[MarsComm.calibration]}\n{MarsComm.imu1Angle}deg, {MarsComm.imu2Angle}deg, {MarsComm.imu3Angle}deg, {MarsComm.imu4Angle}deg";
        statusText.text = _status;

        // Check if scene is to be changed.
        if (MarsComm.CALIBRATION[MarsComm.calibration] == "YESCALIB")
        {
            instructionText.text = "MARS calibration successful.";
            SceneManager.LoadScene(nextScene);
        }
        else
        {
            // Check if all angles are within 20deg.
            if (Mathf.Abs(MarsComm.imu1Angle) > 20 || Mathf.Abs(MarsComm.imu2Angle) > 20 || Mathf.Abs(MarsComm.imu3Angle) > 20 || Mathf.Abs(MarsComm.imu4Angle) > 20)
            {
                instructionText.text = "Make sure all angles are within 20 degrees.";
                instructionText.color = new Color32(202, 0, 0, 255);
            }
            else
            {
                instructionText.text = "Press the MARS Button when ready.";
                instructionText.color = new Color32(202, 108, 0, 255);
            }
        }
    }

    public void InitUI()
    {
        statusText.text = "";
        instructionText.text = "";
        uaLength.text = (100 * AppData.Instance.userData.uaLength).ToString() + "cm";
        faLength.text = (100 * AppData.Instance.userData.faLength).ToString() + "cm";
        epPosition.text = $"{(100 * MarsComm.xEndpoint).ToString(FLOAT_FORMAT)}cm, {(100 * MarsComm.yEndpoint).ToString(FLOAT_FORMAT)}cm, {(100 * MarsComm.zEndpoint).ToString(FLOAT_FORMAT)}cm";
    }

    public void onMarsButtonReleased()
    {
        // Send the calibration command.
        MarsComm.calibrate();
    }
    private void OnDestroy()
    {
        MarsComm.OnMarsButtonReleased -= onMarsButtonReleased;
    }
    private void OnApplicationQuit()
    {

        Application.Quit();
        JediComm.Disconnect();
    }
}
