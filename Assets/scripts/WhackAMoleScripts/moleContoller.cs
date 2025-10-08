using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static MoleControllerN;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class MoleControllerN : MonoBehaviour
{
    Animator anim;
    public GameObject floatingTextPrefab;
    public Canvas uiCanvas;               // assign your UI Canvas (Screen Space - Overlay or Camera)
    public Vector3 spawnOffset = new Vector3(0, 1f, 0);
    //public FloatingTextSpawner floatingTextSpawner;
    void Awake()
    {
        anim = GetComponent<Animator>();
    }
    private void Start()
    {
        //PlayPopup();
        idle();
    }
   
    public void PlayPopup()
    {
        anim.Play("popup", -1, 0f); // Play from start
    }
    public void PlayDown() { anim.Play("pulldown", -1, 0f); }
    public void PlayHit() { 
        anim.Play("hit", -1, 0f);
        // spawn +1 text above mole
       
        //floatingTextSpawner.SpawnFloatingText(transform.position);
    }
    public void idle()
    {
        anim.Play("idle", -1, 0f);
    }
  


public class FloatingTextSpawner : MonoBehaviour
{
    public GameObject floatingTextPrefab; // your UI prefab inside Canvas
    public Canvas uiCanvas;               // the Canvas in scene
    public Vector3 spawnOffset = new Vector3(0, 1f, 0); // offset above mole

    public void SpawnFloatingText(Vector3 moleWorldPos)
    {
        if (floatingTextPrefab == null || uiCanvas == null) return;

        // Instantiate as child of the Canvas
        GameObject go = Instantiate(floatingTextPrefab, uiCanvas.transform);

        // Convert mole world position to screen point
        Vector3 screenPos = Camera.main.WorldToScreenPoint(moleWorldPos + spawnOffset);

        // Convert screen point to Canvas local position
        RectTransform canvasRect = uiCanvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera,
            out Vector2 localPoint
        );

        // Apply position and scale
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = localPoint;
        rt.localScale = Vector3.one;
    }
}


}
