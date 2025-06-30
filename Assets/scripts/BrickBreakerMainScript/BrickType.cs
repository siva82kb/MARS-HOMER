using UnityEngine;

public class BrickType : MonoBehaviour
{
    public int BrickId; // Unique ID for the brick
  

    void Start()
    {
        // Automatically add `BrickType` to all child bricks in the prefab
        foreach (Transform child in transform)
        {
            if (child.GetComponent<BrickType>() == null)
            {
                BrickType brickType = child.gameObject.AddComponent<BrickType>();
                brickType.BrickId = BrickId;
            }
        }
    }

  
}
