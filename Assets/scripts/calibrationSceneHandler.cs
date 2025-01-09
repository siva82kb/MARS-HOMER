using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class calibrationSceneHandler : MonoBehaviour
{
    //ui related variables
    public TMP_Text messageBox;
    public readonly string nextScene = "chooseMovementScene";
    public GameObject panel;
    Image panelImage;
    
    //flags   
    public int calibrationState;
    public int CALIBRATION_FAILD = 1;
    public int CALIBRATION_SUCCESS = -1;
    public static bool changeScene = false;
    public bool calibrating;
    const int SEND_UPPERARM_LENGTH = 1998;
    const int SEND_FOREARM_LENGTH = 1999;
    const int SEND_SHOLDER_POSITION_X = 2000;
    const int SEND_SHOLDER_POSITION_Y = 2001;
    const int SEND_SHOLDER_POSITION_Z = 2002;
    const int SENT_SUCCESSFULLY = 2003;
    void Start()
    {
        //AppData.InitializeRobot();
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        MarsComm.OnButtonReleased += onMarsButtonReleased;

    }

    void Update()
    {
        if (calibrating)
        {
            messageBox.text = "calibrating....";
            calibrating = false;
        }


        //if (calibrationState > 0)
        //{
        //    if (MarsComm.desThree > 1997 &&  MarsComm.desThree < 1999)
        //    {
        //        AppData.dataSendToRobot = new float[] { AppData.lu / 1000.0f,MarsComm.thetades1, 1999, MarsComm.controlStatus };
        //        Debug.Log("Sending arm length lu");
        //    }
        //    else if (MarsComm.desThree > 1998 && MarsComm.desThree < 2000)
        //    {
        //        AppData.dataSendToRobot = new float[] { AppData.lf / 1000.0f, MarsComm.thetades1, 2000, MarsComm. controlStatus };
        //        Debug.Log("Sending arm length lf");
        //    }
        //    else if (MarsComm.desThree > 1999 && MarsComm.desThree < 2001)
        //    {
        //        AppData.dataSendToRobot = new float[] { MarsComm.shPos[0] / 1000.0f, MarsComm.thetades1, 2001,MarsComm. controlStatus };
        //        Debug.Log("Sending shx");
        //    }
        //    else if (MarsComm.desThree > 2000 && MarsComm.desThree < 2002)
        //    {
        //        AppData.dataSendToRobot = new float[] { MarsComm.shPos[1] / 1000.0f, MarsComm.thetades1, 2002,MarsComm. controlStatus };
        //        Debug.Log("Sending shy");
        //    }
        //    else if (MarsComm.desThree > 2001 && MarsComm.desThree < 2003)
        //    {
        //        AppData.dataSendToRobot = new float[] { MarsComm.shPos[2] / 1000.0f, MarsComm.thetades1, 2003, MarsComm.controlStatus };
        //        Debug.Log("Sending shz");
        //    }
        //    else if (MarsComm.desThree > 2002 && MarsComm.desThree < 2004)
        //    {
        //        //Debug.Log(Mathf.Abs(MarsComm.shPos[0])+";"+Mathf.Abs(MarsComm.shPos[1]));


        //        if (Mathf.Abs(MarsComm.shPos[0]) > 50 || Mathf.Abs(MarsComm.shPos[1]) > 50)
        //        {
        //            panel.GetComponent<Image>().color = new Color32(255, 0, 0, 100);
        //            messageBox.text = "Align Shoulder Joint properly\\nShoulder Position:   x - " + MarsComm.shPos[0] + "  , y - " + MarsComm.shPos[1] + "  , z - " + MarsComm.shPos[2];
        //            calibrationState = CALIBRATION_FAILD;
        //            //Debug.Log("calibrat failed");
        //        }
        //        else
        //        {

        //            panel.GetComponent<Image>().color = new Color32(0, 255, 0, 100);
        //            messageBox.text = "Kinematic Calibration completed\\nShoulder Position: x- " + MarsComm.shPos[0] + "  , y - " + MarsComm.shPos[1] + "  , z- " + MarsComm.shPos[2]+"\\nCalibration Completed - Press Mars Button To Select Movements";
        //            calibrationState = CALIBRATION_SUCCESS;
        //            //Debug.Log("success");
        //        }

        //    }
        //}
        if (calibrationState > 0)
        {
            if (MarsComm.desThree == SEND_UPPERARM_LENGTH)
            {
                AppData.dataSendToRobot = new float[] { AppData.lu / 1000.0f, MarsComm.thetades1, SEND_FOREARM_LENGTH, MarsComm.controlStatus };
                Debug.Log("Sending arm length lu");
            }
            else if (MarsComm.desThree == SEND_FOREARM_LENGTH)
            {
                AppData.dataSendToRobot = new float[] { AppData.lf / 1000.0f, MarsComm.thetades1, SEND_SHOLDER_POSITION_X, MarsComm.controlStatus };
                Debug.Log("Sending arm length lf");
            }
            else if (MarsComm.desThree == SEND_SHOLDER_POSITION_X)
            {
                AppData.dataSendToRobot = new float[] { MarsComm.shPos[0] / 1000.0f, MarsComm.thetades1, SEND_SHOLDER_POSITION_Y, MarsComm.controlStatus };
                Debug.Log("Sending shx");
            }
            else if (MarsComm.desThree == SEND_SHOLDER_POSITION_Y)
            {
                AppData.dataSendToRobot = new float[] { MarsComm.shPos[1] / 1000.0f, MarsComm.thetades1, SEND_SHOLDER_POSITION_Z, MarsComm.controlStatus };
                Debug.Log("Sending shy");
            }
            else if (MarsComm.desThree == SEND_SHOLDER_POSITION_Z)
            {
                AppData.dataSendToRobot = new float[] { MarsComm.shPos[2] / 1000.0f, MarsComm.thetades1, SENT_SUCCESSFULLY, MarsComm.controlStatus };
                Debug.Log("Sending shz");
            }
            else if (MarsComm.desThree == SENT_SUCCESSFULLY)
            {
                //Debug.Log(Mathf.Abs(MarsComm.shPos[0])+";"+Mathf.Abs(MarsComm.shPos[1]));


                if (Mathf.Abs(MarsComm.shPos[0]) > 50 || Mathf.Abs(MarsComm.shPos[1]) > 50)
                {
                    panel.GetComponent<Image>().color = new Color32(255, 0, 0, 100);
                    messageBox.text = "Align Shoulder Joint properly\\nShoulder Position:   x - " + MarsComm.shPos[0] + "  , y - " + MarsComm.shPos[1] + "  , z - " + MarsComm.shPos[2];
                    calibrationState = CALIBRATION_FAILD;
                    //Debug.Log("calibrat failed");
                }
                else
                {

                    panel.GetComponent<Image>().color = new Color32(0, 255, 0, 100);
                    messageBox.text = "Kinematic Calibration completed\\nShoulder Position: x- " + MarsComm.shPos[0] + "  , y - " + MarsComm.shPos[1] + "  , z- " + MarsComm.shPos[2] + "\\nCalibration Completed - Press Mars Button To Select Movements";
                    calibrationState = CALIBRATION_SUCCESS;
                    //Debug.Log("success");
                }

            }
        }

        if (changeScene == true)
        {
            LoadTargetScene();
            changeScene = false;
        }

    }
  
    public void onMarsButtonReleased()
    {
        //Log the data to Applogger
        if (calibrationState == CALIBRATION_FAILD)
        {
            AppLogger.LogInfo("Align Shoulder Joint properly, Shoulder Position:   " + MarsComm.shPos[0] + "  ,  " + MarsComm.shPos[1] + "  ,  " + MarsComm.shPos[2]);
           
        }
       
        // Check if it time to switch to the next scene
        if (calibrationState == CALIBRATION_SUCCESS)
        {
           
            AppLogger.LogInfo("Kinematic Calibration completed, Shoulder Position:   " + MarsComm.shPos[0] + "  ,  " + MarsComm.shPos[1] + "  ,  " + MarsComm.shPos[2]);
            AppLogger.LogInfo("Mars button released");
            changeScene = true;
            
        }
        else
        {
            AppLogger.LogInfo("Mars button released");
            MarsComm.computeShouderPosition();
            AppLogger.LogInfo("calculating the shoulder posistion");
            calibrating = true;
            calibrationState++;
        }
      
    }
  

    private void LoadTargetScene()
    {
        AppLogger.LogInfo($"Switching to the next scene '{nextScene}'.");
        SceneManager.LoadScene(nextScene);
    }

    //To control the motor manually hold and release 
    public void onClickHold()
    {
        MarsComm.onclickHold();
    }
    public void onClickRelease()
    {
        MarsComm.onclickRealease();
    }


    private void OnDestroy()
    {
        MarsComm.OnButtonReleased -= onMarsButtonReleased;
    }
    private void OnApplicationQuit()
    {

        Application.Quit();
        JediComm.Disconnect();
    }
}
