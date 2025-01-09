using UnityEngine;
using System.IO;
//using System.IO.Ports;
using System;
using System.ComponentModel;


public static class MarsComm 
{
 
    static public float[] currentSensorData;
    static public byte currentButtonState,previousButtonState;
    static int sensorDataLength = 10;

    // Button released event.
    public delegate void MarsButtonReleasedEvent();
    public static event MarsButtonReleasedEvent OnButtonReleased;

    //constant variables
    private const float len1 = 475.0f, len2 = 291.0f;
    static public float shF, shA, elF;

    static public float theta1, theta2, theta3, theta4;
    public static float[] shPos = new float[3];
    static public float[] endPt = new float[3];
    static public float[] zvec = new float[3];
    static public float[] elPt = new float[3];
    static public float[] fA = new float[3];
    static public float[] uA = new float[3];
    public static int[] CONTROL_STATUS_CODE = new int[] { 1001, 0 };//hold,release
    public static float SUPPORT;
    public static float[] SUPPORT_CODE = new float[] {0.0f,0.1f,0.5f};//NoSupport,FullWeightSupport,HalfweightSupport
    public static float[] DEPENDENT = new float[] { 0, -0.01745f, 0.01745f };//1-left,2-right

    public static int controlStatus;
    public static float thetades1;

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
    static public float forceTwo
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
    static public byte buttonState
    {
        get
        {
            return currentButtonState;
        }
    }
    static MarsComm()
    {
        currentSensorData = new float[sensorDataLength];
    }
    static public void parseRawBytes(byte[] rawBytes, uint payloadSize)
    {
        //Debug.Log(payloadSize);
        //Debug.Log(rawBytes.Length);
        if (payloadSize != 41 || rawBytes.Length < payloadSize)
        {
            //Debug.Log("working");
            return;
        }
        try
        {
            previousButtonState = currentButtonState;
            for (int i = 0; i < sensorDataLength; i++)
            {
                int byteIndex = i * 4;
                if (byteIndex + 3 < rawBytes.Length)
                {
                    currentSensorData[i] = BitConverter.ToSingle(rawBytes, byteIndex);
                }
                else
                {
                    Debug.LogError("Index out of bounds while parsing rawBytes.");
                    return;
                }
            }
            currentButtonState = rawBytes[payloadSize - 1];
            // Check if the button has been released.
            if (previousButtonState == 0 && currentButtonState == 1)
            {
                OnButtonReleased?.Invoke();
            }
            AppData.sendToRobot();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in parseRawBytes: {ex.Message}");
        }

    }
    static public void computeShouderPosition()
    {
           
            theta1 = DEPENDENT[AppData.useHand] * angleOne;
            theta2 = DEPENDENT[AppData.useHand] * angleTwo;
            theta3 = DEPENDENT[AppData.useHand] * angleThree;
            theta4 = DEPENDENT[AppData.useHand] * angleFour;

            zvec[0] = Mathf.Cos(theta1) * Mathf.Cos(theta2 + theta3 + theta4);
            zvec[1] = Mathf.Sin(theta1) * Mathf.Cos(theta2 + theta3 + theta4);
            zvec[2] = -Mathf.Sin(theta2 + theta3 + theta4);

            endPt[0] = Mathf.Cos(theta1) * (len1 * Mathf.Cos(theta2) + len2 * Mathf.Cos(theta2 + theta3));
            endPt[1] = Mathf.Sin(theta1) * (len1 * Mathf.Cos(theta2) + len2 * Mathf.Cos(theta2 + theta3));
            endPt[2] = -len1 * Mathf.Sin(theta2) - len2 * Mathf.Sin(theta2 + theta3);

            shPos = new float[] { endPt[0], endPt[1] + AppData.lu, endPt[2] - AppData.lf };
            elPt[0] = endPt[0] - AppData.lf * zvec[0];
            elPt[1] = endPt[1] - AppData.lf * zvec[1];
            elPt[2] = endPt[2] - AppData.lf * zvec[2];

            fA[0] = endPt[0] - elPt[0];
            fA[1] = endPt[1] - elPt[1];
            fA[2] = endPt[2] - elPt[2];

            uA[0] = elPt[0] - shPos[0];
            uA[1] = elPt[1] - shPos[1];
            uA[2] = elPt[2] - shPos[2];

            if (((fA[0] * uA[0] + fA[1] * uA[1] + fA[2] * uA[2]) / (AppData.lf*AppData.lu)) > 0.9999999f)
            {
                elF = 0;
            }
            else
            {
                elF = Mathf.Acos((fA[0] * uA[0] + fA[1] * uA[1] + fA[2] * uA[2]) / (AppData.lf * AppData.lu));
            }
            shF = Mathf.Atan2(endPt[1], endPt[0]);
            shA = Mathf.Asin(uA[2] / AppData.lu);

    }

    //To control the motor manually hold and release 
    static public void onclickHold()
    {
        AppLogger.LogInfo($"motor on hold");
        controlStatus = CONTROL_STATUS_CODE[0];
        thetades1 = MarsComm.angleOne;
        AppData.dataSendToRobot = new float[] { 0, thetades1, 0, controlStatus };

        Debug.Log("Hold enabled");
    }
    static public void onclickRealease()
    {
        AppLogger.LogInfo($"motor on released");
        controlStatus = CONTROL_STATUS_CODE[1];
        AppData.dataSendToRobot = AppData.dataSendToRobot = new float[] { 1.0f * AppData.useHand, 0.0f, 1998.0f, controlStatus };

    }

}
