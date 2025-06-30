using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;

public class History : MonoBehaviour
{
    public Text hits_count;
    public Text days_count;
    public Text level_count;
    public Text duration_count;
    string p_hospno;
    DateTime total_duration;

    // Start is called before the first frame update
    void Start()
    {
        string  path_to_data = System.AppDomain.CurrentDomain.BaseDirectory;

        float player_level;

        //p_hospno = Welcome.p_hospno;
        // Debug.Log("Enter: "+p_hospno);

        // string currentDirectoryPath = @"C:\Users\BioRehab\Desktop\Sathya\unity-pose\Arebo demo\Data\"+p_hospno;
        string currentDirectoryPath = path_to_data+"\\"+"Patient_Data"+"\\"+p_hospno;
        if(Directory.Exists(currentDirectoryPath))
        {
            var lines = File.ReadAllLines(currentDirectoryPath+"\\"+"flappy_hits.csv");
            var count = lines.Length;
            
            List<string> first_row = new List<string>();
            List<string> second_row = new List<string>();
            List<string> fifth_row = new List<string>();
            List<string> sixth_row = new List<string>();

            List<string> first_row_dist = new List<string>();
            
            if (count>1)
            {
                foreach (var item in lines)
                {
                    var rowItems = item.Split(',');
                    
                    first_row.Add(rowItems[0]);
                    second_row.Add(rowItems[2]);
                    fifth_row.Add(rowItems[5]);
                    sixth_row.Add(rowItems[6]);
                    
                }
                
                // days_count.text = (((DateTime.Parse(first_row[count-1]) - DateTime.Parse(first_row[1])).TotalDays)+1).ToString();
                first_row.RemoveAt(0);
                first_row_dist = first_row.Distinct().ToList();

                days_count.text = first_row_dist.Count.ToString();
                
                if((-float.Parse(second_row[count-1]))>0)
                {                   
                    player_level = (float.Parse(second_row[count-1])+2.0f)/2.0f;
                    // Debug.Log(player_level+" :player_level");
                    level_count.text = (-player_level+1).ToString();
                    // Debug.Log(second_row[count-1]);
                }

                fifth_row.RemoveAt(0);
                foreach (string item in fifth_row)
                {
                    DateTime x = DateTime.Parse(item);
                    total_duration = total_duration.Add(DateTime.Parse(item).TimeOfDay);
                }
                duration_count.text = total_duration.ToString("HH:mm:ss");

                sixth_row.RemoveAt(0);
                hits_count.text = (sixth_row.Sum(x => Convert.ToInt32(x))).ToString();

                // duration_count.text = total_duration.ToString("HH:mm:ss");
                // duration_count.text = total_duration.ToString("HH:mm:ss");

            }
            
        }
        else
        {
            Debug.Log("Hosp No is not registered");
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void onclick_next()
    {
        SceneManager.LoadScene("FlappyCalibrate");
        // SceneManager.LoadScene("FlappyGame");
    }
}
