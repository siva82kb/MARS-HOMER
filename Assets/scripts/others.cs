using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using static AppData;
using System.IO;
using Unity.VisualScripting;
using System.Text;
using System.Diagnostics.Eventing.Reader;



public static class MarsDefs
{
    public static readonly string[] Movements = new string[] { "SABDU", "ELFE", "SFE" };
   
    public static int getMovementIndex(string Movement)
    {
        return Array.IndexOf(Movements, Movement);
    }
}

public class MarsUserData
{
    // Static variables.
    static public string DATEFORMAT = "dd-MM-yyyy";
    // File headers
    static public string movement = "Movement";
    static public string moveTime = "MoveTime";
    static public string dateTime = "DateTime";
    static public string hosno = "hospno";
    static public string startDateH = "startdate";
    static public string useHandHeader = "TrainingSide";
    static public string forearmLength = "forearmLength";
    static public string upperarmLength = "upperarmLength";

    public bool isExceeded { get; private set; }
    public DataTable dTableConfig { get; private set; } = null;
    public DataTable dTableSession { get; private set; } = null;
    public DataTable dTableAssessment { get; private set; } = null;
    public DataTable dTableSupportConfig { get; private set; } = null;
    public DataTable dTableLimbParam { get; private set; } = null;
    public string hospNumber;
    public DateTime startDate;
    public bool rightArm { private set; get; }
    public int limb { get { return rightArm ? 1 : 2; } }
    public float faLength { get; private set; }
    public float uaLength { get; private set; }
    public float trainingPlaneAngle { get; private set; } = 0f; // In degrees
    public void setUALength(float uaLength)
    {
        this.uaLength = uaLength;
    }
    public void setFALength(float faLength)
    {
        this.faLength = faLength;
    }
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
        // Read parse configuration if it exists.
        if (File.Exists(configFile)) readParseTherapyConfigData(configFile);
        else return;

