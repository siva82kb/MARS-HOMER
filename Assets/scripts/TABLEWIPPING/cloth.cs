using UnityEngine;
using static TWGameController;
using XCharts.Runtime;
using TMPro;

public class TWPlayer : MonoBehaviour
{
    public static TWPlayer instance; 
    // Screen limits
    private float[] screenBounds;
    private Vector3 endPoint;
    public static float xScreenMin, yScreenMin, xScreenMax, yScreenMax;
    public static float xScreenMidPoint, yScreenMidPoint;
    public static float xScreenRange, yScreenRange;
    // Robot endpoint
    public float yEndPoint, zEndPoint;
    float xPoint, yPoint;
    public TextMeshProUGUI co_ordinates;
    public Vector3 lastPosition;
    private SpriteRenderer clothsprite;
    public Vector2 x1; // Top corner in game space
    public Vector2 x2; // Right corner in game space
    public Vector2 y1; // Bottom corner in game space
    public Vector2 y2; // Left corner in game space

    // Robot AROM limits values.
    public float zEndPointMin;
    public float zEndPointMax;
    public float zEndPointMid;
    public float zEndPointRange;
    public float yEndPointMin;
    public float yEndPointMax;
    public float yEndPointMid;
    public float yEndPointRange;
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
        clothsprite = GetComponent<SpriteRenderer>();   
        // Initialize the robot to screen mapping variables.
        Initialize();

        // Initialize previous and current target selection.
        prevTargetSelection = new float[] { 1f, 0f, 0f, 0f };
        currTargetSelection = new float[] { 1f, 0f, 0f, 0f };
    }

    public void Initialize()
    {
        // Get screen bounds
        screenBounds = MarsGameDefs.SCREEN_LIMITS["TW"];
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
            AppLogger.LogError("Selected movement or current AROM is null in TWPlayer. Setting endpoint limits to default robot values.");
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
        //LIMBSCALE = -1;
        //clothsprite.flipX = true;
        //Set the appropriate scale
        LIMBSCALE = (AppData.Instance.userData == null || AppData.Instance.userData.rightArm) ? -1 : 1;
        clothsprite.flipX = AppData.Instance.userData.rightArm;

        // Convert ROM corners to game space
        UpdateROMCorners();
    }
    private void FixedUpdate()
    {
        if (TWGameController.Instance.debug) return;
        endPoint = MarsComm.epPosInThePlane;
        yEndPoint = endPoint.y;
        zEndPoint = endPoint.z;

        //co_ordinates.text = $"{zEndPoint}/{yEndPoint}";
        Vector3 targetPosition = new Vector3(
            Mathf.Clamp(robotToUnityX(endPoint.z), xScreenMin, xScreenMax),
            Mathf.Clamp(robotToUnityY(endPoint.y), yScreenMin, yScreenMax),
            0f
        );
        // Smoothly interpolate from current to target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, 0.8f);
    }
    void Update()
    {
        if (!TWGameController.Instance.debug) return;
        Vector3 mp = Input.mousePosition;
        mp.z = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mp);

        transform.position = worldPos;
    }
    private float robotToUnityX(float z) => LIMBSCALE * (xScreenMidPoint + xScreenRange * (z - zEndPointMid) / zEndPointRange);
    private float robotToUnityY(float y) => yScreenMidPoint + yScreenRange * (y - yEndPointMid) / yEndPointRange;
    public float unityYToRobotY(float y) => ((y / LIMBSCALE) - yScreenMidPoint) * (yEndPointRange / yScreenRange) + yEndPointMid;
    public float unityXToRobotZ(float x) => ((x / LIMBSCALE) - xScreenMidPoint) * (zEndPointRange / xScreenRange) + zEndPointMid;

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
    float DistanceToNearestEdge(Vector2 p)
    {
        float dx = Mathf.Min(p.x - xScreenMin, xScreenMax - p.x);
        float dy = Mathf.Min(p.y - yScreenMin, yScreenMax - p.y);

        return Mathf.Min(dx, dy);
    }


    public StainSize DecideStainSize(Vector2 gameTarget)
    {
        float distToEdge = DistanceToNearestEdge(gameTarget);

        float quadWidth = xScreenMax - xScreenMin;
        float quadHeight = yScreenMax - yScreenMin;

        float edgeThreshold = 0.2f * Mathf.Min(quadWidth, quadHeight);

        return (distToEdge <= edgeThreshold)
            ? StainSize.Small
            : StainSize.Big;
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

    private void UpdateROMCorners()
    {
        // Convert ROM corners from robot space to game space
        if (AppData.Instance.selectedMovement?.currentArom == null) return;

        x1 = new Vector2(robotToUnityX(AppData.Instance.selectedMovement.currentArom.topAdjusted.x),
                         robotToUnityY(AppData.Instance.selectedMovement.currentArom.topAdjusted.y));
        x2 = new Vector2(robotToUnityX(AppData.Instance.selectedMovement.currentArom.rightAdjusted.x),
                         robotToUnityY(AppData.Instance.selectedMovement.currentArom.rightAdjusted.y));
        y1 = new Vector2(robotToUnityX(AppData.Instance.selectedMovement.currentArom.bottomAdjusted.x),
                         robotToUnityY(AppData.Instance.selectedMovement.currentArom.bottomAdjusted.y));
        y2 = new Vector2(robotToUnityX(AppData.Instance.selectedMovement.currentArom.leftAdjusted.x),
                         robotToUnityY(AppData.Instance.selectedMovement.currentArom.leftAdjusted.y));
    }

    public void DrawROMQuad(LineRenderer lineRenderer)
    {
        // Draw the ROM quad for visualization using the provided LineRenderer
        if (lineRenderer == null)
        {
            AppLogger.LogWarning("LineRenderer is null, cannot draw ROM quad");
            return;
        }

        lineRenderer.positionCount = 5;
        lineRenderer.SetPosition(0, new Vector3(x1.x, x1.y, 0));
        lineRenderer.SetPosition(1, new Vector3(x2.x, x2.y, 0));
        lineRenderer.SetPosition(2, new Vector3(y1.x, y1.y, 0));
        lineRenderer.SetPosition(3, new Vector3(y2.x, y2.y, 0));
        lineRenderer.SetPosition(4, new Vector3(x1.x, x1.y, 0)); // Close the quad

        Debug.Log($"ROM Quad drawn - Corners: Top({x1.x}, {x1.y}) Right({x2.x}, {x2.y}) Bottom({y1.x}, {y1.y}) Left({y2.x}, {y2.y})");
    }

    public bool IsPositionWithinROMQuad(Vector2 position)
    {
        // Check if position is within ROM quad bounds
        float minX = Mathf.Min(x1.x, x2.x, y1.x, y2.x);
        float maxX = Mathf.Max(x1.x, x2.x, y1.x, y2.x);
        float minY = Mathf.Min(x1.y, x2.y, y1.y, y2.y);
        float maxY = Mathf.Max(x1.y, x2.y, y1.y, y2.y);

        bool withinBounds = position.x >= minX && position.x <= maxX &&
                           position.y >= minY && position.y <= maxY;

        if (!withinBounds)
        {
            AppLogger.LogWarning($"Stain spawned outside ROM quad! Position: ({position.x}, {position.y}), ROM bounds: X({minX}, {maxX}), Y({minY}, {maxY})");
        }

        return withinBounds;
    }
}

