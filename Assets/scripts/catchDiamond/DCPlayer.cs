
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DCPlayer : MonoBehaviour
{

    // Start is called before the first frame update
    public static DCPlayer instance;
    
    // Screen limits
    private float[] screenBounds;
    private Vector3 endPoint;
    public static float xScreenMin, yScreenMin, xScreenMax, yScreenMax;
    public static float xScreenMidPoint, yScreenMidPoint;
    public static float xScreenRange, yScreenRange;
    // Robot endpoint
    float yEndPoint, zEndPoint;
    float xPoint, yPoint;
    
    public Vector3 lastPosition;
    private bool debug;

    // Points of the quadrilateral
    private Vector2 top;
    private Vector2 bottom;
    private Vector2 left;
    private Vector2 right;
    public Vector2 x1;
    public Vector2 x2;
    public Vector2 y1;
    public Vector2 y2;

    // Robot AROM limits values.
    public static float zEndPointMin;
    public static float zEndPointMax;
    public static float zEndPointMid;
    public static float zEndPointRange;
    public static float yEndPointMin;
    public static float yEndPointMax;
    public static float yEndPointMid;
    public static float yEndPointRange;
    public int LIMBSCALE;
   
   //UI related Variables
    public GameObject pointTextPrefab;
    public Canvas uiCanvas;
    public LineRenderer test;

    // Target generation variables.
    private float[] prevTargetSelection = new float[] { 0f, 0f, 0f, 0f };
    private float prevTargetSelectionSum => prevTargetSelection[0] + prevTargetSelection[1] + prevTargetSelection[2] + prevTargetSelection[3];
    private float[] currTargetSelection = new float[] { 0f, 0f, 0f, 0f };
    private float[] alphas = new float[] { 0f, 0f, 0f, 0f };

    void Awake() => instance = this;
    
    void Start()
    {
        lastPosition = transform.position;

        // Initialize the robot to screen mapping variables.
        Initialize();

        // Initialize previous and current target selection.
        prevTargetSelection = new float[] { 1f, 0f, 0f, 0f };
        currTargetSelection = new float[] { 1f, 0f, 0f, 0f };
    }
    
    public void Initialize()
    {
        // Get screen bounds
        screenBounds = MarsGameDefs.SCREEN_LIMITS["DC"];
        xScreenMin = screenBounds[0];
        xScreenMax = screenBounds[1];
        yScreenMin = screenBounds[2];
        yScreenMax = screenBounds[3];
        // Compute midpoints and ranges
        xScreenMidPoint = (xScreenMin + xScreenMax) / 2.0f;
        xScreenRange = xScreenMax - xScreenMin;
        yScreenMidPoint = (yScreenMin + yScreenMax) / 2.0f;
        yScreenRange = yScreenMax - yScreenMin;

        // Get the current AROM data.
        // Check of selected movement is null
        if (AppData.Instance.selectedMovement == null || AppData.Instance.selectedMovement.currentArom == null)
        {
            AppLogger.LogError("Selected movement or current AROM is null in DCPlayer. Setting endpoint limits to default robot values.");
            zEndPointMin = MarsDefs.EPMINZ;
            zEndPointMax = MarsDefs.EPMAXZ;
            zEndPointMid = MarsDefs.EPCENTERZ;
            yEndPointMin = MarsDefs.EPMINY;
            yEndPointMax = MarsDefs.EPMAXY;
            yEndPointMid = MarsDefs.EPCENTERY;
            zEndPointRange = zEndPointMax - zEndPointMin;
            yEndPointRange = yEndPointMax - yEndPointMin;
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

    private void FixedUpdate()
    {
        endPoint = MarsComm.epPosInThePlane;
        yEndPoint = endPoint.y;
        zEndPoint = endPoint.z;

        Vector3 targetPosition = new Vector3(
            Mathf.Clamp(robotToUnityX(endPoint.z), xScreenMin, xScreenMax),
            Mathf.Clamp(robotToUnityY(endPoint.y), yScreenMin, yScreenMax),
            0f
        );
        // Smoothly interpolate from current to target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, 0.8f);
    }

    // Update is called once per frame
    void Update()
    {

        if (DCGameController.Instance.target != null)
            GetComponent<SpriteRenderer>().flipX = !DCGameController.Instance.target.GetComponent<SpriteRenderer>().flipX;
        if (!debug) return;
        float moveX = Input.GetAxis("Mouse X");
        float moveY = Input.GetAxis("Mouse Y");

        if (Input.GetMouseButton(0))
        {
            Vector3 temp = transform.position;
            // Move proportional to mouse movement
            temp.x += moveX * 50 * Time.deltaTime;
            temp.y += moveY * 50 * Time.deltaTime;
            transform.position = temp;
        }
    }

    private float robotToUnityX(float z) => LIMBSCALE * (xScreenMidPoint + xScreenRange * (z - zEndPointMid) / zEndPointRange);
    private float robotToUnityY(float y) => yScreenMidPoint + yScreenRange * (y - yEndPointMid) / yEndPointRange;

    public (UnityEngine.Vector2 endPointTarget, UnityEngine.Vector2 gameTarget) GenerateNextRandomTarget()
    {
        // Generate current target selection so that there is less than 100% overlap with previous target selection.
        GenerateNewTargetSelection(1f);

        // Generate perturbed scalars for convex combination.
        GenerateScalarsForConvexCombination();

        // Target in the robot/task space.
        UnityEngine.Vector2 endPointTarget = alphas[0] * AppData.Instance.selectedMovement.currentArom.topAdjusted
                        + alphas[1] * AppData.Instance.selectedMovement.currentArom.rightAdjusted
                        + alphas[2] * AppData.Instance.selectedMovement.currentArom.leftAdjusted
                        + alphas[3] * AppData.Instance.selectedMovement.currentArom.bottomAdjusted;

        UnityEngine.Vector2 gameTarget = new UnityEngine.Vector2(robotToUnityX(endPointTarget.x), robotToUnityY(endPointTarget.y));

        // Convert to Unity space.
        return (endPointTarget, gameTarget);
    }

    private void GenerateScalarsForConvexCombination()
    {
        float sum = 0;
        for (int i = 0; i < 4; i++)
        {
            alphas[i] = currTargetSelection[i] + UnityEngine.Random.Range(0f, 0.25f);
            sum += alphas[i];
        }

        // Normalized alphas
        for (int i = 0; i < alphas.Length; i++) alphas[i] /= sum;
    }

    private void GenerateNewTargetSelection(float minOverlap)
    {
        // Generate current target selection so that there is less than 100% overlap with previous target selection.
        float overlap = 0;
        do
        {
            overlap = 0;
            for (int i = 0; i < 4; i++)
            {
                currTargetSelection[i] = UnityEngine.Random.Range(0, 2);
                overlap += currTargetSelection[i] * prevTargetSelection[i];
            }
            // Percent overlap
            overlap /= prevTargetSelectionSum;
        } while (overlap >= minOverlap);

        // Update previous target selection.
        prevTargetSelection = (float[])currTargetSelection.Clone();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (DCGameController.Instance.gameState != DCGameController.GameStates.FAILURE)
            DCGameController.Instance.SetPlayerIn();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (DCGameController.Instance.gameState == DCGameController.GameStates.PLAYERIN && DCGameController.Instance.gameState != DCGameController.GameStates.PLAYEREXIT)
            DCGameController.Instance.SetPlayerOut();
    }

    public void AddScore()
    {
        // Instantiate near the player position
        GameObject obj = Instantiate(pointTextPrefab, uiCanvas.transform);

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.0f);//just above the sheep
        obj.transform.position = screenPos;

        obj.GetComponent<point>().SetText("+1");
    }

}
