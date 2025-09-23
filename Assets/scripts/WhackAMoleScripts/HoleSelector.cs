using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class HoleSelector : MonoBehaviour
{
    public Transform[] holes;
    public MoleController[] moles;
    public GameObject selectionIndicator;
    public TextMeshProUGUI scoreText;
    public Transform hammer;
    private int selectedHoleIndex = -1;
    private int lastScoredHoleIndex = -1;
    public int score;
    public float selectionRadius = 1.0f;
    private int Level;
    public AudioSource moleHitSound;

    void Start()
    {
        scoreText.text = score.ToString();
    }

    void Update()
    {
        HandleMouseSelection();
      
    }

    void HandleMouseSelection()
    {
        Vector3 position = hammer.position;
        Transform closestHole = null;
        MoleController mole = null;
        float closestDistance = selectionRadius;

        for (int i = 0; i < holes.Length; i++)
        {
            float distance = Vector3.Distance(position, holes[i].position);
            if (distance < closestDistance)
            {
                closestHole = holes[i];
                mole = moles[i];
                selectedHoleIndex = i;
                closestDistance = distance;
            }
        }

        if (closestHole != null && mole.moleVisible)
        {
            selectionIndicator.SetActive(true);
            selectionIndicator.transform.position = closestHole.position;
            CheckAndScore();
        }
        else
        {
            selectionIndicator.SetActive(false);
            selectedHoleIndex = -1;
        }
    }

    void CheckAndScore()
    {
        if (selectedHoleIndex == -1) return;

        MoleController selectedMole = moles[selectedHoleIndex];
        selectedMole.spriteRenderer.sprite = selectedMole.sprites[1];
        if (selectedMole != null && selectedMole.IsMoleAtHole(holes[selectedHoleIndex]))
        {
            
            if (!selectedMole.HasBeenHit() && selectedHoleIndex != lastScoredHoleIndex)
            {
                moleHitSound.Play();
                score++;
                scoreText.SetText(score.ToString());
               
                selectedMole.RegisterHit();
                selectedMole.succDownMole();
                lastScoredHoleIndex = selectedHoleIndex;
            }
        }
    }
}
