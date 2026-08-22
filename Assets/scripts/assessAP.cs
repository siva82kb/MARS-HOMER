using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static MarsAssessAROM1;

public class assessAP : MonoBehaviour
{
    // Start is called before the first frame update
    // Scenes to change to.
    private readonly string preScene = "CHOOSEMOVE";
    private readonly string robotCalibScene = "ROBOTCALIB";
    private readonly string marsSetUp = "MARSSETUP";
    public static int OFFSET { get; private set; }
    public static readonly float SCALEX = 10f;
    public static readonly float SCALEY = 10f;
    private MarsArom oldMarsArom = null;
    bool changeScene = false;
    string movement;
    public LineRenderer aromBoxLineRenderer;
    public LineRenderer aromBoxLineRendererOld;
    // Other private variables
    private Vector3 tempLeftPos;
    private Vector3 tempRightPos;
    private Vector3 tempTopPos;
    private Vector3 tempBottomPos;
    public static readonly Color DARKER_RED = new Color(0.6f, 0f, 0f);
    public static readonly Color LIGHT_GREEN = new Color(0.5f, 1f, 0.5f);
  
    public LineRenderer aromLine1Renderer;
    public LineRenderer aromLine2Renderer;
    public LineRenderer aromLine1RendererOld;
    public LineRenderer aromLine2RendererOld;
    public GameObject aromAreaBox;
    public GameObject aromAreaBoxOld;
    // Other private constants
    private const float leftRightY = 2.5f;
    private const float topBottomX = 2.5f;
    protected enum AROM_ADJUST_STATES
    {
        NONE,
        TOP,
        BOTTOM,
        LEFT,
        RIGHT,
    }
    protected AROM_ADJUST_STATES aromAdjustState = AROM_ADJUST_STATES.NONE;
    void Awake()
    {
       
        MarsComm.sendHeartbeat();
        MarsComm.setControlType("POSITION");
    }

