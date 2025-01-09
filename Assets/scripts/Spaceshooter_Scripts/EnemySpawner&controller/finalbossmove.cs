using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinalBossMove : MonoBehaviour
{
    public Transform player; // Reference to the player GameObject
    public float speed = 0.2f; // Speed at which the boss moves
    public float smoothFactor = 0.01f; // Smoothing factor for movement

    void Update()
    {
        // Ensure the player reference is set
        if (player != null)
        {
            // Get the player's X position
            float targetX = player.position.x;

            // Smoothly move the boss toward the target X position
            float newX = Mathf.Lerp(transform.position.x, targetX, smoothFactor);

            // Update the boss's position, keeping Y and Z unchanged
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        }
    }
}
