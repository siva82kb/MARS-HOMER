using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerupScript : MonoBehaviour
{
  public float speed;
  void Update()
  {
    transform.Translate(new Vector2(0f, -1f) * Time.deltaTime * speed);

    if (transform.position.y < -5.3f)
    {
      Destroy(gameObject);
    }
  }
}
