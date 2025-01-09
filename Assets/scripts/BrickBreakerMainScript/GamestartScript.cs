using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GamestartScript : MonoBehaviour
{
     // Reference to the panel GameObject
    public GameObject GameStartPanel;
   
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
     // Reference to the panel GameObject
    public void Game()
    {
        // Hide the panel
        GameStartPanel.SetActive(false);
        Debug.Log("Game Started");
    }
    
}
