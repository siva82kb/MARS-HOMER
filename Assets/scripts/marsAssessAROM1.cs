
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.UI;
using System;
using UnityEditor;
using System.IO;
using JetBrains.Annotations;

public class MarsAssessAROM1 : MonoBehaviour
{
    float epmaxX, epmaxY, epminX, epminY;
    List<Vector3> endPoints;
    List<Vector3> unityPoints;
    public static MarsAssessAROM1 instance;
    private bool changeScene = false;
    public Text messageTxt;
    public Text thresholdText;
    public Text epPosText;
    public Text scaleupRule;
    private LineRenderer lineRenderer;
    public LineRenderer boxLR;
    public LineRenderer scaleUpBox;
    public GameObject circlePrefab;
    public GameObject currentCircle;
    private GameObject topCircle;
    private GameObject bottomCircle;
    private GameObject leftCircle;
    private GameObject rightCircle;
    private GameObject testCircle;

    public Text moveTxt;

    // Scenes to change to.
    private readonly string preScene = "CHOOSEMOVE";
    private readonly string robotCalibScene = "ROBOTCALIB";
    private string marsSetUp = "MARSSETUP";

    // Points of the quadrilateral
    private Vector2 top;
    private Vector2 bottom;
    private Vector2 left;
    private Vector2 right;
    public Vector2 x1;
    public Vector2 x2;
    public Vector2 y1;
    public Vector2 y2;

    // AROM raw assessment states
    public enum AROM_RAW_ASSESS_STATES
    {
        INIT,
        ASSESSROM,
        INITIATECIRCLE,
        WAITTOREACH,
        TEST,
        DONE,
    }
    // AROM adjustment states 
    public enum AROM_ADJUST_STATES
    {
        NONE,
        TOP,
        BOTTOM,
        LEFT,
        RIGHT,
    }
    private AROM_ADJUST_STATES aromAdjustState = AROM_ADJUST_STATES.NONE;
    private AROM_RAW_ASSESS_STATES aromRawAssessState = AROM_RAW_ASSESS_STATES.INIT;

    // Dynamic
    float minX , maxX , minY , maxY ;
    // ROM
    float minxpre, minypre,maxxpre,maxypre,meanZpre,meanYpre;
    // BOUND
    float minxBound, minyBound, maxxBound,maxyBound;

    float minxpres;
    float minypres;
    float maxxpres;
    float maxypres;

