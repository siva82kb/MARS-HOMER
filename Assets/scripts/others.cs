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
    // Values to Draw the line [ROBOT endPoints (Meters)]
    public static readonly float EPMAXZ = 0.490f;
    public static readonly float EPMINZ = 0.010f;
    public static readonly float EPMAXY = 0.765f;
    public static readonly float EPMINY = 0.145f;
    // Centre position
    public static readonly float EPCENTERZ = (EPMAXZ + EPMINZ) / 2;
    public static readonly float EPCENTERY = (EPMAXY + EPMINY) / 2;
    
    public static readonly string[] Movements = new string[] { "ML", "AP", "MLAP" };
   
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

// Class representing movements trained by MARS
public class MarsMovement
{
    public string name { get; private set; }
    public string side { get; private set; }

    public MarsArom oldArom { get; private set; } = null;
    public MarsArom newArom { get; private set; } = null;
    public MarsArom currentArom { get => newArom != null ? newArom : (oldArom != null ? oldArom : null); }

    // Trial details for the mechanism.
    public int trialNumberDay { get; private set; }
    public int trialNumberSession { get; private set; }

    public MarsMovement(string name, string side, int sessno)
    {
        this.name = name?.ToUpper() ?? string.Empty;
        this.side = side;
        // Check if AROM file exists.
        if (MarsArom.AromFileExists(name)) oldArom = new MarsArom(this.name, readFromFile: true);
        else
        {
            oldArom = null;
            AppLogger.LogInfo($"No existing AROM file found for movement '{this.name}'. A new assessment is required.");
        }
        newArom = null;
        this.side = side;
        UpdateTrialNumbers(sessno);
    }

    public void NextTrail()
    {
        trialNumberDay += 1;
        trialNumberSession += 1;
    }

    // public void SetNewRomValues(float minx, float maxx, float miny, float maxy, float origMinx, float origMaxx, float origMiny, float origMaxy)
    // {
    //     newRom.setRom(minx, maxx, miny, maxy,origMinx,origMaxx,origMiny,origMaxy);
    //     if (minx != 0 || maxx != 0 || miny != 0 || maxy != 0) aromCompleted = true;

    //     if (newRom.movement == null)
    //     {
    //         newRom.SetMovement(this.name);
    //     }

    // }
    // public void SaveAssessmentData()
    // {
    //     if (aromCompleted)
    //     {
    //         // Save the new ROM values.
    //         newRom.WriteToAssessmentFile();

    //     }
    // }

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
        trialNumberSession = selRows.Max(row => Convert.ToInt32(row.Field<string>("TrialNumberSession")));
    }
}

// MARS Active Range of Motion (AROM) class.
public class MarsArom
{
    public static string[] FILEHEADER = new string[] { "DateTime", "AssessNo", "TrainingPlaneAngle",
        "TopRawX", "TopRawY", "BottomRawX", "BottomRawY", "LeftRawX", "LeftRawY", "RightRawX", "RightRawY",
        "TopAdjustedX", "TopAdjustedY", "BottomAdjustedX", "BottomAdjustedY", "LeftAdjustedX", "LeftAdjustedY", "RightAdjustedX", "RightAdjustedY",
        "filename" };
    // Class attributes to store data read from the file
    public string datetime;
    public int assessno { get; private set; }
    public string movement { get; private set; }
    public bool isReadOnly { get; private set; } = false;

    // Plane in which the AROM assessment is done.
    public float trainingPlaneAngle { get; private set; }

    // Raw data recorded during the assessment of AROM.
    private List<float[]> rawData;

    // Locations of the raw AROM quadrilateral
    public Vector2 topRaw { get; private set; }
    public Vector2 bottomRaw { get; private set; }
    public Vector2 leftRaw { get; private set; }
    public Vector2 rightRaw { get; private set; }

    // Locations of the adjusted AROM quadrilateral
    public Vector2 topAdjusted { get; private set; }
    public Vector2 bottomAdjusted { get; private set; }
    public Vector2 leftAdjusted { get; private set; }
    public Vector2 rightAdjusted { get; private set; }

    public static bool AromFileExists(string movementName) => File.Exists(DataManager.GetRomFileName(movementName));

    // Constructor that reads the file and initializes values based on the mechanism
    public MarsArom(string movementName, bool readFromFile = true)
    {
        isReadOnly = false;
        if (movementName == null) return;
        if (readFromFile) isReadOnly = ReadFromFile(movementName);
        else
        {
            // Handle case when no matching movement is found
            initializeNewAssessment(movementName);
        }
    }

    private void initializeNewAssessment(string movementName)
    {
        datetime = DateTime.Now.ToString();
        movement = movementName;
        assessno = 1;
        rawData = null;
        topRaw = Vector2.zero;
        bottomRaw = Vector2.zero;
        leftRaw = Vector2.zero;
        rightRaw = Vector2.zero;
        topAdjusted = Vector2.zero;
        bottomAdjusted = Vector2.zero;
        leftAdjusted = Vector2.zero;
        rightAdjusted = Vector2.zero;
        trainingPlaneAngle = 0f;
    }

    public void setMovement(string movName) => movement = (movement == null) ? movName : movement;

    public void startAromAssessment()
    {
        if (rawData == null) rawData = new List<float[]>();
    }

    public void addAromDataPoint(float x, float y)
    {
        if (rawData != null)
        {
            rawData.Add(new float[] { x, y });
        }
    }

