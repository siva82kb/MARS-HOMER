using System;
using System.Linq;
using System.IO;
using System.Data;
using UnityEngine;
using System.Text;

/*
 * Summary Data Class
 */
public struct DaySummary
{
    public string Day { get; set; }
    public string Date { get; set; }
    public float MoveTime { get; set; }
}

public class DataManager : MonoBehaviour
{
    public static readonly string basePath = FixPath(Path.Combine(Application.dataPath, "data"));
    public static string userPath;
    public static string sessionPath { get; private set; }
    public static string rawPath { get; private set; }
    public static string romPath { get; private set; }
    public static string armWeightPath { get; private set; }
    public static string trainingPlanePath { get; private set; }
    public static string gamePath { get; private set; }
    public static string logPath { get; private set; }

    public static string sessionFile { get; private set; }
    public static string configFile;
    private static readonly string configFileName = "configdata.csv";
    public static string trainingPlaneFile;
    public static string armWeightFile;
    private static readonly string armWeightFileName = "armweight.csv";
    private static readonly string trainingPlaneFileName = "trainingplane.csv";
    public static string romFile;
    private static readonly string romFileName = "rom.csv";
    public static string[] TRAININGPLANEFILEHEADER = new string[] {
        "DateTime", "TrainingPlaneAngle"
    };
    public static string[] ARMWEIGHTFILEHEADER = new string[] {
        "DateTime", "TrainingPlaneAngle",
        "LeftTargetX", "LeftTargetY", "LeftActualX", "LeftActualY", "LeftForce",
        "RightTargetX", "RightTargetY", "RightActualX", "RightActualY", "RightForce",
        "TopTargetX", "TopTargetY", "TopActualX", "TopActualY", "TopForce",
        "BottomTargetX", "BottomTargetY", "BottomActualX", "BottomActualY", "BottomForce",
        "CenterTargetX", "CenterTargetY", "CenterActualX", "CenterActualY", "CenterForce",
        "RawDataFileName"
    };
    // Session file name.
    private static string sessionFileName = "sessions.csv";
    // Session file header
    public static string[] SESSIONFILEHEADER = new string[] {
        "SessionNumber", "DateTime",
        "TrialNumberDay", "TrialNumberSession", "TrialStartTime", "TrialStopTime", "TrialRawDataFile",
        "Movement", "TrainingPlaneAngle",
        "GameName", "GameParameter", "ReachSpeed", "GameSpeed", "GameDuration",
        "SuccessRate", "MoveTime",
        "CurrentTargets", "CurrentHits", "CurrentMisses",
        "CummulativeTargets", "CummulativeHits", "CummulativeMisses",
        "RawDataFileName"
    };
    // Raw data header.
    public static string[] RAWFILEHEADER = new string[]
    {
        "DeviceRunTime", "PacketNumber",
        "Status", "ControlType", "ErrorStatus",
        "Limb", "Calibration",
        "MarsAngle1", "MarsAngle2", "MarsAngle3", "MarsAngle4",
        "ImuMarsAngle1", "ImuMarsAngle2", "ImuMarsAngle3", "ImuMarsAngle4",
        "Force",
        "Target", "Desired", "Control",
        "Button",
        "EndPointX", "EndPointY", "EndPointZ",
        "EndPointYPlane","EndPointZPlane",
        "EndPointTargetY", "EndPointTargetZ",
        "Error", "ErrorDiff", "ErrorSum",
        "GamePlayerX", "GamePlayerY",
        "GameTargetX", "GameTargetY",
        "GameState",
        "Annotation"
    };
    public static string DATETIMEFORMAT = "yyyy-MM-dd HH:mm:ss";

