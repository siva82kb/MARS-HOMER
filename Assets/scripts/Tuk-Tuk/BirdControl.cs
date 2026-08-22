using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BirdControl : MonoBehaviour
{
    public static BirdControl Instance;
    public static Rigidbody2D rb2d;
    public Image life;
    public FlappyGameControl FGC;

    public MarsArom currRom;

    public CapsuleCollider2D playercollider;
    public bool isInitialized { get; private set; } = false;

    // Screen boundaries
    public static float xScreenMin, yScreenMin, xScreenMax, yScreenMax;
    public static float yScreenMidPoint;
    public static float yScreenRange;
    public static float xScreenMidPoint;
    public static float xScreenRange;

    // Robot endpoint Y limits
    public static float yEndPointMin;
    public static float yEndPointMax;
    public static float yEndPointMid;
    public static float yEndPointRange;
    //Robot endpoint X limits
    public static float zEndPointMin;
    public static float zEndPointMax;
    public static float zEndPointMid;
    public static float zEndPointRange;
    public static int LIMBSCALE;
    
    float yPoint, yEndPoint;
    public  float unityYToRobotY(float y) =>((y / LIMBSCALE) - yScreenMidPoint) * (yEndPointRange / yScreenRange) + yEndPointMid;
    public  float robotYToUnityY(float y) =>(yScreenMidPoint + yScreenRange * (y - yEndPointMid) / yEndPointRange);
    public float unityXToRobotZ(float x) => ((x / LIMBSCALE) - xScreenMidPoint) * (zEndPointRange / xScreenRange) + zEndPointMid;



    public bool set = false;
    private bool isDead = false;

    int totalLife = 5;
    int currentLife = 0;
    bool columnHit;

    public float spriteBlinkingTimer = 0.0f;
    public float spriteBlinkingMiniDuration = 0.1f;
    public float spriteBlinkingTotalTimer = 0.0f;
    public float spriteBlinkingTotalDuration = 2f;
    public bool startBlinking = false;
    float targetAngle, position;
    float startTime, PLAYSIZE;
    float endTime;

    static float topBound = 6F;
    static float bottomBound = -3F;
    public static float playSize;
    private float[] screenBounds;
    private Vector3 endPoint;
    private float smoothSpeed = 20f;

    public GameObject player;

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
      
        playSize = topBound - bottomBound;
        currRom = AppData.Instance.selectedMovement.currentArom;

        // Get screen limits for the Pong (PP) scene
        screenBounds = MarsGameDefs.SCREEN_LIMITS["TT"];
        xScreenMin = screenBounds[0];
        xScreenMax = screenBounds[1];
        yScreenMin = screenBounds[2];
        yScreenMax = screenBounds[3];

        xScreenMidPoint = (xScreenMin + xScreenMax) / 2.0f;
        xScreenRange = xScreenMax - xScreenMin;

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
            zEndPointMin = currRom.leftAdjusted.x;
            zEndPointMax = currRom.rightAdjusted.x;
            zEndPointMid = (zEndPointMin + zEndPointMax) / 2.0f;
            zEndPointRange = zEndPointMax - zEndPointMin;
            yEndPointMin = currRom.bottomAdjusted.y;
            yEndPointMax = currRom.topAdjusted.y;
            yEndPointMid = (yEndPointMin + yEndPointMax) / 2.0f;
            yEndPointRange = yEndPointMax - yEndPointMin;

           
        }
        LIMBSCALE = (AppData.Instance.userData == null || AppData.Instance.userData.rightArm) ? -1 : 1;
        rb2d = GetComponent<Rigidbody2D>();
        isInitialized = true;

        playSize = playercollider.bounds.max.x;


    }
 
    void FixedUpdate()
    {
        if (startTime < 2)
        {
            startTime += Time.deltaTime;
        }
        if (startBlinking == true)
        {
            SpriteBlinkingEffect();
        }
        if (!isInitialized) return;
        updatePlayerPosition();
       
    }

    public void SpriteBlinkingEffect()
    {
        spriteBlinkingTotalTimer += Time.deltaTime;
        if (spriteBlinkingTotalTimer >= spriteBlinkingTotalDuration)
        {
            startBlinking = false;
            spriteBlinkingTotalTimer = 0.0f;
            this.gameObject.GetComponent<SpriteRenderer>().enabled = true;   
            return;
        }

        spriteBlinkingTimer += Time.deltaTime;
        if (spriteBlinkingTimer >= spriteBlinkingMiniDuration)
        {
            spriteBlinkingTimer = 0.0f;
            if (this.gameObject.GetComponent<SpriteRenderer>().enabled == true)
            {
                this.gameObject.GetComponent<SpriteRenderer>().enabled = false;  //make changes
            }
            else
            {
                this.gameObject.GetComponent<SpriteRenderer>().enabled = true;   //make changes
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {

        if (collision.gameObject.tag == "TopCollider" || collision.gameObject.tag == "BottomCollider")
        {
            
            startBlinking = true;
            columnHit = true;
           
        }
    }

    private void updatePlayerPosition()
    {
       
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
