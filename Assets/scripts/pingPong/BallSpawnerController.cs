using UnityEngine;
using System.Collections;

public class BallSpawnerController : MonoBehaviour {

	public GameObject ball;
	public static Vector3 ballPos;
	GameObject ballClone;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (!pongGameController.Instance.isGamePlaying) return;
		if (pongGameController.Instance.gameSpeed == 0) return;
        if (transform.childCount == 0)
        {
            // GameObject ballClone;
            ballClone = Instantiate(ball, transform.position, transform.rotation);
            ballClone.transform.SetParent(transform);
        }
        ballPos = ballClone.transform.position;
	}  
}
