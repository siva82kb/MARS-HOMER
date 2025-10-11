using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class player : MonoBehaviour
{

    // Start is called before the first frame update
    public static player instance;
    public float speed = 4f;
    //screenvalues
    public float xMin, xMax, yMin, yMax;
    float yEndPoint, zEndPoing;
    float xPoint, yPoint;
    private Vector3 endPoint;
    public int OFFSET;
    public MarsArom currRom;
    public Vector3 lastPosition;
    //default values
    public static float yMinendPnt;
    public static float yMaxendPnt;
    public static float zMinendPnt;
    public static float zMaxendPnt;
    public bool debug;
    private bool isColliding = false;
    private Coroutine eatingCoroutine;
    public GameObject highlightPrefeb;
    private GameObject highlight;
    public GameObject pointTextPrefab;
    public Canvas uiCanvas;  // assign the main Canvas here
    public ParticleSystem hightlightPartical;
    public ParticleSystem moveingupstars;
    public ParticleSystem moveingupstarsobj;
    public ParticleSystem highligtsobj;
    public GameObject SuccesstimerPrefeb;
    public GameObject Successtimer;
    void Awake()
    {
        instance = this;

    }
    void Start()
    {
        debug = DCGameController.Instance.debug;
        lastPosition = transform.position;
        if (debug) return;

        MarsComm.sendHeartbeat();
        //GET ROM DATA
        currRom = AppData.Instance.selectedMovement.currentArom;
        zMinendPnt = currRom.rightAdjusted.x;
        zMaxendPnt = currRom.leftAdjusted.x;
        yMinendPnt = currRom.bottomAdjusted.y;
        yMaxendPnt = currRom.topAdjusted.y;

        OFFSET = AppData.Instance.userData.limb == 1 ? -1 : 1;
    }
    private void FixedUpdate()
    {
        if (debug) return;
        MarsComm.sendHeartbeat();
        endPoint = MarsComm.epPosInThePlane;
        yEndPoint = endPoint.y;
        zEndPoing = endPoint.z;
        xPoint = ((xMin + xMax) / 2.0f + (xMax - xMin) / (zMaxendPnt - zMinendPnt) * (zEndPoing - ((zMinendPnt + zMaxendPnt) / 2.0f)));
        yPoint = -((yMin + yMax) / 2.0f - (yMax - yMin) / (yMaxendPnt - yMinendPnt) * (yEndPoint - ((yMinendPnt + yMaxendPnt) / 2.0f)));

        Vector3 targetPosition = new Vector3(
            Mathf.Clamp(xPoint, xMin, xMax),
            Mathf.Clamp(yPoint, yMin, yMax),
            0f
        );

        // Smoothly interpolate from current to target position
        float smoothSpeed = 10f; // Adjust this for more or less smoothing
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

    }
    // Update is called once per frame
    void Update()
    {
        //sheepChangeMovingDirection();
        if (DCGameController.Instance.target != null)
            GetComponent<SpriteRenderer>().flipX = !DCGameController.Instance.target.GetComponent<SpriteRenderer>().flipX;
        if (!debug) return;
        float moveX = Input.GetAxis("Mouse X");
        float moveY = Input.GetAxis("Mouse Y");
        //if (isColliding) return;
        if (Input.GetMouseButton(0))
        {
            Vector3 temp = transform.position;

            // Move proportional to mouse movement
            temp.x += moveX * 50 * Time.deltaTime;
            temp.y += moveY * 50 * Time.deltaTime;

            transform.position = temp;
        }

    }
    public void sheepChangeMovingDirection()
    {
        // Calculate movement direction
        float moveDirection = transform.position.x - lastPosition.x;

        if (moveDirection > 0) // Moving right
            GetComponent<SpriteRenderer>().flipX = false;
        else if (moveDirection < 0) // Moving left
            GetComponent<SpriteRenderer>().flipX = true;

        lastPosition = transform.position; // Update last position
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Debug.Log(collision.gameObject.name);
        //if (DCGameController.Instance.targetBubble != null)
        //    DCGameController.Instance.targetBubble.SetActive(false);
        //// Check if this is the target object
        //if (collision.gameObject == DCGameController.Instance.target && !DCGameController.Instance.isSuccess)
        //{
        //    isColliding = true;
       
        DCGameController.Instance.setPlayerIn();
            //destroyParticals();
            //Successtimer = Instantiate(SuccesstimerPrefeb, DCGameController.Instance.target.transform.position, Quaternion.identity);
            //moveingupstarsobj = Instantiate(moveingupstars, DCGameController.Instance.target.transform.position, Quaternion.identity);
            ////highligtsobj = Instantiate(hightlightPartical, DCGameController.Instance.target.transform.position, Quaternion.identity);

            //moveingupstarsobj.Play();
            //highligtsobj.Play();

        //    if (eatingCoroutine == null)
        //    {
        //        eatingCoroutine = StartCoroutine(DCGameController.Instance.PlayEatingAnimation());
        //    }
        //}
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
       
        if (DCGameController.Instance.gameState == DCGameController.GameStates.PLAYERIN)
            DCGameController.Instance.setPlayerOut();
        
    }
    public void destroyParticals()
    {
         Destroy(highligtsobj);
        Destroy(moveingupstarsobj);
    }
    public void AddScore()
    {
        // Instantiate near the player position
        GameObject obj = Instantiate(pointTextPrefab, uiCanvas.transform);

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.0f);//just above the sheep
        obj.transform.position = screenPos;

        obj.GetComponent<point>().SetText("+1");
    }
    public bool IsCollidingWithTarget()
    {
        return isColliding;
    }
}
