using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

public class BallScript : MonoBehaviour
{

    public Rigidbody2D ballrb;
    public bool inPlay;
    public Transform paddle;
    public float speed;
    public Transform Explosion;
    public GameManager gm;
    public Transform Powerup;
    private GamestartScript gs;
    
    // Start is called before the first frame update
    void Start()
    {
      gm=FindObjectOfType<GameManager>();
      gs=FindObjectOfType<GamestartScript>();
      ballrb=GetComponent<Rigidbody2D>(); 
    }

    // Update is called once per frame
    void Update()
    {
      if(gm.gameOver){
        return;
      }
      if(gs.GameStartPanel.activeInHierarchy){
        return;
      }
      if(!inPlay){
          transform.position=paddle.position;
      } 
      if(Input.GetButtonDown("Jump") && !inPlay){
          inPlay=true;
          ballrb.AddForce(Vector2.up*speed);  
      } 
    }
    void OnTriggerEnter2D(Collider2D other){
      if(other.CompareTag("Bottom")){
        //Debug.Log("ball hit the bottom");
        ballrb.velocity=Vector2.zero;
        inPlay=false;
        gm.UpdateLives(-1);
        
      }
    }
    
    void OnCollisionEnter2D(Collision2D other){
      if (other.transform.CompareTag("Brick")){
        BrickScript brickScript=other.gameObject.GetComponent<BrickScript>();
        if(brickScript.hitsToBreak>1){
          brickScript.BreakBrick();
        }else{
        int randomfall=Random.Range(1,101);
        if(randomfall<20){
            Instantiate(Powerup,other.transform.position,other.transform.rotation);
        }
          Transform newExplosion=Instantiate(Explosion,other.transform.position,other.transform.rotation);
          Destroy(newExplosion.gameObject,2.5f);

         
          gm.UpdateScore(brickScript.points);
         
          gm.UpdateNumberOfBricks();
          Destroy(other.gameObject);
      }
      }
    }
    
}
