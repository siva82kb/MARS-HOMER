using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using UnityEditor.ShaderKeywordFilter;
using Unity.Mathematics;
using Unity.VisualScripting;

public class DiagnosticSceneHandler : MonoBehaviour
{
    public TMP_Text dataDisplayText;
    public Dropdown ddLimbType;
    public Button btnCalibrate;
    public Toggle tglLogData;
    public Slider sldrTarget;
    public TMP_Text targetValueText;
    public Button btnSetTarget;
    public Toggle tglCtrlPosition;
    
    private static string FLOAT_FORMAT = "+0.000;-0.000";
    private string fileName = "";
    private StreamWriter fileWriter = null;
    private bool updateControlControls = true;
    private const int MAX_ENDPOINTS = 10;
    private int endpointCount = 0;
    private Vector3[] endpointPositions = new Vector3[100];
    private float setHLimbKinUALength = 0.0f;
    private float setHLimbKinFALength = 0.0f;
    private float setHLimbKinShPosZ = 0.0f;
    private float setHLimbDynUAWeight = 0.0f;
    private float setHLimbDynFAWeight = 0.0f;
    // Flag to check the human limb kinematic parameters.
    private bool hLimbKinParamCheckFlag = false;
    private bool hLimbKinParamSetDone = false;
    // Flags to run the human limb dynamic parameter estimation statemachine.
    // X | X | X | X | All Done | Test AWS | Param Set | Start Estimation | Set Up Ready
    private byte stateMachineFlags = 0x00;
    // Dynamic parameter estimation variables.
    private enum LimbDynEstState
    {
        WAITFORSTART = 0x00,
        SETUPFORESTIMATION = 0x01,
        WAITFORESTIMATION = 0x02,
        ESTIMATE = 0x03,
        ESTIMATIONDONE = 0x04,
        WAITFORHOLDTEST = 0x05,
        HOLDTEST = 0x06,
        ALLDONE = 0x07
    }
    private LimbDynEstState hLimbDynEstState = LimbDynEstState.WAITFORSTART;
    private const int nParams = 2;
    private const int maxParamEstimates = 250;
    private RecursiveLeastSquares rlsHLimbWeights = new RecursiveLeastSquares(nParams);
    private float[,] weightParams = new float[maxParamEstimates, nParams];
    private int weightParamsIndex = 0;
    private float[] weightParamsMean = new float[nParams];
    private float[] weightParamsStdev = new float[nParams];
    // Transition control testing variable.s
    private bool updateTransCtrlUI = true;

    void Start()
    {
        // Connect to the robot.
        ConnectToRobot.Connect(AppData.COMPort);

        // Initialize UI
        InitializeUI();
        // Attach callbacks
        AttachControlCallbacks();
        
        // Reset the human limb dynamic parameter estimation statemachine flag.
        stateMachineFlags = 0x00;

        // Get device version.
        MarsComm.getVersion();
        MarsComm.startSensorStream();
        MarsComm.setDiagnosticMode();
    }

    private void Update()
    {
        // MARS heartbeat
        MarsComm.sendHeartbeat();

        // Display device data
        DisplayDeviceData();

        // Update UI.
        UpdateUI();

        // Run the human limb dynamic parameter estimation state machine.
        RunHumanLimbDynParamEstimation();
    }

