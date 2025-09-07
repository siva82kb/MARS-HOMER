using UnityEngine;
using System.IO;
//using System.IO.Ports;
using System;
using System.ComponentModel;
using System.Text;
using System.Numerics;
using System.Data;
using System.Drawing.Drawing2D;


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
    public static readonly string[] OUTDATATYPE = new string[] { "VERSION", "SENSORSTREAM", "CONTROLPARAM", "DIAGNOSTICS" };
    public static readonly string[] LIMBTYPE = new string[] { "NOLIMB", "RIGHT", "LEFT" };
    public static readonly string[] LIMBTEXT = new string[] {
        "No Limb",
        "Right",
        "Left"
    };
    public static readonly string[] CALIBRATION = new string[] { "NOCALIB", "YESCALIB" };
    public static readonly string[] COMMAND_STATUS = new string[] { "NONE", "SUCCESS", "FAIL" };
    public static readonly string[] CONTROLTYPE = new string[] { "NONE", "POSITION" };
    public static readonly string[] CONTROLTYPETEXT = new string[] {
        "None",
        "Position"
    };
    public static readonly int[] SENSORNUMBER = new int[] {
        0,   // Dummy
        11,  // SENSORSTREAM 
        0,   // CONTROLPARAM
        16,  // DIAGNOSTICS
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
    static int sensorDataLength;

    // Button released event.
    public delegate void MarsButtonReleasedEvent();
    public static event MarsButtonReleasedEvent OnMarsButtonReleased;
    public delegate void CalibButtonReleasedEvent();
    public static event CalibButtonReleasedEvent OnCalibButtonReleased;
    // New data event.
    public delegate void MarsNewDataEvent();
    public static event MarsNewDataEvent OnNewMarsData;
    // Control change event.
    public delegate void MarsControlModeChangeEvent();
    public static event MarsControlModeChangeEvent OnControlModeChange;
    
    // MARS Robot Parameters
    private const float L1 = 475.0f;
    private const float L2 = 291.0f;
    // static public float shF, shA, elF;

    // static public float theta1, theta2, theta3, theta4;
    // public static float[] shPos = new float[3];
    // static public float[] endPt = new float[3];
    // static public float[] zvec = new float[3];
    // static public float[] elPt = new float[3];
    // static public float[] fA = new float[3];
    // static public float[] uA = new float[3];
    // public static int[] CONTROL_STATUS_CODE = new int[] { 1001,  //hold
    //                                                          0   //release
    //                                                     };
    // public static float SUPPORT;
    // public static float[] SUPPORT_CODE = new float[] {
    //                                                      0.0f,//NoSupport
    //                                                      1.0f,//FullWeightSupport
    //                                                      0.5f //HalfweightSupport
    //                                                   };
    // public static float[] OFFSET = new float[] { 0,
    //                                             -0.01745f,//LEFT-HAND 
    //                                              0.01745f //RIGHT-HAND
    //                                             };

    // public static int controlStatus;
    // public static float thetades1;

    // Private variables
    static private byte[] rawBytes = new byte[256];
    // For the following arrays, the first element represents the number of elements in the array.
    static private int[] previousStateData = new int[16];
    static private int[] currentStateData = new int[16];
    static private float[] currentSensorData = new float[32];

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
        get => status >> 4;

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
    static public int limb
    {
        get => currentStateData[3] & 0x03;
    }
    static public int marButton
    {
        get => (currentStateData[3] >> 4) & 0x01;
    }
    static public int calibButton
    {
        get => (currentStateData[3] >> 5) & 0x01;
    }
    static public int recentCommandStatus
    {
        get => currentStateData[3] >> 6;
    }
    static public float angle1
    {
        get => currentSensorData[1];
    }
    static public float angularVelocity1
    {
        get => currentSensorData[19];
    }
    static public float angle2
    {
        get => currentSensorData[2];
    }
    static public float angle3
    {
        get => currentSensorData[3];
    }
    static public float angle4
    {
        get => currentSensorData[4];
    }
    static public float force
    {
        get => currentSensorData[5];
    }
    static public float torque
    {
        get => force * Mathf.Sqrt(Mathf.Pow(xEndpoint, 2) + Mathf.Pow(yEndpoint, 2));
    }
    static public float xEndpoint
    {
        get => currentSensorData[6];
    }
    static public float yEndpoint
    {
        get => currentSensorData[7];
    }
    static public float zEndpoint
    {
        get => currentSensorData[8];
    }
    static public float target
    {
        get => currentSensorData[9];
    }
    static public float desired
    {
        get => currentSensorData[10];
    }
    static public float control
    {
        get => currentSensorData[11];
    }
    static public float errP
    {
        get => currentSensorData[15];
    }
    static public float errD
    {
        get => currentSensorData[16];
    }
    static public float errI
    {
        get => currentSensorData[17];
    }
    static public float gravityCompensationTorque
    {
        get => currentSensorData[18];
    }
    static public short imu1Angle
    {
        get => (sbyte)currentStateData[4];
    }
    static public short imu2Angle
    {
        get => (sbyte)currentStateData[5];
    }
    static public short imu3Angle
    {
        get => (sbyte)currentStateData[6];
    }
    static public short imu4Angle
    {
        get => (sbyte)currentStateData[7];
    }
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
    
    public static void parseByteArray(byte[] payloadBytes, int payloadCount, DateTime payloadTime)
    {
        int offset;
        int nSensors;
        int i;
        if (payloadCount == 0)
        {
            return;
        }
        Array.Copy(currentStateData, previousStateData, currentStateData.Length);
        rawBytes = payloadBytes;
        previousTime = currentTime;
        currentTime = payloadTime;
        prevRunTime = runTime;

        // Updat current state data
        // Status
        currentStateData[1] = rawBytes[1];
        // Error
        prevErrorStatus = errorStatus;
        currentStateData[2] = 255 * rawBytes[3] + rawBytes[2];
        // Check if the error is not 0.
        if (errorStatus != 0)
        {
            // Print when error changes. If error is the same, then flip a coin to decide if we print or not.
            // This is to avoid flooding the log with the same error message. 
            // if (prevErrorStatus != errorStatus || GetRandomNumber() <= 5) MarsCommLogger.LogError($"Error: {errorString} ({errorStatus}) | Time: {runTime:F2}");
        }
        else
        {
            // Print if the error is resolved.
            // if (prevErrorStatus != errorStatus) MarsCommLogger.LogInfo($"Error Resolved: {errorString} | Previous Error: {getErrorString(prevErrorStatus)}({prevErrorStatus}) | Time: {runTime:F2}");
        }
        // Limb type
        currentStateData[3] = rawBytes[4];

        // Handle data based on what type of data it is.
        byte _datatype = (byte)(currentStateData[1] >> 4);
        // Debug.Log($"Data Type: {_datatype} {OUTDATATYPE[_datatype]}");
        switch (OUTDATATYPE[_datatype])
        {
            case "SENSORSTREAM":
            case "DIAGNOSTICS":
                // Get the packet number.
                packetNumber = BitConverter.ToUInt16(new byte[] { rawBytes[5], rawBytes[6] });

                // Get the runtime.
                runTime = 0.001f * BitConverter.ToUInt32(new byte[] { rawBytes[7], rawBytes[8], rawBytes[9], rawBytes[10] });

                // Update current sensor data
                offset = 10;
                nSensors = SENSORNUMBER[_datatype];
                currentSensorData[0] = nSensors;
                for (i = 0; i < nSensors; i++)
                {
                    currentSensorData[i + 1] = BitConverter.ToSingle(
                        new byte[] {
                            rawBytes[offset + 1 + (i * 4)],
                            rawBytes[offset + 2 + (i * 4)],
                            rawBytes[offset + 3 + (i * 4)],
                            rawBytes[offset + 4 + (i * 4)] },
                        0
                    );
                }

                // IMU angles
                currentStateData[4] = rawBytes[offset + nSensors * 4 + 1];
                currentStateData[5] = rawBytes[offset + nSensors * 4 + 2];
                currentStateData[6] = rawBytes[offset + nSensors * 4 + 3];
                currentStateData[7] = rawBytes[offset + nSensors * 4 + 4];

                // Number of current state data
                currentStateData[0] = 3;

                // Updat framerate
                frameRate = 1 / (runTime - prevRunTime);

                // Check if the MARS button has been released.
                if ((((previousStateData[3] >> 4) & 0x01) == 0) && (((currentStateData[3] >> 4) & 0x01) == 1))
                {
                    MarsCommLogger.LogInfo($"MARS Button Released | Button: {currentStateData[6]} | Time: {runTime:F2}");
                    OnMarsButtonReleased?.Invoke();
                }

                // Check if the Calib button has been released.
                if ((((previousStateData[3] >> 5) & 0x01) == 0) && (((currentStateData[3] >> 5) & 0x01) == 1))
                {
                    MarsCommLogger.LogInfo($"Calibration Button Released | Button: {currentStateData[6]} | Time: {runTime:F2}");
                    OnCalibButtonReleased?.Invoke();
                }

                // Check if the control mode has been changed.
                if (getControlType(previousStateData[1]) != getControlType(currentStateData[1]))
                {
                    MarsCommLogger.LogInfo($"Control Mode Changed | ControlType: {getControlType(currentStateData[1])} | Time: {runTime:F2}");
                    OnControlModeChange?.Invoke();
                }

                // Invoke the new data event only for SENSORSTREAM or DIAGNOSTICS data.
                OnNewMarsData?.Invoke();
                break;
            case "VERSION":
                // Read the bytes into a string.
                deviceId = Encoding.ASCII.GetString(rawBytes, 5, rawBytes[0] - 4 - 1).Split(",")[0];
                version = Encoding.ASCII.GetString(rawBytes, 5, rawBytes[0] - 4 - 1).Split(",")[1];
                compileDate = Encoding.ASCII.GetString(rawBytes, 5, rawBytes[0] - 4 - 1).Split(",")[2];
                MarsCommLogger.LogInfo($"Received Version | Version: {version} | Compile Date: {compileDate} | Device ID: {deviceId}");
                break;
        }
    }
   
    
    public static void startSensorStream()
    {
        MarsCommLogger.LogInfo("Starting Sensor Stream");
        JediComm.SendMessage(new byte[] { (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "START_STREAM")] });
    }

    public static void stopSensorStream()
    {
        MarsCommLogger.LogInfo("Stopping Sensor Stream");
        JediComm.SendMessage(new byte[] { (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "STOP_STREAM")] });
    }

    public static void setDiagnosticMode()
    {
        MarsCommLogger.LogInfo("Setting Diagnostic Mode");
        JediComm.SendMessage(new byte[] { (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "SET_DIAGNOSTICS")] });
    }

    public static void setLimb(string limb)
    {
        MarsCommLogger.LogInfo($"Setting Limb: {limb}");
        JediComm.SendMessage(
            new byte[] {
                (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "SET_LIMB")],
                (byte)Array.IndexOf(LIMBTYPE, limb)
            }
        );
    }

    public static void setControlType(string controlType)
    {
        MarsCommLogger.LogInfo($"Setting Control Type: {controlType}");
        Debug.Log("Setting Control Type: " + controlType);
        JediComm.SendMessage(
            new byte[] {
                (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "SET_CONTROL_TYPE")],
                (byte)Array.IndexOf(CONTROLTYPE, controlType)
            }
        );
    }

    public static void setControlTarget(float tgt, float dur = 6.0f)
    {
        if (CONTROLTYPE[controlType] == "NONE") return;
        MarsCommLogger.LogInfo($"Setting Control Target: {target}");
        // Set the start position.
        byte[] tgt0Bytes;
        if (CONTROLTYPE[controlType] == "POSITION")
        {
            tgt0Bytes = (target == INVALID_TARGET) ? BitConverter.GetBytes(angle1) : BitConverter.GetBytes(target);
        }
        else if (CONTROLTYPE[controlType] == "TORQUE")
        {
            tgt0Bytes = (target == INVALID_TARGET) ? BitConverter.GetBytes(torque) : BitConverter.GetBytes(target);
        }
        else
        {
            tgt0Bytes = (target == INVALID_TARGET) ? BitConverter.GetBytes(1.0) : BitConverter.GetBytes(target);
        }
        byte[] t0Bytes = BitConverter.GetBytes(0.0f);
        byte[] tgt1Bytes = BitConverter.GetBytes(tgt);
        byte[] durBytes = BitConverter.GetBytes(dur);
        JediComm.SendMessage(
            new byte[] {
                (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "SET_CONTROL_TARGET")],
                tgt0Bytes[0],   tgt0Bytes[1],   tgt0Bytes[2],   tgt0Bytes[3],
                t0Bytes[0],     t0Bytes[1],     t0Bytes[2],     t0Bytes[3],
                tgt1Bytes[0],   tgt1Bytes[1],   tgt1Bytes[2],   tgt1Bytes[3],
                durBytes[0],    durBytes[1],    durBytes[2],    durBytes[3]
            }
        );
    }

    public static void calibrate()
    {
        MarsCommLogger.LogInfo("Calibrating");
        JediComm.SendMessage(new byte[] { (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "CALIBRATE")] });
    }

    public static void getVersion()
    {
        MarsCommLogger.LogInfo("Getting Version");
        JediComm.SendMessage(new byte[] { (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "GET_VERSION")] });
    }

    public static void resetPacketNo()
    {
        MarsCommLogger.LogInfo($"Resetting Packet Number");
        JediComm.SendMessage(new byte[] { (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "RESET_PACKETNO")] });
    }

    public static void sendHeartbeat()
    {
        JediComm.SendMessage(new byte[] { (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "HEARTBEAT")] });
    }

    // //To control the motor manually hold and release
    // static public void onclickHold()
    // {
    //     AppLogger.LogInfo($"motor on hold");
    //     controlStatus = CONTROL_STATUS_CODE[0];
    //     thetades1 = MarsComm.angle1;
    //     AppData.dataSendToRobot = new float[] { 0, thetades1, 0, controlStatus };
    //     // AppData.sendToRobot(AppData.dataSendToRobot);

    //     Debug.Log("Hold enabled");
    // }
    // static public void onclickRealease()
    // {
    //     AppLogger.LogInfo($"motor on released");
    //     controlStatus = CONTROL_STATUS_CODE[1];
    //     AppData.dataSendToRobot = AppData.dataSendToRobot = new float[] { 0.0f, 0.0f, 0.0f, controlStatus };
    //     // AppData.sendToRobot(AppData.dataSendToRobot);

    // }
}

