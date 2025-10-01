using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Text;


public static class MarsDefs
{
    public static readonly string[] Movements = new string[] { "ML", "AP", "ML-AP" };
   
    public static int getMovementIndex(string Movement)
    {
        return Array.IndexOf(Movements, Movement);
    }
}

public class MarsUserData
{
    // Static variables.
    public const string DATEFORMAT = "dd-MM-yyyy";
    // File headers
    public const string MOVEMENT = "Movement";
    public const string MOVETIME = "MoveTime";
    public const string DATETIME = "DateTime";
    public const string HOSPITALNUMBER = "HospitalNumber";
    public const string STARTEDATEH = "StartDate";
    public const string TRAININGSIDE = "TrainingSide";
    public const string FORARMLENGTH = "forearmLength";
    public const string UPPERARMLENGTH = "upperarmLength";

    public bool isExceeded { get; private set; }
    public DataTable dTableConfig { get; private set; } = null;
    public DataTable dTableSession { get; private set; } = null;
 
    public string userID { get; private set; }
    public string hospNumber { get; private set; }
    public DateTime startDate { get; private set; }
    public bool rightArm { private set; get; }
    public int limb { get { return rightArm ? 1 : 2; } }
    
    public float trainingPlaneAngle { get; private set; } = 0f; // In degrees
 
    public Dictionary<string, float> moveTimePrsc { get; private set; } // Prescribed movement time
    public Dictionary<string, float> moveTimeCurr { get; private set; } // Current movement time
    public Dictionary<string, float> moveTimePrev { get; private set; } // Previous movement time 
   
    // Total movement times.
    public float totalMoveTimePrsc
    {
        get
        {
            if (moveTimePrsc == null) return -1f;
            else return moveTimePrsc.Values.Sum();
        }
    }
    public int totalMoveTimeRemaining
    {
        get
        {
            float _total = 0f;
            float _Prsc = 0f;
            foreach (string movement in MarsDefs.Movements)
            {
                _Prsc += moveTimePrsc[movement];
                _total += moveTimePrev[movement] - moveTimeCurr[movement];
            }
            if (_Prsc < _total)
            {
                isExceeded = true;
                _total = (_total - _Prsc);
                return (int)_total;
            }
            else
            {
                isExceeded = false;
                _total = (_Prsc - _total);
                return (int)_total;
            }
        }
    }

    public MarsUserData(string configFile, string sessionFile, string userID)
    {
        // Set the user ID.
        this.userID = userID;

        // Read parse configuration if it exists.
        if (File.Exists(configFile)) readParseTherapyConfigData(configFile);
        else return;

        // Read parse the session data if it exists.
        if (!File.Exists(sessionFile)) DataManager.CreateSessionFile(this.userID, "MARS", GetDeviceLocation());
        readParseSessionData(sessionFile);

        // Read parse the training plane data if it exists.
        if (!File.Exists(DataManager.trainingPlaneFile)) DataManager.CreateTrainingPlaneFile(this.userID, "MARS", GetDeviceLocation());
        readParseTrainingPlaneData(DataManager.trainingPlaneFile);
    }
    public void parsemoveTimePrev()
    {
        moveTimePrev = createMoveTimeDictionary();
        for (int i = 0; i < MarsDefs.Movements.Length; i++)
        {
            var _totalMoveTime = dTableSession.AsEnumerable()
                .Where(row => DateTime.ParseExact(row.Field<string>(DATETIME), DataManager.DATETIMEFORMAT, CultureInfo.InvariantCulture).Date == DateTime.Now.Date)
                .Where(row => row.Field<string>(MOVEMENT) == MarsDefs.Movements[i])
                .Sum(row => Convert.ToInt32(row[MOVETIME]));
            moveTimePrev[MarsDefs.Movements[i]] = _totalMoveTime / 60f;
        }
    }

    public static Dictionary<string, float> createMoveTimeDictionary()
    {
        Dictionary<string, float> _temp = new Dictionary<string, float>();
        for (int i = 0; i < MarsDefs.Movements.Length; i++)
        {
            _temp.Add(MarsDefs.Movements[i], 0f);
        }
        return _temp;
    }

