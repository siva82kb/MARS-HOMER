using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class point : MonoBehaviour
{
   

    public float moveSpeed = 2f;
    public float lifetime = 1f;
    public Vector3 moveDirection = new Vector3(0, 1f, 0);

    private Text textMesh;
    private CanvasGroup canvasGroup;

    void Start()
    {
        textMesh = GetComponent<Text>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        Destroy(gameObject, lifetime); // auto destroy after 1 sec
    }

    void Update()
    {
        // Move upward
        transform.position += moveDirection * 10* Time.deltaTime;

    }

    public void SetText(string text)
    {
        if (textMesh == null) textMesh = GetComponent<Text>();
        textMesh.text = text;
    }


}
