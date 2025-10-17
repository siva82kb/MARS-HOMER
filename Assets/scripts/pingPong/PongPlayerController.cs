using UnityEngine;

public class PongPlayerController : MonoBehaviour
{
    public static PongPlayerController instance;

    private float[] screenBounds;
    private Vector3 endPoint;
    private float smoothSpeed = 20f;

    public GameObject player;
    public MarsArom currRom;

    public bool isInitialized { get; private set; } = false;

    // Screen boundaries
    public static float xScreenMin, yScreenMin, xScreenMax, yScreenMax;
    public static float yScreenMidPoint;
    public static float yScreenRange;

    // Robot endpoint Y limits
    public static float yEndPointMin;
    public static float yEndPointMax;
    public static float yEndPointMid;
    public static float yEndPointRange;
    public static int LIMBSCALE;

    static float topBound = 3.6F;
    static float bottomBound = -3.6F;
    public static float playSize;

    float yPoint, yEndPoint;
    public static float unityYToRobotY(float y) =>
        ((y / LIMBSCALE) - yScreenMidPoint) * (yEndPointRange / yScreenRange) + yEndPointMid;
    public static float robotYToUnityY(float y) =>
         (yScreenMidPoint + yScreenRange * (y - yEndPointMid) / yEndPointRange);

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        playSize = topBound - bottomBound;
        currRom = AppData.Instance.selectedMovement.currentArom;

        // Get screen limits for the Pong (PP) scene
        screenBounds = MarsGameDefs.SCREEN_LIMITS["PP"];
        xScreenMin = screenBounds[0];
        xScreenMax = screenBounds[1];
        yScreenMin = screenBounds[2];
        yScreenMax = screenBounds[3];

        yScreenMidPoint = (yScreenMin + yScreenMax) / 2.0f;
        yScreenRange = yScreenMax - yScreenMin;

        // Get robot endpoint Y limits
        if (AppData.Instance.selectedMovement == null || AppData.Instance.selectedMovement.currentArom == null)
        {
            AppLogger.LogError("Selected movement or current AROM is null. Using default Y endpoints.");
            yEndPointMin = MarsDefs.EPMINY;
            yEndPointMax = MarsDefs.EPMAXY;
            yEndPointMid = MarsDefs.EPCENTERY;
            yEndPointRange = yEndPointMax - yEndPointMin;
        }
        else
        {
            yEndPointMin = currRom.bottomAdjusted.y;
            yEndPointMax = currRom.topAdjusted.y;
            yEndPointMid = (yEndPointMin + yEndPointMax) / 2.0f;
            yEndPointRange = yEndPointMax - yEndPointMin;
        }

        LIMBSCALE = (AppData.Instance.userData == null || AppData.Instance.userData.rightArm) ? -1 : 1;
        isInitialized = true;
    }

    void FixedUpdate()
    {
        if (!isInitialized) return;
        updatePlayerPosition();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        pongGameController.Instance.BallHitted();
    }

    private void updatePlayerPosition()
    {
        if (Mathf.Abs(MarsComm.angle1) <= (Mathf.Abs(MovementSceneHandler.initialAngle) - 20))
        {
            // inactive state → yellow
            player.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 0, 255);
            return;
        }

        // active → green
        player.GetComponent<SpriteRenderer>().color = new Color32(0, 255, 0, 255);

        // Get Y position from robot
        endPoint = MarsComm.epPosInThePlane;
        yEndPoint = endPoint.y;

        // Convert robot Y → Unity Y
        yPoint = robotYToUnityY(yEndPoint);

        // Target position (only vertical changes)
        Vector3 targetPos = new Vector3(
            transform.position.x,
            Mathf.Clamp(yPoint, yScreenMin, yScreenMax),
            transform.position.z
        );

        // Smooth movement
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            smoothSpeed * Time.fixedDeltaTime
        );

        if (transform.position.y > topBound)
            transform.position = new Vector3(transform.position.x, topBound, 0);
        else if (transform.position.y < bottomBound)
            transform.position = new Vector3(transform.position.x, bottomBound, 0);
    }
}
