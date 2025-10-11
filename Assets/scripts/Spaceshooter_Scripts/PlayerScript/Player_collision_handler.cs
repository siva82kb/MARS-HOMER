using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Player_collision_handler : MonoBehaviour
{
    private Animator animator;
    private bool isDestroyed = false; // To ensure animation plays only once
    public static Player_collision_handler instance;    
    public AudioClip ExplosionSound;
    private AudioSource audioSource;
   
    public float blinkDuration = 2f; // Duration for blinking effect
    public float blinkInterval = 0.1f; // Time between blinks
    private bool isBlinking = false;
    private Renderer playerRenderer;
    public bool isHit = false;
    public void Awake()
    {
        instance = this;
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
       
        playerRenderer = GetComponent<Renderer>();

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the player is hit by an asteroid
        //if ((other.CompareTag("Asteroid") && !isDestroyed) || (other.CompareTag("Enemy") && !isDestroyed) || (other.CompareTag("EnemyLaser") && !isDestroyed))
        if ((other.CompareTag("Asteroid") && !isDestroyed))//check if any asteroid or enemy hit the player 
        {
            audioSource.PlayOneShot(ExplosionSound);
            //StartCoroutine(Blink());
            isHit = true;
            spaceShooterGameContoller.Instance.nFailure++;
            spaceShooterGameContoller.Instance.setisFailure();
           
            Destroy(other.gameObject); //  destroy  the asteroid


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
