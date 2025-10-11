
using UnityEngine;

public class Player_controller_s : MonoBehaviour
{
    public static Player_controller_s instance;

    // Start is called before the first frame update
    Camera mainCamera;
    private Vector2 screenBounds;
    private Vector3 endPoint;

    [SerializeField]
    private GameObject Player_bullet;

    public AudioClip laserSound;
    private AudioSource audioSource;

    [SerializeField]
    private Transform Spawn_point;
    public int[] DEPENDENT = new int[] { 0, -1, 1 };

    public static float xMin, yMin, xMax, yMax;

    private float ShootInterval = 0.5f;
    private float timeSinceLastShot = 0f;  // Timer to track intervals between shots
  
    float  yEndPoint, zEndPoing;
    float xPoint, yPoint;
    public float tilt;
    public MarsArom currRom;

    //default values
    public static float yMinendPnt;
    public static float yMaxendPnt;
    public static float zMinendPnt;
    public static float zMaxendPnt;
    public int OFFSET;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        mainCamera = Camera.main;
        screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.transform.position.z));
        xMin = -0.9f * screenBounds.x;
        xMax = 0.9f * screenBounds.x;
        yMin = -screenBounds.y + 0.2f;//change into 1 from 4
        yMax = screenBounds.y - screenBounds.y / 1.5f;
        audioSource = GetComponent<AudioSource>();

        //GET ROM DATA
        currRom = AppData.Instance.selectedMovement.currentArom;
        zMinendPnt = currRom.leftAdjusted.x;
        zMaxendPnt = currRom.rightAdjusted.x;

        OFFSET = AppData.Instance.userData.rightArm ? -1 : 1;
    }

    private float smoothSpeed = 15f;

    public void FixedUpdate()
    {
        endPoint = MarsComm.epPosInThePlane;
        zEndPoing = endPoint.z;
        xPoint = OFFSET * (
            (xMin + xMax) / 2.0f +
            (xMax - xMin) / (zMaxendPnt - zMinendPnt) *
            (zEndPoing - ((zMinendPnt + zMaxendPnt) / 2.0f))
        );

       

        // target position
        Vector3 targetPos = new Vector3(
            Mathf.Clamp(xPoint, xMin, xMax),
            -4.0f,
            -8.0f
        );

        //// constant-speed movement (no delay)
        //transform.position = Vector3.MoveTowards(
        //    transform.position,
        //    targetPos,
        //    smoothSpeed * Time.fixedDeltaTime
        //);
        // smooth movement
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
