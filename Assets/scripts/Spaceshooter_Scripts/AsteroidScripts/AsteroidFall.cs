using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidFall : MonoBehaviour
{
    // Speed at which the asteroid falls
    public float fallSpeed = 1.5f;
    public static AsteroidFall instance;

    private void Awake() => instance = this;

    void Update() => transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
    
    public void SetFallSpeed(float newSpeed) => fallSpeed = newSpeed;
}
