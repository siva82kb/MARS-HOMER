using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour {

	//Speed of the enemy
	public float speed = 10f;
 
    //the ball
    Transform ball;

	//the ball's rigidbody 2D
	Rigidbody2D ballRig2D;

	//bounds of enemy
	public float topBound = 4.5F;
	public float bottomBound = -4.5F;
	public float stopWatch;

	void Start()
	{
		// Continously Invokes Move every x seconds (values may differ)
		InvokeRepeating("Move", .02F, .02F);
	}
	
    private void OnCollisionEnter2D(Collision2D collision)
	{
		pongGameController.Instance.BallReturned();
    }
    
    // Movement for the paddle
    void Move ()
	{
		// Finding the ball
		if (!pongGameController.Instance.isGamePlaying) return;
        if (pongGameController.Instance.gameSpeed == 0) return;
		
        if (ball == null)
		{
			ball = GameObject.FindGameObjectWithTag("Target").transform==null?null: GameObject.FindGameObjectWithTag("Target").transform;
		}

		// Setting the ball's rigidbody to a variable
		ballRig2D = ball.GetComponent<Rigidbody2D>();

		// Checking x direction of the ball
		
		if (AppData.Instance.userData.limb == 1?ballRig2D.velocity.x < 0: ballRig2D.velocity.x > 0)
		{
			// Checking y direction of ball
			if (ball.position.y < this.transform.position.y - .3F)
			{
				// Move ball down if lower than paddle
				transform.Translate(Vector3.down * speed * Time.deltaTime);
			}
			else if (ball.position.y > this.transform.position.y + .3F)
			{
				// Move ball up if higher than paddle
				transform.Translate(Vector3.up * speed * Time.deltaTime);
			}
		}

		// Set bounds of enemy
		if (transform.position.y > topBound)
		{
			transform.position = new Vector3(transform.position.x, topBound, 0);
		}
		else if (transform.position.y < bottomBound)
		{
			transform.position = new Vector3(transform.position.x, bottomBound, 0);
		}
	}
}
