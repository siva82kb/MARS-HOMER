using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class BulletScript : MonoBehaviour
{
    // Start is called before the first frame update
    public float BULLET_SPEED = 8f;
    private SpaceShooterGameContoller gm;

    void Start()
    {
        gm = FindFirstObjectByType<SpaceShooterGameContoller>();
    }

    // Update is called once per frame
    void Update()
    {
        // Stop spawning bullets when game is over.
        if (gm != null && gm.isGameFinished) return;
        // Move the bullet upwards
        Vector3 temp = transform.position;
        temp.y += BULLET_SPEED * Time.deltaTime;
        transform.position = temp;
    }
}