    public int getCurrentDayOfTraining()
    {
        TimeSpan duration = DateTime.Now - startDate;
        return (int)duration.TotalDays;
    }

    private void readParseTherapyConfigData(string configFile)
    {
        dTableConfig = DataManager.loadCSV(configFile);
        DataRow lastRow = dTableConfig.Rows[dTableConfig.Rows.Count - 1];
        hospNumber = lastRow.Field<string>(HOSPITALNUMBER);
        rightArm = lastRow.Field<string>(TRAININGSIDE).ToLower() == "RIGHT";
        startDate = DateTime.ParseExact(lastRow.Field<string>(STARTEDATEH), "dd-MM-yyyy", CultureInfo.InvariantCulture);
        moveTimePrsc = createMoveTimeDictionary();
        for (int i = 0; i < MarsDefs.Movements.Length; i++)
        {
            moveTimePrsc[MarsDefs.Movements[i]] = float.Parse(lastRow.Field<string>(MarsDefs.Movements[i]));
        }
    
    }

    public void readParseSessionData(string sessionFile)
    {
        // Read the session file
        dTableSession = DataManager.loadCSV(sessionFile);
        // Create the current move time dictionary for the current session.
        moveTimeCurr = createMoveTimeDictionary();
     
        // Get the summary of move times from the previous sessions.
        parsemoveTimePrev();
    }

    private void readParseTrainingPlaneData(string trainingPlaneFile)
    {
        DataTable dTrainPlane = DataManager.loadCSV(trainingPlaneFile);
        trainingPlaneAngle = 0f;
        if (dTrainPlane.Rows.Count == 0) return;
        DataRow lastRow = dTrainPlane.Rows[dTrainPlane.Rows.Count - 1];
        trainingPlaneAngle = float.Parse(lastRow.Field<string>("TrainingPlaneAngle"));
    }

    public void writeUpdateTrainingPlaneData(float tpAngle)
    {
        // Create the file if it does not exist.
        if (!File.Exists(DataManager.trainingPlaneFile)) DataManager.CreateTrainingPlaneFile(userID, "MARS", GetDeviceLocation());
        // Append the new training plane angle to the file.
        using (var writer = new StreamWriter(DataManager.trainingPlaneFile, true, Encoding.UTF8))
        {
            string _dtstr = DateTime.Now.ToString(DataManager.DATETIMEFORMAT);
            writer.WriteLine($"{_dtstr},{tpAngle}");
        }
    }

    public string GetDeviceLocation() => dTableConfig.Rows[dTableConfig.Rows.Count - 1].Field<string>("Location");

    public int getTodayMoveTimeForMovement(string movement)
    {
        return (int)moveTimePrev[movement] + (int)moveTimeCurr[movement];
    }

    public DaySummary[] CalculateMoveTimePerDay(int noOfPastDays = 7)
    {
        DateTime today = DateTime.Now.Date;
        DaySummary[] daySummaries = new DaySummary[noOfPastDays];
        // Find the move times for the last seven days excluding today. If the date is missing, then the move time is set to zero.
        for (int i = 1; i <= noOfPastDays; i++)
        {
            DateTime _day = today.AddDays(-i);
            // Get the summary data for this date.
            var _moveTime = AppData.Instance.userData.dTableSession.AsEnumerable()
                .Where(row => DateTime.ParseExact(row.Field<string>(DATETIME), DataManager.DATETIMEFORMAT, CultureInfo.InvariantCulture).Date == _day)
                .Sum(row => Convert.ToInt32(row[MOVETIME]));
            // Create the day summary.
            daySummaries[i - 1] = new DaySummary
            {
                Day = Miscellaneous.GetAbbreviatedDayName(_day.DayOfWeek),
                Date = _day.ToString("dd/MM"),
                MoveTime = _moveTime / 60f
            };
            //Debug.Log($"{i} | {daySummaries[i - 1].Day} | {daySummaries[i - 1].Date} | {daySummaries[i - 1].MoveTime}");
        }
        return daySummaries;
    }

    
}