    void Start()
    {
        // AROM Box
        aromBoxLineRenderer.startWidth = 0.05f;
        aromBoxLineRenderer.endWidth = 0.05f;
        aromBoxLineRenderer.startColor = DARKER_RED;
        aromBoxLineRenderer.endColor = DARKER_RED;
        aromBoxLineRendererOld.startWidth = 0.05f;
        aromBoxLineRendererOld.endWidth = 0.05f;
        aromBoxLineRendererOld.startColor = DARKER_RED;
        aromBoxLineRendererOld.endColor = DARKER_RED;
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

        // Set the movement.
        movement = "AP";
        oldMarsArom = AppData.Instance.selectedMovement?.currentArom;
        SetOffset();
        Debug.Log(oldMarsArom);
        showOldAdjustAromForMLAP();
    }
    void Update()
    {
        MarsComm.sendHeartbeat();
      

        // Check if its time to change scene.
        if (changeScene)
        {
            SceneManager.LoadScene(preScene);
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            tempLeftPos = oldMarsArom.leftAdjusted;
            aromAdjustState = AROM_ADJUST_STATES.LEFT;
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            tempRightPos = oldMarsArom.rightAdjusted;
            aromAdjustState = AROM_ADJUST_STATES.RIGHT;
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            tempTopPos = oldMarsArom.topAdjusted;
            aromAdjustState = AROM_ADJUST_STATES.TOP;
        }
        else if (Input.GetKeyDown(KeyCode.B))
        {
            tempBottomPos = oldMarsArom.bottomAdjusted;
            aromAdjustState = AROM_ADJUST_STATES.BOTTOM;
        }
        showAdjustedAromBoxLines();
    }
    private void SetOffset()
    {
        OFFSET = AppData.Instance.userData?.limb == 1 ? -1 : 1;
    }
    private float robotToUnityX(float robotX) => OFFSET * SCALEX * ((robotX - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
    private float robotToUnityY(float robotY) => SCALEY * ((robotY - MarsDefs.EPCENTERY) / (MarsDefs.EPMAXY - MarsDefs.EPMINY));
    private void showAdjustedAromBoxLines()
    {
        // What we show depends on the movement.
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        // Update temporary positions.
        float _adjustedX = MarsDefs.EPCENTERZ + mouseWorldPos.x * (MarsDefs.EPMAXZ - MarsDefs.EPMINZ) / (OFFSET * SCALEX);
        float _adjustedY = MarsDefs.EPCENTERY + mouseWorldPos.y * (MarsDefs.EPMAXY - MarsDefs.EPMINY) / SCALEY;
        switch (aromAdjustState)
        {
            case AROM_ADJUST_STATES.LEFT:
                tempLeftPos.x = _adjustedX;
                tempLeftPos.y = _adjustedY;
                break;
            case AROM_ADJUST_STATES.RIGHT:
                tempRightPos.x = _adjustedX;
                tempRightPos.y = _adjustedY;
                break;
            case AROM_ADJUST_STATES.TOP:
                tempTopPos.x = _adjustedX;
                tempTopPos.y = _adjustedY;
                break;
            case AROM_ADJUST_STATES.BOTTOM:
                tempBottomPos.x = _adjustedX;
                tempBottomPos.y = _adjustedY;
                break;
            default:
                // Optional: handle unexpected states or do nothing
                break;
        }
        // Update display based on the current movement.
        switch (AppData.Instance.selectedMovement?.name)
        {
            case "ML":
                // Show the raw AROM lines
                //showAdjustAromForML();
                break;
            case "AP":
                //showAdjustAromForAP();
                break;
            case "MLAP":
                showAdjustAromForMLAP();
                break;
        }
    }

    private void showAdjustAromForMLAP()
    {
        // Verticex.
        Vector3[] vertices = new Vector3[]
        {
            // Left point
            new Vector3(robotToUnityX(tempLeftPos.x), robotToUnityY(tempLeftPos.y), 0),
            // Bottom point
            new Vector3(robotToUnityX(tempBottomPos.x), robotToUnityY(tempBottomPos.y), 0),
            // Right point
            new Vector3(robotToUnityX(tempRightPos.x), robotToUnityY(tempRightPos.y), 0),
            // Top point
            new Vector3(robotToUnityX(tempTopPos.x), robotToUnityY(tempTopPos.y), 0)
        };
        // Show the adjusted AROM Box.
        aromBoxLineRenderer.positionCount = 5;
        aromBoxLineRenderer.SetPositions(new Vector3[]
        {
            // Left point
            vertices[0],
            // Bottom point
            vertices[1],
            // Right point
            vertices[2],
            // Top point
            vertices[3],
            // Closing the box (back to Left point)
            vertices[0]
        });
    }
    private void showOldAdjustAromForMLAP()
    {
        // Vertices
        Vector3[] vertices = new Vector3[]
        {
            // Left point
            new Vector3(robotToUnityX(oldMarsArom.leftAdjusted.x), robotToUnityY(oldMarsArom.leftAdjusted.y), 0),
            // Bottom point
            new Vector3(robotToUnityX(oldMarsArom.bottomAdjusted.x), robotToUnityY(oldMarsArom.bottomAdjusted.y), 0),
            // Right point
            new Vector3(robotToUnityX(oldMarsArom.rightAdjusted.x), robotToUnityY(oldMarsArom.rightAdjusted.y), 0),
            // Top point
            new Vector3(robotToUnityX(oldMarsArom.topAdjusted.x), robotToUnityY(oldMarsArom.topAdjusted.y), 0)
        };
        aromBoxLineRendererOld.positionCount = 5;
        aromBoxLineRendererOld.SetPositions(new Vector3[]
        {
            // Left point
            vertices[0],
            // Bottom point
            vertices[1],
            // Right point
            vertices[2],
            // Top point
            vertices[3],
            // Closing the box (back to Left point)
            vertices[0]
        });
        // Show the parallel vertical lines
        float leftX = OFFSET * SCALEX * ((oldMarsArom.leftAdjusted.x - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
        float rightX = OFFSET * SCALEX * ((oldMarsArom.rightAdjusted.x - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
        aromLine1RendererOld.positionCount = 2;
        aromLine1RendererOld.SetPositions(new Vector3[]
        {
            new Vector3(leftX, leftRightY, 0),
            new Vector3(leftX, -leftRightY, 0)
        });
       aromLine2RendererOld.positionCount = 2;
       aromLine2RendererOld.SetPositions(new Vector3[]
        {
            new Vector3(rightX, leftRightY, 0),
            new Vector3(rightX, -leftRightY, 0)
        });
        // Display old area
        // Fill the area between the adjusted lines with a semi-transparent box.
        Vector3 boxCenter = new Vector3((leftX + rightX) / 2, 0, 0);
        Vector3 boxSize = new Vector3(Mathf.Abs(rightX - leftX), leftRightY * 2, 0.1f);
        aromAreaBoxOld.transform.position = boxCenter;
        aromAreaBoxOld.transform.localScale = boxSize;
        aromAreaBoxOld.SetActive(true);
        // Transparent blue color
        Color boxColor = new Color(0f, 0f, 1f, 0.3f);
        aromAreaBoxOld.GetComponent<Renderer>().material.color = boxColor;


        float topY = SCALEY * ((tempTopPos.y - MarsDefs.EPCENTERY) / (MarsDefs.EPMAXY - MarsDefs.EPMINY));
        float bottomY = SCALEY * ((tempBottomPos.y - MarsDefs.EPCENTERY) / (MarsDefs.EPMAXY - MarsDefs.EPMINY));
        aromLine1Renderer.positionCount = 2;
        aromLine1Renderer.SetPositions(new Vector3[]
        {
            new Vector3(topBottomX, topY, 0),
            new Vector3(-topBottomX, topY, 0)
        });
        aromLine2Renderer.positionCount = 2;
        aromLine2Renderer.SetPositions(new Vector3[]
        {
            new Vector3(topBottomX, bottomY, 0),
            new Vector3(-topBottomX, bottomY, 0)
        });
        // Fill the area between the adjusted lines with a semi-transparent box.
        Vector3 boxCenter1 = new Vector3(0, (topY + bottomY) / 2, 0);
        Vector3 boxSize1 = new Vector3(2 * topBottomX, Mathf.Abs(bottomY - topY), 0.1f);
        aromAreaBox.transform.position = boxCenter1;
       aromAreaBox.transform.localScale = boxSize1;
       aromAreaBox.SetActive(true);
        // Transparent red color
        Color boxColor1 = new Color(1f, 0f, 0f, 0.5f);
        aromAreaBox.GetComponent<Renderer>().material.color = boxColor1;
    }

}
