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
    public MarsArom currRom;

    //default values
    public static float yMinendPnt;
    public static float yMaxendPnt;
    public static float zMinendPnt;
    public static float zMaxendPnt;
    Animator anim;
    Vector3 normalscale;
    Vector3 scaleupscale;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }
    void Start()
    {
        idle();
        MarsComm.sendHeartbeat();
        //GET ROM DATA

        currRom = AppData.Instance.selectedMovement.currentArom;
        zMinendPnt = currRom.leftAdjusted.x;
        zMaxendPnt = currRom.rightAdjusted.x;
        yMinendPnt = currRom.bottomAdjusted.y;
        yMaxendPnt = currRom.topAdjusted.y;

        OFFSET = AppData.Instance.userData.rightArm ? -1 : 1;

    }
    private void FixedUpdate()
    {
        MarsComm.sendHeartbeat();
        endPoint = MarsComm.epPosInThePlane;
        yEndPoint = endPoint.y;
        zEndPoing = endPoint.z;
        xPoint = ((xMin + xMax) / 2.0f + (xMax - xMin) / (zMaxendPnt - zMinendPnt) * (zEndPoing - ((zMinendPnt + zMaxendPnt) / 2.0f)));
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
    //private void OnTriggerEnter2D(Collider2D collision)
    //{
    //    MoleControllerN mole = collision.GetComponent<MoleControllerN>();

    //    // Make sure it's the current active mole
    //    if (mole != null && mole == WAMGameController.Instance.currnetMole && WAMGameController.Instance.gameState == WAMGameController.GameStates.WAITFORHIT)
    //    {
    //        StartCoroutine(HitMoleAfterDelay(mole));
    //    }
    //}
    private IEnumerator HitMoleAfterDelay(MoleControllerN mole)
    {
        WAMGameController.Instance.isProcessingHit = true; // Pause the timer
        WAMGameController.Instance.setSuccess();

        yield return new WaitForSeconds(0.7f); // User must hold position
        hit();
        yield return new WaitForSeconds(0.2f);
        WAMGameController.Instance.moleHitSound.Play();
        mole.PlayHit();
        WAMGameController.Instance.isProcessingHit = false; // Resume (though state will change)
    }

    private Dictionary<MoleControllerN, Coroutine> activeTimers = new Dictionary<MoleControllerN, Coroutine>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        MoleControllerN mole = collision.GetComponent<MoleControllerN>();

        if (mole != null && mole == WAMGameController.Instance.currnetMole
            && WAMGameController.Instance.gameState == WAMGameController.GameStates.WAITFORHIT)
        {
            // Start timer when collision begins
            if (!activeTimers.ContainsKey(mole))
            {
                Coroutine timer = StartCoroutine(CheckHoldForSuccess(mole));
                activeTimers[mole] = timer;

                // Scale up mole for visual feedback
                mole.transform.localScale *= 1.1f;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        MoleControllerN mole = collision.GetComponent<MoleControllerN>();

        if (mole != null && activeTimers.ContainsKey(mole))
        {
            // Cancel timer if pointer leaves early
            StopCoroutine(activeTimers[mole]);
            activeTimers.Remove(mole);

            // Reset scale
            mole.transform.localScale = Vector3.one;
            WAMGameController.Instance.isProcessingHit = false;
        }
    }

    private IEnumerator CheckHoldForSuccess(MoleControllerN mole)
    {
        WAMGameController.Instance.isProcessingHit = true;
        yield return new WaitForSeconds(1f); // must stay for 1 second

        if (mole != null && mole == WAMGameController.Instance.currnetMole
            && WAMGameController.Instance.gameState == WAMGameController.GameStates.WAITFORHIT)
        {
            // Success
           
            WAMGameController.Instance.setSuccess();

            hit();
          
            WAMGameController.Instance.moleHitSound.Play();
            mole.PlayHit();

            WAMGameController.Instance.isProcessingHit = false;
        }

        // Reset scale after success
        mole.transform.localScale = Vector3.one;
        activeTimers.Remove(mole);
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