public static class MarsKinDynamics
{

    public static float l1 = 0.475f; // Length of upper arm in m
    public static float l2 = 0.291f; // Length of forearm in m
    public static float l3 = 0.075f; // Length of hand in m
    public static float l4 = 0.0438f; // Length of wrist in m

    public static UnityEngine.Vector3 ForwardKinematics(float theta1, float theta2, float theta3)
    {
        float x, y, z;
        // Change the angles to radians
        theta1 = theta1 * Mathf.Deg2Rad;
        theta2 = theta2 * Mathf.Deg2Rad;
        theta3 = theta3 * Mathf.Deg2Rad;
        float _temp = l1 * Mathf.Cos(theta2) + l2 * Mathf.Cos(theta2 + theta3);
        x = Mathf.Cos(theta1) * _temp;
        y = Mathf.Sin(theta1) * _temp;
        z = -l1 * Mathf.Sin(theta2) - l2 * Mathf.Sin(theta2 + theta3);
        return new UnityEngine.Vector3(x, y, z);
    }

    public static UnityEngine.Vector3 ForwardKinematicsExtended(float theta1, float theta2, float theta3, float theta4)
    {
        float x, y, z;
        // Change the angles to radians
        theta1 = theta1 * Mathf.Deg2Rad;
        theta2 = theta2 * Mathf.Deg2Rad;
        theta3 = theta3 * Mathf.Deg2Rad;
        theta4 = theta4 * Mathf.Deg2Rad;
        float _temp = l1 * Mathf.Cos(theta2)
                      + l2 * Mathf.Cos(theta2 + theta3)
                      + l3 * Mathf.Cos(theta2 + theta3 + theta4)
                      - l4 * Mathf.Sin(theta2 + theta3 + theta4);
        x = Mathf.Cos(theta1) * _temp;
        y = Mathf.Sin(theta1) * _temp;
        z = -l1 * Mathf.Sin(theta2)
            - l2 * Mathf.Sin(theta2 + theta3)
            - l3 * Mathf.Sin(theta2 + theta3 + theta4)
            - l4 * Mathf.Cos(theta2 + theta3 + theta4);
        return new UnityEngine.Vector3(x, y, z);
    }
}

