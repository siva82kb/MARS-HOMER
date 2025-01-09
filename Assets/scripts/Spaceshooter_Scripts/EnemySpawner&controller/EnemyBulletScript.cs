using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBulletScript : MonoBehaviour
{
    
    public float speed = 2f;
    
    private GameManagerScript gm;
    void Start()
    {
        gm = FindObjectOfType<GameManagerScript>();
    }

    // Update is called once per frame
    void Update()
    {
        Move();
    }
    void Move()
    {
        if (gm != null && gm.isGameOver)
        {
            return; // Stop spawning when the game is over

        }
        Vector3 temp = transform.position;
        temp.y -= speed * Time.deltaTime;
        transform.position = temp;
    }
}
