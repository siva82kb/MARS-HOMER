using UnityEngine;
using System.IO;
//using System.IO.Ports;
using System;
using System.ComponentModel;


public static class MarsComm 
{
    // For error logging
    // Thread-safe version using lock
    private static readonly System.Random _random = new System.Random();
    private static readonly object _lock = new object();

    private static int GetRandomNumber()
    {
        lock (_lock)
        {
            return _random.Next(1, 101);
        }
    }

    // Device Level Constants
    public static readonly string[] OUTDATATYPE = new string[] { "SENSORSTREAM", "CONTROLPARAM", "DIAGNOSTICS", "VERSION" };
    public static readonly string[] LIMBTYPE = new string[] { "NONE", "RIGHT", "LEFT" };
    public static readonly string[] LIMBTEXT = new string[] {
        "None",
        "Right",
        "Left"
    };
    public static readonly string[] CALIBRATION = new string[] { "NOCALIB", "YESCALIB" };
    public static readonly string[] CONTROLTYPE = new string[] { "NONE", "POSITION", "TORQUE", "ARM_WEIGHT_SUPPORT" };
    public static readonly string[] CONTROLTYPETEXT = new string[] {
        "None",
        "Position",
        "Torque",
        "Arm Weight Support"
    };
    public static readonly int[] SENSORNUMBER = new int[] {
        5,  // SENSORSTREAM 
        0,  // CONTROLPARAM
        5   // DIAGNOSTICS
    };
    public static readonly double MAXTORQUE = 1.0; // Nm
    public static readonly int[] INDATATYPECODES = new int[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x80 };
    public static readonly string[] INDATATYPE = new string[] {
        "GET_VERSION",
        "RESET_PACKETNO",
        "SET_LIMB",
        "CALIBRATE",
        "START_STREAM",
        "STOP_STREAM",
        "SET_CONTROL_TYPE",
        "SET_CONTROL_TARGET",
        "SET_DIAGNOSTICS",
        "HEARTBEAT",
    };
    public static readonly string[] ERRORTYPES = new string[] {
        "ANGSENSERR",
        "MCURRSENSERR",
        "NOHEARTBEAT"
    };
    public static readonly int INVALID_TARGET = 999;
    
    static public byte currentButtonState, previousButtonState;
    static int sensorDataLength ; 
   
    // Button released event.
    public delegate void MarsButtonReleasedEvent();
    public static event MarsButtonReleasedEvent OnMarsButtonReleased;
    public delegate void CalibButtonReleasedEvent();
    public static event CalibButtonReleasedEvent OnCalibButtonReleased;
    // New data event.
    public delegate void MarsNewDataEvent();
    public static event MarsNewDataEvent OnNewMarsData;


    // MARS Robot Parameters
    private const float L1 = 475.0f;
    private const float L2 = 291.0f;
    static public float shF, shA, elF;

    static public float theta1, theta2, theta3, theta4;
    public static float[] shPos = new float[3];
    static public float[] endPt = new float[3];
    static public float[] zvec = new float[3];
    static public float[] elPt = new float[3];
    static public float[] fA = new float[3];
    static public float[] uA = new float[3];
    public static int[] CONTROL_STATUS_CODE = new int[] { 1001,  //hold
                                                             0   //release
                                                        };
    public static float SUPPORT;
    public static float[] SUPPORT_CODE = new float[] {
                                                         0.0f,//NoSupport
                                                         1.0f,//FullWeightSupport
                                                         0.5f //HalfweightSupport
                                                      };
    public static float[] OFFSET = new float[] { 0,
                                                -0.01745f,//LEFT-HAND 
                                                 0.01745f //RIGHT-HAND
                                                };

    public static int controlStatus;
    public static float thetades1;


    // Private variables
    static private byte[] rawBytes = new byte[256];
    // For the following arrays, the first element represents the number of elements in the array.
    static private int[] previousStateData = new int[32];
    static private int[] currentStateData = new int[32];
    static private float[] currentSensorData = new float[10];

