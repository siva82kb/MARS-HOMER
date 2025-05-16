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
    public Transform mouse_pointer;
    private int selectedHoleIndex = -1;
    private int lastScoredHoleIndex = -1;
    public int score;
    public float selectionRadius = 1.0f;
    private int Level;
    private string filepath = Path.Combine(Application.dataPath, "Patient_Data", "Whack_Score.csv");

    void Start()
    {
        if (File.Exists(filepath))
        {
            string[] lines = File.ReadAllLines(filepath);
            string[] values = lines[0].Split(',');
            if (values.Length >= 2)
            {
                int.TryParse(values[0], out score);
                int.TryParse(values[1],out Level);
            }
        }

        scoreText.text = score.ToString();
    }

    void Update()
    {
        HandleMouseSelection();
         if (File.Exists(filepath))
        {
            string data = $"{score},{Level}";
            File.WriteAllText(filepath, data);
        }
    }

    void HandleMouseSelection()
    {
        Vector3 position = mouse_pointer.position;
        Transform closestHole = null;
        float closestDistance = selectionRadius;

        for (int i = 0; i < holes.Length; i++)
        {
            float distance = Vector3.Distance(position, holes[i].position);
            if (distance < closestDistance)
            {
                closestHole = holes[i];
                selectedHoleIndex = i;
                closestDistance = distance;
            }
        }

        if (closestHole != null)
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

        if (selectedMole != null && selectedMole.IsMoleAtHole(holes[selectedHoleIndex]))
        {
            if (!selectedMole.HasBeenHit() && selectedHoleIndex != lastScoredHoleIndex)
            {
                score++;
                scoreText.SetText(score.ToString());

                selectedMole.RegisterHit();
                selectedMole.ForceMoleDown();

                lastScoredHoleIndex = selectedHoleIndex;
            }
        }
    }
}
