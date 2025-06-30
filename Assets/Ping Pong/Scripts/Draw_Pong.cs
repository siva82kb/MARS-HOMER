using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using UnityEngine.UI;

public class Draw_Pong : MonoBehaviour
{
    public static Draw_Pong instance;
    public float[] Encoder = new float[6];
    public float[] ELValue = new float[6];
    
    public float max_y;
    public float min_y;

    List<Vector3> paths;

    public Text support;
    public float supporti;
    public GameObject supportText;

    //void Awake()
    //{
    //    instance = this;
    //}


    void Start()
    {
        paths = new List<Vector3>();
        //if (GameSceneScript.ctrlMode < 2)
        //{
        //    support.text = "Support: " + Mathf.Round(weightEstimation.support * 100.0f).ToString() + " %";
        //    supportText.SetActive(true);
        //}
        //else
        //{
        //    supportText.SetActive(false);
        //}
    }


    public void onclick_game()
    {
        SceneManager.LoadScene("pong_menu");

    //    // Debug.Log(xxx.Count);
    //    // Debug.Log(xxx.Last()+" :Last");
    //    // Debug.Log(xxx.First()+" :First");

     paths = Drawlines.paths_pass;

        max_y = paths.Max(v => v.y);
        min_y = paths.Min(v => v.y);
        PlayerPrefs.SetFloat( "y max", max_y);
        PlayerPrefs.SetFloat("y min", min_y);

        //    //Debug.Log(max_x + ......+ min_x + ...... +)
        //}
        //public void onclickStart()
        //{
        //    paths = Drawlines.paths_pass;

        //    max_x = paths.Max(v => v.x);
        //    min_x = paths.Min(v => v.x);
        //    max_y = paths.Max(v => v.y);
        //    min_y = paths.Min(v => v.y);
    }
    public void Update()
    {
        SensorValue();
        // System.DateTime currentTime = System.DateTime.Now;
        //if (Input.GetKeyDown(KeyCode.A))
        //{
        //    Debug.Log("increase support");
        //    weightEstimation.support += 0.05f;
        //    if (weightEstimation.support > 1)
        //    {
        //        weightEstimation.support = 1.0f;
        //    }
        //    if (GameSceneScript.ctrlMode < 2)
        //    {
        //        AppData.PCParam = new float[] { weightEstimation.support, 0.0f, 2006, 0.0f };
        //        supporti = Mathf.Round(weightEstimation.support * 100.0f);
        //        support.text = "Support: " + supporti.ToString() + " %";
        //    }
        //}
        //else if (Input.GetKeyDown(KeyCode.S))
        //{
        //    Debug.Log("Decrease support");
        //    weightEstimation.support -= 0.05f;
        //    if (weightEstimation.support < 0)
        //    {
        //        weightEstimation.support = 0.0f;
        //    }
        //    if (GameSceneScript.ctrlMode < 2)
        //    {
        //        AppData.PCParam = new float[] { weightEstimation.support, 0.0f, 2006, 0.0f };
        //        supporti = Mathf.Round(weightEstimation.support * 100.0f);
        //        support.text = "Support: " + supporti.ToString() + " %";
        //    }
        //}
    }
    public void SensorValue()
    {

        //ELValue[0] = enc_bluetooth.ang1;
        //ELValue[1] = enc_bluetooth.ang2;
        //ELValue[2] = enc_bluetooth.ang3;
        //ELValue[3] = enc_bluetooth.ang4;
        //ELValue[4] = enc_bluetooth.force1;
        //ELValue[5] = enc_bluetooth.force2;
        //Encoder = new float[] { ELValue[0], ELValue[1], ELValue[2], ELValue[3], ELValue[4], ELValue[5] };


        string DataPath = Application.dataPath;
        Directory.CreateDirectory(DataPath + "\\" + "Patient_Data" + "\\" );
        string filepath_Endata = DataPath + "\\" + "Patient_Data" +  "\\" + "Ping-Pong-Data.csv";
        //if (IsCSVEmpty(filepath_Endata))
        //{

        //}
        //else
        //{

        //}
    }

    //private bool IsCSVEmpty(string filepath_Endata)
    //{
    //    if (File.Exists(filepath_Endata))
    //    {
    //        //check the file is empty,write header
    //        if (new FileInfo(filepath_Endata).Length == 0)
    //        {
    //            string Endata = "Time,Encoder1,Encoder2,Encoder3,Encoder4,Loadcell 1,Loadcell 2\n";
    //            File.WriteAllText(filepath_Endata, Endata);
    //            DateTime currentDateTime = DateTime.Now;
    //            string Position = currentDateTime + "," + Encoder[0] + "," + Encoder[1] + Encoder[2] + "," + Encoder[3] + "," + Encoder[4] + "," + Encoder[5] + '\n';
    //            return true;
    //        }
    //        else
    //        {
    //            //If the file is not empty,return false
    //            DateTime currentDateTime = DateTime.Now;
    //            string Position = currentDateTime + "," + Encoder[0] + "," + Encoder[1] + Encoder[2] + "," + Encoder[3] + "," + Encoder[4] + "," + Encoder[5] + '\n';
    //            File.AppendAllText(filepath_Endata, Position);
    //            return false;
    //        }
    //    }
    //    else
    //    {
    //        //If the file doesnt exist
    //        string DataPath = Application.dataPath;
    //        //Directory.CreateDirectory(DataPath + "\\" + "Patient_Data" + "\\" + Welcome.p_hospno);
    //        //string filepath_Endata1 = DataPath + "\\" + "Patient_Data" + "\\" + Welcome.p_hospno + "\\" + "Ping-Pong-Data.csv";
    //        //string Endata1 = "Time,Encoder1,Encoder2,Encoder3,Encoder4,Loadcell 1,Loadcell 2\n";
    //        //File.WriteAllText(filepath_Endata1, Endata1);
    //        //DateTime currentDateTime = DateTime.Now;
    //        //string Position = currentDateTime + "," + Encoder[0] + "," + Encoder[1] + Encoder[2] + "," + Encoder[3] + "," + Encoder[4] + "," + Encoder[5] + '\n';
    //        //File.AppendAllText(filepath_Endata1, Position);
    //        //return true;
    //    }

    //}
    public void onclick_recalibrate()
    {
        SceneManager.LoadScene("Draw_Pong");

    }
    //public void onclickHalfweightSupport()
    //{
    //    SceneManager.LoadScene("Half Weight Support");
    //}

}

