
using UnityEngine;

public class SSPlayerController : MonoBehaviour
{
    public static SSPlayerController instance;

    // Start is called before the first frame update
    Camera mainCamera;
    private Vector2 screenBounds;
    private Vector3 endPoint;

    [SerializeField]
    private GameObject Player_bullet;

    public AudioClip laserSound;
    private AudioSource audioSource;

    public bool isInitialized { get; private set; } = false;

    [SerializeField]
    private Transform Spawn_point;
    public int[] DEPENDENT = new int[] { 0, -1, 1 };

    public static float xScreenMin, yScreenMin, xScreenMax, yScreenMax;
    public static float xScreenMidPoint;
    public static float xScreenRange;

    private float ShootInterval = 0.5f;
    private float timeSinceLastShot = 0f;  // Timer to track intervals between shots
  
    float  yEndPoint, zEndPoint;
    float xPoint, yPoint;
    public float tilt;
    private float smoothSpeed = 20f;

    // Robot AROM limits values.
    public static float zEndPointMin;
    public static float zEndPointMax;
    public static float zEndPointMid;
    public static float zEndPointRange;
    public int LIMBSCALE;

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
        mainCamera = Camera.main;
        screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.transform.position.z));
        xScreenMin = -0.9f * screenBounds.x;
        xScreenMax = 0.9f * screenBounds.x;
        yScreenMin = -screenBounds.y + 0.2f;//change into 1 from 4
        yScreenMax = screenBounds.y - screenBounds.y / 1.5f;
        // Compute midpoints and ranges
        xScreenMidPoint = (xScreenMin + xScreenMax) / 2.0f;
        xScreenRange = xScreenMax - xScreenMin;

        // Get the audio source component
        audioSource = GetComponent<AudioSource>();

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
        xPoint = LIMBSCALE * (xScreenMidPoint + xScreenRange * (zEndPoint - zEndPointMid) / zEndPointRange);

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
        // Initiate Bullet
        shootTime();
    }


    void shootTime()
    {
        if (spaceShooterGameContoller.Instance == null 
            || spaceShooterGameContoller.Instance.isGameFinished
            || !spaceShooterGameContoller.Instance.isGameStarted
            )
        {
            return; // Stop shooting when the game is ove
        }
        if (Player_collision_handler.instance.isHit)
        {
            timeSinceLastShot = 0f;
            ShootInterval = 3f;
            Player_collision_handler.instance.isHit = false;

        }
      
        // Track time passed
        timeSinceLastShot += Time.deltaTime;

        // Check if it's time to shoot
        if (timeSinceLastShot >= ShootInterval)
        {
            Attack();
            timeSinceLastShot = 0f;  // Reset timer after shooting
            ShootInterval = 0.5f;
        }
    }
    void Attack()
    {
        GameObject Laser = Instantiate(Player_bullet, Spawn_point.position, Quaternion.identity);
        // Destroy laser after 5 seconds to prevent clutter
        if (audioSource != null && laserSound != null)
        {
            audioSource.PlayOneShot(laserSound);
        }
        Destroy(Laser, 1.5f);
    }
 
    public void DestroyPlayer()
    {
        Destroy(gameObject);
    }
   
}
