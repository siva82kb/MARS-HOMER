using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundMaterialRotator : MonoBehaviour
{
    public Material[] backgroundMaterials; // Array to hold your materials
    private static int currentIndex = 0; // Static variable to persist across scene reloads

    void Start()
    {
        if (backgroundMaterials.Length > 0)
        {
            // Get the Renderer component and set the material
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = backgroundMaterials[currentIndex];
                currentIndex = (currentIndex + 1) % backgroundMaterials.Length; // Cycle to the next material
            }
        }
        else
        {
            Debug.LogWarning("No materials assigned to the BackgroundMaterialRotator!");
        }
    }
}
