
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.UI;
using System;
public class AssessROM : MonoBehaviour
{
    float epmaxX, epmaxY, epminX, epminY;
    List<Vector3> endPoints;
    List<Vector3> unityPoints;
    public static AssessROM instance;

    public bool changeScene = false;
    public Text messageTxt;
    public Text thersholdText;
    public Text thersholdTextY;
    public Text test;
    private LineRenderer lineRenderer;
    public LineRenderer boxLR;
    public LineRenderer scaleUpBox;
    public GameObject circlePrefab;
    public GameObject currentCircle;
  

    //Values to Draw the line [ROBOT endPoints (Meters)]
    public const float endPointMaxZ = 0.360f;
    public const float endPointMinZ = -0.160f;
    public const float endPointMaxY = 0.754f;
    public const float endPointMinY = 0.092f;
    //UNITY HIGHT AND WIDTH
    public const float SCALEX = 12f;
    public const float SCALEY = 5.5f;
    public float centerValX { get; private set; }
    public float centerValY {  get; private set; }

    public int OFFSET {  get; private set; }
    public Text moveTxt;
    public readonly string preScene = "CHOOSEMOVE";
    public enum ASSESSSTATE
    {
        ASSESSROM,
        INTIIATECIRCLE,
        WAITTOREACH,
        DONE,
       
    }
    public ASSESSSTATE currState = ASSESSSTATE.ASSESSROM;
    //Dynamic
    float minX , maxX , minY , maxY ;
    //ROM
    float minxpre, minypre,maxxpre,maxypre;
    //BOUND
    float minxBound, minyBound, maxxBound,maxyBound;

    float minxpres;
    float minypres;
    float maxxpres;
    float maxypres ;
    void Awake()
    {
        instance = this;
        MarsComm.sendHeartbeat();
        lineRenderer = GetComponent<LineRenderer>();
    }
    
    void Start()
    {
       //logging about the scene
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        AppData.Instance.selectedMovement.ResetRomValues();
        MarsComm.OnMarsButtonReleased += OnMarsButtonReleased;
        moveTxt.text = MarsComm.MOVETYPE[MarsDefs.getMovementIndex(AppData.Instance.selectedMovement.name)];
        //Dependent on Limb
        OFFSET = AppData.Instance.userData.limb == 1 ? -1 : 1;

        centerValX = (endPointMaxZ + endPointMinZ) / 2;
        centerValY = (endPointMaxY + endPointMinY) / 2;

        createWorkSpace();
       
    }
    void Update()
    { 
        MarsComm.sendHeartbeat();
        updateUI();
    }
    public void createWorkSpace()
    {
        // Compute corners (centered)
        Vector3 topLeft = new Vector3((float)((endPointMinZ - centerValX) / (endPointMaxZ - endPointMinZ)) * SCALEX,
                                      (float)((endPointMaxY - centerValY) / (endPointMaxY - endPointMinY)) * SCALEY,
                                      0);
        Vector3 topRight = new Vector3((float)((endPointMaxZ - centerValX) / (endPointMaxZ - endPointMinZ)) * SCALEX,
                                       (float)((endPointMaxY - centerValY) / (endPointMaxY - endPointMinY)) * SCALEY,
                                       0);
        Vector3 bottomRight = new Vector3((float)((endPointMaxZ - centerValX) / (endPointMaxZ - endPointMinZ)) * SCALEX,
                                          (float)((endPointMinY - centerValY) / (endPointMaxY - endPointMinY)) * SCALEY,
                                          0);
        Vector3 bottomLeft = new Vector3((float)((endPointMinZ - centerValX) / (endPointMaxZ - endPointMinZ)) * SCALEX,
                                         (float)((endPointMinY - centerValY) / (endPointMaxY - endPointMinY)) * SCALEY,
                                         0);

        // Draw box — close loop by adding the first point at the end
        Vector3[] corners = new Vector3[]
        {
        topLeft, topRight, bottomRight, bottomLeft, topLeft
        };
        minxBound = corners.Min(c => c.x);
        maxxBound = corners.Max(c => c.x);
        minyBound = corners.Min(c => c.y);
        maxyBound = corners.Max(c => c.y);
        createFrame(boxLR, corners, Color.red);
    }


    public void updateUI()
    {
        //get latest points
        unityPoints = Drawlines.unityDrawValues;
        endPoints = Drawlines.endPntPos;
       
        messageTxt.text = "Press Mars Button to Finish";
        test.gameObject.SetActive(currState == ASSESSSTATE.WAITTOREACH);
        if ((unityPoints != null && unityPoints.Count > 0) && (endPoints != null && endPoints.Count > 0))
        {
            //get Robot endPoints
            epmaxX = endPoints.Max(v => v.x);
            epminX = endPoints.Min(v => v.x);
            epmaxY = endPoints.Max(v => v.y);
            epminY = endPoints.Min(v => v.y);
            runStateMachine();
            thersholdTextY.text = $"{(Math.Abs(epmaxY - epminY) * 100).ToString("F0")}cm";
            thersholdText.text = $"{(Math.Abs(epmaxX - epminX) * 100).ToString("F0")}cm";
        }
        
    }
 