public class MarsMovement
{
    public string name { get; private set; }
    public string side { get; private set; }
  
    public ROM oldRom { get; private set; }
    public ROM newRom { get; private set; }
    public ROM currRom { get => newRom.isaromRomSet ? newRom : (oldRom.isaromRomSet ? oldRom : null); }
    public bool aromCompleted { get; private set; }

    // Trial details for the mechanism.
    public int trialNumberDay { get; private set; }
    public int trialNumberSession { get; private set; }

    public MarsMovement(string name, string side, int sessno)
    {
        this.name = name?.ToUpper() ?? string.Empty;
        this.side = side;
        oldRom = new ROM(this.name);
        newRom = new ROM();
        aromCompleted = false;
        this.side = side;
        UpdateTrialNumbers(sessno);
    }

    public void NextTrail()
    {
        trialNumberDay += 1;
        trialNumberSession += 1;
    }

    public float[] CurrentArom => currRom == null ? null : new float[] { currRom.aromMinX, currRom.aromMaxX, currRom.aromMinY, currRom.aromMaxY };
  

    public void ResetRomValues()
    {
        newRom.setRom(0, 0, 0, 0, 0, 0, 0, 0);
        aromCompleted = false;
    }

  

    public void SetNewRomValues(float minx, float maxx, float miny, float maxy, float origMinx, float origMaxx, float origMiny, float origMaxy)
    {
        newRom.setRom(minx, maxx, miny, maxy,origMinx,origMaxx,origMiny,origMaxy);
        if (minx != 0 || maxx != 0 || miny != 0 || maxy != 0) aromCompleted = true;

        if (newRom.movement == null)
        {
            newRom.SetMovement(this.name);
        }
       
    }
    public void SaveAssessmentData()
    {
        if (aromCompleted)
        {
            // Save the new ROM values.
            newRom.WriteToAssessmentFile();
          
        }
    }

    /*
     * Function to update the trial numbers for the day and session for the movement for today.
     */
    public void UpdateTrialNumbers(int sessno)
    {
        // Get the last row for the today, for the selected MarsSupport.
        var selRows = AppData.Instance.userData.dTableSession.AsEnumerable()?
            .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), DataManager.DATETIMEFORMAT, CultureInfo.InvariantCulture).Date == DateTime.Now.Date)
            .Where(row => row.Field<string>("Movement") == this.name);

        // Check if the selected rows is null.
        if (selRows.Count() == 0)
        {
            // Set the trial numbers to 1.
            trialNumberDay = 0;
            trialNumberSession = 0;
            return;
        }
        // Get the trial number as the maximum number for the trialNumber Day.
        trialNumberDay = selRows.Max(row => Convert.ToInt32(row.Field<string>("TrialNumberDay")));

        // Now let's get the session number for the current session.
        selRows = AppData.Instance.userData.dTableSession.AsEnumerable()?
            .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), DataManager.DATETIMEFORMAT, CultureInfo.InvariantCulture).Date == DateTime.Now.Date)
            .Where(row => Convert.ToInt32(row.Field<string>("SessionNumber")) == sessno)
            .Where(row => row.Field<string>("Movement") == this.name);
        if (selRows.Count() == 0)
        {
            // Set the trial numbers to 1.
            trialNumberSession = 0;
            return;
        }
        // Get the maximum trial number for the session.
        UnityEngine.Debug.Log(selRows.Count());
        trialNumberSession = selRows.Max(row => Convert.ToInt32(row.Field<string>("TrialNumberSession")));
    }
}

