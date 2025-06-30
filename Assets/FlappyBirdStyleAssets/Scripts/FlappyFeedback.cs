using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;

public class FlappyFeedback : MonoBehaviour
{
    public Text hits_count;
    public Text duration_count;
    public Text level_count;
    public Text hits_count_overall;
    public Text level_count_started;
    public Text duration_total_count;
    public Slider slide;

    string p_hospno;
    DateTime total_duration;
    float player_level;

    int count_ = 0;
    List<int> count_list = new List<int>();
    
    DateTime todays_duration;

    // Start is called before the first frame update
    void Start()
    {
        string  path_to_data = System.AppDomain.CurrentDomain.BaseDirectory;
        
        System.DateTime today = System.DateTime.Today;
        //p_hospno = Welcome.p_hospno;
        // string currentDirectoryPath = @"C:\Users\BioRehab\Desktop\Sathya\unity-pose\Arebo demo\Data\"+p_hospno;
        string currentDirectoryPath = path_to_data+"\\"+"Patient_Data"+"\\"+p_hospno;
        if(Directory.Exists(currentDirectoryPath))
        {
            var lines = File.ReadAllLines(currentDirectoryPath+"\\"+"flappy_hits.csv");
            var count = lines.Length;

            List<string> zero_row = new List<string>();
            List<string> fifth_row = new List<string>();
            List<string> first_row = new List<string>();
            List<string> second_row = new List<string>();
            List<string> sixth_row = new List<string>();

            // slide.value = 0.5f;
            // Debug.Log(count+": count_out");
            if (count>1)
            {
                Debug.Log(count+": count");

                foreach (var item in lines)
                {
                    var rowItems = item.Split(',');
                    
                    zero_row.Add(rowItems[0]);
                    first_row.Add(rowItems[1]);
                    second_row.Add(rowItems[2]);
                    fifth_row.Add(rowItems[5]);
                    sixth_row.Add(rowItems[6]);
                    
                }

                // zero_row.RemoveAt(0);
                foreach (string i in zero_row)
                {
                    count_ = count_+1;
                    if (String.Equals(today.ToString(), i))
                    {
                        count_list.Add(count_);
                        Debug.Log(count_);
                    }
                    
                }


                

                foreach (int i in count_list)
                {
                    
                    todays_duration = todays_duration.Add(DateTime.Parse(fifth_row[i-1]).TimeOfDay);
                }
                

                // total_duration = fifth_row[count-1];
                // duration_count.text = fifth_row[count-1];
                duration_count.text = todays_duration.ToString("HH:mm:ss");
                // DateTime played_duration_per_game = DateTime.Parse(fifth_row[count-1]);
                DateTime played_duration_per_game = todays_duration;
                // Debug.Log(played_duration_per_game+" :played_duration_per_game");
                string time_hr = played_duration_per_game.ToString("HH");
                string time_mm = played_duration_per_game.ToString("mm");
                string time_ss = played_duration_per_game.ToString("ss");
                // Debug.Log(time_hr+" "+time_mm+ " "+time_ss);
                int hr_time = Int32.Parse(time_hr);
                int mm_time = Int32.Parse(time_mm);
                int ss_time = Int32.Parse(time_ss);
                // Debug.Log(hr_time+" "+mm_time+ " "+ss_time);
                TimeSpan MySpan = new TimeSpan(hr_time, mm_time, ss_time);
                double playedDurationperGame = MySpan.TotalSeconds;
                // Debug.Log(MySpan.TotalSeconds+" :played_duration_per_game");
                // double seconds_played = played_duration_per_game.TotalSeconds;
                
                slide.value = (float)playedDurationperGame/900.00f;

                fifth_row.RemoveAt(0);
                foreach (string item in fifth_row)
                {
                    DateTime x = DateTime.Parse(item);
                    total_duration = total_duration.Add(DateTime.Parse(item).TimeOfDay);
                }
                duration_total_count.text = total_duration.ToString("HH:mm:ss");

                player_level = ((-float.Parse(second_row[count-1]))-2.0f)/2.0f;
                level_count.text = (player_level+1).ToString();
                // Debug.Log(level_count.text+" :Level");

                player_level = ((-float.Parse(first_row[count-1]))-2.0f)/2.0f;
                level_count_started.text = (player_level+1).ToString();
                // Debug.Log(level_count_started.text+" :level_count_started");

                // Debug.Log(float.Parse(second_row[count-1])+" :float.Parse(second_row[count-1])");
                // if((-float.Parse(second_row[count-1]))>0)
                // {
                //     player_level = (float.Parse(second_row[count-1])+2.0f)/2.0f;
                //     level_count.text = (player_level+1).ToString();
                //     Debug.Log(level_count.text+" :Level");
                // }

                // if((-float.Parse(first_row[count-1]))>0)
                // {
                //     player_level = ((-float.Parse(first_row[count-1]))-2.0f)/2.0f;
                //     level_count_started.text = (player_level+1).ToString();
                //     Debug.Log(level_count_started.text+" :level_count_started");
                // }
                

                hits_count.text = (sixth_row[count-1]).ToString();
                sixth_row.RemoveAt(0);
                hits_count_overall.text = (sixth_row.Sum(x => Convert.ToInt32(x))).ToString();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void onclick_menu()
    {
        SceneManager.LoadScene("ChooseGame");
    }
}