public class RecursiveLeastSquares
{
    public float[] theta { get; private set; }
    public float[,] P { get; private set; }
    public float lambda { get; private set; } = 1.0f;
    public float[] K { get; private set; }
    public int i { get; private set; } = 0;
    private int N;

    public RecursiveLeastSquares(int N)
    {
        this.N = N;
        theta = new float[N]; // Initialize theta to zero
        P = new float[N, N];
        K = new float[N];
        ResetEstimator();
    }

    public void ResetEstimator()
    {
        i = 0;
        // Initialize P to identity matrix
        for (int row = 0; row < N; row++)
        {
            for (int col = 0; col < N; col++)
            {
                P[row, col] = (row == col) ? 1.0f : 0.0f; // Identity matrix
            }
            theta[row] = 0.0f;
        }
    }

    public void Update(float[] x, float y)
    {
        // Compute the Kalman gain
        updateKalmanGain(x);

        // Compute the prediction error
        float error = y - InnerProduct(theta, x);
        // Update the parameter estimates
        for (int row = 0; row < N; row++)
        {
            theta[row] += K[row] * error;
        }

        // Update the covariance matrix P
        float[,] PAdjust = MatrixMultiply(OuterProduct(K, x), P);
        for (int row = 0; row < N; row++)
        {
            for (int col = 0; col < N; col++)
            {
                P[row, col] -= PAdjust[row, col] / lambda;
            }
        }

        // Update the covariance matrix P with the new values
        i += 1;
    }
    // public void Update(float[] x, float y)
    // {
    //     // Compute the Kalman gain
    //     updateKalmanGain(x);

