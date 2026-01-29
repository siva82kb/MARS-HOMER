using System;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Ball : MonoBehaviour
{
    int index;

    float fallSpeed;
    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        float distance =
            MarsGameDefs.MatchCatch.tarSTARTPOINT -
            MarsGameDefs.MatchCatch.tarENDPOINT;

        float exactTime = 13f; // seconds to reach basket
        fallSpeed = distance / exactTime;
    }

    public void setColorIndex(int index)
    {
        this.index = index;
    }

    void FixedUpdate()
    {
        rb.velocity = new Vector2(0, -fallSpeed);

        if (rb.position.y <= MarsGameDefs.MatchCatch.BOTTOMLIMIT)
        {
            if (!MCGameController.Instance.isSuccess &&
                !MCGameController.Instance.isFailure)
            {
                MCGameController.Instance.setIsFailure();
            }

            Destroy(gameObject);
        }
    }

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

    public void SetExactFallTime(float seconds)
    {
        float distance =
            MarsGameDefs.MatchCatch.tarSTARTPOINT -
            MarsGameDefs.MatchCatch.tarENDPOINT;

        fallSpeed = distance / seconds;
    }
}
