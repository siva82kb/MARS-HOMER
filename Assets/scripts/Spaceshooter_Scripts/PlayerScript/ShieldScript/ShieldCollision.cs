
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldCollision : MonoBehaviour
{
    // Start is called before the first frame update
    private PlayerScore ps;
    void Start()
    {
       ps = FindObjectOfType<PlayerScore>(); 
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnTriggerEnter2D(Collider2D other){
        if(other.CompareTag("EnemyLaser")){
            Destroy(other.gameObject);
        }
        else if( other.CompareTag("Enemy")){
            Destroy(other.gameObject);
            ps.AddScore();
        }
        else if( other.CompareTag("Asteroid")){
            Destroy(other.gameObject);
            ps.AsteroidScore();
        }
    }
}
