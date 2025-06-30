using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShieldDropScript : MonoBehaviour
{
    // Start is called before the first frame update
    private bool isDestroyed=false;
    public Transform Powerup_Shield;
    
    private int randomfall;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
     void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the collider is the player's laser
        if (other.CompareTag("Laser")  && !isDestroyed)
        {
             randomfall = Random.Range(1, 101);
            if (randomfall < 50)
            {
                Instantiate(Powerup_Shield, other.transform.position, other.transform.rotation);
            }

        }
        }
}
