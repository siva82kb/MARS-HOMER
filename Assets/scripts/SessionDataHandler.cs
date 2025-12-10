using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using static AppData;

public static class SessionDataHandler
{

    public static float[] moveTimeData;
    public static string[] dateData;
    public static string DATEFORMAT = "dd/MM";

    //SESSION FILE  HEADER FORMAT AND DATETIME FORMAT
    public static string DATEFORMAT_INFILE = "yyyy-MM-dd HH:mm:ss";
    public static string DATETIME = "DateTime";
    public static string MOVETIME = "moveTime";
    public static string STARTTIME = "StartTime";
    public static string STOPTIME = "StopTime";
    public static string MOVEMENT = "Movement";
    public static Dictionary<string, float> movementMoveTimePrev { get; private set; } // Previous movement time 


    //CALCULATE MOVETIME PER DAY FOR ALL MOVEMENTS
    public static void MovTimePerDay()
    {
        // 1. Group sessions by DATE and compute total movetime
        var movTimePerDay = AppData.Instance.userData.dTableSession.AsEnumerable()
            .GroupBy(row => DateTime.ParseExact(
                row.Field<string>(DATETIME),
                DATEFORMAT_INFILE,
                CultureInfo.InvariantCulture
            ).Date)
            .Select(group => new
            {
                Date = group.Key,
                TotalMovTime = group.Sum(row => Convert.ToInt32(row[MOVETIME]))
            })
            .ToDictionary(x => x.Date, x => x.TotalMovTime/60f);   // Convert to dictionary for easy looku

        float[] _moveTimeData = new float[movTimePerDay.Count];
        string[] _dateData = new string[movTimePerDay.Count];

        int totalDays = (AppData.Instance.userData.endDate - AppData.Instance.userData.startDate).Days + 1;
        dateData = new string[totalDays];
        moveTimeData = new float[totalDays];

        for (int i = 0; i < totalDays; i++)
        {
            DateTime current = AppData.Instance.userData.startDate.AddDays(i);

            dateData[i] = current.ToString(DATEFORMAT);

            moveTimeData[i] = movTimePerDay.ContainsKey(current)
                ? (float)movTimePerDay[current]
                : 0f;
        }
    }
    public static void _MovTimePerDay()
    {
        // 1. Group sessions by DATE and compute total movetime
        var movTimePerDay = AppData.Instance.userData.dTableSession.AsEnumerable()
            .GroupBy(row => DateTime.ParseExact(
                row.Field<string>(DATETIME),
                DATEFORMAT_INFILE,
                CultureInfo.InvariantCulture
            ).Date)
            .Select(group => new
            {
                Date = group.Key,
                TotalMovTime = group.Sum(row => Convert.ToInt32(row[MOVETIME]))
            })
            .ToDictionary(x => x.Date, x => x.TotalMovTime);   // Convert to dictionary for easy lookup


        // 2. Prepare final arrays for ALL days between startDate and endDate
        int totalDays = (AppData.Instance.userData.endDate - AppData.Instance.userData.startDate).Days + 1;

        dateData = new string[totalDays];
        moveTimeData = new float[totalDays];

        // 3. Fill data day-by-day
        for (int i = 0; i < totalDays; i++)
        {
            DateTime current = AppData.Instance.userData.startDate.AddDays(i);

            dateData[i] = current.ToString(DATEFORMAT);

            // If data exists for that day → convert seconds → minutes
            if (movTimePerDay.TryGetValue(current, out int seconds))
            {
                moveTimeData[i] = seconds / 60f;
            }
            else
            {
                moveTimeData[i] = 0f;
            }

          
        }
    }


    //CALCULATE MOVETIME PER DAY FOR SELECTED MOVEMENT
    public static void SelectedMovement(string movement)
    {
  
        var filteredData = AppData.Instance.userData.dTableSession.AsEnumerable()
            .Where(row => row.Field<string>(MOVEMENT) == movement)
            .Select(row => new
            {
                Date = DateTime.ParseExact(row.Field<string>(DATETIME), DATEFORMAT_INFILE , CultureInfo.InvariantCulture).Date,
                MovTime = Convert.ToDouble(row[MOVETIME])
            })
            .GroupBy(entry => entry.Date)
            .Select(group => new
            {
                Date = group.Key,
                TotalMovTime = group.Sum(entry => entry.MovTime) / 60.0 // Convert to minutes
            })
            .OrderBy(result => result.Date)
            .ToList();

        dateData = new string[filteredData.Count];
        moveTimeData = new float[filteredData.Count];

        for (int i = 0; i < filteredData.Count; i++)
        {
            dateData[i] = filteredData[i].Date.ToString(DATEFORMAT); // Format date as "dd/MM"
            moveTimeData[i] = (float)filteredData[i].TotalMovTime; // Store movement time in minutes
            
        }
    }

}
