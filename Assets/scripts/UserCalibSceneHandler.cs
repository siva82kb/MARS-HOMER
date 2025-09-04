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
    public TMP_InputField uaLength;
    public TMP_InputField faLength;
    public Button btnLimbLengthSet;
    private readonly string nextScene = "USERCALIB";
    private bool attachMarsButtonEvent = true;
    private string _limb;
    private bool buttonPressed;

    void Start()
    {
        // Initialize AppData
        AppData.Instance.Initialize(SceneManager.GetActiveScene().name);

        // Check if the directory exists
        if (!Directory.Exists(DataManager.basePath)) Directory.CreateDirectory(DataManager.basePath);
        if (!File.Exists(DataManager.configFile)) SceneManager.LoadScene("CONFIG");

        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

        // Reset limb.
        MarsComm.setLimb("NOLIMB");

        // Set limb for MARS.
        _limb = AppData.Instance.userData.rightHand ? "RIGHT" : "LEFT";

        // Initialize UI
        statusText.text = "";
        instructionText.text = "";
    }

    void Update()
    {
        MarsComm.sendHeartbeat();

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