    // Functions to generate file names.
    public static string GetRomFileName(string movement) => FixPath(Path.Combine(romPath, $"{movement}-rom.csv"));
    public static string GetRomRawFileName(string movement, string datetime) => FixPath(Path.Combine(romPath, $"romraw-{movement}-{datetime.Replace(" ", "_").Replace(":", "-")}.csv"));
    public static string GetArmWeightRawFileName(string datetime) => FixPath(Path.Combine(armWeightPath, $"armweightraw-{datetime.Replace(" ", "_").Replace(":", "-")}.csv"));
    public static string GetTrialRawDataFileName(int sessNo, int trialNo, string game, string movement) => FixPath(Path.Combine(rawPath, $"raw-sess{sessNo:D2}-trial{trialNo:D3}-{game}-{movement}.csv"));

    public static void CreateFileStructure(string userID)
    {
        // Update the user ID path. If the userID is empty, do nothing.
        if (string.IsNullOrEmpty(userID)) return;
        // User ID is not empty.
        userPath = FixPath(Path.Combine(basePath, userID, "data"));
        configFile = userPath + $"/{configFileName}";
        sessionPath = userPath + "/sessions";
        romPath = userPath + "/rom";
        rawPath = userPath + "/rawdata";
        gamePath = userPath + "/game";
        logPath = userPath + "/applog";
        // Training Plane
        trainingPlanePath = userPath + "/trainingplane";
        trainingPlaneFile = trainingPlanePath + $"/{trainingPlaneFileName}";
        // Arm Weight
        armWeightPath = userPath + "/armweight";
        armWeightFile = armWeightPath + $"/{armWeightFileName}";
        // Session
        sessionFile = FixPath(Path.Combine(sessionPath, sessionFileName));
        Directory.CreateDirectory(sessionPath);
        Directory.CreateDirectory(romPath);
        Directory.CreateDirectory(trainingPlanePath);
        Directory.CreateDirectory(armWeightPath);
        Directory.CreateDirectory(rawPath);
        Directory.CreateDirectory(gamePath);
        Directory.CreateDirectory(logPath);
        Debug.Log("Directory created at: " + userPath);
    }

    public static string FixPath(string path) => path.Replace("\\", "/");

    public static void CreateSessionFile(string userID, string device, string location, string[] header = null)
    {
        // Ensure the Sessions.csv file has headers if it doesn't exist
        if (!File.Exists(sessionFile))
        {
            header ??= SESSIONFILEHEADER;
            using (var writer = new StreamWriter(sessionFile, false, Encoding.UTF8))
            {
                // Write the preheader details
                writer.WriteLine($":Location: {location}");
                writer.WriteLine($":Device: {device}");
                writer.WriteLine($":User: {userID}");
                writer.WriteLine(string.Join(",", header));
            }
            AppLogger.LogWarning("Sessions.csv file not found. Created one.");
        }
    }

    public static void CreateTrainingPlaneFile(string userID, string device, string location, string[] header = null)
    {
        // Ensure the TrainingPlanes.csv file has headers if it doesn't exist
        if (!File.Exists(trainingPlaneFile))
        {
            header ??= TRAININGPLANEFILEHEADER;
            using (var writer = new StreamWriter(trainingPlaneFile, false, Encoding.UTF8))
            {
                // Write the preheader details
                writer.WriteLine($":Location: {location}");
                writer.WriteLine($":Device: {device}");
                writer.WriteLine($":User: {userID}");
                writer.WriteLine(string.Join(",", header));
            }
            AppLogger.LogWarning($"{trainingPlaneFileName} file not found. Created one.");
        }
    }

    public static void CreateArmWeightFile(string userID, string device, string location, string[] header = null)
    {
        // Ensure the ArmWeight.csv file has headers if it doesn't exist
        if (!File.Exists(armWeightFile))
        {
            header ??= ARMWEIGHTFILEHEADER;
            using (var writer = new StreamWriter(armWeightFile, false, Encoding.UTF8))
            {
                // Write the preheader details
                writer.WriteLine($":Location: {location}");
                writer.WriteLine($":Device: {device}");
                writer.WriteLine($":User: {userID}");
                writer.WriteLine(string.Join(",", header));
            }
            AppLogger.LogWarning($"{armWeightFileName} file not found. Created one.");
        }
    }

