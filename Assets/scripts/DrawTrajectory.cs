using UnityEngine;

using System.Collections.Generic;
using NUnit.Framework.Constraints;

public static class DrawParams
{
    // Static variables related to drawing the trajectory.
    public static int OFFSET { get; private set; }
    public static readonly float SCALEX = 12f;
    public static readonly float SCALEY = 5.5f;
    public static readonly float DIST_THRESHOLD = 0.01f; // meters

    public static void Initialize()
    {
        DrawParams.OFFSET = AppData.Instance.userData?.limb == 1 ? -1 : 1;
    }
}

// Class for drawing lines based on endpoint positions.
public class DrawTrajectory : MonoBehaviour
{
    // Line render
    // public static LineRenderer lineRenderer;
    // public static List<Vector3> unityPoints;
    // public static List<Vector3> actualPoints;
    // public static double zEndPoint, unityValX, yEndPoint, unityValY;
    // public static AssessROMML.AROM_RAW_ASSESS_STATES currentState;

    // void Start()
    // {
    //     // Update OFFSET.
    //     // Initialize lists and line renderer.
    //     unityPoints = new List<Vector3>();
    //     actualPoints = new List<Vector3>();
    //     lineRenderer = null;
    //     currentState = AssessROMML.AROM_RAW_ASSESS_STATES.INIT;
    // }

    // void FixedUpdate()
    // {
    //     zEndPoint = MarsComm.epPosInThePlane.z;
    //     yEndPoint = MarsComm.epPosInThePlane.y;

    //     // Scaling + centering
    //     Vector3 sceneCenter = Vector3.zero;  // adjust if needed

    //     // Compute unity coordinates.
    //     unityValX = DrawParams.OFFSET * ((zEndPoint - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ)) * DrawParams.SCALEX;
    //     unityValY = ((yEndPoint - MarsDefs.EPCENTERY) / (MarsDefs.EPMAXY - MarsDefs.EPMINY)) * DrawParams.SCALEY;
    //     Vector3 toDrawValues = new Vector3((float)unityValX, (float)unityValY, 0.0f) + sceneCenter;
    //     Vector3 endPointValues = new Vector3((float)zEndPoint, (float)yEndPoint, 0.0f);
    //     // Get the last endpoint position that was added.
    //     Vector3 lastEndPoint = actualPoints.Count > 0 ? actualPoints[actualPoints.Count - 1] : Vector3.zero;

    //     // If the distance between the current and last endpoint is less than a threshold, skip adding this point.
    //     if (Vector3.Distance(endPointValues, lastEndPoint) < DrawParams.DIST_THRESHOLD) return;

    //     // Add the point and update the lists and the plot.
    //     unityPoints.Add(toDrawValues);
    //     actualPoints.Add(endPointValues);

    //     // Redraw based on the current state.
    //     switch (AssessROM.instance.aromRawAssessState)
    //     {
    //         case AssessROM.AROM_RAW_ASSESS_STATES.ASSESSROM:
    //         case AssessROM.AROM_RAW_ASSESS_STATES.INITIATECIRCLE:
    //             lineRenderer.positionCount = unityPoints.Count;
    //             lineRenderer.SetPositions(unityPoints.ToArray());
    //             lineRenderer.useWorldSpace = true;
    //             break;
    //         case AssessROM.AROM_RAW_ASSESS_STATES.WAITTOREACH:
    //         case AssessROM.AROM_RAW_ASSESS_STATES.TEST:
    //             if (AssessROM.instance.currentCircle != null)
    //             {
    //                 AssessROM.instance.currentCircle.transform.position = toDrawValues;
    //             }
    //             break;
    //     }
    // }
    
    // public void Initialize()
    // {
    //     DrawParams.Initialize();
    //     lineRenderer = lr;
    //     lineRenderer.startWidth = 0.05f;
    //     lineRenderer.endWidth = 0.05f;
    // }
}