    private void RunHumanLimbDynParamEstimation()
    {
        if (hLimbDynEstState == LimbDynEstState.WAITFORSTART) return;
        Debug.Log(hLimbDynEstState);
        switch (hLimbDynEstState)
        {
            case LimbDynEstState.SETUPFORESTIMATION:
                // Wait for the start of the estimation.
                handleSetupDynParamEstimation();
                // Check statemachine flag
                if ((stateMachineFlags & 0x01) == 0x01)
                {
                    hLimbDynEstState = LimbDynEstState.WAITFORESTIMATION;
                }
                break;
            case LimbDynEstState.WAITFORESTIMATION:
                // Check statemachine flag
                if ((stateMachineFlags & 0x02) == 0x02)
                {
                    hLimbDynEstState = LimbDynEstState.ESTIMATE;
                }
                break;
            case LimbDynEstState.ESTIMATE:
                // Update the estimtate if parameters are not yet estimated.
                // rlsHLimbWeights.Update(new float[] {
                //     (float) (Math.Sin(MarsComm.phi1 * Mathf.Deg2Rad) * Math.Cos(MarsComm.phi2 * Mathf.Deg2Rad)),
                //     (float) (Math.Sin(MarsComm.phi1 * Mathf.Deg2Rad) * Math.Cos((MarsComm.phi2 - MarsComm.phi3) * Mathf.Deg2Rad))
                // }, MarsComm.torque);
                // // Check if we need to change state.
                // if (computeMeanAndStdev())
                // {
                //     hLimbDynEstState = LimbDynEstState.ESTIMATIONDONE;
                // }
                break;
            case LimbDynEstState.ESTIMATIONDONE:
                // if ((stateMachineFlags & 0x04) == 0x04)
                // {
                //     // Check statemachine flag
                //     hLimbDynEstState = LimbDynEstState.WAITFORHOLDTEST;
                // }
                // else
                // {
                //     // Send the weight parameters to MARS.
                //     Debug.Log("Setting human limb dynamic parameters: " +
                //         $"UA Weight = {weightParamsMean[0]:F3}, FA Weight = {weightParamsMean[1]:F3}");
                //     setHLimbDynUAWeight = weightParamsMean[0];
                //     setHLimbDynFAWeight = weightParamsMean[1];
                //     MarsComm.setHumanLimbDynParams(setHLimbDynUAWeight, setHLimbDynFAWeight);
                //     // Get the human limb dynamic parameters from MARS.
                //     MarsComm.getHumanLimbDynParams();
                // }
                break;
            case LimbDynEstState.WAITFORHOLDTEST:
                // Check statemachine flag
                if ((stateMachineFlags & 0x08) == 0x08)
                {
                    hLimbDynEstState = LimbDynEstState.HOLDTEST;
                }
                break;
            case LimbDynEstState.HOLDTEST:
                // Perform hold testing with the estimated weights.
                // Check the current control type.
                // if (MarsComm.CONTROLTYPE[MarsComm.controlType] != "AWS")
                // {
                //     // Set the control mode to AWS.
                //     MarsComm.transitionControl("AWS", 1.0f, 5.0f);
                // }
                // // Check statemachine flag
                // if ((stateMachineFlags & 0x10) == 0x10)
                // {
                //     hLimbDynEstState = LimbDynEstState.ALLDONE;
                // }
                break;
            case LimbDynEstState.ALLDONE:
                // Handle all done.
                // Wait for the start of the estimation.
                // if (MarsComm.CONTROLTYPE[MarsComm.controlType] != "POSITION")
                // {
                //     // Set the control mode to AWS.
                //     MarsComm.transitionControl("POSITION", -90f, 5.0f);
                // }
                // else
                // {
                //     hLimbDynEstState = LimbDynEstState.WAITFORSTART;
                // }
                break;
        }
    }
    
