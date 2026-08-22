using UnityEngine;
using System.Collections;

public class BallController : MonoBehaviour {
	public static BallController instance;
	public float ballSpeed;
	public static float playerPos = 6f, topWall = 5.5f, bottomWall = -5.5f, bounciness = 1.2f;
	Rigidbody2D rigidBody2D;
    
    // Audio clip ballCollision,Win, Loose
    public AudioClip[] audioClips;
	
	private void Awake()
	{
		instance = this;
	}

	void Start()
	{
        // Ensure wall bouncing works at any speed — Unity's default threshold (1.0)
        // suppresses bounciness when contact velocity is too low.
        Physics2D.bounceThreshold = 0.001f;

        rigidBody2D = this.gameObject.GetComponent<Rigidbody2D>();
        ballSpeed = pongGameController.Instance.gameSpeed;
        rigidBody2D.velocity = (AppData.Instance.userData.limb == 1 ? new Vector2(-1, 1) : new Vector2(1, -1)) * ballSpeed;
    }

    void Update()
	{
        MarsComm.sendHeartbeat();
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

			Vector2 dir = new Vector2(AppData.Instance.userData.limb == 1 ? 1 : -1, y).normalized;
			rigidBody2D.velocity = dir * ballSpeed;

			// Predict where it will reach player's side (x = +6)//Need to check
			float predictedY = PredictPlayerImpactOnY(playerPos, topWall, bottomWall, bounciness);
			pongGameController.Instance.targetEndPointPosition = new Vector3(0f, predictedY, 0f);
			//Debug.Log("Predicted hit Y on player side: " + predictedY);
		}
		if (col.gameObject.tag == "Player")
		{
            // Calculate enc1
            float y = launchAngle(transform.position, col.transform.position, col.collider.bounds.size.y);
			pongGameController.Instance.targetEndPointPosition = Vector3.zero;
			// Set enc1 and speed
			Vector2 dir = new Vector2(AppData.Instance.userData.limb == 1 ? -1 : 1, y).normalized;
			rigidBody2D.velocity = dir * ballSpeed;
		}
	}

    // Calculates the angle at which the ball hits the paddle
    float launchAngle(Vector2 ballPos, Vector2 paddlePos, float paddleHeight)
    {
        return 0.2f * Mathf.Sign(ballPos.y - paddlePos.y) + (ballPos.y - paddlePos.y) / paddleHeight;
    }
    
    float PredictPlayerImpactOnY(float xPlayer, float topWallY, float bottomWallY, float wallBounciness)
    {
        Vector2 pos = transform.position;
        Vector2 vel = rigidBody2D.velocity;

        if (vel.x <= 0)
            return float.NaN;

        float predictedY = pos.y;
        float predictedX = pos.x;

        // Simulate until the ball crosses the player's X position
        while (predictedX < xPlayer)
        {
            float timeToTop = (topWallY - predictedY) / vel.y;
            float timeToBottom = (bottomWallY - predictedY) / vel.y;

            // Time to reach player's X
            float timeToPlayer = (xPlayer - predictedX) / vel.x;

            // If it reaches player before a wall
            if ((vel.y > 0 && timeToPlayer < timeToTop) ||
                (vel.y < 0 && timeToPlayer < timeToBottom))
            {
                predictedY += vel.y * timeToPlayer;
                break;
            }

            // Otherwise bounce off a wall
            if (vel.y > 0) // hitting top
            {
                predictedY = topWallY - (topWallY - predictedY) + 0.001f; // move slightly below wall
                vel.y = -vel.y * wallBounciness; // reverse + amplify
                predictedX += vel.x * timeToTop;
            }
            else // hitting bottom
            {
                predictedY = bottomWallY - (bottomWallY - predictedY) - 0.001f; // move slightly above wall
                vel.y = -vel.y * wallBounciness;
                predictedX += vel.x * timeToBottom;
            }
        }

        return predictedY;
    }


	
}