    public static DataTable loadCSV(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }
        DataTable dTable = new DataTable();
        var lines = File.ReadAllLines(filePath);
        if (lines.Length == 0) return null;

        // Ignore all preheaders that start with ':'
        int i = 0;
        while (lines[i].StartsWith(":")) i++;
        // Only preheader lines are present
        if (i >= lines.Length) return null;
        lines = lines.Skip(i).ToArray();
        // Nothing to read
        if (lines.Length == 0) return null;

        // Read and parse the header line
        var headers = lines[0].Split(',');
        foreach (var header in headers)
        {
            dTable.Columns.Add(header);
        }

        // Read the rest of the data lines
        for (i = 1; i < lines.Length; i++)
        {
            var row = dTable.NewRow();
            var fields = lines[i].Split(',');
            for (int j = 0; j < headers.Length; j++)
            {
                row[j] = fields[j];
            }
            dTable.Rows.Add(row);
        }
        return dTable;
    }
}


// Start is called before the first frame update
public enum LogMessageType
 {
        INFO,
        WARNING,
        ERROR
 }
 

public static class AppLogger
{
    private static string logFilePath;
    private static StreamWriter logWriter = null;
    private static readonly object logLock = new object();
    public static string currentScene { get; private set; } = "";
    public static string currentMovement { get; private set; } = "";
    public static string currentGame { get; private set; } = "";
    public static bool DEBUG = true;
    public static string InBraces(string text) => $"[{text}]";

    public static bool isLogging
    {
        get
        {
            return logFilePath != null;
        }
    }

    public static string StartLogging(string scene)
    {
        // Start Log file only if we are not already logging.
        if (isLogging)
        {
            return null;
        }
        if (!Directory.Exists(DataManager.logPath))
        {
            Directory.CreateDirectory(DataManager.logPath);
        }
        string _dtstr = DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss");
        logFilePath = Path.Combine(DataManager.logPath, $"{_dtstr}-application.log");

        // Create the log file and write the header.
        logWriter = new StreamWriter(logFilePath, true, Encoding.UTF8);
        currentScene = scene;
        LogInfo("Created MARS application log file.");
        return _dtstr;
    }

    public static void SetCurrentScene(string scene)
    {
        if (isLogging)
        {
            currentScene = scene;
            LogInfo($"Scene set to '{currentScene}'.");
        }
    }

    public static void SetCurrentMovement(string movement)
    {
        if (isLogging)
        {
            currentMovement = movement;
            LogInfo($"Mars movement set to '{currentMovement}'.");
        }
    }

    public static void SetCurrentGame(string game)
    {
        if (isLogging)
        {
            currentGame = game;
            LogInfo($"MARS game set to '{currentGame}'.");
        }
    }

    public static void StopLogging()
    {
        if (logWriter != null)
        {
            LogInfo("Closing application log file.");
            logWriter.Close();
            logWriter = null;
            logFilePath = null;
            currentScene = "";
        }
    }

    public static void LogMessage(string message, LogMessageType logMsgType)
    {
        lock (logLock)
        {
            if (logWriter != null)
            {
                string _user = AppData.Instance.userData != null ? AppData.Instance.userData.hospNumber : "";
                string _msg = $"{DateTime.Now:dd-MM-yyyy HH:mm:ss} {logMsgType,-7} {InBraces(_user),-10} {InBraces(currentScene),-12} {InBraces(currentMovement),-8} {InBraces(currentGame),-8} >> {message}";
                logWriter.WriteLine(_msg);
                logWriter.Flush();
                if (DEBUG) Debug.Log(_msg);
            }
        }
    }

    public static void LogInfo(string message)
    {
        LogMessage(message, LogMessageType.INFO);
    }

    public static void LogWarning(string message)
    {
        LogMessage(message, LogMessageType.WARNING);
    }

    public static void LogError(string message)
    {
        LogMessage(message, LogMessageType.ERROR);
    }
}
