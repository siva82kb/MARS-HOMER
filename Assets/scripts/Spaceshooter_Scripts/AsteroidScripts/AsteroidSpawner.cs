using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    public GameObject asteroidPrefab;
    public Transform spawnArea;
    // public float spawnInterval = 5f;
    public float minX = -8f;
    public float maxX = 8f;
    public float spawnY = 6f;
    private GameManagerScript gameManager;
    // private float timer_r = 5;
    private GameObject currentAsteroid;
    void Start()
    {
        gameManager = FindObjectOfType<GameManagerScript>();
        SpawnAsteroid();
    }

    void Update()
    {
        if (gameManager != null && gameManager.isGameOver)
        {
            return; // Stop spawning when the game is over

        }
        // timer_r += Time.deltaTime;
        // // Debug.Log(timer_r);
        // if (timer_r >= spawnInterval)
         if (currentAsteroid == null)
        {
            SpawnAsteroid();
            // timer_r = 0;

        }

    }

    void SpawnAsteroid()
    {
        // Generate a random X position within specified bounds
        float randomX = Random.Range(minX, maxX);

        // Create a spawn position
        Vector3 spawnPosition = new Vector3(randomX, spawnY, 0f);

        // Instantiate the asteroid prefab 
        currentAsteroid=Instantiate(asteroidPrefab, spawnPosition, Quaternion.identity);
    }
}
