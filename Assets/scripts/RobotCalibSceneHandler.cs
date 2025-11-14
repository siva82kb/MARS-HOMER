using System.Text.RegularExpressions;
using TMPro;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;


public class RobotCalibrationSceneHandler : MonoBehaviour
{
    //ui related variables
    public TMP_Text instructionText;
    public TMP_Text statusText;
    public readonly string choosePlaneScene = "CHOOSEPLANE";
    public readonly string marsSetupScene = "MARSSETUP";
    private bool attachMarsButtonEvent = true;
    private bool setLimbFlag = true;
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
        _limb = MarsComm.LIMBTYPE[AppData.Instance.userData.limb];

        // Initialize UI
        statusText.text = "";
        instructionText.text = "";
    }

    void Update()
    {
        MarsComm.sendHeartbeat();

        // Wait for a second before doing anything.
        if (Time.timeSinceLevelLoad < 0.25) return;

        // Set the limb.
        if (setLimbFlag)
        {
            // Check if the limb is set correctly.
            if (MarsComm.LIMBTYPE[MarsComm.limb] == _limb)
            {
                setLimbFlag = false;
                AppLogger.LogInfo($"Limb set to {_limb}");
            }
            else
            {
                // Set limb
                MarsComm.setLimb(_limb);
            }
        }

        // Update status text.
        string _status = string.Join(" | ", new string[] {
            MarsComm.imuAngle1.ToString("+00;-00"),
            MarsComm.imuAngle2.ToString("+00;-00"),
            MarsComm.imuAngle3.ToString("+00;-00"),
            MarsComm.imuAngle4.ToString("+00;-00")
        });
        statusText.text = $"[ {_status} ] deg\n" + $"Angles must be less than {MarsComm.CALIB_ANGLE_LIMIT} deg." ;

        // If limb is not set, there is nothing more to do.
        if (setLimbFlag) return;

        // Check if scene is to be changed.
        if (MarsComm.CALIBRATION[MarsComm.calibration] == "YESCALIB")
        {
            instructionText.text = "MARS calibration successful.";
            AppLogger.LogInfo($"MARS calibration successfully completed.");
            // Check of the training plane angle is set.
            if (AppData.Instance.userData.trainingPlaneAngle == 0f || AppData.Instance.userData.trainingPlaneAngle == 999)
            {
                AppLogger.LogInfo("Training Plane Angle is not set. Going to Choose Plane scene.");
                SceneManager.LoadScene(choosePlaneScene);
                return;
            }
            else
            {
                AppLogger.LogInfo("Training Plane Angle is set. Going to Choose Move scene.");
                SceneManager.LoadScene(marsSetupScene);
                return;
            }
        }
        else
        {
            // Check if all angles are within CALIB_ANGLE_LIMIT.
            if (Mathf.Abs(MarsComm.imuAngle1) > MarsComm.CALIB_ANGLE_LIMIT || Mathf.Abs(MarsComm.imuAngle2) > MarsComm.CALIB_ANGLE_LIMIT || Mathf.Abs(MarsComm.imuAngle3) > MarsComm.CALIB_ANGLE_LIMIT || Mathf.Abs(MarsComm.imuAngle4) > MarsComm.CALIB_ANGLE_LIMIT)
            {
                if (!attachMarsButtonEvent)
                {
                    attachMarsButtonEvent = true;
                    MarsComm.OnMarsButtonReleased -= onMarsButtonReleased;
                    AppLogger.LogInfo($"MARS angle outside the limit of {MarsComm.CALIB_ANGLE_LIMIT} | {MarsComm.imuAngle1:F2}, {MarsComm.imuAngle2:F2}, {MarsComm.imuAngle3:F2}, {MarsComm.imuAngle4:F2}.");
                }
                instructionText.text = $"Make sure all angles are within {MarsComm.CALIB_ANGLE_LIMIT} degrees.";
                instructionText.color = new Color32(202, 0, 0, 255);
            }
            else
            {
                if (attachMarsButtonEvent)
                {
                    attachMarsButtonEvent = false;
                    MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
                    AppLogger.LogInfo($"MARS angle inside the limit of {MarsComm.CALIB_ANGLE_LIMIT} | {MarsComm.imuAngle1:F2}, {MarsComm.imuAngle2:F2}, {MarsComm.imuAngle3:F2}, {MarsComm.imuAngle4:F2}.");
                }
                instructionText.text = "Press the MARS Button when ready.";
                instructionText.color = new Color32(202,108 ,0, 255);
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
