using UnityEngine;
using System.Collections;
using System;
using System.IO;
using static AppData;
using System.Data;



public class PongPlayerController : MonoBehaviour
{
    public static PongPlayerController instance;
	//speed of player
	public float speed = 10;
   // float max_y;
   // float min_y;
   // double y_c;
    //float[] positions_moved_z;
    //bounds of player
    static float topBound = 3.6F;
	static float bottomBound = -3.6F;
    // player controls
    float theta1, theta2, theta3,theta4;
    public static float playSize;
    public static float[] rom;
    // private float[] angZRange = { -40, -120 };
    //  public static int FlipAngle = 1;
    //public static float tempRobot, tempBird;
    public int[] DEPENDENT = new int[] { 0, -1, 1 };
    public static int reps;
    int hand_use_pong;
    private string hospno;
    public int PongGameCount;

    public GameObject player;
    public float y_max, y_min;
    public float time;

    void Start () {
        playSize = topBound - bottomBound;
        PongGameCount++;
        getAssessmentData();
    }

    // Update is called once per frame
    void Update ()
    {
        
        Game();
        time += Time.deltaTime;
        //recordData();
       
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Target")
        {
            //AppData.reps += 1;
            //Debug.Log(AppData.reps);
            
        }
    }
    private void OntriggerEnter2D(Collision2D collision)
    {
        Debug.Log("hello");
    }
    public void getAssessmentData()
    {
        UserData.dTableAssessment = DataManager.loadCSV(DataManager.filePathAssessmentData);
        DataRow lastRow = UserData.dTableAssessment.Rows[UserData.dTableAssessment.Rows.Count - 1];
        y_max = float.Parse((lastRow.Field<string>(AppData.miny)));
        y_min = float.Parse((lastRow.Field<string>(AppData.maxy)));
        //Debug.Log(zMaxMars + "," + zMaxMars + "," + yMaxMars + "," + yMinMars);
       
    }
    public void Game()
    {

   
        Debug.Log(y_max);
        Debug.Log(y_min);


        if (Mathf.Abs(MarsComm.angleOne) > (Mathf.Abs(MovementSceneHandler.initialAngle) - 20))
        {
            //    if (AppData.useHand == 2)
            //    {

            //        this.transform.position = new Vector2(this.transform.position.x, -Angle2ScreenZ((Mathf.Sin(3.14f / 180 * MarsComm.angleOne) * (475 * Mathf.Cos(3.14f / 180 * MarsComm.angleTwo) + 291 * Mathf.Cos(3.14f / 180 * MarsComm.angleTwo + 3.14f / 180 * MarsComm.angleThree))), y_min, y_max));
            //        Debug.Log(Angle2ScreenZ((Mathf.Sin(3.14f / 180 * MarsComm.angleOne) * (475 * Mathf.Cos(3.14f / 180 * MarsComm.angleTwo) + 291 * Mathf.Cos(3.14f / 180 * MarsComm.angleTwo + 3.14f / 180 * MarsComm.angleThree))), y_min, y_max));
            //        if (transform.position.y > topBound)
            //        {
            //            transform.position = new Vector3(transform.position.x, topBound, 0);
            //        }
            //        else if (transform.position.y < bottomBound)
            //        {
            //            transform.position = new Vector3(transform.position.x, bottomBound, 0);
            //        }
            //    }
            //    else if (AppData.useHand == 1)
            //    {
            //        theta1 = -MarsComm.angleOne;
            //        theta2 = -MarsComm.angleTwo;
            //        theta3 = -MarsComm.angleThree;
            //        theta4 = -MarsComm.angleFour;
            //        this.transform.position = new Vector2(this.transform.position.x, -Angle2ScreenZ((Mathf.Sin(3.14f / 180 * theta1) * (475 * Mathf.Cos(3.14f / 180 * theta2) + 291 * Mathf.Cos(3.14f / 180 * theta2 + 3.14f / 180 * theta3))), y_min, y_max));
            //        Debug.Log(Angle2ScreenZ((Mathf.Sin(3.14f / 180 * theta1) * (475 * Mathf.Cos(3.14f / 180 * theta2) + 291 * Mathf.Cos(3.14f / 180 * theta2 + 3.14f / 180 * theta3))), y_min, y_max));
            //        if (transform.position.y > topBound)
            //        {
            //            transform.position = new Vector3(transform.position.x, topBound, 0);
            //        }
            //        else if (transform.position.y < bottomBound)
            //        {
            //            transform.position = new Vector3(transform.position.x, bottomBound, 0);
            //        }
            //    }
            theta1 = DEPENDENT[AppData.useHand]*MarsComm.angleOne;
            theta2 = DEPENDENT[AppData.useHand] * MarsComm.angleTwo;
            theta3 = DEPENDENT[AppData.useHand] * MarsComm.angleThree;
            theta4 = DEPENDENT[AppData.useHand] * MarsComm.angleFour;
            this.transform.position = new Vector2(this.transform.position.x, -Angle2ScreenZ((Mathf.Sin(3.14f / 180 * theta1) * (475 * Mathf.Cos(3.14f / 180 * theta2) + 291 * Mathf.Cos(3.14f / 180 * theta2 + 3.14f / 180 * theta3))), y_min, y_max));
            Debug.Log(Angle2ScreenZ((Mathf.Sin(3.14f / 180 * theta1) * (475 * Mathf.Cos(3.14f / 180 * theta2) + 291 * Mathf.Cos(3.14f / 180 * theta2 + 3.14f / 180 * theta3))), y_min, y_max));
            if (transform.position.y > topBound)
            {
                transform.position = new Vector3(transform.position.x, topBound, 0);
            }
            else if (transform.position.y < bottomBound)
            {
                transform.position = new Vector3(transform.position.x, bottomBound, 0);
            }
            //player.GetComponent<SpriteRenderer>().color = new Color32(0, 255, 0, 255);
        }
        else
        {
            //player.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 0, 255);
        }
        //PongData();


    }