    void Awake()
    {
        instance = this;
        MarsComm.sendHeartbeat();
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Start()
    {
        // Initialize AppData if needed
        if (AppData.Instance.userData == null)
        {
            AppData.Instance.Initialize(SceneManager.GetActiveScene().name);
        }

        // Check if the directory exists
        if (!Directory.Exists(DataManager.basePath)) Directory.CreateDirectory(DataManager.basePath);
        if (!File.Exists(DataManager.configFile)) SceneManager.LoadScene("CONFIG");

        // Logging the scene
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

        // If the robot is not calibrated go to the robot calib scene.
        if (MarsComm.CALIBRATION[MarsComm.calibration] == "NOCALIB") SceneManager.LoadScene(robotCalibScene);

        // If the robot is not in position control go to the mars setup scene.
        if (MarsComm.CONTROLTYPE[MarsComm.controlType] != "POSITION") SceneManager.LoadScene(marsSetUp);

        // AppData.Instance.selectedMovement.ResetRomValues();
        MarsComm.OnNewMarsData += onMarsNewData;
        MarsComm.OnMarsButtonReleased += OnMarsButtonReleased;
        // moveTxt.text = MarsComm.MOVETYPE[MarsDefs.getMovementIndex(AppData.Instance.selectedMovement.name)];

        // Dependent on Limb
        // OFFSET = AppData.Instance.userData.limb == 1 ? -1 : 1;

        // Create workspace for display.
        // createWorkSpace();

        // Initialize draw trajectory
        // DrawParams.Initialize(lineRenderer);
    }
    void Update()
    { 
        MarsComm.sendHeartbeat();
        
        // Run the assessment state machine
        runStateMachine();

        // Update the UI.
        updateUI();
        //Test Targets insdide the quad
        //if (Input.GetKeyDown(KeyCode.G))
        //{
        //    if (aromRawAssessState == AROM_RAW_ASSESS_STATES.WAITTOREACH)
        //    {
        //        aromRawAssessState = AROM_RAW_ASSESS_STATES.TEST;
        //        Debug.Log(aromRawAssessState);
        //    }
        //}
        //if (Input.GetKeyDown(KeyCode.Y))
        //{
           
        //    getRandomTargt();
        //}
    }
    
    // public void createWorkSpace()
    // {
    //     // Compute corners (centered)
    //     Vector3 topLeft = new Vector3((float)((MarsDefs.EPMINZ - MarsDefs.EPCENTERZ)DrawTrajectory. / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ)) * SCALEX,
    //                                   (float)((endPointMaxY - MarsDefs.EPCENTERY) / (endPointMaxY - endPointMinY)) * SCALEY,
    //                                   0);
    //     Vector3 topRight = new Vector3((float)((MarsDefs.EPMAXZ - MarsDefs.EPCENTERZ)DrawTrajectory. / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ)) * SCALEX,
    //                                    (float)((endPointMaxY - MarsDefs.EPCENTERY) / (endPointMaxY - endPointMinY)) * SCALEY,
    //                                    0);
    //     Vector3 bottomRight = new Vector3((float)((MarsDefs.EPMAXZ - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ DrawTrajectory.- MarsDefs.EPMINZ)) * SCALEX,
    //                                       (float)((endPointMinY - MarsDefs.EPCENTERY) / (endPointMaxY - endPointMinY)) * SCALEY,
    //                                       0);
    //     Vector3 bottomLeft = new Vector3((float)((MarsDefs.EPMINZ - MarsDefs.EPCENTERZ) / DrawTrajectory.(MarsDefs.EPMAXZ DrawTrajectory.- MarsDefs.EPMINZ)) * SCALEX,
    //                                      (float)((endPointMinY - MarsDefs.EPCENTERY) / (endPointMaxY - endPointMinY)) * SCALEY,
    //                                      0);

    //     // Draw box — close loop by adding the first point at the end
    //     Vector3[] corners = new Vector3[]
    //     {
    //     topLeft, topRight, bottomRight, bottomLeft, topLeft
    //     };
    //     minxBound = corners.Min(c => c.x);
    //     maxxBound = corners.Max(c => c.x);
    //     minyBound = corners.Min(c => c.y);
    //     maxyBound = corners.Max(c => c.y);
    //     createFrame(boxLR, corners, Color.red);
    // }


    public void updateUI()
    {
        // Get latest points
        // unityPoints = DrawTrajectory.unityPoints;
        // endPoints = DrawTrajectory.actualPoints;

        messageTxt.text = "Press Mars Button to Finish";
        scaleupRule.gameObject.SetActive(aromRawAssessState == AROM_RAW_ASSESS_STATES.WAITTOREACH);

        //update min and max value of endpoints
        if ((unityPoints != null && unityPoints.Count > 0) && (endPoints != null && endPoints.Count > 0))
        {
            //get Robot endPoints
            epmaxX = endPoints.Max(v => v.x);
            epminX = endPoints.Min(v => v.x);
            epmaxY = endPoints.Max(v => v.y);
            epminY = endPoints.Min(v => v.y);
            thresholdText.text = $"{(Math.Abs(epmaxX - epminX) * 100).ToString("F0")}cm";
        }
        
        //scaleup each side seperately
        //TOP
        if (Input.GetKeyDown(KeyCode.T))
        {
            aromAdjustState = AROM_ADJUST_STATES.TOP;
        }
        //LEFT
        if (Input.GetKeyDown(KeyCode.L))
        {
            aromAdjustState = AROM_ADJUST_STATES.LEFT;
        }
        //BOTTOM
        if (Input.GetKeyDown(KeyCode.B))
        {
            aromAdjustState = AROM_ADJUST_STATES.BOTTOM;
        }
        //RIGHT
        if (Input.GetKeyDown(KeyCode.R))
        {
            aromAdjustState = AROM_ADJUST_STATES.RIGHT;
        }
       
        if(aromRawAssessState == AROM_RAW_ASSESS_STATES.WAITTOREACH)
        {
            topCircle.GetComponent<SpriteRenderer>().color = Color.grey;
            bottomCircle.GetComponent<SpriteRenderer>().color = Color.grey;
            leftCircle.GetComponent<SpriteRenderer>().color = Color.grey;
            rightCircle.GetComponent<SpriteRenderer>().color = Color.grey;
        }
        
        switch (aromAdjustState)
        {
            case AROM_ADJUST_STATES.TOP:
                topCircle.GetComponent<SpriteRenderer>().color = Color.red;
              
                break;
            case AROM_ADJUST_STATES.BOTTOM:
                bottomCircle.GetComponent<SpriteRenderer>().color = Color.red;
                
                break;
            case AROM_ADJUST_STATES.LEFT:
                leftCircle.GetComponent<SpriteRenderer>().color = Color.red;
               
                break;
            case AROM_ADJUST_STATES.RIGHT:
                rightCircle.GetComponent<SpriteRenderer>().color = Color.red;
                break;
        }
      
    }

    // Assessment statemachine
    public void runStateMachine()
    {
        switch (aromRawAssessState)
        {
            case AROM_RAW_ASSESS_STATES.INIT:
                // Do nothing. Just wait for the button to be pressed0
                messageTxt.text = "Press MARS button to start assessment.";
                break;
            case AROM_RAW_ASSESS_STATES.ASSESSROM:
                minX = unityPoints.Min(v => v.x);
                maxX = unityPoints.Max(v => v.x);
                minY = unityPoints.Min(v => v.y);
                maxY = unityPoints.Max(v => v.y);
                UpdateOutline(minX, maxX, minY, maxY, lineRenderer, new Color(0f / 255f, 100f / 255f, 0f / 255f));// 
                messageTxt.text = "Press Mars Button To Fix ROM";
                break;
            case AROM_RAW_ASSESS_STATES.INITIATECIRCLE:
                if (currentCircle == null)
                {
                    GameObject circle = Instantiate(circlePrefab, circlePrefab.transform.position, Quaternion.identity);
                    GameObject circle1 = Instantiate(circlePrefab, new Vector3(meanZpre, minY, 0), Quaternion.identity);
                    GameObject circle2 = Instantiate(circlePrefab, new Vector3(maxX, meanYpre, 0), Quaternion.identity);
                    GameObject circle3 = Instantiate(circlePrefab, new Vector3(meanZpre, maxY, 0), Quaternion.identity);
                    GameObject circle4 = Instantiate(circlePrefab, new Vector3(minX, meanYpre, 0), Quaternion.identity);
                    currentCircle = circle;
                    currentCircle.GetComponent<SpriteRenderer>().color = Color.green;
                    bottomCircle = circle1;
                    rightCircle = circle2;
                    topCircle = circle3;
                    leftCircle = circle4;
                }
                if (currentCircle != null)
                {
                    aromRawAssessState = AROM_RAW_ASSESS_STATES.WAITTOREACH;
                }
                minxpres = minxpre;//scaleup value = previous value
                minypres = minypre;
                maxxpres = maxxpre;
                maxypres = maxypre;
                break;
            case AROM_RAW_ASSESS_STATES.WAITTOREACH:

                messageTxt.text = "Press Mars Button To Finish";
                //To Modify the Range of Motion
                scaleupStateMachine();

                //Draw quad for the Modifyed Range of Motion
                DrawQuad(minxpres, maxxpres, minypres, maxypres, meanZpre, meanYpre, scaleUpBox, new Color(137 / 255f, 175 / 255f, 253 / 255f));

                //reverse unity value to RobotEnpoint values in meter
                // float scaleZmin = (MarsDefs.EPMAXZ - MarsDefs.EPMINZ) * minxpres / (DrawParams.OFFSET * DrawParams.SCALEX) + MarsDefs.EPCENTERZ;
                // float scaleZMax = (MarsDefs.EPMAXZ - MarsDefs.EPMINZ) * maxxpres / (DrawParams.OFFSET * DrawParams.SCALEX) + MarsDefs.EPCENTERZ;
                // float scaleYmin = (MarsDefs.EPMAXY - MarsDefs.EPMINY) * minypres / DrawParams.SCALEY + MarsDefs.EPCENTERY;
                // float scaleYmax = (MarsDefs.EPMAXY - MarsDefs.EPMINY) * maxypres / DrawParams.SCALEY + MarsDefs.EPCENTERY;
                // AppData.Instance.selectedMovement.SetNewRomValues(scaleZmin, scaleZMax, scaleYmin,scaleYmax,epminX,epmaxX,epminY,epmaxY);

                break;
            case AROM_RAW_ASSESS_STATES.TEST:
                //Test Targets inside the Range Of Motion
                //Assign the Cornor points of the quad
                top = new Vector2(meanZpre, maxypres);
                bottom = new Vector2(meanZpre, minypres);
                left = new Vector2(minxpres, meanYpre);
                right = new Vector2(maxxpres, meanYpre);

                //genrate vector
                x1 = bottom - left;
                y1 = top - left;
                x2 = bottom - right;
                y2 = top - right;

                break;
            case AROM_RAW_ASSESS_STATES.DONE:
                SceneManager.LoadScene(preScene);
                break;

        }
        //Debug.Log(aromRawAssessState);
    }
   
    void scaleupStateMachine()
    {
        // scale value 0.5 cm on both side
        float stepX = 0.5f;/// ((MarsDefs.EPMAXZ - MarsDefs.EPMINZ) * 100f / DrawParams.SCALEX);
        float stepY = 0.5f;/// ((MarsDefs.EPMAXY - MarsDefs.EPMINY) * 100f / DrawParams.SCALEY);

        switch (aromAdjustState)
        {
            case AROM_ADJUST_STATES.TOP:

                topCircle.transform.position = new Vector3(meanZpre, maxypres, 0);
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    // Grow outward (+0.5 cm each side)
                    maxypres = Mathf.Min(maxypres + stepY);
                }

                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    // Shrink inward (-0.5 cm each side)
                    maxypres = Mathf.Max(maxypres - stepY);
                }
                top = new Vector2(meanZpre, maxxpres);

                break;
            case AROM_ADJUST_STATES.BOTTOM:

                bottomCircle.transform.position = new Vector3(meanZpre, minypres, 0);
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    // Shrink inward (-0.5 cm each side)
                    minypres = Mathf.Min(minypres + stepY);

                }

                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    // Grow outward (+0.5 cm each side)
                    minypres = Mathf.Max(minypres - stepY);

                }
                bottom = new Vector2(meanZpre, minypres);
                break;
            case AROM_ADJUST_STATES.LEFT:
                leftCircle.transform.position = new Vector3(minxpres, meanYpre, 0);
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    // Grow outward (+0.5 cm each side)
                    minxpres = Mathf.Min(minxpres + stepX);

                }

                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    // Shrink inward (-0.5 cm each side)
                    minxpres = Mathf.Max(minxpres - stepX);
                }
                left = new Vector2(minxpres, meanYpre);
                break;
            case AROM_ADJUST_STATES.RIGHT:
                rightCircle.transform.position = new Vector3(maxxpres, meanYpre, 0);
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    //Shrink (+0.5 cm each side)  
                    maxxpres = Mathf.Max(maxxpres + stepX); // don’t cross min

                }

                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    //Grow (-0.5 cm each side)
                    maxxpres = Mathf.Min(maxxpres - stepX);

                }
                right = new Vector2(maxxpres, meanYpre);

                break;
        }

    }

    void getRandomTargt()
    {
        Vector2 t;
        Vector2 target;
      
        //rx+ry<=1
        float rx = UnityEngine.Random.Range(0, 1f);
        float ry = UnityEngine.Random.Range(0, (1f - rx));

        //find left or right
        int random = UnityEngine.Random.value < 0.5f ? -1 : 1;
     
        if (random == 1)
        {
           
            t = (rx * x1) + (ry * y1);
            target = t + left;
            
        }
        else
        {
            t = (rx * x2) +( ry * y2);
            target = t + right;
        }

        GameObject circle6 = Instantiate(circlePrefab, target, Quaternion.identity);
        //testCircle = circle6;
        if (testCircle != null)
            Destroy(testCircle);
        testCircle = circle6;

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
    void DrawQuad(float minX, float maxX, float minY, float maxY, float meanz, float meany,LineRenderer lr, Color color)
    {

        Vector3[] corners = new Vector3[5]
        {
            new Vector3(meanz, minY, 0), // bottom-center
            new Vector3(maxX, meany, 0), // center-right
            new Vector3(meanz, maxY, 0), // top-center
            new Vector3(minX, meany, 0), // center-left
            new Vector3(meanz, minY, 0)  // close the loop
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
    
    public void onMarsNewData()
    {
        // This function is called whenever new data is received from the robot.
        // You can process the data here if needed.
        // For example, you might want to update the UI or log the data.
    }

    public void OnMarsButtonReleased()
    {
        // React differently based on the current assessment state
        switch (aromRawAssessState)
        {
            case AROM_RAW_ASSESS_STATES.INIT:
                aromRawAssessState = AROM_RAW_ASSESS_STATES.ASSESSROM;
                break;
            case AROM_RAW_ASSESS_STATES.ASSESSROM:
                //get unity UI points
                minxpre = minX;
                minypre = minY;
                maxxpre = maxX;
                maxypre = maxY;
                meanYpre = (minypre + maxypre) / 2;
                meanZpre = (minxpre + maxxpre) / 2;

                //clear the unity values to remote line after the get orignial ROM
                unityPoints.Clear();
                endPoints.Clear();
                // DrawTrajectory.unityPoints.Clear();
                // DrawTrajectory.actualPoints.Clear();
                aromRawAssessState = AROM_RAW_ASSESS_STATES.INITIATECIRCLE;
                break;
            case AROM_RAW_ASSESS_STATES.WAITTOREACH:
                // AppData.Instance.selectedMovement.SaveAssessmentData();
                aromRawAssessState = AROM_RAW_ASSESS_STATES.DONE;
                break;
            //Test Targets inside the Quad
            case AROM_RAW_ASSESS_STATES.TEST:
                // AppData.Instance.selectedMovement.SaveAssessmentData();
                aromRawAssessState = AROM_RAW_ASSESS_STATES.DONE;
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
