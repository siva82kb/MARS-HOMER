using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShieldScript : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject Shield;
    public float timer_S=15;

    public TextMeshProUGUI shieldTiming;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Shield.activeInHierarchy){
            shield();
        }
         
    }
     void OnTriggerEnter2D(Collider2D other){
       if(other.gameObject.CompareTag("ShieldPowerup")){
        if(!Shield.activeInHierarchy){
        Shield.SetActive(true);
        Destroy(other.gameObject);
      
        }
        
    }
    } 
    void shield(){
        timer_S-=Time.deltaTime;
        if(shieldTiming!=null){
            shieldTiming.text = "Shield:" + Mathf.CeilToInt(timer_S).ToString() + "s"; // Show remaining time
            if(timer_S<=0){
                if(Shield.activeInHierarchy){
                    Shield.SetActive(false);
                    timer_S=15;
                }
            }
        }
        

    }
}