    // Public variables
    static public DateTime previousTime { get; private set; }
    static public DateTime currentTime { get; private set; }
    static public double frameRate { get; private set; }
    static public String deviceId { get; private set; }
    static public String version { get; private set; }
    static public String compileDate { get; private set; }
    static public ushort packetNumber { get; private set; }
    static public float runTime { get; private set; }
    static public float prevRunTime { get; private set; }
    public static int GetMarsCodeFromLabel(string[] array, string value)
    {
        return Array.IndexOf(array, value) - 1;
    }
    static public int status
    {
        get
        {
            return currentStateData[1];
        }
    }
    static public int dataType
    {
        get
        {
            return (status >> 4);
        }
    }
    static private int prevErrorStatus = 0;
    static public int errorStatus
    {
        get
        {
            return currentStateData[2];
        }
    }
    static public string errorString
    {
        get => getErrorString(errorStatus);
    }
    static public int controlType
    {
        get
        {
            return getControlType(status);
        }
    }
    static public int calibration
    {
        get
        {
            return status & 0x01;
        }
    }
    static public int mechanism
    {
        get
        {
            return currentStateData[3] >> 4;
        }
    }
    static public int actuated
    {
        get
        {
            return currentStateData[3] & 0x01;
        }
    }
    static public int button
    {
        get
        {
            return currentStateData[7];
        }
    }
    
    static public float angleOne
    {
        get
        {
            return currentSensorData[0];
        }
    }
    static public float angleTwo
    {
        get
        {
            return currentSensorData[1];
        }
    }
    static public float angleThree
    {
        get
        {
            return currentSensorData[2];
        }
    }
    static public float angleFour
    {
        get
        {
            return currentSensorData[3];
        }
    }
    static public float forceOne
    {
        get
        {
            return currentSensorData[4];
        }
    }
    static public float calibBtnState
    {
        get
        {
            return currentSensorData[5];
        }
    }
    static public float desOne
    {
        get
        {
            return currentSensorData[6];
        }
    }
     static public float desTwo
    {
        get
        {
            return currentSensorData[7];
        }
    }
    static public float desThree
    {
        get
        {
            return currentSensorData[8];
        }
    }
    static public float pcParameter
    {
        get
        {
            return currentSensorData[9];
        }
    }
    static public float shPosX
    {
        get
        {
            return currentSensorData[10];
        }
    }
    static public float shPosY
    {
        get
        {
            return currentSensorData[11];
        }
    }
    static public float shPosZ
    {
        get
        {
            return currentSensorData[12];
        }
    }
    static public float lenUpperArm
    {
        get
        {
            return currentSensorData[13];
        }
    }
    static public float lenLowerArm
    {
        get
        {
            return currentSensorData[14];
        }
    }
    static public float weight1
    {
        get
        {
            return currentSensorData[15];
        }
    }
    static public float weight2
    {
        get
        {
            return currentSensorData[16];
        }
    }
    static public float imuAng1
    {
        get
        {
            return currentSensorData[17];
        }
    }
    static public float imuAng2
    {
        get
        {
            return currentSensorData[18];
        }
    }
    static public float imuAng3
    {
        get
        {
            return currentSensorData[19];
        }
    }
    static public float imuAng4
    {
        get
        {
            return currentSensorData[20];
        }
    }
    //if we need to get imuRaw values
    //static public float imu1aX
    //{
    //    get
    //    {
    //        return currentSensorData[21];
    //    }
    //}
    //static public float imu1aY
    //{
    //    get
    //    {
    //        return currentSensorData[22];
    //    }
    //}
    //static public float imu1aZ
    //{
    //    get
    //    {
    //        return currentSensorData[23];
    //    }
    //}
    //static public float imu2aX
    //{
    //    get
    //    {
    //        return currentSensorData[24];
    //    }
    //}
    //static public float imu2aY
    //{
    //    get
    //    {
    //        return currentSensorData[25];
    //    }
    //}
    //static public float imu2aZ
    //{
    //    get
    //    {
    //        return currentSensorData[26];
    //    }
    //}
    //static public float imu3aX
    //{
    //    get
    //    {
    //        return currentSensorData[27];
    //    }
    //}
    //static public float imu3aY
    //{
    //    get
    //    {
    //        return currentSensorData[28];
    //    }
    //}
    //static public float imu3aZ
    //{
    //    get
    //    {
    //        return currentSensorData[29];
    //    }
    //}
    static public byte buttonState
    {
        get
        {
            return currentButtonState;
        }
    }
    
    private static int getControlType(int statusByte)
    {
        return (statusByte & 0x0E) >> 1;
    }

    private static string getErrorString(int err)
    {
        if (err == 0) return "NOERROR";
        string _str = "";
        for (int i = 0; i < 16; i++)
        {
            if ((errorStatus & (1 << i)) != 0)
            {
                _str += (_str != "") ? " | " : "";
                _str += ERRORTYPES[i];
            }
        }
        return _str;
    }
   
