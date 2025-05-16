using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManagerW : MonoBehaviour
{
    public GameObject gameOverPanel;
    public GameObject NextLevelPanel;
    public bool isgameover;
    public int requiredscore;
    public MoleController[] moles;
    public float minWait = 1f;
    public float maxWait = 3f;
    private float Timer;
    public float gameDuration = 60f;
    public TextMeshProUGUI TimerText;
    private List<int> recentHoles = new List<int>();
    private bool moleIsActive = false;
    public int Level;
    private string filepath = Path.Combine(Application.dataPath, "Patient_Data", "Whack_Score.csv");
    public Image scoreProgressBar; 
    public HoleSelector hs;

    void Start()
    {
        gameOverPanel.SetActive(false);
        isgameover = false;
        StartCoroutine(SpawnMoles());
        Timer = gameDuration;
        UpdateScoreProgress();
       

    }
    void UpdateScoreProgress()
    {
        if (scoreProgressBar != null)
        {
            

            scoreProgressBar.fillAmount = (float)hs.score / requiredscore;
           
        }
        else
        {
            Debug.Log("jnot set");
        }
    }

    IEnumerator SpawnMoles()
    {
        while (!isgameover)
        {
            if ((Level == 1 || Level == 2) && moleIsActive)
            {
                yield return null;
                continue;
            }
            
            if (Level == 3 || Level == 4)
            {
                yield return new WaitUntil(() => !moleIsActive);
                yield return new WaitForSeconds(Random.Range(0.5f, 2f)); // Faster spawning for Levels 3 & 4
            }
            else
            {
                yield return new WaitForSeconds(Random.Range(minWait, maxWait));
            }

            int randomIndex;
            do
            {
                randomIndex = Random.Range(0, moles.Length);
            } while (recentHoles.Contains(randomIndex) && moles.Length > 2);

            recentHoles.Add(randomIndex);
            if (recentHoles.Count > 2) recentHoles.RemoveAt(0);

            moleIsActive = true;
            StartCoroutine(moles[randomIndex].StartMoleRoutine(Level == 1 || Level == 2));
        }
    }

    public void MoleCycleComplete()
    {
        moleIsActive = false;
    }

    void Update()
    {
        Timer -= Time.deltaTime;
        if (TimerText != null)
        {
            TimerText.text = "Time: " + Mathf.CeilToInt(Timer) + "s";
        }

        if (Timer <= 0)
        {
            gameOverPanel.SetActive(true);
            isgameover = true;
            Timer = 0;
            StopAllCoroutines();
        }
       
    }
    public void onclick_Back(){
        SceneManager.LoadScene("Whack_levels");
    }

    public void onclick_Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void onclic_2ndlevel(){
         if (File.Exists(filepath))
        {
            string data = "0,2";
            File.WriteAllText(filepath, data);
        }
        SceneManager.LoadScene("Whack_Lvl2");
    }
     public void onclic_3rdlevel(){
         if (File.Exists(filepath))
        {
            string data = "0,3";
            File.WriteAllText(filepath, data);
        }
        SceneManager.LoadScene("Whack_Lvl3");
    }
     public void onclic_4thlevel(){
         if (File.Exists(filepath))
        {
            string data = "0,4";
            File.WriteAllText(filepath, data);
        }
        SceneManager.LoadScene("Whack_Lvl4");
    }
    public void Back_to_differentGame(){
        //can add different scene or simply stop the game and quit application
        // #if UNITY_EDITOR
        //     UnityEditor.EditorApplication.isPlaying=false;
        // #endif
        // Application.Quit();
    }
}


