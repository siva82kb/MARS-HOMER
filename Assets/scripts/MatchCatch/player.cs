using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class player : MonoBehaviour
{
    public static player Instance;
  
    public float speed = 10f;
    public SpriteRenderer sr;
    public Sprite[] sprites;
    public int lastColorIndex = -1;
    public string[] basketColors = new string[] { "brown", "green", "red", "violet" };

    // Start is called before the first frame update
    private float[] screenBounds;
    private Vector3 endPoint;
    public bool isInitialized { get; private set; } = false;
    public static float xScreenMin, yScreenMin, xScreenMax, yScreenMax;
    public static float xScreenMidPoint;
    public static float xScreenRange;
    public static float yScreenMidPoint;
    public static float yScreenRange;
    float zEndPoint;
    float xPoint;
    private float smoothSpeed = 20f;

    // Robot AROM limits values.
    public static float zEndPointMin;
    public static float zEndPointMax;
    public static float zEndPointMid;
    public static float zEndPointRange;
    public static int LIMBSCALE;
    // Robot endpoint Y limits
    public static float yEndPointMin;
    public static float yEndPointMax;
    public static float yEndPointMid;
    public static float yEndPointRange;
    public static float robotZToUnityX(float z) => LIMBSCALE * (xScreenMidPoint + xScreenRange * (z - zEndPointMid) / zEndPointRange);
    public static float unityXToRobotZ(float x) => ((x / LIMBSCALE) - xScreenMidPoint) * (zEndPointRange / xScreenRange) + zEndPointMid;
    public static float unityYToRobotY(float y) =>
           ((y / LIMBSCALE) - yScreenMidPoint) * (yEndPointRange / yScreenRange) + yEndPointMid;

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        SetRandomColor();
        // Get screen bounds
        screenBounds = MarsGameDefs.SCREEN_LIMITS["MC"];
        xScreenMin = screenBounds[0];
        xScreenMax = screenBounds[1];
        yScreenMin = screenBounds[2];
        yScreenMax = screenBounds[3];
        // Compute midpoints and ranges
        xScreenMidPoint = (xScreenMin + xScreenMax) / 2.0f;
        xScreenRange = xScreenMax - xScreenMin;
        yScreenMidPoint = (yScreenMin + yScreenMax) / 2.0f;
        yScreenRange = yScreenMax - yScreenMin;
       
        // Check of selected movement is null
        if (AppData.Instance.selectedMovement == null || AppData.Instance.selectedMovement.currentArom == null)
        {
            AppLogger.LogError("Selected movement or current AROM is null in Player_controller_s. Setting endpoint limits to default robot values.");
            zEndPointMin = MarsDefs.EPMINZ;
            zEndPointMax = MarsDefs.EPMAXZ;
            zEndPointMid = MarsDefs.EPCENTERZ;
            zEndPointRange = zEndPointMax - zEndPointMin;
        }
        else
        {
            zEndPointMin = AppData.Instance.selectedMovement.currentArom.leftAdjusted.x;
            zEndPointMax = AppData.Instance.selectedMovement.currentArom.rightAdjusted.x;
            zEndPointMid = (zEndPointMin + zEndPointMax) / 2.0f;
            zEndPointRange = zEndPointMax - zEndPointMin;
            yEndPointMin = AppData.Instance.selectedMovement.currentArom.bottomAdjusted.y;
            yEndPointMax = AppData.Instance.selectedMovement.currentArom.topAdjusted.y;
            yEndPointMid = (yEndPointMin + yEndPointMax) / 2.0f;
            yEndPointRange = yEndPointMax - yEndPointMin;
        }
        // Set the appropriate scale
        LIMBSCALE = (AppData.Instance.userData == null || AppData.Instance.userData.rightArm) ? -1 : 1;
    }
    public void FixedUpdate()
    {
        if (MCGameController.Instance.debug) return;
        // Get the current endpoint position in the training plane.
        endPoint = MarsComm.epPosInThePlane;
        zEndPoint = endPoint.z;
        xPoint = robotZToUnityX(zEndPoint); // LIMBSCALE * (xScreenMidPoint + xScreenRange * (zEndPoint - zEndPointMid) / zEndPointRange);


        // Player position
        Vector3 targetPos = new Vector3(
            Mathf.Clamp(xPoint, xScreenMin, xScreenMax),
            -4.0f,
            -8.0f
        );
        // Smoothen player movement
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            smoothSpeed * Time.fixedDeltaTime
        );

    }
    void Update()
    {
        if (!MCGameController.Instance.debug)
        {
            MarsComm.sendHeartbeat();
            return;
        }
           
        Vector3 mp = Input.mousePosition;
        mp.z = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mp);

        transform.position = new Vector3(
            worldPos.x,          // move with mouse X
            transform.position.y,// keep current Y
            transform.position.z // keep current Z
        );
    }
  
   

    public void SetRandomColor()
    {
       
        int newIndex;
        do
        {
            newIndex = Random.Range(0, basketColors.Length);
        }
        while (newIndex == lastColorIndex && basketColors.Length > 1);

        lastColorIndex = newIndex;
        sr.sprite = sprites[lastColorIndex];

    }
}