    //     // Compute the prediction error
    //     float error = y;
    //     for (int row = 0; row < N; row++)
    //     {
    //         error -= theta[row] * x[row];
    //     }

    //     // Update the parameter estimates
    //     for (int row = 0; row < N; row++)
    //     {
    //         theta[row] += K[row] * error;
    //     }

    //     // Update the covariance matrix P
    //     // Computer
    //     for (int row = 0; row < N; row++)
    //     {
    //         for (int col = 0; col < N; col++)
    //         {
    //             P[row, col] -= K[row] * x[col] * P[row, col] / lambda;
    //         }
    //     }

    //     // Update the covariance matrix P with the new values
    //     i += 1;
    // }

    private void updateKalmanGain(float[] x)
    {
        K = MatrixVectorMultiply(P, x);
        float denom = 1 + InnerProduct(x, K);
        // Update the Kalman gain
        for (int row = 0; row < N; row++)
        {
            K[row] /= denom;
        }
    }

    private static float InnerProduct(float[] a, float[] b)
    {
        float sum = 0.0f;
        for (int i = 0; i < a.Length; i++)
        {
            sum += a[i] * b[i];
        }
        return sum;
    }

    private static float[,] OuterProduct(float[] a, float[] b)
    {
        float[,] result = new float[a.Length, b.Length];
        for (int i = 0; i < a.Length; i++)
        {
            for (int j = 0; j < b.Length; j++)
            {
                result[i, j] = a[i] * b[j];
            }
        }
        return result;
    }

