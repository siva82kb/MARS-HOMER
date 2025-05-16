using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFall : MonoBehaviour
{
    public float fallSpeed = 1.5f;  // Speed at which the enemy falls
    public float zigZagDuration = 1f; // Time to complete one zigzag (left to right or vice versa)
    public float minX = -7.5f; // Left boundary of the screen
    public float maxX = 7f; // Right boundary of the screen

    private float zigZagTimer; // Tracks time within a zigzag
    private int direction = 1; // 1 for right, -1 for left

    void Start()
    {
        zigZagTimer = 0f; // Initialize zigzag timer
    }

    void Update()
    {
        MoveEnemy();
    }

    void MoveEnemy()
    {
        // Move the enemy downward at a constant speed
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

        // Update zigzag timer
        zigZagTimer += Time.deltaTime;

        // Switch direction when zigzag duration is reached
        if (zigZagTimer >= zigZagDuration)
        {
            zigZagTimer = 0f;
            direction *= -1; // Toggle direction
        }

        // Calculate horizontal movement for zigzag
        float xOffset = direction * ((maxX - minX) / zigZagDuration) * Time.deltaTime;

        // Update position with zigzag and clamp to boundaries
        float newXPosition = Mathf.Clamp(transform.position.x + xOffset, minX, maxX);
        transform.position = new Vector3(newXPosition, transform.position.y, transform.position.z);
    }

    // Method to dynamically change fall speed
    public void SetFallSpeed(float newSpeed)
    {
        fallSpeed = newSpeed;
    }
}
