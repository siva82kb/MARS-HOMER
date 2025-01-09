using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFall : MonoBehaviour
{
    // Start is called before the first frame update
    public float fallSpeed = 1.5f;  // Speed at which the asteroid falls
    private GameManagerScript gameManager;
    public float zigZagAmplitude = 10f;  // Amplitude of the zig-zag movement
    public float zigZagFrequency = 10f;  // Frequency of the zig-zag movement
    private float startTime;  // Tracks the starting time for sine wave calculation
    
  
     void Start()
    {
        startTime = Time.time;
        
    }

    void Update()
    {
         MovePlayer();
       
    }
void MovePlayer()
{
    // Move the asteroid downwards
    transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

    // Horizontal zig-zag movement using a sine wave
    float zigZagOffset = Mathf.Sin((Time.time - startTime) * zigZagFrequency) * zigZagAmplitude;

    // Target horizontal position
    float targetXPosition = Mathf.Clamp(zigZagOffset, -8f, 8f);

    // Smoothly transition to the target position using Mathf.Lerp
    float smoothXPosition = Mathf.Lerp(transform.position.x, targetXPosition, Time.deltaTime * 1f);

    // Apply the updated position
    transform.position = new Vector3(smoothXPosition, transform.position.y, transform.position.z);
}



    // void MovePlayer(){
    //     //  if (gameManager != null && gameManager.isGameOver)
    //     // {
    //     //     return; // Stop spawning when the game is over

    //     // }
    //     // Move the asteroid downwards
    //     transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
    //      // Horizontal zig-zag movement using sine wave
    //     float zigZagOffset = Mathf.Sin((Time.time - startTime) * zigZagFrequency) * zigZagAmplitude;

    //     // Calculate the new horizontal position
    //     float newXPosition = Mathf.Clamp(transform.position.x + zigZagOffset * Time.deltaTime, -8f, 8f);

    //     // Apply the updated position
    //     transform.position = new Vector3(newXPosition, transform.position.y, transform.position.z);
    // }

    //  method to change speed if needed while in game
    public void SetFallSpeed(float newSpeed)
    {
        fallSpeed = newSpeed;
    }
    
}