    private static float[,] MatrixMultiply(float[,] A, float[,] B)
    {
        int rowsA = A.GetLength(0);
        int colsA = A.GetLength(1);
        int rowsB = B.GetLength(0);
        int colsB = B.GetLength(1);

        if (colsA != rowsB)
            throw new InvalidOperationException("Matrix dimensions do not match for multiplication.");

        float[,] result = new float[rowsA, colsB];
        for (int i = 0; i < rowsA; i++)
        {
            for (int j = 0; j < colsB; j++)
            {
                result[i, j] = 0.0f;
                for (int k = 0; k < colsA; k++)
                {
                    result[i, j] += A[i, k] * B[k, j];
                }
            }
        }
        return result;
    }

    // Post-multiply a matrix with a vector
    private static float[] MatrixVectorMultiply(float[,] A, float[] b)
    {
        int rowsA = A.GetLength(0);
        int colsA = A.GetLength(1);

        if (colsA != b.Length)
            throw new InvalidOperationException("Incompatible matrix and vector dimensions.");

        float[] result = new float[rowsA];
        for (int i = 0; i < rowsA; i++)
        {
            result[i] = 0.0f;
            for (int j = 0; j < colsA; j++)
            {
                result[i] += A[i, j] * b[j];
            }
        }
        return result;
    }

