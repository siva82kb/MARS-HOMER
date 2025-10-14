using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOffScreen : MonoBehaviour
{
    // Y-position threshold for destroying the asteroid
    public float yThreshold { get; private set; } = -5.3f; 

    public void SetYThreshold(float newThreshold) => yThreshold = newThreshold;
    
    void Update()
    {
        // Check if the asteroid has fallen below the specified threshold
        if (transform.position.y <= yThreshold)
        {
            SpaceShooterGameContoller.Instance.setIsFailure();
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
