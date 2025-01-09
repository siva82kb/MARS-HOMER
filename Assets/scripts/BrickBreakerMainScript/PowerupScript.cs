using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerupScript : MonoBehaviour
{
    public float speed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
      transform.Translate(new Vector2(0f,-1f)*Time.deltaTime*speed);  

      if(transform.position.y<-5.3f){
        Destroy(gameObject);
      }
    }
}
