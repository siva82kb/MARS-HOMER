using UnityEngine;

using System.Collections.Generic;

public class Drawlines : MonoBehaviour
{
    //Line render
    public static LineRenderer lr;
    public static List<Vector3> unityDrawValues;
    public static List<Vector3> endPntPos;

    public AssessForce assessForce;
    public readonly string robotCalibScene = "ROBOTCALIB";
    //public static Drawlines Instance;

    public static double zEndPoint, unityValX, yEndPoint, unityValY;

    void Start()
    {
       
        unityDrawValues = new List<Vector3>();
        endPntPos = new List<Vector3>();
        lr = GetComponent<LineRenderer>();
        lr.SetWidth(0.05f, 0.05f);
       assessForce = GetComponent<AssessForce>();

    }

    void FixedUpdate()
    {

        Vector3 endPointPosition = MarsComm.epPosInThePlane;

        zEndPoint = endPointPosition.z;
        yEndPoint = endPointPosition.y;

        // Scaling + centering
        Vector3 sceneCenter = Vector3.zero;  // adjust if needed
      

        unityValX = AssessROM1.instance.OFFSET*((zEndPoint - AssessROM1.instance.centerValX) / (AssessROM1.endPointMaxZ - AssessROM1.endPointMinZ)) * AssessROM1.SCALEX;
        unityValY = ((yEndPoint - AssessROM1.instance.centerValY) / (AssessROM1.endPointMaxY - AssessROM1.endPointMinY)) * AssessROM1.SCALEY;
        Vector3 toDrawValues = new Vector3((float)unityValX, (float)unityValY, 0.0f) + sceneCenter;
        Vector3 endPointValues = new Vector3((float)zEndPoint, (float)yEndPoint, 0.0f);

        unityDrawValues.Add(toDrawValues);
        endPntPos.Add(endPointValues);
        Debug.Log(endPointPosition);
        switch (AssessROM1.instance.currState)
        {
            case AssessROM1.ASSESSSTATE.ASSESSROM:
            case AssessROM1.ASSESSSTATE.INTIIATECIRCLE:
                lr.positionCount = unityDrawValues.Count;
                lr.SetPositions(unityDrawValues.ToArray());
                lr.useWorldSpace = true;
                break;

            case AssessROM1.ASSESSSTATE.WAITTOREACH:
            case AssessROM1.ASSESSSTATE.TEST:
               
                
                if (AssessROM1.instance.currentCircle != null)
                {
                    AssessROM1.instance.currentCircle.transform.position = toDrawValues;
                 
                }
                break;
        }
       

    }


}
