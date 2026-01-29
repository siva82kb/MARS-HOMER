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

    void Start()
    {

    }

    void Update()
    {
     
        // Only if the game object is active
        if (gameObject.activeSelf)
        {
            gameSpeed = AppData.Instance.selectedGame.reachTime;
            gameSpeedText.text = $"{gameSpeed:F2}";
        }
    }
}