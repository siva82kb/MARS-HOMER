using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidFall : MonoBehaviour
{
    public float fallSpeed = 1.5f;  // Speed at which the asteroid falls

    void Update()
    {
        // Move the asteroid downwards
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
    }

    //  method to change speed if needed while in game
    public void SetFallSpeed(float newSpeed)
    {
        fallSpeed = newSpeed;
    }
}