        // Ready parse the session data if it exists.
        if (!File.Exists(sessionFile)) DataManager.CreateSessionFile(userID, "MARS", GetDeviceLocation());
        readParseSessionData(sessionFile);
    }

    public void parsemoveTimePrev()
    {
        moveTimePrev = createMoveTimeDictionary();
        for (int i = 0; i < MarsDefs.Movements.Length; i++)
        {
            var _totalMoveTime = dTableSession.AsEnumerable()
                .Where(row => DateTime.ParseExact(row.Field<string>(dateTime), DataManager.DATETIMEFORMAT, CultureInfo.InvariantCulture).Date == DateTime.Now.Date)
                .Where(row => row.Field<string>(movement) == MarsDefs.Movements[i])
                .Sum(row => Convert.ToInt32(row[moveTime]));
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
        hospNumber = lastRow.Field<string>("HospitalNumber");
        rightArm = lastRow.Field<string>("TrainingSide").ToLower() == "RIGHT";
        startDate = DateTime.ParseExact(lastRow.Field<string>("StartDate"), "dd-MM-yyyy", CultureInfo.InvariantCulture);
        moveTimePrsc = createMoveTimeDictionary();
        for (int i = 0; i < MarsDefs.Movements.Length; i++)
        {
            moveTimePrsc[MarsDefs.Movements[i]] = float.Parse(lastRow.Field<string>(MarsDefs.Movements[i]));
        }
        faLength = float.Parse(lastRow.Field<string>("ForearmLength"));
        uaLength = float.Parse(lastRow.Field<string>("UpperarmLength"));
        trainingPlaneAngle = float.Parse(lastRow.Field<string>("TrainingPlaneAngle"));
    }

    private void readParseSessionData(string sessionFile)
    {
        // Read the session file
        dTableSession = DataManager.loadCSV(sessionFile);
        // Create the current move time dictionary for the current session.
        moveTimeCurr = createMoveTimeDictionary();
        // Get the summary of move times from the previous sessions.
        parsemoveTimePrev();
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
                .Where(row => DateTime.ParseExact(row.Field<string>(dateTime), DataManager.DATETIMEFORMAT, CultureInfo.InvariantCulture).Date == _day)
                .Sum(row => Convert.ToInt32(row[moveTime]));
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

    public List<float> GetLastTwoSuccessRates(string movement, string gameName)
    {
        List<float> lastTwoSuccessRates = new List<float>();

        dTableSession = DataManager.loadCSV(DataManager.sessionFile);

        if (dTableSession == null || dTableSession.Rows.Count == 0)
        {
            return new List<float> { 0f, 0f };
        }

        var today = DateTime.Today;

        var filteredRows = dTableSession.AsEnumerable()
            .Where(row =>
                row.Field<string>("Movement") == movement &&
                row.Field<string>("GameName") == gameName)
            .OrderByDescending(row => DateTime.ParseExact(row.Field<string>("TrialStartTime"), DataManager.DATETIMEFORMAT, CultureInfo.InvariantCulture))
            .ToList();

        var successRows = dTableSession.AsEnumerable()
        .Where(row =>
            row.Field<string>("Mechanism") == movement &&
            row.Field<string>("GameName") == gameName &&
            !string.IsNullOrWhiteSpace(row.Field<string>("SuccessRate")) &&
            !string.IsNullOrWhiteSpace(row.Field<string>("CurrentControlBound")))
        .ToList();

        if (successRows.Any())
        {
            Others.highestSuccessRate = successRows
                .Max(row =>
                {
                    float successRate = float.Parse(row.Field<string>("SuccessRate"), CultureInfo.InvariantCulture);
                    return successRate;
                    //float controlBound = float.Parse(row.Field<string>("CurrentControlBound"), CultureInfo.InvariantCulture);
                    //return successRate * (PlutoAANController.MAXCONTROLBOUND - controlBound);
                });

            Debug.Log(Others.highestSuccessRate);
        }
        else
        {
            Others.highestSuccessRate = 0f;
        }
        if (!filteredRows.Any())
        {
            return null;
        }
        // Get all success rates from today
        var todayRates = filteredRows
            .Where(row => DateTime.ParseExact(row.Field<string>("TrialStartTime"), DataManager.DATETIMEFORMAT, CultureInfo.InvariantCulture).Date == today)
            .Select(row => Convert.ToSingle(row["SuccessRate"]))
            .ToList();
        if (todayRates.Count >= 2)
        {
            lastTwoSuccessRates.Add(todayRates[1]);
            lastTwoSuccessRates.Add(todayRates[0]);
        }
        else if (todayRates.Count == 1)
        {

            var previousDayRate = filteredRows
                .Where(row => DateTime.ParseExact(row.Field<string>("TrialStartTime"), DataManager.DATETIMEFORMAT, CultureInfo.InvariantCulture).Date < today)
                .Select(row => Convert.ToSingle(row["SuccessRate"]))
                .FirstOrDefault();

            lastTwoSuccessRates.Add(previousDayRate);
            lastTwoSuccessRates.Add(todayRates[0]);

        }
        else
        {
            var previousDayRate = filteredRows
                .Where(row => DateTime.ParseExact(row.Field<string>("TrialStartTime"), DataManager.DATETIMEFORMAT, CultureInfo.InvariantCulture).Date < today)
                .Select(row => Convert.ToSingle(row["SuccessRate"]))
                .FirstOrDefault();

            lastTwoSuccessRates.Add(previousDayRate);
            lastTwoSuccessRates.Add(0f);
        }
        while (lastTwoSuccessRates.Count < 2) lastTwoSuccessRates.Add(0f);
        return lastTwoSuccessRates;
    }
}

public static class Others
{
    public static float gameTime = 0f;
    public static float highestSuccessRate = 0f;
    public static string GetAbbreviatedDayName(DayOfWeek dayOfWeek)
    {
        return dayOfWeek.ToString().Substring(0, 3);
    }
}


public class MarsMovement
{
    public string name { get; private set; }
    public string side { get; private set; }

    public string MarsMode { get; private set; }
    public void setMode(String mode)
    {
        MarsMode = mode;
    }

    //MarsMode - FWS
    public ROM oldRomFWS { get; private set; }
    public ROM newRomFWS { get; private set; }
    public ROM currRomFWS { get => newRomFWS.isaromRomSet ? newRomFWS : (oldRomFWS.isaromRomSet ? oldRomFWS : null); }
    public bool aromCompletedFWS { get; private set; }

    //MarsMode - HWS
    public ROM oldRomHWS { get; private set; }
    public ROM newRomHWS { get; private set; }
    public ROM currRomHWS { get => newRomHWS.isaromRomSet ? newRomHWS : (oldRomHWS.isaromRomSet ? oldRomHWS : null); }
    public bool aromCompletedHWS { get; private set; }

    //MarsMOde - NWS
    public ROM oldRomNWS { get; private set; }
    public ROM newRomNWS { get; private set; }
    public ROM currRomNWS { get => newRomNWS.isaromRomSet ? newRomNWS : (oldRomNWS.isaromRomSet ? oldRomNWS : null); }
    public bool aromCompletedNWS { get; private set; }

    public float currSpeed { get; private set; } = -1f;
    // Trial details for the mechanism.
    public int trialNumberDay { get; private set; }
    public int trialNumberSession { get; private set; }

    public MarsMovement(string name, string side, int sessno)
    {
        this.name = name?.ToUpper() ?? string.Empty;
        this.side = side;

        //objs MarsMode - FWS
        oldRomFWS = new ROM(this.name, "FWS");
        newRomFWS = new ROM();
        aromCompletedFWS = false;

        //objs MarsMode - FWS
        oldRomHWS = new ROM(this.name, "HWS");
        newRomHWS = new ROM();
        aromCompletedHWS = false;

        //objs MarsMode - FWS
        oldRomNWS = new ROM(this.name, "NWS");
        newRomNWS = new ROM();
        aromCompletedNWS = false;

        this.side = side;
        //currSpeed = -1f;
        UpdateTrialNumbers(sessno);
    }

    public void NextTrail()
    {
        trialNumberDay += 1;
        trialNumberSession += 1;
    }

    public float[] CurrentAromFWS => currRomFWS == null ? null : new float[] { currRomFWS.aromMinX, currRomFWS.aromMaxX, currRomFWS.aromMinY, currRomFWS.aromMaxY };
    public float[] CurrentAromHWS => currRomHWS == null ? null : new float[] { currRomHWS.aromMinX, currRomHWS.aromMaxX, currRomHWS.aromMinY, currRomHWS.aromMaxY };
    public float[] CurrentAromNWS => currRomNWS == null ? null : new float[] { currRomNWS.aromMinX, currRomNWS.aromMaxX, currRomNWS.aromMinY, currRomNWS.aromMaxY };

    public void ResetRomValuesFWS()
    {
        newRomFWS.setRom(0, 0, 0, 0);
        aromCompletedFWS = false;
    }

    public void ResetRomValuesHWS()
    {
        newRomHWS.setRom(0, 0, 0, 0);
        aromCompletedHWS = false;
    }

    public void ResetRomValuesNWS()
    {
        newRomNWS.setRom(0, 0, 0, 0);
        aromCompletedNWS = false;
    }

    public void SetNewRomValuesFWS(float minx, float maxx, float miny, float maxy)
    {
        newRomFWS.setRom(minx, maxx, miny, maxy);
        if (minx != 0 || maxx != 0 || miny != 0 || maxy != 0) aromCompletedFWS = true;

        if (newRomFWS.movement == null)
        {
            newRomFWS.SetMovement(this.name);
        }
        if (newRomFWS.mode == null)
        {
            newRomFWS.SetMarsMode(this.MarsMode);
        }
    }

    public void SetNewRomValuesHWS(float minx, float maxx, float miny, float maxy)
    {
        newRomHWS.setRom(minx, maxx, miny, maxy);
        if (minx != 0 || maxx != 0 || miny != 0 || maxy != 0) aromCompletedHWS = true;

        if (newRomHWS.movement == null)
        {
            newRomHWS.SetMovement(this.name);
        }
        if (newRomHWS.mode == null)
        {
            newRomHWS.SetMarsMode(this.MarsMode);
        }
    }

    public void SetNewRomValuesNWS(float minx, float maxx, float miny, float maxy)
    {
        newRomNWS.setRom(minx, maxx, miny, maxy);
        if (minx != 0 || maxx != 0 || miny != 0 || maxy != 0) aromCompletedNWS = true;

        if (newRomNWS.movement == null)
        {
            newRomNWS.SetMovement(this.name);
        }
        if (newRomNWS.mode == null)
        {
            newRomNWS.SetMarsMode(this.MarsMode);
        }
    }


    public void SaveAssessmentData()
    {
        if (aromCompletedFWS && aromCompletedHWS && aromCompletedNWS)
        {
            // Save the new ROM values.
            newRomFWS.WriteToAssessmentFile();
            newRomHWS.WriteToAssessmentFile();
            newRomNWS.WriteToAssessmentFile();
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
    public static string[] FILEHEADER = new string[] { "DateTime", "MinX", "MaxX", "MinY", "MaxY" };
    // Class attributes to store data read from the file
    public string datetime;
    public float aromMinX { get; private set; }
    public float aromMaxX { get; private set; }
    public float aromMinY { get; private set; }
    public float aromMaxY { get; private set; }
    public string mode { get; private set; }
    public bool isAromRomXSet { get => aromMinX != 0 || aromMaxX != 0; }
    public bool isaromRomYSet { get => aromMinY != 0 || aromMaxY != 0; }

    public bool isaromRomSet { get => isAromRomXSet && isaromRomYSet; }

    public string movement { get; private set; }

    // Constructor that reads the file and initializes values based on the mechanism
    public ROM(string movementName, string marsMode, bool readFromFile = true)
    {
        SetMarsMode(marsMode);
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
        //mode = null;
        movement = null;
        datetime = null;
    }

    public void SetMovement(string mov) => movement = (movement == null) ? mov : movement;
    public void SetMarsMode(string Mode) => mode = (mode == null) ? Mode : mode; // fws,hws,nws modes


    public void setRom(float Minx, float Maxx, float Miny, float Maxy)
    {
        aromMinX = Minx;
        aromMaxX = Maxx;
        aromMinY = Miny;
        aromMaxY = Maxy;
        datetime = DateTime.Now.ToString();
    }
    public void WriteToAssessmentFile()
    {
        string fileName = DataManager.GetRomFileName(movement, mode);

        // Create the file if it doesn't exist
        if (!File.Exists(fileName))
        {
            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                writer.WriteLine(string.Join(",", FILEHEADER));
            }
        }

        Debug.Log(fileName + "filenameass" + movement + mode);
        using (StreamWriter file = new StreamWriter(fileName, true))
        {
            file.WriteLine(string.Join(",", new string[] { datetime, aromMinX.ToString(), aromMaxX.ToString(), aromMinY.ToString(), aromMaxY.ToString() }));
        }
    }

    private void ReadFromFile(string movementName)
    {
        if (mode == null)
            return;
        string fileName = DataManager.GetRomFileName(movementName, mode);
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