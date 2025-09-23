using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MoleController : MonoBehaviour
{
    public float popUpHeight = 1.44f;
    public float speed = 5f;
    public float popUpTime = 1.5f; // Only for Levels > 2

    private Vector3 hiddenPosition;
    private Vector3 visiblePosition;
    public bool moleVisible = false;
    private bool hasBeenHit = false;
    private WAMGameController gm;

    public SpriteRenderer spriteRenderer;   // Drag your SpriteRenderer here
    public Sprite[] sprites;//0-happy,1-sad,2-surprise Assign multiple sprites in Inspector//

    //private int currentIndex = 0;

  
  
    void Start()
    {
        gm = FindObjectOfType<WAMGameController>();
        hiddenPosition = transform.position;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        visiblePosition = transform.position + Vector3.up * popUpHeight;
        transform.position = hiddenPosition; // Start hidden
    }

    
    public void PopUPMole(bool waitForHit)
    {
        spriteRenderer.sprite = sprites[0];
        StartCoroutine(MoveMole(visiblePosition));
        moleVisible = true;
        hasBeenHit = false;
    }

    public void failDownMole()
    {
       
        spriteRenderer.sprite = sprites[2];
        Invoke(nameof(MoveDown), 0.2f);
    }
    public void succDownMole()
    {
       
        spriteRenderer.sprite = sprites[1];
        Invoke(nameof(MoveDown), 0.2f);

    }
    private void MoveDown()
    {
        StartCoroutine(MoveMole(hiddenPosition));
        moleVisible = false;
        hasBeenHit = false;
    }

    IEnumerator MoveMole(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

    public bool IsMoleAtHole(Transform hole)
    {
        return moleVisible && transform.position == visiblePosition;
    }

    public void RegisterHit()
    {
        WAMGameController.Instance.nSuccess++;
        hasBeenHit = true;
    }

    public bool HasBeenHit()
    {

        return hasBeenHit;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.name == "cursor")
        {
            RegisterHit();
            Debug.Log("hit");
        }
        Debug.Log(collision.gameObject.name);
    }
}


