using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidCollisionHandler : MonoBehaviour
{
    // Track if the asteroid is destroyed
    private bool isDestroyed = false;
    // Reference to the Animator component
    private Animator animator;
    public AudioClip ExplosionSound;
    private AudioSource audioSource;

    void Start()
    {
        // Get the Animator and AudioSource components
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check the other object is tagged as a laser
        if (other.CompareTag("Laser") && !isDestroyed)
        {
            SpaceShooterGameContoller.Instance.setIsSuccess();
            // Mark asteroid as destroyed
            isDestroyed = true;
            gameObject.transform.localScale = new Vector3(0.33f, 0.33f, 0);
            // Play destruction animation
            animator.SetTrigger("TriggerDestroy");
            if (audioSource != null && ExplosionSound != null) audioSource.PlayOneShot(ExplosionSound); 
            AsteroidSpawner.Instance.currentAsteroid = null;
            Destroy(other.gameObject); // Destroy the laser
        }
    }

    public void DestroyAsteroid()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);// Destroy the asteroid
    }
}