    // Pre-multiply a vector with a matrix
    private static float[] MatrixVectorMultiply(float[] b, float[,] A)
    {
        int rowsA = A.GetLength(0);
        int colsA = A.GetLength(1);

        if (rowsA != b.Length)
            throw new InvalidOperationException("Incompatible matrix and vector dimensions.");

        float[] result = new float[colsA];
        for (int i = 0; i < colsA; i++)
        {
            result[i] = 0.0f;
            for (int j = 0; j < rowsA; j++)
            {
                result[i] += A[j, i] * b[j];
            }
        }
        return result;
    }
}

public static class MarsCommLogger
{
    private static string logFilePath;
    private static StreamWriter logWriter = null;
    private static readonly object logLock = new object();

    public static bool DEBUG = false;
    public static string InBraces(string text) => $"[{text}]";

    public static bool isLogging
    {
        get
        {
            return logFilePath != null;
        }
    }

    public static void StartLogging(string dtstr)
    {
        // Start Log file only if we are not already logging.
        if (isLogging) return;
        if (!Directory.Exists(DataManager.logPath)) Directory.CreateDirectory(DataManager.logPath);
        // Create the log file name.
        logFilePath = Path.Combine(DataManager.logPath, $"{dtstr}-marscomm.log");

        // Create the log file writer.
        logWriter = new StreamWriter(logFilePath, true);
        LogInfo("Created MARS log file.");
    }

    public static void StopLogging()
    {
        if (logWriter != null)
        {
            LogInfo("Closing log file.");
            logWriter.Close();
            logWriter = null;
            logFilePath = null;
        }
    }

    public static void LogMessage(string message, LogMessageType logMsgType)
    {
        lock (logLock)
        {
            if (logWriter != null)
            {
                string _user = AppData.Instance.userData != null ? AppData.Instance.userData.hospNumber : "";
                string _msg = $"{DateTime.Now:dd-MM-yyyy HH:mm:ss} {logMsgType,-7} {InBraces(_user),-10} {InBraces(AppLogger.currentScene),-12} {InBraces(AppLogger.currentMechanism),-8} {InBraces(AppLogger.currentGame),-8} >> {message}";
                logWriter.WriteLine(_msg);
                logWriter.Flush();
                if (DEBUG) Debug.Log(_msg);
            }
        }
    }

    public static void LogInfo(string message)
    {
        LogMessage(message, LogMessageType.INFO);
    }

    public static void LogWarning(string message)
    {
        LogMessage(message, LogMessageType.WARNING);
    }

    public static void LogError(string message)
    {
        LogMessage(message, LogMessageType.ERROR);
    }
}

