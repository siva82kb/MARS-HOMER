using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPaddleFollow : MonoBehaviour
{
    public Transform ball;
    public float followSpeed = 5f; // Speed at which the enemy paddle follows the ball
    public float leftscreenedge = -7f; // Left boundary
    public float rightscreenedge = 7f; // Right boundary

    private void Update()
    {
        if (ball != null)
        {
            if (ball.position.y <= transform.position.y)
            {
                // Smoothly follow the ball on the X-axis
                float targetX = Mathf.Lerp(transform.position.x, ball.position.x, followSpeed * Time.deltaTime);

                // Clamp the target position to stay within boundaries
                targetX = Mathf.Clamp(targetX, leftscreenedge, rightscreenedge);

                // Update the paddle's position
                transform.position = new Vector2(targetX, transform.position.y);
            }
        }
    }
}