    public static float Angle2ScreenZ(float angleZ, float y_Min, float y_Max)
    {
        
        return Mathf.Clamp(bottomBound + (angleZ - y_Min) * ((playSize) / (y_Max - y_Min)), -3.6f * playSize, 3.6f * playSize);
    }

    //public void recordData()
    //{
    //    string path_to_data = Welcome.Path_to_data + "/" + "GameData" +"/" + Welcome.currentDate + "/" + "Ping Pong";

    //    //Verify if folder exits
    //    if (!Directory.Exists(path_to_data))
    //    {
    //        Directory.CreateDirectory(path_to_data);
    //    }

    //    string filePath = path_to_data + "/" + PongGameCount.ToString() + "_Pong.csv";
    //    //Debug.Log("calibPath =   " + filePath_Shoulder);

    //    //Create CSV file if it does not exist
    //    if (!File.Exists(filePath))
    //    {
    //        //write basic infos
    //        string basicInfoHeader = "Shoulder Pos_x, Shoulder Pos_y, Shoulder Pos_z, UpperArm Length, Forearm Length, b1, b2, Unity_y_min, Unity_y_max, MARS_y_min, MARS_y_max\n";
    //        File.WriteAllText(filePath, basicInfoHeader);

    //        string basicInfo = VRCalibScript.shPos[0].ToString() + "," + VRCalibScript.shPos[1].ToString() + "," + VRCalibScript.shPos[2].ToString() + "," +
    //            VRCalibScript.lu.ToString() + "," + VRCalibScript.lf.ToString() + "," +
    //            weightEstimation.sol[0].ToString() + "," + weightEstimation.sol[1].ToString() + "," +
    //            bottomBound.ToString() + "," + topBound.ToString() + "," +
    //             y_min.ToString() + "," + y_max.ToString() + '\n';
    //        File.AppendAllText(filePath, basicInfo);
    //        //write header
    //        string header = "TIME, theta1, theta2, theta3, theta4, force1, force2, playerPos_y, BallPos_x, BallPos_y, playerScore, enemyScore, support, controlMode\n";
    //        File.AppendAllText(filePath, header);
    //    }

    //    DateTime currentDateTime = DateTime.Now;
    //    string position = time + "," + enc_bluetooth.ang1.ToString() + "," + enc_bluetooth.ang2.ToString() + "," + enc_bluetooth.ang3.ToString() + "," +
    //        enc_bluetooth.ang3.ToString() + "," + enc_bluetooth.force1.ToString() + "," + enc_bluetooth.force2.ToString() + "," +
    //        transform.position.y.ToString() + "," + BallSpawnerController.ballPos.x.ToString() + "," + BallSpawnerController.ballPos.y.ToString() + "," +
    //        BoundController.playerScore.ToString() + "," + BoundController.enemyScore.ToString() + "," + weightEstimation.support.ToString() + "," + enc_bluetooth.PCParam + '\n';
    //    Debug.Log("Appending Text");
    //    File.AppendAllText(filePath, position);
    //}
}
