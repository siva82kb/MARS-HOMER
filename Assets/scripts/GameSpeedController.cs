using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.UI;
using System;
using UnityEditor;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using System.Diagnostics;


public class GameSpeedController : MonoBehaviour
{
    // TextMeshPro text
    public TextMeshProUGUI gameSpeedText;
    public Button decreaseButton;
    public Button increaseButton;
    private float gameSpeed;
    public Image speedIndicator;
    [Range(0f, 1f)]
    private float value; // 0 = red, 1 = green


    void Start()
    {

    }

    void Update()
    {
        

        // Only if the game object is active
        if (gameObject.activeSelf)
        {
           
            value = AppData.Instance.selectedGame.reachSpeed/MarsGameDefs.MAX_REACH_SPEED;
            speedIndicator.color = Color.Lerp(Color.green, Color.red, value);
            speedIndicator.fillAmount = value;
            //gameSpeedText.text = $"{gameSpeed:F2}";
        }
    }
}
