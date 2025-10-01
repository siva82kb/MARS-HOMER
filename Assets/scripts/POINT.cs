using UnityEngine;
using TMPro; // only if you use TextMeshPro

using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public float moveUpSpeed = 50f; // pixels per second
    public float lifetime = 1f;

    RectTransform rect;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        rect.anchoredPosition += Vector2.up * moveUpSpeed * Time.deltaTime;
    }
}


