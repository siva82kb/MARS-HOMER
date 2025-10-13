using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Text;
using System.Numerics;
using UnityEngine;


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
    
    public static readonly float TRAINING_PLANE_ANGLE_THRESHOLD = 5f; // Degrees
   
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

    public void reloadTrainingPlaneAngle()
    {
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
        rightArm = lastRow.Field<string>(TRAININGSIDE).ToUpper() == "RIGHT";
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

    public bool IsAromAssessmentAvailableForTrainingAngle(string movement)
    {
        if (MarsArom.AromFileExists(movement))
        {
            var arom = new MarsArom(movement, readFromFile: true);
            return Mathf.Abs(arom.trainingPlaneAngle - AppData.Instance.userData.trainingPlaneAngle) <= MarsDefs.TRAINING_PLANE_ANGLE_THRESHOLD;
        }
        return false;
    } 
}

// Class representing movements trained by MARS
public class MarsMovement
{
    public string name { get; private set; }
    public string side { get; private set; }
    private int sessno;

    public MarsArom oldArom { get; private set; } = null;
    public MarsArom newArom { get; set; } = null;
    public MarsArom currentArom { get => newArom != null ? newArom : (oldArom != null ? oldArom : null); }


    // Trial details for the mechanism.
    public int trialNumberDay { get; private set; }
    public int trialNumberSession { get; private set; }

    public MarsMovement(string name, string side, int sessno)
    {
        this.name = name?.ToUpper() ?? string.Empty;
        this.side = side;
        this.sessno = sessno;
        // Check if AROM file exists.
        if (MarsArom.AromFileExists(name)) oldArom = new MarsArom(this.name, readFromFile: true);
        else
        {
            oldArom = null;
            AppLogger.LogInfo($"No existing AROM file found for movement '{this.name}'. A new assessment is required.");
        }
        newArom = null;
        UpdateTrialNumbers(this.sessno);
    }

    public void NextTrial()
    {
        trialNumberDay += 1;
        trialNumberSession += 1;
    }

    public void ReloadMovementData()
    {
        // Check if AROM file exists.
        if (MarsArom.AromFileExists(name)) oldArom = new MarsArom(this.name, readFromFile: true);
        else
        {
            oldArom = null;
            AppLogger.LogInfo($"No existing AROM file found for movement '{this.name}'. A new assessment is required.");
        }
        newArom = null;
        UpdateTrialNumbers(this.sessno);
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
        trialNumberSession = selRows.Max(row => Convert.ToInt32(row.Field<string>("TrialNumberSession")));
    }
}

// MARS Active Range of Motion (AROM) class.
public class MarsArom
{

    public static string[] FILEHEADER = new string[] { "DateTime", "TrainingPlaneAngle",
        "TopRawX", "TopRawY", "BottomRawX", "BottomRawY", "LeftRawX", "LeftRawY", "RightRawX", "RightRawY",
        "TopAdjustedX", "TopAdjustedY", "BottomAdjustedX", "BottomAdjustedY", "LeftAdjustedX", "LeftAdjustedY", "RightAdjustedX", "RightAdjustedY",
        "RawDataFileName" };

    // Class attributes to store data read from the file
    public string datetime;
    public string movement { get; private set; }
    public bool isReadOnly { get; private set; } = false;

    // Plane in which the AROM assessment is done.
    public float trainingPlaneAngle { get; private set; }

    // Raw data recorded during the assessment of AROM.
    private List<float[]> rawData;

    // Locations of the raw AROM quadrilateral
    public UnityEngine.Vector2 topRaw { get; private set; }
    public UnityEngine.Vector2 bottomRaw { get; private set; }
    public UnityEngine.Vector2 leftRaw { get; private set; }
    public UnityEngine.Vector2 rightRaw { get; private set; }

