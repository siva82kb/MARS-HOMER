using UnityEngine;
using System.Collections;

public class BoundController : MonoBehaviour {

	// Enemy transform
	public Transform enemy;

	public static int enemyScore;
	public static int playerScore;
    public AudioClip[] audioClips; // win ,loose

    void Start(){
		enemyScore = 0;
		playerScore = 0;
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.gameObject.tag == "Target")
		{
			if (other.gameObject.GetComponent<Rigidbody2D>().velocity.x > 0)
			{
				playAudio(1);
				pongGameController.Instance.BallMissed();
			}
			else
			{
				playAudio(0);
			}
			// Destroys other object
			Destroy(other.gameObject);
		}
	}
	
    void playAudio(int clipNumber)
    {
        AudioSource audio = GetComponent<AudioSource>();
        audio.clip = audioClips[clipNumber];
        audio.Play();
    }
}
