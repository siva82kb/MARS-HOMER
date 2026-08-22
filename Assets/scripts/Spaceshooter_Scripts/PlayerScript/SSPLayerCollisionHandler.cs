using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SSPLayerCollisionHandler : MonoBehaviour
{
    private Animator animator;
    // To ensure animation plays only once
    private bool isDestroyed = false; 
    public static SSPLayerCollisionHandler instance;    
    public AudioClip ExplosionSound;
    private AudioSource audioSource;

    // Duration for blinking effect
    public float blinkDuration = 2f;
    // Time between blinks
    public float blinkInterval = 0.1f;
    private bool isBlinking = false;
    private Renderer playerRenderer;
    public bool isHit = false;

    public void Awake() => instance = this;

    void Start()
    {
        // Get the different components
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        playerRenderer = GetComponent<Renderer>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!SpaceShooterGameContoller.Instance.isSuccess) return;
        // Check if the player is hit by an asteroid, and if not already destroyed.
        if (other.CompareTag("Asteroid") && !isDestroyed && !SpaceShooterGameContoller.Instance.isSuccess)
        {
            audioSource.PlayOneShot(ExplosionSound);
            SpaceShooterGameContoller.Instance.setIsFailure();
            Destroy(other.gameObject);
        }
    }
    
    IEnumerator Blink()
    {
        isBlinking = true;
        float endTime = Time.time + blinkDuration;

        while (Time.time < endTime)
        {
            // Toggle visibility
            playerRenderer.enabled = !playerRenderer.enabled;
            yield return new WaitForSeconds(blinkInterval);
        }
        // Ensure visibility is restored
        playerRenderer.enabled = true;
        isBlinking = false;
    }
    
}