    // Locations of the adjusted AROM quadrilateral
    public UnityEngine.Vector2 topAdjusted { get; private set; }
    public UnityEngine.Vector2 bottomAdjusted { get; private set; }
    public UnityEngine.Vector2 leftAdjusted { get; private set; }
    public UnityEngine.Vector2 rightAdjusted { get; private set; }
    private string rawDataFilename;
    public bool isAssessing => rawData != null;

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
        rawData = null;
        topRaw = UnityEngine.Vector2.zero;
        bottomRaw = UnityEngine.Vector2.zero;
        leftRaw = UnityEngine.Vector2.zero;
        rightRaw = UnityEngine.Vector2.zero;
        topAdjusted = UnityEngine.Vector2.zero;
        bottomAdjusted = UnityEngine.Vector2.zero;
        leftAdjusted = UnityEngine.Vector2.zero;
        rightAdjusted = UnityEngine.Vector2.zero;
        trainingPlaneAngle = AppData.Instance.userData.trainingPlaneAngle;
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
            // Top point is the average of the bottom 5% of the points.
            topRaw = AverageofExtremeEnds(orderedByY, 0.05f, false);
            // Bottom point is the average of the top 5% of the points.
            bottomRaw = AverageofExtremeEnds(orderedByY, 0.05f, true);
            // Left point is the average of the top 5% of the points.
            leftRaw = AverageofExtremeEnds(orderedByX, 0.05f, true);
            // Right point is the average of the bottom 5% of the points.
            rightRaw = AverageofExtremeEnds(orderedByX, 0.05f, false);
            // Adjusted points are same as raw points initially.
            topAdjusted = new UnityEngine.Vector2(topRaw.x, topRaw.y);
            bottomAdjusted = new UnityEngine.Vector2(bottomRaw.x, bottomRaw.y);
            leftAdjusted = new UnityEngine.Vector2(leftRaw.x, leftRaw.y);
            rightAdjusted = new UnityEngine.Vector2(rightRaw.x, rightRaw.y);
        }
        rawData = null;
    }

    public void setAdjustedAromTop(float x, float y) => topAdjusted = new UnityEngine.Vector2(x, y);

    public void setAdjustedAromBottom(float x, float y) => bottomAdjusted = new UnityEngine.Vector2(x, y);

    public void setAdjustedAromLeft(float x, float y) => leftAdjusted = new UnityEngine.Vector2(x, y);

    public void setAdjustedAromRight(float x, float y) => rightAdjusted = new UnityEngine.Vector2(x, y);

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
        // First write the raw data file.
        string _rawfilename = DataManager.GetRomRawFileName(movement, datetime);
        // Write the assessment data to the file.
        using (StreamWriter file = new StreamWriter(fileName, true))
        {
            // Write the actual data
            file.WriteLine(string.Join(",", new string[] {
                datetime, trainingPlaneAngle.ToString(),
                topRaw.x.ToString(), topRaw.y.ToString(), bottomRaw.x.ToString(), bottomRaw.y.ToString(),
                leftRaw.x.ToString(), leftRaw.y.ToString(), rightRaw.x.ToString(), rightRaw.y.ToString(),
                topAdjusted.x.ToString(), topAdjusted.y.ToString(), bottomAdjusted.x.ToString(), bottomAdjusted.y.ToString(),
                leftAdjusted.x.ToString(), leftAdjusted.y.ToString(), rightAdjusted.x.ToString(), rightAdjusted.y.ToString(),
                _rawfilename
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
        // Assign the raw locations
        topRaw = new UnityEngine.Vector2(float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("TopRawX")),
                                         float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("TopRawY")));
        bottomRaw = new UnityEngine.Vector2(float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("BottomRawX")),
                                            float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("BottomRawY")));
        leftRaw = new UnityEngine.Vector2(float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("LeftRawX")),
                                          float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("LeftRawY")));
        rightRaw = new UnityEngine.Vector2(float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("RightRawX")),
                                           float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("RightRawY")));
        // Assign the adjusted locations
        topAdjusted = new UnityEngine.Vector2(float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("TopAdjustedX")),
                                              float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("TopAdjustedY")));
        bottomAdjusted = new UnityEngine.Vector2(float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("BottomAdjustedX")),
                                                 float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("BottomAdjustedY")));
        leftAdjusted = new UnityEngine.Vector2(float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("LeftAdjustedX")),
                                               float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("LeftAdjustedY")));
        rightAdjusted = new UnityEngine.Vector2(float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("RightAdjustedX")),
                                                float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("RightAdjustedY")));
        trainingPlaneAngle = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("TrainingPlaneAngle"));
        return true;
    }

    private UnityEngine.Vector2 AverageofExtremeEnds(List<float[]> orderList, float percentage = 0.1f, bool fromStart = true)
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
        return new UnityEngine.Vector2(avgX, avgY);
    }
}

// Arm Weight Class.
public class ArmWeight
{
    // Class attributes to store data read from the file
    public enum ARMWEIGHT_TARGET
    {
        NONE = -1,
        LEFT = 0,
        RIGHT = 1,
        TOP = 2,
        BOTTOM = 3,
        CENTER = 4,
    }

    public string datetime;
    public MarsArom mlapArom { get; private set; }
    public float trainingPlaneAngle => mlapArom != null ? mlapArom.trainingPlaneAngle : 0f;

