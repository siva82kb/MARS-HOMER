using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickSpawner : MonoBehaviour
{
    public GameObject[] formations; // Array of brick formation prefabs
    public float spawnHeight = 5f; // Spawn from above the screen
    public float minX = -8f; // Minimum X position for spawning
    public float maxX = 8f; // Maximum X position for spawning
    private GameObject currentFormation; // The currently spawned formation
    private GameManager gameManager;
    private GamestartScript gs;
    private GameManage4S g4s;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        gs = FindObjectOfType<GamestartScript>();
        g4s = FindObjectOfType<GameManage4S>();

        // Spawn the first formation
        SpawnFormation();
    }

    void Update()
    {
        // Skip spawning if the game is over or if the game start panel is active
        if ((gameManager != null && gameManager.gameOver) || (gs.GameStartPanel.activeInHierarchy) || (g4s != null && g4s.gameOver))
        {
            return;
        }

        // Check if the current formation is destroyed (all child bricks destroyed)
        if (currentFormation != null && currentFormation.transform.childCount == 0)
        {
            Destroy(currentFormation); // Destroy the empty parent object
            currentFormation = null;  // Mark as null so a new formation can be spawned
        }

        // Spawn a new formation if the current one is null
        if (currentFormation == null)
        {
            SpawnFormation();
        }
    }

    private void SpawnFormation()
    {
        // Select a random formation prefab
        GameObject prefabToSpawn = formations[Random.Range(0, formations.Length)];

        // Generate a random spawn position
        Vector2 spawnPosition = new Vector2(Random.Range(minX, maxX), spawnHeight);
        // Debug.Log(spawnPosition);

        // Spawn the formation and assign it to currentFormation
        currentFormation = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
    }
}
