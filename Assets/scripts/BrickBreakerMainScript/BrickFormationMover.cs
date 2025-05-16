using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickFormationMover : MonoBehaviour
{
    public float targetY = 3.0f; // The Y position where the movement should stop
    public float speed = 0.5f;  // Speed at which the formation moves down

    private bool isMoving = true;
    private GamestartScript gs;

    void Start(){
        gs=FindObjectOfType<GamestartScript>();
    }

    void Update()
    {
        if(gs.GameStartPanel.activeInHierarchy){
        return;
       }
        if (isMoving)
        {
            // Move the formation down smoothly
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(transform.position.x, targetY, transform.position.z),
                speed * Time.deltaTime
            );

            // Stop moving when the target height is reached
            if (Mathf.Abs(transform.position.y - targetY) < 0.01f)
            {
                isMoving = false;
            }
        }
    }
}