    // Raw data recorded during the assessment of AROM: each entry is [x, y, force]
    private List<float[]> rawData;
    public ARMWEIGHT_TARGET currentTarget { get; private set; } = ARMWEIGHT_TARGET.NONE;
    public float[,] targetPos { get; private set; }
    public float[,] actualPos { get; private set; }
    public float[] actualForce { get; private set; }
    private string rawDataFilename;
    public bool[] targetAssessmentStatus { get; private set; }
    public bool isAssessmentComplete => targetAssessmentStatus != null && targetAssessmentStatus.All(status => status);
    public bool isAssessingTarget => rawData != null;
    public bool isReadOnly { get; private set; } = false;

    public static bool ArmWeightFileExists() => File.Exists(DataManager.armWeightFile);

    // Constructor that reads the file and initializes values based on the mechanism
    public ArmWeight(bool readFromFile)
    {
        if (readFromFile) isReadOnly = ReadFromFile();
        if (!isReadOnly) initializeNewArmWeightAssessment();
    }

    private void initializeNewArmWeightAssessment()
    {
        datetime = DateTime.Now.ToString();
        mlapArom = new MarsArom("MLAP", readFromFile: true);
        targetPos = null;
        actualPos = null;
        actualForce = null;
        targetAssessmentStatus = null;
        rawData = null;
        isReadOnly = false;
    }

    public void initializeArmWeightAssessment()
    {
        currentTarget = ARMWEIGHT_TARGET.NONE;
        targetPos = new float[5, 2];
        actualPos = new float[5, 2];
        actualForce = new float[5];
        targetAssessmentStatus = new bool[5];
        // Assign all assessment status to false.
        for (int i = 0; i < targetAssessmentStatus.Length; i++)
        {
            targetAssessmentStatus[i] = false;
        }
        AppLogger.LogInfo("Initialized new arm weight assessment.");
    }

    public void startArmWeightAssessment(ARMWEIGHT_TARGET target)
    {
        if (target == ARMWEIGHT_TARGET.NONE) return;
        if (rawData == null)
        {
            rawData = new List<float[]>();
            currentTarget = target;
            int idx = (int)currentTarget;
            float[] _targetPos = getMLAPAromTarget(currentTarget);  
            targetPos[idx, 0] = _targetPos[0];
            targetPos[idx, 1] = _targetPos[1];
            AppLogger.LogInfo($"Starting arm weight assessment for target {currentTarget} at position ({_targetPos[0]}, {_targetPos[1]}).");
        }
    }

    public void addArmWeightDataPoint(float x, float y, float force)
    {
        if (rawData != null)
        {
            rawData.Add(new float[] { x, y, force });
        }
    }

    public void stopArmWeightAssessment()
    {
        if (rawData != null && rawData.Count > 0)
        {
            // Compute the average of the recorded positions and force.
            int idx = (int)currentTarget;
            actualPos[idx, 0] = rawData.Average(p => p[0]);
            actualPos[idx, 1] = rawData.Average(p => p[1]);
            actualForce[idx] = rawData.Average(p => p[2]);
            // Update target assessment status
            targetAssessmentStatus[idx] = true;
        }
        rawData = null;
        currentTarget = ARMWEIGHT_TARGET.NONE;
    }

