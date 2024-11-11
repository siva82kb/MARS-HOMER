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

    static public float thetaOne
    {
        get
        {
            return currentSensorData[0];
        }
    }
    static public float thetatwo
    {
        get
        {
            return currentSensorData[1];
        }
    }
    static public float thetaThree
    {
        get
        {
            return currentSensorData[2];
        }
    }
    static public float thetafour
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
    static public float descOne
    {
        get
        {
            return currentSensorData[6];
        }
    }
     static public float descTwo
    {
        get
        {
            return currentSensorData[7];
        }
    }
    static public float descThree
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
        if (payloadSize != 41 || rawBytes.Length < payloadSize)
        {
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
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in parseRawBytes: {ex.Message}");
        }

    }

}
