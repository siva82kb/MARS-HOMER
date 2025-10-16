
using UnityEngine;

public class SSPlayerController : MonoBehaviour
{
    public static SSPlayerController instance;

    // Start is called before the first frame update
    private float[] screenBounds;
    private Vector3 endPoint;
    public bool isInitialized { get; private set; } = false;
    public static float xScreenMin, yScreenMin, xScreenMax, yScreenMax;
    public static float xScreenMidPoint;
    public static float xScreenRange;
    float  zEndPoint;
    float xPoint;
    private float smoothSpeed = 20f;

    // Robot AROM limits values.
    public static float zEndPointMin;
    public static float zEndPointMax;
    public static float zEndPointMid;
    public static float zEndPointRange;
    public static int LIMBSCALE;

    public static float robotZToUnityX(float z) => LIMBSCALE * (xScreenMidPoint + xScreenRange * (z - zEndPointMid) / zEndPointRange);
    public static float unityXToRobotZ(float x) => ((x / LIMBSCALE) - xScreenMidPoint) * (zEndPointRange / xScreenRange) + zEndPointMid;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
    }

    public void Initialize()
    {
        // Get screen bounds
        screenBounds = MarsGameDefs.SCREEN_LIMITS["SS"];
        xScreenMin = screenBounds[0];
        xScreenMax = screenBounds[1];
        yScreenMin = screenBounds[2];
        yScreenMax = screenBounds[3];
        // Compute midpoints and ranges
        xScreenMidPoint = (xScreenMin + xScreenMax) / 2.0f;
        xScreenRange = xScreenMax - xScreenMin;

        // // Get the audio source component
        // audioSource = GetComponent<AudioSource>();

        // Get the current AROM data.
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
        }
        // Set the appropriate scale
        LIMBSCALE = (AppData.Instance.userData == null || AppData.Instance.userData.rightArm) ? -1 : 1;
        isInitialized = true;
    }

    public void FixedUpdate()
    {
        // Nothing to do if not initialized
        if (!isInitialized) return;

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
    
    public void DestroyPlayer()
    {
        Destroy(gameObject);
    }
   
}
