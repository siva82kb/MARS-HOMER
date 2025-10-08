using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class sheepController : MonoBehaviour
{

    // Start is called before the first frame update
    public static sheepController instance;
    public float speed = 4f;
    //screenvalues
    public float xMin, xMax, yMin, yMax;
    float yEndPoint, zEndPoing;
    float xPoint, yPoint;
    private Vector3 endPoint;
    public int OFFSET;
    public float[] currRom;
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

    void Awake()
    {
        instance = this;
       
    }
    void Start()
    {
        debug = GameController.Instance.debug;
        lastPosition = transform.position;
        if (debug) return;
        
        MarsComm.sendHeartbeat();
        //GET ROM DATA
        //currRom = AppData.Instance.selectedMovement.CurrentArom;
        //zMinendPnt = currRom[0];
        //zMaxendPnt = currRom[1];
        //yMinendPnt = currRom[2];
        //yMaxendPnt = currRom[3];

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
        float smoothSpeed = 5f; // Adjust this for more or less smoothing
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

    }
    // Update is called once per frame
    void Update()
    {
       //sheepChangeMovingDirection();
       if(GameController.Instance.target!=null)
            GetComponent<SpriteRenderer>().flipX = !GameController.Instance.target.GetComponent<SpriteRenderer>().flipX;
        if (!debug) return;
        float moveX = Input.GetAxis("Mouse X");
        float moveY = Input.GetAxis("Mouse Y");

        if (Input.GetMouseButton(0))
        {
            Vector3 temp = transform.position;

            // Move proportional to mouse movement
            temp.x += moveX * 50* Time.deltaTime;
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
        Debug.Log(collision.gameObject.name);

        // Check if this is the target object
        if (collision.gameObject == GameController.Instance.target)
        {
            isColliding = true;
            highlight = Instantiate(highlightPrefeb, GameController.Instance.target.transform.position, Quaternion.identity);
            // Start eating process only once
            if (eatingCoroutine == null)
            {
                eatingCoroutine = StartCoroutine(GameController.Instance.PlayEatingAnimation());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        isColliding = false;
        if (collision.gameObject == GameController.Instance.target)
        {
            isColliding = false;
            if(highlight != null)
            {
                Destroy(highlight);
            }
            // If player moves away while eating, cancel and mark as failure
            if (eatingCoroutine != null)
            {
                Debug.Log("Player moved away! Eating failed.");
                StopCoroutine(eatingCoroutine);
                // Immediately trigger failed animation
                GameController.Instance.target
                    .GetComponent<Animator>()
                    .Play("faild", -1, 0f);
                //Destroy(GameController.Instance.target);
                eatingCoroutine = null;
                GameController.Instance.isSuccess = false;
                GameController.Instance.isFailure = true;
            }
        }
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


