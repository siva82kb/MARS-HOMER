using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GamestartScript : MonoBehaviour
{
    // Reference to the panel GameObject
    public GameObject GameStartPanel;

    public void Game()
    {
        // Hide the panel
        GameStartPanel.SetActive(false);
        Debug.Log("Game Started");
    }

}
