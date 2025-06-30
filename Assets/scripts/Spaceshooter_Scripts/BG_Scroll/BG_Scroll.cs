using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BG_Scroll : MonoBehaviour
{
    // Start is called before the first frame update
    public float scroll_speed = 0.5f;
    private MeshRenderer mesh_Renderer;
    private GameManagerScript gameManager;
    private float y_scroll;
    private bool isPaused = false; // Tracks whether the scrolling is paused

    void Awake()
    {
        mesh_Renderer = GetComponent<MeshRenderer>();
        gameManager = FindObjectOfType<GameManagerScript>();
    }

    // Update is called once per frame
    void Update()
    {
        if (gameManager != null && gameManager.isGameOver)
        {
            isPaused = true; // Pause scrolling
        }
        if (!isPaused)
        {
            Scroll();
        }
    }
    void Scroll()
    {
        y_scroll = Time.time * scroll_speed;
        Vector2 offset = new Vector2(0f, y_scroll);
        mesh_Renderer.sharedMaterial.SetTextureOffset("_MainTex", offset);
    }
}