    public void stopAromAssessment()
    {
        if (rawData != null && rawData.Count > 0)
        {
            // Calculate the four points of the quarilateral.
            // Create two lists from the rawData, where one list ordered by 
            // the x value and the other by y value.
            List<float[]> orderedByX = rawData.OrderBy(point => point[0]).ToList();
            List<float[]> orderedByY = rawData.OrderBy(point => point[1]).ToList();
            // Now we can easily find the four corners of the quadrilateral.
            // Top point is the average of the bottom 10% of the points.
            topRaw = AverageofExtremeEnds(orderedByY, 0.1f, false);
            // Bottom point is the average of the top 10% of the points.
            bottomRaw = AverageofExtremeEnds(orderedByY, 0.1f, true);
            // Left point is the average of the top 10% of the points.
            leftRaw = AverageofExtremeEnds(orderedByX, 0.1f, true);
            // Right point is the average of the bottom 10% of the points.
            rightRaw = AverageofExtremeEnds(orderedByX, 0.1f, false);
            // Adjusted points are same as raw points initially.
            topAdjusted = new Vector2(topRaw.x, topRaw.y);
            bottomAdjusted = new Vector2(bottomRaw.x, bottomRaw.y);
            leftAdjusted = new Vector2(leftRaw.x, leftRaw.y);
            rightAdjusted = new Vector2(rightRaw.x, rightRaw.y);
        }
        rawData = null;
    }

    public void setAdjustedAromTop(float x, float y) => topAdjusted = new Vector2(x, y);

    public void setAdjustedAromBottom(float x, float y) => bottomAdjusted = new Vector2(x, y);

    public void setAdjustedAromLeft(float x, float y) => leftAdjusted = new Vector2(x, y);

    public void setAdjustedAromRight(float x, float y) => rightAdjusted = new Vector2(x, y);

    public void WriteToAssessmentFile()
    {
        string fileName = DataManager.GetRomFileName(movement);
        // Create the file if it doesn't exist
        if (!File.Exists(fileName))
        {
            using (var file = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                // Write the pre-header to the file.
                StringBuilder rawDataString = new StringBuilder();
                // Write pre-header and header information
                // rawDataString.AppendLine($":Device: MARS");
                // rawDataString.AppendLine($":Location: {AppData.Instance.userData.GetDeviceLocation()}");
                // rawDataString.AppendLine($":Movement: {movement}");
                rawDataString.AppendLine(string.Join(",", FILEHEADER));
                file.Write(rawDataString.ToString());
            }
        }
        using (StreamWriter file = new StreamWriter(fileName, true))
        {
            // "DateTime", "AssessNo", "TrainingPlaneAngle",
            // "TopRawX", "TopRawY", "BottomRawX", "BottomRawY", "LeftRawX", "LeftRawY", "RightRawX", "RightRawY",
            // "TopAdjustedX", "TopAdjustedY", "BottomAdjustedX", "BottomAdjustedY", "LeftAdjustedX", "LeftAdjustedY", "RightAdjustedX", "RightAdjustedY",
            // "filename"
            // Write the actual data
            file.WriteLine(string.Join(",", new string[] {
                datetime, assessno.ToString(), trainingPlaneAngle.ToString("F2"),
                topRaw.x.ToString(), topRaw.y.ToString(), bottomRaw.x.ToString(), bottomRaw.y.ToString(),
                leftRaw.x.ToString(), leftRaw.y.ToString(), rightRaw.x.ToString(), rightRaw.y.ToString(),
                topAdjusted.x.ToString(), topAdjusted.y.ToString(), bottomAdjusted.x.ToString(), bottomAdjusted.y.ToString(),
                leftAdjusted.x.ToString(), leftAdjusted.y.ToString(), rightAdjusted.x.ToString(), rightAdjusted.y.ToString(),
                fileName
            }));
        }
    }

    private bool ReadFromFile(string movementName)
    {
        string fileName = DataManager.GetRomFileName(movementName);
        if (!File.Exists(fileName))
        {
            AppLogger.LogWarning($"No AROM file found for movement '{movementName}'. Starting new assessment.");
            return false;
        }

        // Load the data from the file
        DataTable romData = DataManager.loadCSV(fileName);

        // Check the number of rows.
        if (romData.Rows.Count == 0)
        {
            initializeNewAssessment(movementName);
            return false;
        }
        // Assign ROM from the last row.
        datetime = romData.Rows[romData.Rows.Count - 1].Field<string>("DateTime");
        movement = movementName;
        assessno = int.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("assessno"));
        // Assign the raw locations
        topRaw = new Vector2(float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("TopRawX")),
                             float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("TopRawY")));
        bottomRaw = new Vector2(float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("BottomRawX")),
                                float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("BottomRawY")));
        leftRaw = new Vector2(float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("LeftRawX")),
                              float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("LeftRawY")));
        rightRaw = new Vector2(float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("RightRawX")),
                               float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("RightRawY")));
        // Assign the adjusted locations
        topAdjusted = new Vector2(float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("TopAdjustedX")),
                                  float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("TopAdjustedY")));
        bottomAdjusted = new Vector2(float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("BottomAdjustedX")),
                                     float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("BottomAdjustedY")));
        leftAdjusted = new Vector2(float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("LeftAdjustedX")),
                                   float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("LeftAdjustedY")));
        rightAdjusted = new Vector2(float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("RightAdjustedX")),
                                    float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("RightAdjustedY")));
        trainingPlaneAngle = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("TrainingPlaneAngle"));
        return true;
    }
    
    private Vector2 AverageofExtremeEnds(List<float[]> orderList, float percentage = 0.1f, bool fromStart = true)
    {
        int count = (int)(orderList.Count * percentage);
        float avgX = 0f;
        float avgY = 0f;
        for (int i = 0; i < count; i++)
        {
            avgX += fromStart ? orderList[i][0] : orderList[orderList.Count - 1 - i][0];
            avgY += fromStart ? orderList[i][1] : orderList[orderList.Count - 1 - i][1];
        }
        avgX /= count;
        avgY /= count;
        return new Vector2(avgX, avgY);
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