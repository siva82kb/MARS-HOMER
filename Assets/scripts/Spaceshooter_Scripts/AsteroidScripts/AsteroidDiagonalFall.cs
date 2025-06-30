using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidDiagonalFall : MonoBehaviour
{
     public float fallSpeed = 1.5f; // Speed at which the asteroid falls
    public Vector2 fallDirection = new Vector2(-1, -1); // Direction of fall (-1, -1 for diagonal left-down)

    void Start()
    {
        // Normalize the fall direction to ensure consistent speed
        fallDirection = fallDirection.normalized;
    }

    void Update()
    {
        // Move the asteroid diagonally
        transform.Translate(fallDirection * fallSpeed * Time.deltaTime);
    }

    // Method to change speed dynamically during the game
    public void SetFallSpeed(float newSpeed)
    {
        fallSpeed = newSpeed;
    }

    // Method to change the fall direction dynamically
    public void SetFallDirection(Vector2 newDirection)
    {
        fallDirection = newDirection.normalized;
    } 
}
