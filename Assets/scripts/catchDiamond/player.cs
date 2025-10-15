
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
    Vector2 lastTarget;
    public GameObject highlightPrefeb;

    public GameObject pointTextPrefab;
    public Canvas uiCanvas;  // assign the main Canvas here
    public LineRenderer test;
    void Awake()
    {
        instance = this;

    }
    void Start()
    {
        debug = DCGameController.Instance.debug;
        lastPosition = transform.position;
        //DrawQuad(test, Color.green);

        if (debug) return;

        MarsComm.sendHeartbeat();
        //GET ROM DATA
        currRom = AppData.Instance.selectedMovement.currentArom;
        zMinendPnt = currRom.rightAdjusted.x;
        zMaxendPnt = currRom.leftAdjusted.x;
        yMinendPnt = currRom.bottomAdjusted.y;
        yMaxendPnt = currRom.topAdjusted.y;

        createVectors();
        OFFSET = AppData.Instance.userData.limb == 1 ? -1 : 1;
        DrawQuad(test, Color.green);
       

    }



    private void FixedUpdate()
    {
        //Debug.Log(robotToUnityX(currRom.topAdjusted.x) + "," + currRom.topAdjusted.x + "check");
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
    void DrawQuad(LineRenderer lr, Color color)

    {
        
        Vector3[] corners = new Vector3[5] 
        { 
            new Vector3(top.x, top.y, 0), 
            new Vector3(left.x, left.y, 0), 
            new Vector3(bottom.x, bottom.y, 0), 
            new Vector3(right.x, right.y), 
            new Vector3(top.x, top.y, 0), 
        };

        createFrame(lr, corners, color);
    }
    public void createFrame(LineRenderer lr, Vector3[] corners, Color color)
    {
        lr.positionCount = corners.Length;
        lr.startColor = lr.endColor = color;
        lr.SetPositions(corners);
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.loop = false;
        lr.useWorldSpace = true;
    }
    public void createVectors()
    {
        top = new Vector2(robotToUnityX(currRom.topAdjusted.x), Mathf.Max(robotToUnityY(currRom.topAdjusted.y),yMax));
        bottom = new Vector2(robotToUnityX(currRom.bottomAdjusted.x), Mathf.Min(robotToUnityY(currRom.bottomAdjusted.y),yMin));
        left = new Vector2(Mathf.Max(robotToUnityX(currRom.leftAdjusted.x),xMax), robotToUnityY(currRom.leftAdjusted.y));
        right = new Vector2(Mathf.Min(robotToUnityX(currRom.rightAdjusted.x),xMin), robotToUnityY(currRom.rightAdjusted.y));

        //genrate vector
        x1 = bottom - left;
        y1 = top - left;
        x2 = bottom - right;
        y2 = top - right;
    }
   

    public Vector2 getRandomTargt()
    {


        Vector2 t;
        Vector2 target;

        //rx+ry<=1
        float rx = UnityEngine.Random.Range(0, 1f);
        float ry = UnityEngine.Random.Range(0, (1f - rx));

        //find left or right
        int random = UnityEngine.Random.value < 0.5f ? -1 : 1;

        if (random == 1)
        {

            t = (rx * x1) + (ry * y1);
            target = t + left;

        }
        else
        {
            t = (rx * x2) + (ry * y2);
            target = t + right;
        }

        return target;

    }
    //private float robotToUnityX(float robotX) => 10f * ((robotX - MarsDefs.EPCENTERZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ));
    //private float robotToUnityY(float robotY) => 10f * ((robotY - MarsDefs.EPCENTERY) / (MarsDefs.EPMAXY - MarsDefs.EPMINY));
    private float robotToUnityX(float robotX)
    {
        float norm = (robotX - MarsDefs.EPMINZ) / (MarsDefs.EPMAXZ - MarsDefs.EPMINZ);
        return Mathf.Lerp(xMax, xMin, norm);
    }

    private float robotToUnityY(float robotY)
    {
        float norm = (robotY - MarsDefs.EPMINY) / (MarsDefs.EPMAXY - MarsDefs.EPMINY);
        return Mathf.Lerp(yMin, yMax, norm);
    }



    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(DCGameController.Instance.gameState != DCGameController.GameStates.FAILURE)
           DCGameController.Instance.setPlayerIn();

    }

    private void OnTriggerExit2D(Collider2D collision)
    {

        if (DCGameController.Instance.gameState == DCGameController.GameStates.PLAYERIN
            && DCGameController.Instance.gameState != DCGameController.GameStates.PLAYEREXIT
          )
            DCGameController.Instance.setPlayerOut();

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
