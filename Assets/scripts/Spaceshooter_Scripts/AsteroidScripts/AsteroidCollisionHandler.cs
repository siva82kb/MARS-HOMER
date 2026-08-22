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
        if (!other.CompareTag("Laser") || isDestroyed) return;

        isDestroyed = true;
        SpaceShooterGameContoller.Instance.setIsSuccess();
        gameObject.transform.localScale = new Vector3(0.33f, 0.33f, 0);

        if (animator == null) animator = GetComponent<Animator>();
        if (animator != null)
            animator.Play("Asteroidexplosion", -1, 0f);

        if (audioSource != null && ExplosionSound != null)
            audioSource.PlayOneShot(ExplosionSound);

        AsteroidSpawner.Instance.currentAsteroid = null;
        Destroy(other.gameObject);

        StartCoroutine(DestroyAfterDelay(0.5f)); // let animation play
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }


    public void DestroyAsteroid()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);// Destroy the asteroid
    }
}
