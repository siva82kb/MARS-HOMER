using System.Collections;
using UnityEngine;

public class MoleController : MonoBehaviour
{
    public float popUpHeight = 1.5f;
    public float speed = 5f;
    public float popUpTime = 1.5f; // Only for Levels > 2

    private Vector3 hiddenPosition;
    private Vector3 visiblePosition;
    private bool moleVisible = false;
    private bool hasBeenHit = false;
    private GameManagerW gm;

    public bool IsMoleActive() => moleVisible;

    void Start()
    {
        gm = FindObjectOfType<GameManagerW>();
        hiddenPosition = transform.position;
        visiblePosition = transform.position + Vector3.up * popUpHeight;
        transform.position = hiddenPosition; // Start hidden
    }

    public IEnumerator StartMoleRoutine(bool waitForHit)
    {
        yield return MoveMole(visiblePosition);
        moleVisible = true;
        hasBeenHit = false;

        if (waitForHit)
        {
            yield return new WaitUntil(() => hasBeenHit); // Wait until the mole is hit
        }
        else
        {
            yield return new WaitForSeconds(popUpTime); // Disappear after some time
        }

        yield return MoveMole(hiddenPosition);
        moleVisible = false;

        gm.MoleCycleComplete(); // Notify GameManager
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
        hasBeenHit = true;
    }

    public bool HasBeenHit()
    {
        return hasBeenHit;
    }

    public void ForceMoleDown()
    {
        if (moleVisible)
        {
            StopAllCoroutines();
            StartCoroutine(MoveMole(hiddenPosition));
            moleVisible = false;
            gm.MoleCycleComplete(); // Notify GameManager
        }
    }
}


