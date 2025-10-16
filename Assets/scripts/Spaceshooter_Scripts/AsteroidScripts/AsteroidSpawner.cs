using System.Collections;
using System.Collections.Generic;
// using System.Numerics;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    public static AsteroidSpawner Instance;
    public GameObject asteroidPrefab;
    public Transform spawnArea;

    // public float spawnInterval = 5f;
    public const float spawnY = 6f;

    public GameObject currentAsteroid;
    
    private void Awake()
    {
        Instance = this;
    }

    public Vector3 SpawnAsteroid(float xMin, float xMax)
    {
        if (currentAsteroid != null)
        {
            return Vector3.zero;
        }
        // Generate a random X position within specified bounds
        float randomX = Random.Range(xMin, xMax);

        // Create a spawn position
        Vector3 spawnPosition = new Vector3(randomX, spawnY, 0f);

        // Instantiate the asteroid prefab 
        currentAsteroid = Instantiate(asteroidPrefab, spawnPosition, Quaternion.identity);     
        return spawnPosition;
    }
}
