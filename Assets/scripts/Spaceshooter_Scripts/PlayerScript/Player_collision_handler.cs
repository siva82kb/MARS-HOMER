using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_collision_handler : MonoBehaviour
{
    private Animator animator;
    private bool isDestroyed = false; // To ensure animation plays only once
    private GameManagerScript gameManager;
    public AudioClip ExplosionSound;
    private AudioSource audioSource;
    

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        gameManager = FindObjectOfType<GameManagerScript>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the player is hit by an asteroid
        if ((other.CompareTag("Asteroid") && !isDestroyed)||(other.CompareTag("Enemy")&&!isDestroyed)||(other.CompareTag("EnemyLaser")&&!isDestroyed))//check if any asteroid or enemy hit the player 
        {
            // Trigger the explosion animation
            animator.SetTrigger("TriggerExplosion");
            if (audioSource != null && ExplosionSound != null)
            {
                audioSource.PlayOneShot(ExplosionSound);
            }

            // destroy the player GameObject after the animation 
            isDestroyed = true;

            gameManager.GameOver(); // Trigger game over
                                    //  destroy  the asteroid
            Destroy(other.gameObject);
        }
       
    }
    
}
