using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using static AppData;

public class Player_controller_s : MonoBehaviour
{
    Camera mainCamera;
    private Vector2 screenBounds;

    // Start is called before the first frame update
    public float speed = 4f;
    public float min_x, max_x, min_y, max_y;
    [SerializeField]
    private GameObject Player_bullet;

    public AudioClip laserSound;
    private AudioSource audioSource;

    [SerializeField]
    private Transform Spawn_point;
    public int[] DEPENDENT = new int[] { 0, -1, 1 };
    public static float xMin, yMin, xMax, yMax;
    public float threshold;
    public float ShootInterval = 2f;
    private float timeSinceLastShot = 0f;  // Timer to track intervals between shots
    private GameManagerScript gm;
    float th1, th2, th3, yMARS, zMARS;
    float xSS, ySS;
    public float tilt;

    public static float yMinMars = 175;
    public static float yMaxMars = 775;
    public static float zMinMars = 291 - 300;
    public static float zMaxMars = 291 + 300;
    public static new Vector3 playerPos;
    void Start()
    {

        mainCamera = Camera.main;
        //AppData.InitializeRobot();
        screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.transform.position.z));
        xMin = -0.9f * screenBounds.x;
        xMax = 0.9f * screenBounds.x;//changed into x from y
        yMin = -screenBounds.y / 4 + 0.2f;
        yMax = screenBounds.y - screenBounds.y / 0.5f;//changed into 0.5 from 1.5f
        threshold = MarsComm.DEPENDENT[AppData.useHand];
        getAssessmentData();
        audioSource = GetComponent<AudioSource>();
        gm = FindObjectOfType<GameManagerScript>();
    }
    public static void getAssessmentData()
    {
        UserData.dTableAssessment = DataManager.loadCSV(DataManager.filePathAssessmentData);
        DataRow lastRow = UserData.dTableAssessment.Rows[UserData.dTableAssessment.Rows.Count - 1];
        zMinMars = float.Parse((lastRow.Field<string>(AppData.minx)));
        zMaxMars = float.Parse((lastRow.Field<string>(AppData.maxx)));
        yMinMars = float.Parse((lastRow.Field<string>(AppData.miny)));
        yMaxMars = float.Parse((lastRow.Field<string>(AppData.maxy)));
       //Debug.Log(zMaxMars + "," + zMaxMars + "," + yMaxMars + "," + yMinMars);
        Debug.Log("working");
    }
    public void FixedUpdate()
    {
        th1 = threshold * MarsComm.angleOne;
        th2 = threshold * MarsComm.angleTwo;
        th3 = threshold * MarsComm.angleThree;
        yMARS = Mathf.Sin(th1) * (475.0f * Mathf.Cos(th2) + 291.0f * Mathf.Cos(th2 + th3));
        zMARS = (-475.0f * Mathf.Sin(th2) - 291.0f * Mathf.Sin(th2 + th3));
        xSS = DEPENDENT[AppData.useHand]* -((xMin + xMax) / 2.0f + (xMax - xMin) / (zMaxMars - zMinMars) * (zMARS - ((zMinMars + zMaxMars) / 2.0f)));
        ySS = ((yMin + yMax) / 2.0f - (yMax - yMin) / (yMaxMars - yMinMars) * (yMARS - ((yMinMars + yMaxMars) / 2.0f)));
        transform.position = new Vector3(Mathf.Clamp(xSS, xMin, xMax),
            Mathf.Clamp(ySS, yMin, yMax),
            -8.0f);
        playerPos = transform.position;
        Shoot_time(); 
    }
    // Update is called once per frame
    //void Update()
    //{
    //    MovePlayer();
    //    Shoot_time();
    //}
    void MovePlayer()
    {
        if (gm != null && gm.isGameOver)
        {
            return; // Stop spawning when the game is over

        }
        if (Input.GetAxisRaw("Horizontal") > 0f)
        {
            Vector3 temp = transform.position;
            temp.x += speed * Time.deltaTime;

            if (temp.x > max_x)
                temp.x = max_x;
            transform.position = temp;

        }
        else if (Input.GetAxisRaw("Horizontal") < 0f)
        {
            Vector3 temp = transform.position;
            temp.x -= speed * Time.deltaTime;

            if (temp.x < min_x)
                temp.x = min_x;
            transform.position = temp;
        }
        if (Input.GetAxisRaw("Vertical") > 0f)
        {
            Vector3 temp = transform.position;
            temp.y += speed * Time.deltaTime;

            if (temp.y > max_y)
                temp.y = max_y;
            transform.position = temp;
        }
        else if (Input.GetAxisRaw("Vertical") < 0f)
        {
            Vector3 temp = transform.position;
            temp.y -= speed * Time.deltaTime;

            if (temp.y < min_y)
                temp.y = min_y;
            transform.position = temp;
        }
    }
    void Shoot_time()
    {
        if (gm != null && gm.isGameOver)
        {
            return; // Stop spawning when the game is over

        }
        // Track time passed
        timeSinceLastShot += Time.deltaTime;

        // Check if it's time to shoot
        if (timeSinceLastShot >= ShootInterval)
        {
            Attack();
            timeSinceLastShot = 0f;  // Reset timer after shooting
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
        Destroy(Laser, 3.5f);
    }
    public void DestroyPlayer()
    {
        Destroy(gameObject);
    }
    private void OnApplicationQuit()
    {
        Application.Quit();
        JediComm.Disconnect();
    }
}
