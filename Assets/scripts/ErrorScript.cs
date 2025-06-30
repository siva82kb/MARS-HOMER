using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ErrorScript : MonoBehaviour
{
    public GameObject errorMessage;
    public TMP_Text errorType;
    int errorCount = 0, dataCnt = 0;
    public float[] loadCellData = new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    float tim;
    float errorSize;

    float t1, t2, t3, t4, t5;
    float endx, endy, endz, len1 = 475.0f, len2 = 291.0f, shSin;
    // Start is called before the first frame update
    void Start()
    {
        //loadCellData[0] = AppData.plutoData.torque1;
        //loadCellData[1] = AppData.plutoData.torque1;
        //loadCellData[2] = AppData.plutoData.torque1;
        //loadCellData[3] = AppData.plutoData.torque1;
        //loadCellData[4] = AppData.plutoData.torque1;
        //loadCellData[5] = AppData.plutoData.torque1;
        //loadCellData[6] = AppData.plutoData.torque1;
        //loadCellData[7] = AppData.plutoData.torque1;
        //loadCellData[8] = AppData.plutoData.torque1;
        //loadCellData[9] = AppData.plutoData.torque1;
    }

    // Update is called once per frame
    void Update()
    {
        errorData();


        errorSize = Mathf.Abs(loadCellData[0] - loadCellData[9]);

        tim += Time.deltaTime;
        //Debug.Log("time   =   " + tim + ",   LoadCell reading   =   " + AppData.plutoData.torque1 + "   ,   ErrorCode   =   " + AppData.plutoData.torque3);

        //if (AppData.plutoData.torque3 > 100)
        //{
        //    if (errorCount == 1)
        //    {
        //        errorCount++;
        //        errorMessage.SetActive(true);
        //        errorType.text = "Error 101: Check Load Cell";

        //    }
        //}


        //if (errorSize > 20)
        //{
        //    if (errorCount == 0)
        //    {
        //        errorCount++;
        //        AppData.plutoData.PCParam = new float[] { WeightEstimation.support, 0, 5001, 0 };
        //    }
        //}

        //if (errorCount > 0)
        //{
        //    if (AppData.plutoData.torque3 == 101)
        //        errorType.text = "Error 101: Check Load Cell";
        //}

        //t1 = 0.017453f * AppData.plutoData.enc1;
        //t2 = 0.017453f * AppData.plutoData.enc2;
        //t3 = 0.017453f * AppData.plutoData.enc3;

        endx = Mathf.Cos(t1) * (len1 * Mathf.Cos(t2) + len2 * Mathf.Cos(t2 + t3));
        endy = Mathf.Sin(t1) * (len1 * Mathf.Cos(t2) + len2 * Mathf.Cos(t2 + t3));
        endz = -len1 * Mathf.Sin(t2) - len2 * Mathf.Sin(t2 + t3);
        shSin = len1 * Mathf.Cos(t2) + len2 * Mathf.Cos(t2 + t3);
        //Debug.Log("theta2   =   " + t2 + ",   theta3   =   " + t3 + ",   shShin   =   "+ shSin);

        //if (AppData.plutoData.torque3 != 101)
        //{
        //    if (Mathf.Abs(shSin) < 100)
        //    {
        //        errorType.text = "Error 102: Avoid regions close to shoulder singularity";
        //        errorMessage.SetActive(true);
        //    }
        //    else if (Mathf.Abs(AppData.plutoData.enc3) > 150 || Mathf.Abs(AppData.plutoData.enc3) < 30)
        //    {
        //        errorType.text = "Error 103: Avoid elbow singularity";
        //        errorMessage.SetActive(true);
        //    }
        //    else
        //    {
        //        errorMessage.SetActive(false);
        //    }
        //}
    }

    void errorData()
    {
        if (dataCnt < 10)
        {
            //loadCellData[dataCnt] = AppData.plutoData.torque1;
        }
        else
        {
            loadCellData[0] = loadCellData[1];
            loadCellData[1] = loadCellData[2];
            loadCellData[2] = loadCellData[3];
            loadCellData[3] = loadCellData[4];
            loadCellData[4] = loadCellData[5];
            loadCellData[5] = loadCellData[6];
            loadCellData[6] = loadCellData[7];
            loadCellData[7] = loadCellData[8];
            loadCellData[8] = loadCellData[9];
            //loadCellData[9] = AppData.plutoData.torque1;
        }
        dataCnt++;
    }
}
