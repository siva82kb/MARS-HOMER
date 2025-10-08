using UnityEngine;

public class LoadingCircle : MonoBehaviour
{
    //private RectTransform rectComponent;
    private float rotateSpeed = 100f;

    private void Start()
    {
        //rectComponent = GetComponent<RectTransform>();
    }

    private void Update()
    {
        
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }
}
