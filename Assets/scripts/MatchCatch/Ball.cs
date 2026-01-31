using System;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Ball : MonoBehaviour
{
    int index;

    float fallSpeed;
    Rigidbody2D rb;
    float elapsed, duration;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        duration = 3f;
       
    }
    
    public void setColorIndex(int index)
    {
        this.index = index;
    }
    void Update()
    {

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float y = Mathf.Lerp(MarsGameDefs.MatchCatch.tarSTARTPOINT, MarsGameDefs.MatchCatch.BOTTOMLIMIT, t);
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
        if (transform.position.y <= MarsGameDefs.MatchCatch.BOTTOMLIMIT)
        {
            if (!MCGameController.Instance.isSuccess &&
                !MCGameController.Instance.isFailure)
            {
                MCGameController.Instance.setIsFailure();
            }

            Destroy(gameObject);
        }
    }
    //void FixedUpdate()
    //{

    //    rb.velocity = new Vector2(0, -fallSpeed);

    //    if (rb.position.y <= MarsGameDefs.MatchCatch.BOTTOMLIMIT)
    //    {
    //        if (!MCGameController.Instance.isSuccess &&
    //            !MCGameController.Instance.isFailure)
    //        {
    //            MCGameController.Instance.setIsFailure();
    //        }

    //        Destroy(gameObject);
    //    }



    //}

    private void OnCollisionEnter2D(Collision2D collision)
    {
        player basket = collision.gameObject.GetComponent<player>();
        if (basket == null) return;

        if (basket.lastColorIndex == index)
            MCGameController.Instance.setIsSuccess();
        else
            MCGameController.Instance.setIsFailure();

        Destroy(gameObject);
    }
    public void setFallTime(float newTime) => duration = newTime;


    public void SetExactFallTime(float seconds)
    {
        float distance =
            MarsGameDefs.MatchCatch.tarSTARTPOINT -
            MarsGameDefs.MatchCatch.tarENDPOINT;

        fallSpeed = distance / seconds;
    }
}