    private bool ReadFromFile()
    {
        string fileName = DataManager.armWeightFile;
        if (!File.Exists(fileName))
        {
            AppLogger.LogWarning($"No Arm Weight assessment file found. Starting new assessment.");
            return false;
        }

        // Load the data from the file
        DataTable romData = DataManager.loadCSV(fileName);

        // Check the number of rows.
        if (romData.Rows.Count == 0)
        {
            AppLogger.LogWarning($"No previous Arm Weight assessment found in the file. Starting new assessment.");
            return false;
        }
        // Assign Arm Weight from the last row.
        datetime = romData.Rows[romData.Rows.Count - 1].Field<string>("DateTime");
        // Assign the locations and forces
        targetPos = new float[5, 2];
        actualPos = new float[5, 2];
        actualForce = new float[5];
        targetAssessmentStatus = new bool[5];
        // Left
        targetPos[0, 0] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("LeftTargetX"));
        targetPos[0, 1] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("LeftTargetY"));
        actualPos[0, 0] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("LeftActualX"));
        actualPos[0, 1] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("LeftActualY"));
        actualForce[0] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("LeftForce"));
        targetAssessmentStatus[0] = actualForce[0] > 0f;
        // Right
        targetPos[1, 0] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("RightTargetX"));
        targetPos[1, 1] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("RightTargetY"));
        actualPos[1, 0] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("RightActualX"));
        actualPos[1, 1] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("RightActualY"));
        actualForce[1] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("RightForce"));
        targetAssessmentStatus[1] = actualForce[1] > 0f;
        // Top
        targetPos[2, 0] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("TopTargetX"));
        targetPos[2, 1] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("TopTargetY"));
        actualPos[2, 0] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("TopActualX"));
        actualPos[2, 1] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("TopActualY"));
        actualForce[2] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("TopForce"));
        targetAssessmentStatus[2] = actualForce[2] > 0f;
        // Bottom
        targetPos[3, 0] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("BottomTargetX"));
        targetPos[3, 1] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("BottomTargetY"));
        actualPos[3, 0] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("BottomActualX"));
        actualPos[3, 1] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("BottomActualY"));
        actualForce[3] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("BottomForce"));
        targetAssessmentStatus[3] = actualForce[3] > 0f;
        // Center
        targetPos[4, 0] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("CenterTargetX"));
        targetPos[4, 1] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("CenterTargetY"));
        actualPos[4, 0] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("CenterActualX"));
        actualPos[4, 1] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("CenterActualY"));
        actualForce[4] = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("CenterForce"));
        targetAssessmentStatus[4] = actualForce[4] > 0f;
        return true;
    }

    public void WriteToArmWeightFile()
    {
        // Check if assessment is complete. Else there is nothing to write.
        if (!isAssessmentComplete)
        {
            AppLogger.LogWarning("Arm weight assessment is not complete. Cannot write to file.");
            return;
        }

        // Assessment is complete
        string fileName = DataManager.armWeightFile;
        // Create the file if it doesn't exist
        if (!File.Exists(fileName))
        {
            using (var file = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                // Write the pre-header to the file.
                StringBuilder rawDataString = new StringBuilder();
                rawDataString.AppendLine(string.Join(",", DataManager.ARMWEIGHTFILEHEADER));
                file.Write(rawDataString.ToString());
            }
        }
        // First write the raw data file.
        string _rawfilename = DataManager.GetArmWeightRawFileName(datetime);
        // Write the assessment data to the file.
        using (StreamWriter file = new StreamWriter(fileName, true))
        {
            // Write the actual data
            file.WriteLine(string.Join(",", new string[] {
                datetime, trainingPlaneAngle.ToString(),
                targetPos[0,0].ToString(), targetPos[0,1].ToString(), actualPos[0,0].ToString(), actualPos[0,1].ToString(), actualForce[0].ToString(),
                targetPos[1,0].ToString(), targetPos[1,1].ToString(), actualPos[1,0].ToString(), actualPos[1,1].ToString(), actualForce[1].ToString(),
                targetPos[2,0].ToString(), targetPos[2,1].ToString(), actualPos[2,0].ToString(), actualPos[2,1].ToString(), actualForce[2].ToString(),
                targetPos[3,0].ToString(), targetPos[3,1].ToString(), actualPos[3,0].ToString(), actualPos[3,1].ToString(), actualForce[3].ToString(),
                targetPos[4,0].ToString(), targetPos[4,1].ToString(), actualPos[4,0].ToString(), actualPos[4,1].ToString(), actualForce[4].ToString(),
                _rawfilename
            }));
        }
    }
    
    private float[] getMLAPAromTarget(ARMWEIGHT_TARGET target)
    {
        switch(target)
        {
            case ARMWEIGHT_TARGET.LEFT:
                return new float[] { mlapArom.leftAdjusted.x, mlapArom.leftAdjusted.y };
            case ARMWEIGHT_TARGET.RIGHT:
                return new float[] { mlapArom.rightAdjusted.x, mlapArom.rightAdjusted.y };
            case ARMWEIGHT_TARGET.TOP:
                return new float[] { mlapArom.topAdjusted.x, mlapArom.topAdjusted.y };
            case ARMWEIGHT_TARGET.BOTTOM:
                return new float[] { mlapArom.bottomAdjusted.x, mlapArom.bottomAdjusted.y };
            case ARMWEIGHT_TARGET.CENTER:
                return new float[] {
                    (mlapArom.leftAdjusted.x + mlapArom.rightAdjusted.x + mlapArom.topAdjusted.x + mlapArom.bottomAdjusted.x) / 4,
                    (mlapArom.leftAdjusted.y + mlapArom.rightAdjusted.y + mlapArom.topAdjusted.y + mlapArom.bottomAdjusted.y) / 4
                };
        }   
        return null;
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