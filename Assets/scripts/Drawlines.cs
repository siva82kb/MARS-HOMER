using UnityEngine;

using System.Collections.Generic;

public class Drawlines : MonoBehaviour
{
    //Line render
    public static LineRenderer lr;
    public static List<Vector3> unityDrawValues;
    public static List<Vector3> endPntPos;

    public readonly string robotCalibScene = "ROBOTCALIB";
    //public static Drawlines Instance;

    public static double zEndPoint, unityValX, yEndPoint, unityValY;

    void Start()
    {
       
        unityDrawValues = new List<Vector3>();
        endPntPos = new List<Vector3>();
        lr = GetComponent<LineRenderer>();
        lr.SetWidth(0.1f, 0.1f);

    }

    void FixedUpdate()
    {

        Vector3 endPointPosition = MarsComm.planeEndPoints;

        zEndPoint = endPointPosition.z;
        yEndPoint = endPointPosition.y;

        // Scaling + centering
        Vector3 sceneCenter = Vector3.zero;  // adjust if needed
      

        unityValX = AssessROM.instance.OFFSET * ((zEndPoint - AssessROM.instance.centerValX) / (AssessROM.endPointMaxZ - AssessROM.endPointMinZ)) * AssessROM.SCALEX;
        unityValY = ((yEndPoint - AssessROM.instance.centerValY) / (AssessROM.endPointMaxY - AssessROM.endPointMinY)) * AssessROM.SCALEY;
        Vector3 toDrawValues = new Vector3((float)unityValX, (float)unityValY, 0.0f) + sceneCenter;
        Vector3 endPointValues = new Vector3((float)zEndPoint, (float)yEndPoint, 0.0f);

        unityDrawValues.Add(toDrawValues);
        endPntPos.Add(endPointValues);
        
        switch (AssessROM.instance.currState)
        {
            case AssessROM.ASSESSSTATE.ASSESSROM:
            case AssessROM.ASSESSSTATE.INTIIATECIRCLE:
                lr.positionCount = unityDrawValues.Count;
                lr.SetPositions(unityDrawValues.ToArray());
                lr.useWorldSpace = true;
                break;
            case AssessROM.ASSESSSTATE.WAITTOREACH:
               
                if (AssessROM.instance.currentCircle != null)
                {
                    AssessROM.instance.currentCircle.transform.position = toDrawValues;
                 
                }
                break;
        }
      
    }


}