    private void handleSetupDynParamEstimation()
    {
        // Check if the control mode is position, else set and leave.
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] != "POSITION")
        {
            MarsComm.setControlType("POSITION");
            MarsComm.setControlTarget(-90f);
            return;
        }
        // Check if the target position is set to -90 degrees.
        if (MarsComm.target != -90f)
        {
            MarsComm.setControlTarget(-90f);
            return;
        }
        // Check if the robot has reached the target position.
        if (Mathf.Abs(MarsComm.angle1 - MarsComm.target) < 2.5)
        {
            if (hLimbDynEstState == LimbDynEstState.SETUPFORESTIMATION)
            {
                stateMachineFlags = (byte)(stateMachineFlags | 0x01);
                rlsHLimbWeights.ResetEstimator();
            }
        }
    }

    private bool handleTearDownSetup()
    {
        // Check if the target position is set to -90 degrees.
        if (MarsComm.target != 0f)
        {
            MarsComm.setControlTarget(0f);
            return false;
        }
        // Check if the robot has reached the target position.
        if (Mathf.Abs(MarsComm.angle1 - MarsComm.target) > 2.5)
        {
            return false;
        }
        // Check if the control mode is position, else set and leave.
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] != "NONE")
        {
            MarsComm.setControlType("NONE");
            return false;
        }
        return true;
    }

    private void onNewMarsData()
    {
        // Write row to the file if logging is enabled.
        if (fileWriter != null)
        {
            fileWriter.WriteLine($"{MarsComm.runTime},{MarsComm.packetNumber},{MarsComm.status},{MarsComm.errorString},{MarsComm.limb},{MarsComm.calibration},,,{MarsComm.angle1},{MarsComm.angle2},{MarsComm.angle3},{MarsComm.angle4},{MarsComm.force},{MarsComm.torque},{MarsComm.xEndpoint},{MarsComm.yEndpoint},{MarsComm.zEndpoint},,,,{MarsComm.imu1Angle},{MarsComm.imu2Angle},{MarsComm.imu3Angle},{MarsComm.marButton},{MarsComm.calibButton},{MarsComm.target},{MarsComm.desired},{MarsComm.control}");
            fileWriter.Flush();
        }
    }

    private void onMarsButtonReleased()
    {
        // This method is called when the MARS button is released
        Debug.Log("MARS button released.");
    }

    private void onCalibButtonReleased()
    {
    }

    private void onControlModeChange()
    {
        // This method is called when the control mode is changed
        Debug.Log($"Control mode changed to: {MarsComm.CONTROLTYPE[MarsComm.controlType]}");
        // Set the update slider flag for the main thread to update the UI.
        updateControlControls = true;
        updateTransCtrlUI = true;
    }

    private void InitializeUI()
    {
        // Fill dropdown list
        ddLimbType.ClearOptions();
        ddLimbType.AddOptions(MarsComm.LIMBTEXT.ToList());
        // Disable all controls.
        tglCtrlPosition.isOn = false;
        tglCtrlPosition.interactable = false;
        // Disable set target button.
        btnSetTarget.interactable = false;
        // Clear panel selections.
        tglLogData.enabled = true;
        tglLogData.isOn = false;
    }

    private List<Dropdown.OptionData> GetAvailableControlTypes(List<string> list)
    {
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        // Check the current control type.
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "NONE")
        {
            foreach (string item in list)
            {
                if (item != "Arm Weight Support") options.Add(new Dropdown.OptionData(item));
                continue;
            }
        }
        return options;
    }

    public void AttachControlCallbacks()
    {
        // Dropdown value change.
        ddLimbType.onValueChanged.AddListener(delegate { OnLimbTypeChange(); });

        // Calibrate button click.
        btnCalibrate.onClick.AddListener(delegate
        {
            // Send calibration command to MARS.
            MarsComm.calibrate();
            Debug.Log("Calibration command sent.");
        });

        // Toggle button
        tglLogData.onValueChanged.AddListener(onDataLogChange);

        // Controls toggle buttons clicks.
        tglCtrlPosition.onValueChanged.AddListener(delegate { OnCtrlPositionChange(); });

        // Slider value change.
        sldrTarget.onValueChanged.AddListener(delegate { OnControlTargetChange(); });

        // Set target button click.
        btnSetTarget.onClick.AddListener(delegate { OnTargetSet(); });

        // Listen to MARS's event
        MarsComm.OnMarsButtonReleased += onMarsButtonReleased;
        MarsComm.OnCalibButtonReleased += onCalibButtonReleased;
        MarsComm.OnNewMarsData += onNewMarsData;
        MarsComm.OnControlModeChange += onControlModeChange;
    }

    public void UpdateUI()
    {
        // Check if MARS has been calibrated.
        btnCalibrate.enabled = (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB")
            && (MarsComm.LIMBTYPE[ddLimbType.value] != "NOLIMB");

        // Enable control toggle buttons depending on the current control type.
        tglCtrlPosition.interactable = (MarsComm.CALIBRATION[MarsComm.calibration] == "YESCALIB")
            && (MarsComm.CONTROLTYPE[MarsComm.controlType] != "TORQUE");

        // Enable the set target button only if the control is not NONE.
        btnSetTarget.interactable = MarsComm.CONTROLTYPE[MarsComm.controlType] != "NONE";

        // Check of the slider range needs to be updated.
        if (updateControlControls)
        {
            // Change slider range based on control type.
            if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION")
            {
                sldrTarget.minValue = -120f;
                sldrTarget.maxValue = 0f;
                sldrTarget.value = MarsComm.angle1;
                targetValueText.text = sldrTarget.value.ToString(FLOAT_FORMAT) + " deg";
            }
            else if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "TORQUE")
            {
                sldrTarget.minValue = -10f;
                sldrTarget.maxValue = 10f;
                sldrTarget.value = MarsComm.torque;
                targetValueText.text = sldrTarget.value.ToString(FLOAT_FORMAT) + " Nm";
            }
            else
            {
                sldrTarget.minValue = 0f;
                sldrTarget.maxValue = 1f;
                sldrTarget.value = 0;
                targetValueText.text = "";
            }
            updateControlControls = false;
        }
    }

    public void DisplayDeviceData()
    {
        // State text
        string runT = MarsComm.runTime.ToString("0.000").PadRight(15);
        string packNo = MarsComm.packetNumber.ToString().PadRight(8);
        string stateText = string.Join("\n", new string[] {
            $"Time          : {MarsComm.currentTime, -15}",
            $"Dev ID        : {MarsComm.deviceId}",
            $"F/W Version   : {MarsComm.version}",
            $"Compile Date  : {MarsComm.compileDate}",
            "",
            $"Device Time   : {runT} | Packet Number: {packNo}",
            $"Frame Rate    : {MarsComm.frameRate.ToString(FLOAT_FORMAT)} Hz",
            $"Status        : {MarsComm.OUTDATATYPE[MarsComm.dataType], -15} | Error : {MarsComm.errorString}",
            $"Calib         : {MarsComm.CALIBRATION[MarsComm.calibration], -15} | Cmd Status: {MarsComm.COMMAND_STATUS[MarsComm.recentCommandStatus]}",
            $"Limb          : {MarsComm.LIMBTYPE[MarsComm.limb], -15}",
            $"Control       : {MarsComm.CONTROLTYPE[MarsComm.controlType]}",
            ""
        });
        //Construct the display text while ensuring values are safely converted to string
        string robotAngles = string.Join(" ", new string[] {
            MarsComm.angle1.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.angle2.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.angle3.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.angle4.ToString(FLOAT_FORMAT).PadRight(8),
        });
        string epPos = string.Join(" ", new string[] {
            MarsComm.xEndpoint.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.yEndpoint.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.zEndpoint.ToString(FLOAT_FORMAT).PadRight(8),
        });
        string imuAngles = string.Join(" ", new string[] {
            MarsComm.imu1Angle.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.imu2Angle.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.imu3Angle.ToString(FLOAT_FORMAT).PadRight(8),
            MarsComm.imu4Angle.ToString(FLOAT_FORMAT).PadRight(8),
        });
        string sensorText = String.Join("\n", new string[] {
            $"Robot Angles  : {robotAngles}",
            $"IMU Angles    : {imuAngles}",
            $"Endpoint Pos  : {epPos}",
            $"Force         : {MarsComm.force.ToString(FLOAT_FORMAT), -15} | Torque : {MarsComm.torque.ToString(FLOAT_FORMAT)}",
            $"Target        : {MarsComm.target.ToString(FLOAT_FORMAT), -15} | Desired : {MarsComm.desired.ToString(FLOAT_FORMAT)}",
            $"Control       : {MarsComm.control.ToString(FLOAT_FORMAT)}",
            $"MARS Button   : {MarsComm.marButton, -15} | Calib Button : {MarsComm.calibButton}"
        });
        // If DIAGNOSTICS is enabled, append diagnostics data
        if (MarsComm.OUTDATATYPE[MarsComm.dataType] == "DIAGNOSTICS")
        {
            sensorText += String.Join("\n", new string[] {
                "",
                $"ErrorP        : {MarsComm.errP.ToString(FLOAT_FORMAT)}",
                $"ErrorD        : {MarsComm.errD.ToString(FLOAT_FORMAT)}",
                $"ErrorI        : {MarsComm.errI.ToString(FLOAT_FORMAT)}",
                $"Grav. Comp.   : {MarsComm.gravityCompensationTorque.ToString(FLOAT_FORMAT)}",
                $"Ang. Vel.     : {MarsComm.angularVelocity1.ToString(FLOAT_FORMAT)}",
            });
        }
        dataDisplayText.text = stateText + sensorText;
    }

    /*
     * Callback functions.
     */
    private void OnLimbTypeChange()
    {
        // Set MARS limb type.
        MarsComm.setLimb(MarsComm.LIMBTYPE[ddLimbType.value]);
    }

    private void onDataLogChange(bool isOn)
    {
        if (isOn)
        {
            // Enable data logging.
            fileName = $"MARS_Data_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            fileWriter = new StreamWriter(fileName, true);
            fileWriter.WriteLine($"DeviceID: {MarsComm.deviceId}");
            fileWriter.WriteLine("runTime,packetNumber,status,errorString,limb,calib,limbkinparam,limbdynparam,angle1,angle2,angle3,angle4,force,torque,epx,epy,epz,phi1,phi2,phi3,imuangle1,imuangle2,imuangle3,marbtn,calibbtn,target,desired,control");
            Debug.Log($"Data logging started. File: {fileName}");
        }
        else
        {
            // Disable data logging.
            if (fileWriter != null)
            {
                fileWriter.Close();
                fileWriter = null;
                Debug.Log($"Data logging stopped. File saved: {fileName}");
            }
        }
    }
    
    private void OnCtrlPositionChange()
    {
        if (tglCtrlPosition.isOn) MarsComm.setControlType("POSITION");
        else MarsComm.setControlType("NONE");
    }
    
    private void OnControlTargetChange()
    {
        targetValueText.text = sldrTarget.value.ToString(FLOAT_FORMAT)
            + (MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION" ? " deg" : " Nm");
    }
    private void OnTargetSet()
    {
        // Set target value for control.
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "POSITION")
        {
            // Set the target position.
            MarsComm.setControlTarget(sldrTarget.value);
            Debug.Log($"Control target set to: {sldrTarget.value} deg");
        }
        else if (MarsComm.CONTROLTYPE[MarsComm.controlType] == "TORQUE")
        {
            // Set the target torque.
            MarsComm.setControlTarget(sldrTarget.value, dur: 3f);
            Debug.Log($"Control target set to: {sldrTarget.value} Nm");
        }
    }
    
    private void OnApplicationQuit()
    {
        Application.Quit();
        JediComm.Disconnect();
    }
}
