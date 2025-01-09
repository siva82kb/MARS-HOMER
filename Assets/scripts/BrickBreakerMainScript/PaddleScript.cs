using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PaddleScript : MonoBehaviour
{
    public float speed;
    public float leftscreenedge;
    public float rightscreenedge;
    private GameManager gm;
    private GamestartScript gs;
    // Start is called before the first frame update
    void Start()
    {
      gm=FindObjectOfType<GameManager>();
      gs=FindObjectOfType<GamestartScript>();
        
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
        float horizontal = Input.GetAxis("Horizontal");
        transform.Translate(Vector2.right*horizontal*Time.deltaTime*speed);
        if(transform.position.x<leftscreenedge)
            transform.position=new Vector2(leftscreenedge,transform.position.y);
        if(transform.position.x>rightscreenedge)
            transform.position=new Vector2(rightscreenedge,transform.position.y);
    }
    void OnTriggerEnter2D(Collider2D other){
       // Debug.Log("hit"+other.name);
       if(other.CompareTag("ExtraLife")){
       gm.UpdateLives(1);
       Destroy(other.gameObject);
    }
    } 
}
