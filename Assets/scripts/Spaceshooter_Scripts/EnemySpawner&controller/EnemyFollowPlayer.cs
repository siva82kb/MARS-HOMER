using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFollowPlayer : MonoBehaviour
{
     public float followSpeed = 1.5f;  // Speed at which the enemy follows the player
     private Transform player;        // Reference to the player's transform

    void Start()
    {
        // Find the player object by tag or assign it directly in the inspector
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    void Update()
    {
        FollowPlayer();
    }

    void FollowPlayer()
    {
        // Ensure we have a reference to the player
        if (player != null)
        {
            // Move the enemy towards the player's position
            transform.position = Vector3.MoveTowards(transform.position, player.position, followSpeed * Time.deltaTime);
        }
    }

    // Method to change follow speed if needed during gameplay
    public void SetFollowSpeed(float newSpeed)
    {
        followSpeed = newSpeed;
    }

}
