using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidFall : MonoBehaviour
{
    // Speed at which the asteroid falls
    public float fallSpeed = 1.5f;
    public static AsteroidFall instance;
    public float elapsed;

    private void Awake() => instance = this;
    //    void Update()
    //    {
    //        if (transform.position.y > -3f)
    //        {
    //            elapsed += Time.deltaTime;
    //            transform.Translate(Vector3.down* fallSpeed * Time.deltaTime, Space.World);
    //}
    //        else
    //{
    //    Debug.Log("Actual fall time = " + elapsed);
    //}
    //    }
    float duration = 6.4f;


    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        float y = Mathf.Lerp(MarsGameDefs.Spaceshooter.tarSTARTPOINT, MarsGameDefs.Spaceshooter.tarENDPOINT, t);
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }
    //void Update() => transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);

    public void SetFallSpeed(float newSpeed) => fallSpeed = newSpeed;
    public void setFallTime(float newTime) => duration = newTime;    
}