public class ROM
{
    public static string[] FILEHEADER = new string[] { "DateTime", "MinX", "MaxX", "MinY", "MaxY","OriginalMinX", "OriginalMaxX", "OriginalMinY", "OriginalMaxY" };
    // Class attributes to store data read from the file
    public string datetime;
    public float aromMinX { get; private set; }
    public float aromMaxX { get; private set; }
    public float aromMinY { get; private set; }
    public float aromMaxY { get; private set; }
    public float aromOriginalMinX { get; private set; }
    public float aromOriginalMaxX { get; private set; }
    public float aromOriginalMinY { get; private set; }
    public float aromOriginalMaxY { get; private set; }
    public string mode { get; private set; }
    public bool isAromRomXSet { get => aromMinX != 0 || aromMaxX != 0; }
    public bool isaromRomYSet { get => aromMinY != 0 || aromMaxY != 0; }

    public bool isaromRomSet { get => isAromRomXSet && isaromRomYSet; }

    public string movement { get; private set; }

    // Constructor that reads the file and initializes values based on the mechanism
    public ROM(string movementName, bool readFromFile = true)
    {
        
        if (readFromFile) ReadFromFile(movementName);
        else
        {
            // Handle case when no matching movement is found
            datetime = null;
            movement = movementName;
            aromMinX = 0;
            aromMaxX = 0;
            aromMinY = 0;
            aromMaxY = 0;
        }
    }

    public ROM()
    {
        aromMinX = 0;
        aromMaxX = 0;
        aromMinY = 0;
        aromMaxY = 0;
        movement = null;
        datetime = null;
    }

    public void SetMovement(string mov) => movement = (movement == null) ? mov : movement;
   
    public void setRom(float Minx, float Maxx, float Miny, float Maxy, float origMinx, float origMaxx, float origMiny, float origMaxy)
    {
        aromMinX = Minx;
        aromMaxX = Maxx;
        aromMinY = Miny;
        aromMaxY = Maxy;
        aromOriginalMinX = origMinx;
        aromOriginalMaxX = origMaxx;
        aromOriginalMinY = origMiny;
        aromOriginalMaxY = origMaxy;
        datetime = DateTime.Now.ToString();
    }
    public void WriteToAssessmentFile()
    {
        string fileName = DataManager.GetRomFileName(movement);

        // Create the file if it doesn't exist
        if (!File.Exists(fileName))
        {
            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                writer.WriteLine(string.Join(",", FILEHEADER));
            }
        }
        using (StreamWriter file = new StreamWriter(fileName, true))
        {
            file.WriteLine(string.Join(",", new string[] { datetime, aromMinX.ToString(), aromMaxX.ToString(), aromMinY.ToString(), aromMaxY.ToString(),
                                                                     aromOriginalMinX.ToString(),aromOriginalMaxX.ToString(),aromOriginalMinY.ToString(),aromOriginalMaxY.ToString() }));
        }
    }
    private void ReadFromFile(string movementName)
    {
        string fileName = DataManager.GetRomFileName(movementName);
        if (!File.Exists(fileName))
            return;
        DataTable romData = DataManager.loadCSV(fileName);
        // Check the number of rows.
        if (romData.Rows.Count == 0)
        {
            // Set default values for the mechanism.
            datetime = null;
            movement = movementName;
            aromMinX = 0;
            aromMaxX = 0;
            aromMinY = 0;
            aromMaxY = 0;
            return;
        }
        // Assign ROM from the last row.
        datetime = romData.Rows[romData.Rows.Count - 1].Field<string>("DateTime");
        movement = movementName;
        aromMinX = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("MinX"));
        aromMaxX = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("MaxX"));
        aromMinY = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("MinY"));
        aromMaxY = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("MaxY"));

    }
}

public static class Miscellaneous
{
    public static string GetAbbreviatedDayName(DayOfWeek dayOfWeek)
    {
        return dayOfWeek.ToString().Substring(0, 3);
    }

    public static float HumanLimbWeightTorque(float phi1, float phi2, float phi3, float uaWeight, float faWeight)
    {
        float _sp1 = Mathf.Sin(phi1 * Mathf.Deg2Rad);
        float _cp2 = Mathf.Cos(phi2 * Mathf.Deg2Rad);
        float _cp23 = Mathf.Cos((phi2 - phi3) * Mathf.Deg2Rad);
        return uaWeight * _sp1 * _cp2 + faWeight * _sp1 * _cp23;
    }
}