    public void runStateMachine()
    {
        switch (currState)
        {
            case ASSESSSTATE.ASSESSROM:
                 minX = unityPoints.Min(v => v.x);
                 maxX = unityPoints.Max(v => v.x);
                 minY = unityPoints.Min(v => v.y);
                 maxY = unityPoints.Max(v => v.y);
                UpdateOutline(minX, maxX, minY, maxY, lineRenderer, Color.green);
                messageTxt.text = "Press Mars Button To Fix ROM";
                break;
            case ASSESSSTATE.INTIIATECIRCLE:
                if (currentCircle == null)
                {
                    GameObject circle = Instantiate(circlePrefab, circlePrefab.transform.position, Quaternion.identity);
                    currentCircle = circle;
                }
                if (currentCircle != null)
                {
                    currState = ASSESSSTATE.WAITTOREACH;
                }
                minxpres = minxpre;//scaleup value = previous value
                minypres = minypre;
                maxxpres = maxxpre;
                maxypres = maxypre;

                break;
            case ASSESSSTATE.WAITTOREACH:
                
                //0.5 cm on both side
                float stepX = 0.5f / ((endPointMaxZ - endPointMinZ) * 100f / SCALEX);
                float stepY = 0.5f / ((endPointMaxY - endPointMinY) * 100f / SCALEY);
              
                messageTxt.text = "Press Mars Button To Finish";
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    // Grow outward (+0.5 cm each side)
                    maxxpres = Mathf.Min(maxxpres + stepX,maxxBound);  // don’t exceed original max
                    minxpres = Mathf.Max(minxpres - stepX,minxBound);  // don’t exceed original min
                    maxypres = Mathf.Min(maxypres + stepY,maxyBound);
                    minypres = Mathf.Max(minypres - stepY,minyBound);
                }

                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    // Shrink inward (-0.5 cm each side)
                    maxxpres = Mathf.Max(maxxpres - stepX, minxpres + stepX); // don’t cross min
                    minxpres = Mathf.Min(minxpres + stepX, maxxpres - stepX); // don’t cross max
                    maxypres = Mathf.Max(maxypres - stepY, minypres + stepY);
                    minypres = Mathf.Min(minypres + stepY, maxypres - stepY);
                }

                UpdateOutline(minxpres, maxxpres, minypres, maxypres, scaleUpBox, Color.blue);
                float scaleZmin = (minxpres / (OFFSET * SCALEX)) * (endPointMaxZ - endPointMinZ) + centerValX;
                float scaleZMax = (maxxpres / (OFFSET * SCALEX)) * (endPointMaxZ - endPointMinZ) + centerValX;
                float scaleYmax = (-minypres / SCALEY) * (endPointMaxY - endPointMinY) + centerValY;
                float scaleYmin = (-maxypres / SCALEY) * (endPointMaxY - endPointMinY) + centerValY;
                AppData.Instance.selectedMovement.SetNewRomValues(scaleZmin, scaleZMax, scaleYmin,scaleYmax );
                break;
            case ASSESSSTATE.DONE:
                SceneManager.LoadScene(preScene);
                break;

        }
    }
    void UpdateOutline(float minX, float maxX, float minY, float maxY, LineRenderer lr, Color color)
    {

        Vector3[] corners = new Vector3[5]
        {
            new Vector3(minX, minY, 0), // bottom-left
            new Vector3(maxX, minY, 0), // bottom-right
            new Vector3(maxX, maxY, 0), // top-right
            new Vector3(minX, maxY, 0), // top-left
            new Vector3(minX, minY, 0)  // close the loop
        };
        createFrame(lr, corners, color);
    }
    public void createFrame(LineRenderer lr, Vector3[] corners, Color color)
    {
        lr.positionCount = corners.Length;
        lr.startColor = lr.endColor = color;
        lr.SetPositions(corners);
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.loop = false;
        lr.useWorldSpace = true;
    }
    public void OnMarsButtonReleased()
    {

        switch (currState)
        {
            case ASSESSSTATE.ASSESSROM:
                //get unity UI points
                minxpre = minX;
                minypre = minY;
                maxxpre = maxX;
                maxypre = maxY;

                unityPoints.Clear();
                endPoints.Clear();
                AppData.Instance.selectedMovement.SetNewRomValues(epminX, epmaxX, epminY, epmaxY);
                Drawlines.unityDrawValues.Clear();
                Drawlines.endPntPos.Clear();
                currState = ASSESSSTATE.INTIIATECIRCLE;
                break;
            case ASSESSSTATE.WAITTOREACH:
                AppData.Instance.selectedMovement.SaveAssessmentData();
                currState = ASSESSSTATE.DONE;
                break;
        }

    }
    //Assigned to REDO Button
    public void onclick_recalibrate()
    {
        SceneManager.LoadScene("ASSESSROM");

    }
    public void OnDestroy()
    {
        MarsComm.OnMarsButtonReleased -= OnMarsButtonReleased;
    }
}
