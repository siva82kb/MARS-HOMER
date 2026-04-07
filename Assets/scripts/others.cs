using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Text;
using System.Numerics;
using static UnityEngine.Rendering.DebugUI.Table;


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
    public const string HOMERID = "HomerID";
    public const string STARTEDATEH = "StartDate";
    public const string ENDDATEH = "EndDate";
    public const string TRAININGSIDE = "TrainingSide";

    public bool isExceeded { get; private set; }
    public DataTable dTableConfig { get; private set; } = null;
    public DataTable dTableSession { get; private set; } = null;

    public string userID { get; private set; }
    public string hospNumber { get; private set; }
    public DateTime startDate { get; private set; }
    public DateTime endDate { get; private set; }
    public bool rightArm { private set; get; }
    public int limb { get { return rightArm ? 1 : 2; } }

    public float trainingPlaneAngle { get; private set; } = 0f; // In degrees
    public string errorStatus { get; private set; } = null; // In degrees

    public ArmWeight armWeight { get; private set; } = null;

    public Dictionary<string, float> moveTimePrsc { get; private set; } // Prescribed movement time
    public Dictionary<string, float> moveTimeCurr { get; private set; } // Current movement time
    public Dictionary<string, float> moveTimePrev { get; private set; } // Previous movement time 
    private String[] ERRORSTATUS = new string[] { "RESOLVED", "NOTRESOLVED"};
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
        
         // Read parse the training plane data if it exists.
        if (!File.Exists(DataManager.errorLogFile)) DataManager.CreateErrorLogFile(this.userID, "MARS", GetDeviceLocation());
        readErrorLogData(DataManager.errorLogFile);

        // Read the arm weight data if it exists.
        if (!File.Exists(DataManager.armWeightFile)) DataManager.CreateArmWeightFile(this.userID, "MARS", GetDeviceLocation());
        armWeight = new ArmWeight(true);
    }

    public void reloadTrainingPlaneAngle()
    {
        readParseTrainingPlaneData(DataManager.trainingPlaneFile);
    }

    public void reloadArmWeightData()
    {
        armWeight = new ArmWeight(true);
    }

    public void parsemoveTimePrev()
    {
        moveTimePrev = createMoveTimeDictionary();
        for (int i = 0; i < MarsDefs.Movements.Length; i++)
        {
            var _totalMoveTime = dTableSession.AsEnumerable()
                .Where(row => DateTime.ParseExact(row.Field<string>(DATETIME), DataManager.DATETIMEFORMAT, CultureInfo.InvariantCulture).Date == DateTime.Now.Date)
                .Where(row => row.Field<string>(MOVEMENT) == MarsDefs.Movements[i])
                .Sum(row => Convert.ToSingle(row[MOVETIME]));
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
        hospNumber = lastRow.Field<string>(HOMERID);
        rightArm = lastRow.Field<string>(TRAININGSIDE).ToUpper() == "RIGHT";
        startDate = DateTime.ParseExact(lastRow.Field<string>(STARTEDATEH), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        endDate = DateTime.ParseExact(lastRow.Field<string>(ENDDATEH), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
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
    private void readErrorLogData(string errorLogFile)
    {
        DataTable dErrorLog = DataManager.loadCSV(errorLogFile);
        if (dErrorLog.Rows.Count == 0) return;
        DataRow lastRow = dErrorLog.Rows[dErrorLog.Rows.Count - 1];
        errorStatus = lastRow.Field<string>("Status");
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
    public void writeUpdateErrorLogData(String error)
    {
        // Create the file if it does not exist.
        if (!File.Exists(DataManager.errorLogFile)) DataManager.CreateErrorLogFile(userID, "MARS", GetDeviceLocation());
        // Append the new training plane angle to the file.
        using (var writer = new StreamWriter(DataManager.errorLogFile, true, Encoding.UTF8))
        {
            string _dtstr = DateTime.Now.ToString(DataManager.DATETIMEFORMAT);
             string trialNo = AppData.Instance.selectedMovement == null 
            ? "null" 
            : AppData.Instance.selectedMovement.trialNumberDay.ToString();

            writer.WriteLine(
                $"{_dtstr}," +
                $"{AppData.Instance.userData.hospNumber}," +
                $"{AppData.Instance.currentSessionNumber}," +
                $"{trialNo}," +
                $"{AppLogger.currentScene}," +
                $"{AppLogger.currentMovement}," +
                $"{error}," +
                $"{ERRORSTATUS[1]}"
            );
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

    public int DaysSinceAromAssessmentForTrainingAngle(string movement)
    {
        // Check of AROM is available.
        if (!IsAromAssessmentAvailableForTrainingAngle(movement)) return -1;

        // AROM available.
        MarsArom arom = new MarsArom(movement, readFromFile: true);
        // Compuate date difference only considering dates, while ignoring time.
        DateTime aromDate = DateTime.ParseExact(arom.datetime, DataManager.DATETIMEFORMAT, CultureInfo.InvariantCulture);
        TimeSpan duration = DateTime.Now.Date - aromDate.Date;
        return (int)duration.TotalDays;
    }

    public bool IsArmWeightAssessmentAvailableForTrainingAngle()
    {
        if (ArmWeight.ArmWeightFileExists())
        {
            var aw = new ArmWeight(readFromFile: true);

            if (!aw.isAssessmentComplete) return false;
            return Mathf.Abs(aw.trainingPlaneAngle - AppData.Instance.userData.trainingPlaneAngle) <= MarsDefs.TRAINING_PLANE_ANGLE_THRESHOLD;
        }
        return false;
    }

    public int DaysSinceArmWeightAssessmentForTrainingAngle()
    {
        // Check of Arm Weight is available.
        if (!IsArmWeightAssessmentAvailableForTrainingAngle()) return -1;

        // Arm Weight available.
        ArmWeight aw = new ArmWeight(readFromFile: true);
        // Date string format: 13-10-2025 08:05:19
        DateTime awDate = DateTime.ParseExact(aw.datetime, DataManager.DATETIMEFORMAT, CultureInfo.InvariantCulture);
        TimeSpan duration = DateTime.Now.Date - awDate.Date;
        return (int)duration.TotalDays;
    }
    public bool isErrorOccurred()
    {
        if (errorStatus == null) return false;
        if (errorStatus.ToUpper() ==  ERRORSTATUS[1])
        {
            return true;
        }
        return false;
    }
    
    public int[] readCummulativeHitsMissesForGameMovement(string gameName, string movementName)
    {
        // Get the last row for the given game.
        var lastGameRows = dTableSession.AsEnumerable()?
            .Where(row => row.Field<string>("GameName") == gameName && row.Field<string>("Movement") == movementName).LastOrDefault();
        // If there are no rows, set the cummulative score to zero.
        if (lastGameRows == null)
        {
            AppLogger.LogInfo($"No previous data found for game '{gameName}' and movement '{movementName}'. Cummulative hits and misses set to zero.");
            return new int[] { 0, 0, 0 };
        }
        // Get the cummulative hits and misses for the game from the last row.
        int[] cuScores = new int[]
        {
            Convert.ToInt32(lastGameRows.Field<string>("CummulativeTargets")),
            Convert.ToInt32(lastGameRows.Field<string>("CummulativeHits")),
            Convert.ToInt32(lastGameRows.Field<string>("CummulativeMisses"))
        };
        AppLogger.LogInfo($"Cummulative hits and misses for game '{gameName}' and '{movementName}' updated. Targets: {cuScores[0]} | Hits: {cuScores[1]} | Misses: {cuScores[2]}.");
        return cuScores;
    }

    public int[] readStarCounts(string gameName)
    {
        var lastRow = dTableSession.AsEnumerable().LastOrDefault();
        if (lastRow == null) return new int[] { 0, 0 ,0};
        int cummulativeStarCounts = Convert.ToInt32(lastRow.Field<string>("CummulativeStars"));
        DateTime today = DateTime.Today;
        DateTime yesterday = today.AddDays(-1);

        // ⭐ Cumulative stars till yesterday (ACROSS ALL mechanisms)
        int cumulativeStarUntilYesterday = dTableSession.AsEnumerable()
            .Where(r => DateTime.ParseExact(
                    r.Field<string>("DateTime"), DataManager.DATETIMEFORMAT, null).Date <= yesterday)
            .Sum(r => Convert.ToInt32(r["currentStar"]));

       
        var currentStarCount = dTableSession.AsEnumerable()
                                  .Where(row => DateTime.ParseExact(row.Field<string>(DATETIME).Trim(), DataManager.DATETIMEFORMAT, CultureInfo.InvariantCulture).Date == today.Date &&
                                         row.Field<string>("GameName") == gameName
                                         )
                                  .Sum(row => Convert.ToInt32(row["currentStar"]));

        return new int[] { cummulativeStarCounts, currentStarCount ,cumulativeStarUntilYesterday};
    }
    

    public int[] getLastDatesScore(String gameName)
    {
        AppData.Instance.reloadSessionDetails();
        var table = AppData.Instance.userData.dTableSession;

        if (table == null || table.Rows.Count == 0)
            return new[] { 0, 0 };

       
        //if it is a new day then only get last date data
        var lastRow = table.Rows[table.Rows.Count - 1];
        DateTime lastDate = DateTime.ParseExact(lastRow.Field<string>(DATETIME),DataManager.DATETIMEFORMAT,CultureInfo.InvariantCulture);
        Debug.Log($"{lastDate}");

        //confirms only lastDate and Today data Comparison
        if (lastDate.Date != DateTime.Today.Date)
        {
            int score = GetScoreForDate(lastDate, gameName);
            return new[] { 0, score };
        }

        //collect all dates
        List<DateTime> allDates = new List<DateTime>();

        foreach (var row in table.AsEnumerable())
        {
            string dateStr = row.Field<string>(DATETIME);

            if (DateTime.TryParseExact(
                    dateStr,
                    DataManager.DATETIMEFORMAT,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime dt))
            {
                allDates.Add(dt.Date);
            }
        }

        if (allDates.Count == 0)
            return new[] { 0, 0 };

        var distinctDates = allDates
            .Distinct()
            .OrderByDescending(d => d)
            .Take(2)
            .ToList();

        // If there is only ONE unique date:
        if (distinctDates.Count == 1)
            return new[] { GetScoreForDate(distinctDates[0],gameName), 0 };

        DateTime date1 = distinctDates[0]; // today date
        DateTime date2 = distinctDates[1]; // yesterday date

        int score1 = GetScoreForDate(date1, gameName);
        int score2 = GetScoreForDate(date2, gameName);
        //Debug.Log($"{score1},{score2} from getfuntion");
        return new[] { score1, score2 };
    }
    public (DateTime date, int totalStars, float gameParameter)GetLastPlayedDateAndStarsForGame(string gameName)
    {
        float DefalutScrubarea = MarsGameDefs.TableWiping.MAX_SCRUB_SIZE;
        if (dTableSession == null || dTableSession.Rows.Count == 0)
            return (DateTime.MinValue, 0,DefalutScrubarea);

        //Get last played row for THIS game
        var lastRowForGame = dTableSession.AsEnumerable()
            .Where(row => row.Field<string>("GameName") == gameName)
            .OrderByDescending(row =>
                DateTime.ParseExact(
                    row.Field<string>(DATETIME).Trim(),
                    DataManager.DATETIMEFORMAT,
                    CultureInfo.InvariantCulture))
            .FirstOrDefault();

        if (lastRowForGame == null)
            return (DateTime.MinValue, 0, MarsGameDefs.TableWiping.MAX_SCRUB_SIZE);

        DateTime lastPlayedDate = DateTime.ParseExact(
            lastRowForGame.Field<string>(DATETIME).Trim(),
            DataManager.DATETIMEFORMAT,
            CultureInfo.InvariantCulture).Date;

        //  Sum stars for that game on that date
        int totalStars = dTableSession.AsEnumerable()
            .Where(row =>
                row.Field<string>("GameName") == gameName &&
                DateTime.ParseExact(
                    row.Field<string>(DATETIME).Trim(),
                    DataManager.DATETIMEFORMAT,
                    CultureInfo.InvariantCulture).Date == lastPlayedDate)
            .Sum(row => Convert.ToInt32(row["currentStar"]));

        //Get GameParameter  --scrubsize
        float areaToErase = Convert.ToSingle(lastRowForGame["GameParameter"]);
        

        return (lastPlayedDate, totalStars,areaToErase == 0 ?DefalutScrubarea:areaToErase);
    }


    private int GetScoreForDate(DateTime targetDate,String gameName)
    {
        var table = AppData.Instance.userData.dTableSession;
        int total = AppData.Instance.userData.dTableSession.AsEnumerable()
              .Where(row => DateTime.ParseExact(row.Field<string>(DATETIME), DataManager.DATETIMEFORMAT, CultureInfo.InvariantCulture).Date == targetDate.Date &&
                     row.Field<string>("GameName") == gameName
                     )
              .Sum(row => Convert.ToInt32(row["CurrentHits"]));
        return total;
    }

    public float readReachSpeedForGameMovement(string gameName, string movementName)
    {
        // Get the last row for the given game and movement.
        var lastGameRows = dTableSession.AsEnumerable()?
            .Where(row => row.Field<string>("GameName") == gameName && row.Field<string>("Movement") == movementName).LastOrDefault();
        
        // If there are no rows, set the cummulative score to zero.
        if (lastGameRows == null)
        {
            float _defaultReachSpeed = MarsGameDefs.DEFAULT_REACH_SPEED;
            AppLogger.LogInfo($"No previous data found for game '{gameName}' and movement '{movementName}'. Default values set : Reach Speed: {_defaultReachSpeed}.");
            return _defaultReachSpeed;
        }

        // Get the game speed for the game from the last row.
        float rSpeed = float.Parse(lastGameRows.Field<string>("ReachSpeed"));
        AppLogger.LogInfo($"Game speed for game '{gameName}' and movement '{movementName}'. Reach Speed: {rSpeed}.");
        return rSpeed;
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

// Mars Game definitions
public static class MarsGameDefs
{
    public static readonly string[] GAMES = new string[] { "SS", "PP", "DC", "TW","TT","MC" };
    public static readonly string[] GAME_SCENES = new string[] { "SS", "PP", "DC", "TW","TT","MC" };
    public static readonly string[] GAMEFULLNAMES = new string[] { "Space Shooter", "Ping Pong", "Diamond Catcher","Table Wiping" ,"Tuk Tuk","Match Catch" };
    
    // Reach duration are used to compute the games speeds. These are the durations
    // set for reaching from one extreme of the AROM to the other extreme.
    public const float REACH_SPEED_DELTA = 0.005f;      // m/sec
    public const float MIN_REACH_SPEED = 0.025f;        // m/sec
    public const float MAX_REACH_SPEED = 0.25f;         // m/sec
    public const float DEFAULT_REACH_SPEED = 0.025f;    // m/sec

    public static Dictionary<string, float[]> SCREEN_LIMITS = new Dictionary<string, float[]>()
    {
        { "SS", new float[] { Spaceshooter.LEFTLIMIT, Spaceshooter.RIGHTLIMIT, Spaceshooter.BOTTOMLIMIT, Spaceshooter.TOPLIMIT } },
        { "PP", new float[] { PingPong.LEFTLIMIT, PingPong.RIGHTLIMIT, PingPong.BOTTOMLIMIT, PingPong.TOPLIMIT } },
        { "DC", new float[] { DiamondCatcher.LEFTLIMIT, DiamondCatcher.RIGHTLIMIT, DiamondCatcher.BOTTOMLIMIT, DiamondCatcher.TOPLIMIT } },
        { "TT", new float[] { TukTuk.LEFTLIMIT, TukTuk.RIGHTLIMIT, TukTuk.BOTTOMLIMIT, TukTuk.TOPLIMIT } },
        { "TW", new float[] { TableWiping.LEFTLIMIT, TableWiping.RIGHTLIMIT, TableWiping.BOTTOMLIMIT, TableWiping.TOPLIMIT } },
        { "MC", new float[] { MatchCatch.LEFTLIMIT, MatchCatch.RIGHTLIMIT, MatchCatch.BOTTOMLIMIT, MatchCatch.TOPLIMIT } }

    };

    public static Dictionary<string, float> GAMEDURATION = new Dictionary<string, float>()
    {
        { "SS", Spaceshooter.GAMEDURATION },
        { "PP", PingPong.GAMEDURATION },
        { "DC", DiamondCatcher.GAMEDURATION },
        {"TT", TukTuk.GAMEDURATION},
        {"TW", TableWiping.GAMEDURATION },
        {"MC", MatchCatch.GAMEDURATION },
    };

    public static float GetGameSpeedForGame(string game, float reachSpeed, MarsArom arom)
    {
        switch (game.ToUpper())
        {
            case "SS":
                return Spaceshooter.GetGameSpeed(reachSpeed, arom);
            case "PP":
                return PingPong.GetGameSpeed(reachSpeed, arom);
            case "DC":
                return DiamondCatcher.GetGameSpeed(reachSpeed, arom);
            case "TT":
                return TukTuk.GetGameSpeed(reachSpeed, arom);
            case "MC":
                return MatchCatch.GetGameSpeed(reachSpeed, arom);
            default:
                throw new Exception($"Invalid game name '{game}'");
        }
    }

    public static float GetReachDurationForGame(string game, float reachSpeed, MarsArom arom)
    {
        switch (game.ToUpper())
        {
            case "SS":
                return Spaceshooter.GetReachDuration(reachSpeed, arom);
            case "PP":
                return PingPong.GetReachDuration(reachSpeed, arom);
            case "DC":
                return DiamondCatcher.GetReachDuration(reachSpeed, arom);
            case "TT":
                return TukTuk.GetReachDuration(reachSpeed, arom);
            case "MC":
                return MatchCatch.GetReachDuration(reachSpeed, arom);
            default:
                throw new Exception($"Invalid game name '{game}'");
        }
    }

    // SpaceShooter specific definitions
    public static class Spaceshooter
    {
        // Screen limit constants
        public const float LEFTLIMIT = -7.5f;
        public const float RIGHTLIMIT = 7.5f;
        public const float TOPLIMIT = 3.85f;
        public const float BOTTOMLIMIT = -3.85f;

        //Target Limit constants
        public const float tarSTARTPOINT = 5.19f;
        public const float tarENDPOINT = -3;
        // Game duration
        public const float GAMEDURATION = 60f; // seconds

        // Space ship firing constants.
        public const float FIRING_INTERVAL = 0.25f;
        public const float LOW_SPEED_THRESHOLD = 2.5f;  // cm/sec 

        public static float GetReachDuration(float reachSpeed, MarsArom arom)
        {
            // Find the duration for the given speed.
            reachSpeed = Math.Clamp(reachSpeed, MIN_REACH_SPEED, MAX_REACH_SPEED);
            return Math.Abs((arom.rightAdjusted.x - arom.leftAdjusted.x) / reachSpeed);
        }
        public static float GetGameSpeed(float reachSpeed, MarsArom arom)
        {
            // Find the duration for the given speed.
            reachSpeed = Math.Clamp(reachSpeed, MIN_REACH_SPEED, MAX_REACH_SPEED);
            float reachDuration = GetReachDuration(reachSpeed, arom);
            float gameSpeed = Math.Abs((tarSTARTPOINT - tarENDPOINT) / reachDuration);
            AppLogger.LogInfo($"Computing game speed for Space Shooter. Reach Speed: {reachSpeed} | AROM Limits: ({arom.rightAdjusted.x}, {arom.leftAdjusted.x}) | Reach Duration: {reachDuration} | Screen Limits: ({TOPLIMIT}, {BOTTOMLIMIT}) | Game Speed: {gameSpeed}");
            return gameSpeed;
        }
        //Game Achievement Data
        public static int[] GetScores()
        {
            return AppData.Instance.userData.getLastDatesScore("SS");
        }

        public static int[] GetStarsCount()
        {
            return AppData.Instance.userData.readStarCounts("SS");
        }

        public static int[] GetCummulativeScores()
        {
            return AppData.Instance.userData.readCummulativeHitsMissesForGameMovement("SS", "ML");
        }

        public static bool IsAchievedToday()
        {
            var starsCount = GetStarsCount();
            return starsCount[1] > 0;
        }
      
    }

    // PingPong specific definitions
    public static class PingPong
    {
        // Screen limit constants
        public const float LEFTLIMIT = -7f;
        public const float RIGHTLIMIT = 7f;
        public const float TOPLIMIT = 6f;
        public const float BOTTOMLIMIT = -6f;

        // Game duration
        public const float GAMEDURATION = 60f; // seconds

        public static float GetReachDuration(float reachSpeed, MarsArom arom)
        {
            // Find the duration for the given speed.
            reachSpeed = Math.Clamp(reachSpeed, MIN_REACH_SPEED, MAX_REACH_SPEED);
            return Math.Abs((arom.topAdjusted.y - arom.bottomAdjusted.y) / reachSpeed);
        }

        public static float GetGameSpeed(float reachSpeed, MarsArom arom)
        {
            // Find the duration for the given speed.
            reachSpeed = Math.Clamp(reachSpeed, MIN_REACH_SPEED, MAX_REACH_SPEED);
            float reachDuration = GetReachDuration(reachSpeed, arom);
            float gameSpeed = Math.Abs((RIGHTLIMIT - LEFTLIMIT) / reachDuration);
            AppLogger.LogInfo($"Computing game speed for Ping Pong. Reach Speed: {reachSpeed} | AROM Limits: ({arom.topAdjusted.y}, {arom.bottomAdjusted.y}) | Reach Duration: {reachDuration} | Screen Limits: ({RIGHTLIMIT}, {LEFTLIMIT}) | Game Speed: {gameSpeed}");
            return gameSpeed;
        }
        //Game Achievement Data
        public static int[] GetScores()
        {
            return AppData.Instance.userData.getLastDatesScore("PP");
        }

        public static int[] GetStarsCount()
        {
            return AppData.Instance.userData.readStarCounts("PP");
        }

        public static int[] GetCummulativeScores()
        {
            return AppData.Instance.userData.readCummulativeHitsMissesForGameMovement("PP", "AP");
        }

        public static bool IsAchievedToday()
        {
            var starsCount = GetStarsCount();
            return starsCount[1] > 0;
        }
       
    }


    // TukTuk Specific Definitions
     public static class TukTuk
    {
        // Screen limit constants
        public const float LEFTLIMIT = -6.82f;//sprite starting point
        public const float RIGHTLIMIT = 8f;
       
        public const float TOPLIMIT = 6f;
        public const float BOTTOMLIMIT = -3f;

        //Target Limit constants
        // Game duration
        public const float GAMEDURATION = 60f; // seconds

        public static float GetReachDuration(float reachSpeed, MarsArom arom)
        {
            // Find the duration for the given speed.
            reachSpeed = Math.Clamp(reachSpeed, MIN_REACH_SPEED, MAX_REACH_SPEED);
            return Math.Abs((arom.topAdjusted.y - arom.bottomAdjusted.y) / reachSpeed);
        }

        public static float GetGameSpeed(float reachSpeed, MarsArom arom)
        {
            // Find the duration for the given speed.
            reachSpeed = Math.Clamp(reachSpeed, MIN_REACH_SPEED, MAX_REACH_SPEED);
            float reachDuration = GetReachDuration(reachSpeed, arom);
            float gameSpeed = Math.Abs((RIGHTLIMIT - LEFTLIMIT) / reachDuration);
            AppLogger.LogInfo($"Computing game speed for Tuk-Tuk. Reach Speed: {reachSpeed} | AROM Limits: ({arom.topAdjusted.y}, {arom.bottomAdjusted.y}) | Reach Duration: {reachDuration} | Screen Limits: ({RIGHTLIMIT}, {LEFTLIMIT}) | Game Speed: {gameSpeed}");
            return gameSpeed;
        }
        //Game Achievement Data
        public static int[] GetScores()
        {
            return AppData.Instance.userData.getLastDatesScore("TT");
        }

        public static int[] GetStarsCount()
        {
            return AppData.Instance.userData.readStarCounts("TT");
        }

        public static int[] GetCummulativeScores()
        {
            return AppData.Instance.userData.readCummulativeHitsMissesForGameMovement("TT", "AP");
        }

        public static bool IsAchievedToday()
        {
            var starsCount = GetStarsCount();
            return starsCount[1] > 0;
        }
       
    }

    // DiamondCatcher specific definitions
    public static class DiamondCatcher
    {
        // Screen limit constants
        public const float LEFTLIMIT = -7.5f;
        public const float RIGHTLIMIT = 7.5f;
        public const float TOPLIMIT = 4.0f;
        public const float BOTTOMLIMIT = -4.0f;

        // Game duration
        public const float GAMEDURATION = 60f;  // seconds

        // Target reach hold time.
        public const float TARGET_IN_TIME = 1f; // seconds

        public static float GetReachDuration(float reachSpeed, MarsArom arom)
        {
            // Find the duration for the given speed.
            reachSpeed = Math.Clamp(reachSpeed, MIN_REACH_SPEED, MAX_REACH_SPEED);
            return (Math.Abs(arom.rightAdjusted.x - arom.leftAdjusted.x) + Math.Abs(arom.topAdjusted.y - arom.bottomAdjusted.y)) / (2 * reachSpeed);
        }

        public static float GetGameSpeed(float reachSpeed, MarsArom arom)
        {
            // Find the duration for the given speed.
            reachSpeed = Math.Clamp(reachSpeed, MIN_REACH_SPEED, MAX_REACH_SPEED);
            float reachDuration = GetReachDuration(reachSpeed, arom);
            // Average of horizontal and vertical screen limits.
            float _avgscreen = (Math.Abs(RIGHTLIMIT - LEFTLIMIT) + Math.Abs(TOPLIMIT - BOTTOMLIMIT)) / 2;
            float gameSpeed = _avgscreen / reachDuration;
            AppLogger.LogInfo($"Computing game speed for Diamond Catcher. Reach Speed: {reachSpeed} | AROM Limits: H({arom.rightAdjusted.x}, {arom.leftAdjusted.x}) V({arom.topAdjusted.y}, {arom.bottomAdjusted.y}) | Reach Duration: {reachDuration} | Screen Limits: H({RIGHTLIMIT}, {LEFTLIMIT}) V({TOPLIMIT}, {BOTTOMLIMIT}) | Game Speed: {gameSpeed}");
            return gameSpeed;
        }
        //Game Achievement Data
        public static int[] GetScores()
        {
            return AppData.Instance.userData.getLastDatesScore("DC");
        }

        public static int[] GetStarsCount()
        {
            return AppData.Instance.userData.readStarCounts("DC");
        }

        public static int[] GetCummulativeScores()
        {
            return AppData.Instance.userData.readCummulativeHitsMissesForGameMovement("DC", "MLAP");
        }

        public static bool IsAchievedToday()
        {
            var starsCount = GetStarsCount();
            return starsCount[1] > 0;
        }
        
    }
    public static class TableWiping
    {
        // Screen limit constants
        public const float LEFTLIMIT = -8.5f;
        public const float RIGHTLIMIT = 8.5f;
        public const float TOPLIMIT = 4.2f;
        public const float BOTTOMLIMIT = -5f;

        public const float MIN_SCRUB_SIZE = 0.0001f; //m2
        public const float MAX_SCRUB_SIZE = 0.0005f; //m2
        // Game duration
        public const float GAMEDURATION = 60f;  // seconds

        //Game Achievement Data
        public static int[] GetScores()
        {
            return AppData.Instance.userData.getLastDatesScore("TW");
        }

        public static int[] GetStarsCount()
        {
            return AppData.Instance.userData.readStarCounts("TW");
        }

        public static int[] GetCummulativeScores()
        {
            return AppData.Instance.userData.readCummulativeHitsMissesForGameMovement("TW", "MLAP");
        }

        public static bool IsAchievedToday()
        {
            var starsCount = GetStarsCount();
            return starsCount[1] > 0;
        }

    }
    public static class MatchCatch
    {
        // Screen limit constants
        public const float LEFTLIMIT = -7.5f;
        public const float RIGHTLIMIT = 7.5f;
        public const float TOPLIMIT = 3.85f;
        public const float BOTTOMLIMIT = -5f;

        //Target Limit constants
        public const float tarSTARTPOINT = 5f;
        public const float tarENDPOINT = -2.5f;
        // Game duration
        public const float GAMEDURATION = 60f; // seconds

        public static float GetReachDuration(float reachSpeed, MarsArom arom)
        {
            // Find the duration for the given speed.
            reachSpeed = Math.Clamp(reachSpeed, MIN_REACH_SPEED, MAX_REACH_SPEED);
            return Math.Abs((arom.rightAdjusted.x - arom.leftAdjusted.x) / reachSpeed);
        }
        public static float GetGameSpeed(float reachSpeed, MarsArom arom)
        {
            // Find the duration for the given speed.
            reachSpeed = Math.Clamp(reachSpeed, MIN_REACH_SPEED, MAX_REACH_SPEED);
            float reachDuration = GetReachDuration(reachSpeed, arom);
            float gameSpeed = Math.Abs((tarSTARTPOINT - tarENDPOINT) / reachDuration);
            AppLogger.LogInfo($"Computing game speed for Space Shooter. Reach Speed: {reachSpeed} | AROM Limits: ({arom.rightAdjusted.x}, {arom.leftAdjusted.x}) | Reach Duration: {reachDuration} | Screen Limits: ({TOPLIMIT}, {BOTTOMLIMIT}) | Game Speed: {gameSpeed}");
            return gameSpeed;
        }
        //Game Achievement Data
        public static int[] GetScores()
        {
            return AppData.Instance.userData.getLastDatesScore("MC");
        }

        public static int[] GetStarsCount()
        {
            return AppData.Instance.userData.readStarCounts("MC");
        }

        public static int[] GetCummulativeScores()
        {
            return AppData.Instance.userData.readCummulativeHitsMissesForGameMovement("MC","ML");
        }

        public static bool IsAchievedToday()
        {
            var starsCount = GetStarsCount();
            return starsCount[1] > 0;
        }

    }
}

// Class representing MARS games.
public class MarsGame
{
    public string name { get; private set; } = null;
    public string movement { get; set; } = null;
    private float _reachSpeed;
    public float reachSpeed {
        get => _reachSpeed;
        set
        {

            if (this.name == "TW") return;
            _reachSpeed = Math.Clamp(value, MarsGameDefs.MIN_REACH_SPEED, MarsGameDefs.MAX_REACH_SPEED);
            //unity target Speed
            gameSpeed = arom != null ? MarsGameDefs.GetGameSpeedForGame(name, _reachSpeed, arom) : 0f;
            //Reach Duration(sec) for Both player and Target  To complete their respected ROM
            gameParameter = arom != null ? MarsGameDefs.GetReachDurationForGame(name, _reachSpeed, arom) : 0f;


            AppLogger.LogInfo($"Reach speed for game '{name}' and movement '{movement}' set to {_reachSpeed} (Game speed: {gameSpeed}).");
        }
    }
   
    public float gameSpeed { get; private set; }
    public float gameParameter { get; set; }
    public float gameDuration { get; set; } = 0f;
    public MarsArom arom { get; private set; } = null;
    public int currentTargets { get; private set; } = 0;
    public int currentHits { get; private set; } = 0;
    public int currentMisses { get; private set; } = 0;
    public int cummulativeTargets { get; private set; } = 0;
    public int cummulativeHits { get; private set; } = 0;
    public int cummulativeMisses { get; private set; } = 0;
    public int cummulativeStars {  get; private set; } = 0;
    public int currentStar {  get; private set; } = 0;
    public int todayStar {  get; private set; } = 0;

    public MarsGame(string gName, string mName, float rSpeed, float gDuration, MarsArom arom, int gCuTargets, int gCuHits, int gCuMisses, int gCuStars, int TodayStars)
    {
        name = gName?.ToUpper() ?? string.Empty;
        movement = mName?.ToUpper() ?? string.Empty;
        this.arom = arom;
        reachSpeed = rSpeed;
        gameDuration = gDuration;
        cummulativeTargets = gCuTargets;
        cummulativeHits = gCuHits;
        cummulativeMisses = gCuMisses;
        cummulativeStars = gCuStars;
        todayStar = TodayStars;

    }

    public void ResetCummulativeScore()
    {
        cummulativeTargets = 0;
        cummulativeHits = 0;
        cummulativeMisses = 0;
    }

    //Give star once they Achieved yesterday Score
    public void updateCummulativeStars()
    {
       
        cummulativeStars++;
        currentStar = 1;
        todayStar += currentStar;
    }
    //Reset the trailStar Count
    public void resetstarCount()
    {
        currentStar = 0;
    }
   
    //To check if they achieved Today  or not
    public bool isAchievedToday()
    {
        return todayStar > 0 ;
    }
    
    public void UpdateTargetsHitsMisses(int targets, int hits, int misses)
    {
        currentTargets = targets;
        currentHits = hits;
        currentMisses = misses;
        cummulativeTargets += targets;
        cummulativeHits += hits;
        cummulativeMisses += misses;
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
        datetime = DateTime.Now.ToString(DataManager.DATETIMEFORMAT);
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
                //rawDataString.AppendLine($":Device: MARS");
                //rawDataString.AppendLine($":Location: {AppData.Instance.userData.GetDeviceLocation()}");
                //rawDataString.AppendLine($":Movement: {movement}");
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
                _rawfilename.Split('/').Last()
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
    private float _trainingPlaneAngle;
    public float trainingPlaneAngle {
        get => _trainingPlaneAngle;
    }

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
        datetime = DateTime.Now.ToString(DataManager.DATETIMEFORMAT);
        mlapArom = new MarsArom("MLAP", readFromFile: true);
        _trainingPlaneAngle = mlapArom != null? mlapArom.trainingPlaneAngle : 0;
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
        DataTable awData = DataManager.loadCSV(fileName);

        // Check the number of rows.
        if (awData.Rows.Count == 0)
        {
            AppLogger.LogWarning($"No previous Arm Weight assessment found in the file. Starting new assessment.");
            return false;
        }
        // Assign Arm Weight from the last row.
        datetime = awData.Rows[awData.Rows.Count - 1].Field<string>("DateTime");
        _trainingPlaneAngle = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("TrainingPlaneAngle"));
        // Assign the locations and forces
        targetPos = new float[5, 2];
        actualPos = new float[5, 2];
        actualForce = new float[5];
        targetAssessmentStatus = new bool[5];
        // Left
        targetPos[0, 0] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("LeftTargetX"));
        targetPos[0, 1] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("LeftTargetY"));
        actualPos[0, 0] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("LeftActualX"));
        actualPos[0, 1] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("LeftActualY"));
        actualForce[0] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("LeftForce"));
        targetAssessmentStatus[0] = actualForce[0] > 0f;
        targetAssessmentStatus[0] = true;
        // Right
        targetPos[1, 0] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("RightTargetX"));
        targetPos[1, 1] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("RightTargetY"));
        actualPos[1, 0] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("RightActualX"));
        actualPos[1, 1] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("RightActualY"));
        actualForce[1] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("RightForce"));
        targetAssessmentStatus[1] = actualForce[1] > 0f;
        targetAssessmentStatus[1] = true;
        // Top
        targetPos[2, 0] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("TopTargetX"));
        targetPos[2, 1] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("TopTargetY"));
        actualPos[2, 0] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("TopActualX"));
        actualPos[2, 1] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("TopActualY"));
        actualForce[2] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("TopForce"));
        targetAssessmentStatus[2] = actualForce[2] > 0f;
        targetAssessmentStatus[2] = true;
        // Bottom
        targetPos[3, 0] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("BottomTargetX"));
        targetPos[3, 1] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("BottomTargetY"));
        actualPos[3, 0] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("BottomActualX"));
        actualPos[3, 1] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("BottomActualY"));
        actualForce[3] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("BottomForce"));
        targetAssessmentStatus[3] = actualForce[3] > 0f;
        targetAssessmentStatus[3] = true;
        // Center
        targetPos[4, 0] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("CenterTargetX"));
        targetPos[4, 1] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("CenterTargetY"));
        actualPos[4, 0] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("CenterActualX"));
        actualPos[4, 1] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("CenterActualY"));
        actualForce[4] = float.Parse(awData.Rows[awData.Rows.Count - 1].Field<string>("CenterForce"));
        targetAssessmentStatus[4] = actualForce[4] > 0f;
        targetAssessmentStatus[4] = true;
        AppLogger.LogInfo($"Loaded previous Arm Weight assessment from {datetime} | Training Plane: {trainingPlaneAngle}. Assessment complete: {isAssessmentComplete}");
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
                _rawfilename.Split('/').Last()
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