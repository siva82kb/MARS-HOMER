using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PointerController : MonoBehaviour
{
    // Start is called before the first frame update
    
    public float speed = 4f;
    //screenvalues
    public float xMin, xMax, yMin, yMax;
    float yEndPoint, zEndPoing;
    float xPoint, yPoint;
    private Vector3 endPoint;
    public int OFFSET;
    public float[] currRom;

    //default values
    public static float yMinendPnt;
    public static float yMaxendPnt;
    public static float zMinendPnt;
    public static float zMaxendPnt;
    Animator anim;


    void Awake()
    {
        anim = GetComponent<Animator>();
    }
    void Start()
    {
        idle();
        MarsComm.sendHeartbeat();
        //GET ROM DATA
        currRom = AppData.Instance.selectedMovement.CurrentArom;
        zMinendPnt = currRom[0];
        zMaxendPnt = currRom[1];
        yMinendPnt = currRom[2];
        yMaxendPnt = currRom[3];

        OFFSET = AppData.Instance.userData.rightArm ? -1 : 1;
    }
    private void FixedUpdate()
    {
        MarsComm.sendHeartbeat();
        endPoint = MarsComm.epPosInThePlane;
        yEndPoint = endPoint.y;
        zEndPoing = endPoint.z;
        xPoint = OFFSET * ((xMin + xMax) / 2.0f + (xMax - xMin) / (zMaxendPnt - zMinendPnt) * (zEndPoing - ((zMinendPnt + zMaxendPnt) / 2.0f)));
        yPoint = -((yMin + yMax) / 2.0f - (yMax - yMin) / (yMaxendPnt - yMinendPnt) * (yEndPoint - ((yMinendPnt + yMaxendPnt) / 2.0f)));

        transform.position = new Vector3(Mathf.Clamp(xPoint, xMin, xMax),
            Mathf.Clamp(yPoint, yMin, yMax),
            0f);
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetAxisRaw("Horizontal") > 0f)
        {
            Vector3 temp = transform.position;
            temp.x += speed * Time.deltaTime;

            if (temp.x > xMax)
                temp.x = xMax;
            transform.position = temp;

        }
        else if (Input.GetAxisRaw("Horizontal") < 0f)
        {
            Vector3 temp = transform.position;
            temp.x -= speed * Time.deltaTime;

            if (temp.x < xMin)
                temp.x = xMin;
            transform.position = temp;
        }
        if (Input.GetAxisRaw("Vertical") > 0f)
        {
            Vector3 temp = transform.position;
            temp.y += speed * Time.deltaTime;

            if (temp.y > yMax)
                temp.y = yMax;
            transform.position = temp;
        }
        else if (Input.GetAxisRaw("Vertical") < 0f)
        {
            Vector3 temp = transform.position;
            temp.y -= speed * Time.deltaTime;

            if (temp.y < yMin)
                temp.y = yMin;
            transform.position = temp;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("hiut");
        Debug.Log(collision.gameObject.name);
        if (collision.gameObject.name == WAMGameController.Instance.currnetMole.gameObject.name)
        {
            hit();
            WAMGameController.Instance.currnetMole.PlayHit();
            WAMGameController.Instance.setSuccess();
          
        }

    }
  
    public void idle()
    {
        anim.Play("idleHammer", -1, 0f);
    }
   public void hit()
    {
        anim.Play("hammer", -1, 0f);
    }
}