    static public void initalizeDataLength(int lenght)
    {
        sensorDataLength = (int)(lenght - 2) / 4; //buttonState,checksum bytes
        Debug.Log(sensorDataLength);

        currentSensorData = new float[sensorDataLength];
    }
    static public void parseRawBytes(byte[] rawBytes, uint payloadSize ,DateTime plTime)
    {
        Debug.Log(payloadSize);
        //Debug.Log(rawBytes.Length);
        sensorDataLength = (int)(payloadSize - 2) / 4; //buttonState,checksum bytes
        Debug.Log(sensorDataLength);
        if ( rawBytes.Length < payloadSize)
        {
            //Debug.Log("working");
            return;
        }
        try
        {
            previousButtonState = currentButtonState;
            currentTime = plTime;
            for (int i = 0; i < sensorDataLength; i++)
            {  
               //10 float values
                currentSensorData[i] = BitConverter.ToSingle (
                                            new byte[] { rawBytes[(i * 4) + 1],
                                                         rawBytes[(i * 4) + 2],
                                                         rawBytes[(i * 4) + 3],
                                                         rawBytes[(i * 4) + 4]
                                            }
                                            
                );
                
            }
            currentButtonState = rawBytes[payloadSize - 1];
            //// Check if the button has been released.
            if (previousButtonState == 0 && currentButtonState == 1)
            {
                OnButtonReleased?.Invoke();
            }

        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in parseRawBytes: {ex.Message}");
        }
      
    }
    static public void computeShouderPosition()
    {
           
            theta1 = OFFSET[AppData.useHand] * angleOne;
            theta2 = OFFSET[AppData.useHand] * angleTwo;
            theta3 = OFFSET[AppData.useHand] * angleThree;
            theta4 = OFFSET[AppData.useHand] * angleFour;

            zvec[0] = Mathf.Cos(theta1) * Mathf.Cos(theta2 + theta3 + theta4);
            zvec[1] = Mathf.Sin(theta1) * Mathf.Cos(theta2 + theta3 + theta4);
            zvec[2] = -Mathf.Sin(theta2 + theta3 + theta4);

            endPt[0] = Mathf.Cos(theta1) * (len1 * Mathf.Cos(theta2) + len2 * Mathf.Cos(theta2 + theta3));
            endPt[1] = Mathf.Sin(theta1) * (len1 * Mathf.Cos(theta2) + len2 * Mathf.Cos(theta2 + theta3));
            endPt[2] = -len1 * Mathf.Sin(theta2) - len2 * Mathf.Sin(theta2 + theta3);

            if (calibrationSceneHandler.calibrationState>0) 
            {
              shPos = new float[] { endPt[0], endPt[1] + AppData.lu, endPt[2] - AppData.lf };
            }
            
            elPt[0] = endPt[0] - AppData.lf * zvec[0];
            elPt[1] = endPt[1] - AppData.lf * zvec[1];
            elPt[2] = endPt[2] - AppData.lf * zvec[2];

            fA[0] = endPt[0] - elPt[0];
            fA[1] = endPt[1] - elPt[1];
            fA[2] = endPt[2] - elPt[2];

            uA[0] = elPt[0] - shPos[0];
            uA[1] = elPt[1] - shPos[1];
            uA[2] = elPt[2] - shPos[2];

            if (Mathf.Abs((fA[0] * uA[0] + fA[1] * uA[1] + fA[2] * uA[2]) / (AppData.lf*AppData.lu)) > 0.9999999f)
            {
                elF = 0;
            }
            else
            {
                elF = Mathf.Acos((fA[0] * uA[0] + fA[1] * uA[1] + fA[2] * uA[2]) / (AppData.lf * AppData.lu));
            }
            shF = Mathf.Atan2(endPt[1], endPt[0]);

            if (Mathf.Abs(uA[2] / AppData.lu) < 1)
            {
                shA = Mathf.Asin(uA[2] / AppData.lu);
            }
        
    }

    //To control the motor manually hold and release 
    static public void onclickHold()
    {
        AppLogger.LogInfo($"motor on hold");
        controlStatus = CONTROL_STATUS_CODE[0];
        thetades1 = MarsComm.angleOne;
        AppData.dataSendToRobot = new float[] { 0, thetades1, 0, controlStatus };
        AppData.sendToRobot(AppData.dataSendToRobot);

        Debug.Log("Hold enabled");
    }
    static public void onclickRealease()
    {
        AppLogger.LogInfo($"motor on released");
        controlStatus = CONTROL_STATUS_CODE[1];
        AppData.dataSendToRobot = AppData.dataSendToRobot = new float[] { 0.0f, 0.0f, 0.0f, controlStatus };
        AppData.sendToRobot(AppData.dataSendToRobot);

    }

}
