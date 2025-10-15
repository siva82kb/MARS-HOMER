using UnityEngine;
using System.Collections;

public class BallController : MonoBehaviour {
	public static BallController instance;
	public float ballSpeed; 
	Rigidbody2D rigidBody2D;
    
    // Audio clip ballCollision,Win, Loose
    public AudioClip[] audioClips;
	
	private void Awake()
	{
		instance = this;
	}

	void Start()
	{
		//setting balls Rigidbody 2D
		rigidBody2D = this.gameObject.GetComponent<Rigidbody2D>();
		ballSpeed = pongGameController.Instance.gameSpeed;

		// Moving ball in initial direction and adding speed
		rigidBody2D.velocity = new Vector2(-1, 1) * ballSpeed;
	}

    void Update()
	{
        // If the the state is not playing, stop the ball and destroy it
		if (pongGameController.Instance.gameState == pongGameController.GameStates.STOP)
		{
			rigidBody2D.velocity = Vector2.zero;
			Destroy(this.gameObject);
		}
    }

    void playAudio(int clipNumber)
    {
        AudioSource audio = GetComponent<AudioSource>();
        audio.clip = audioClips[clipNumber];
        audio.Play();
    }

    void OnCollisionEnter2D(Collision2D col) {
        // Calculate enc1
        playAudio(0);
        // Tag check
        if (col.gameObject.tag == "Enemy") 
		{
			float y = launchAngle(transform.position, col.transform.position, col.collider.bounds.size.y);
			
			// Set enc1 and speed
			Vector2 dir = new Vector2(1, y).normalized;
			rigidBody2D.velocity = dir * ballSpeed * 1.5F;
		}
		if (col.gameObject.tag == "Player") 
		{
            // Calculate enc1
            float y = launchAngle(transform.position, col.transform.position, col.collider.bounds.size.y);

			// Set enc1 and speed
			Vector2 dir = new Vector2(-1, y).normalized;
			rigidBody2D.velocity = dir * ballSpeed * 1.5F;
		}
	}

	// Calculates the angle at which the ball hits the paddle
	float launchAngle(Vector2 ballPos, Vector2 paddlePos, float paddleHeight)
	{
		return 0.2f * Mathf.Sign(ballPos.y - paddlePos.y) + (ballPos.y - paddlePos.y) / paddleHeight;
	}
}
