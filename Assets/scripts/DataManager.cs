using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using UnityEngine;
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
    public static readonly string directoryPath = Application.dataPath + "/data";
    static string directoryPathConfig;
   
    static string directoryPathRawData;
    public static string directoryAssessmentData;
    public static string directoryPathSession { get; private set; }
    public static string filePathforConfig = $"{Application.dataPath}/data/configdata.csv";
    public static string filePathSessionData { get; private set; }
    public static string filePathAssessmentData { get; private set; }
   
    public static string filePathUploadStatus = Application.dataPath + "/uploadStatus.txt";
    public static string SupportCalibrationFileName = "SupportCalibration.csv";
    public static string[] ROMWithSupportFileNames = new string[]
    {
      "FullWeightSupport.csv",
      "HalfWeightSupport.csv",
      "NoWeightSupport.csv"
    };
    public static void createFileStructure()
    {
        directoryAssessmentData = directoryPath + "/assessmentData";
        directoryPathSession = directoryPath + "/sessions";
        directoryPathRawData = directoryPath + "/rawdata";
        
        filePathSessionData = directoryPathSession + "/sessions.csv";
        filePathAssessmentData = directoryAssessmentData + "/assessment.csv";

        // Check if the directory exists
        if (!Directory.Exists(directoryPath))
        { 
            // If not, create the directory
            Directory.CreateDirectory(directoryPath);
            //Directory.CreateDirectory(directoryPathConfig);
            Directory.CreateDirectory(directoryPathSession);
            Directory.CreateDirectory(directoryPathRawData);
            Directory.CreateDirectory(directoryAssessmentData);
          
            File.Create(filePathSessionData).Dispose(); // Ensure the file handle is released
            File.Create(filePathAssessmentData).Dispose(); // Ensure the file handle is released
            Debug.Log("Directory created at: " + directoryPath);
        }
        writeHeader(filePathSessionData);
    }

    public static void writeHeader(string path)
    {
        try
        {
            // Check if the file exists and if it is empty (i.e., no lines in the file)
            if (File.Exists(path) && File.ReadAllLines(path).Length == 0)
            {
                // Define the CSV header string, separating each column with a comma
                string headerData = "SessionNumber,DateTime,Assessment,StartTime,StopTime,GameName,TrialDataFileLocation,DeviceSetupFile,AssistMode,AssistModeParameter,Movement,MoveTime,useHand,upperarmLength,forearmLength";

                // Write the header to the file
                File.WriteAllText(path, headerData + "\n"); // Add a new line after the header
                Debug.Log("Header written successfully.");
            }
            else
            {
                //Debug.Log("Writing failed or header already exists.");
            }
        }
        catch (Exception ex)
        {
            // Catch any other generic exceptions
            Debug.LogError("An error occurred while writing the header: " + ex.Message);
        }
    }

    /*
     * Load a CSV file into a DataTable.
     */
    public static DataTable loadCSV(string filePath)
    {
        DataTable dTable = new DataTable();
        if (!File.Exists(filePath))
        {
            return dTable;
        }
        // Read all lines from the CSV file
        var lines = File.ReadAllLines(filePath);
        if (lines.Length == 0) return null;

        // Read the header line to create columns
        var headers = lines[0].Split(',');
        foreach (var header in headers)
        {
            dTable.Columns.Add(header);
        }

        // Read the rest of the data lines
        for (int i = 1; i < lines.Length; i++)
        {
            var row = dTable.NewRow();
            var fields = lines[i].Split(',');
            
            for (int j = 0; j < headers.Length; j++)
            {
                row[j] = fields[j];
                //Debug.Log(row[j]);
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
        public static readonly string directoryPath = Application.dataPath + "/data";
        private static string logFilePath;
        private static StreamWriter logWriter = null;
        private static readonly object logLock = new object();
        public static string currentScene { get; private set; } = "";
        public static string currentMechanism { get; private set; } = "";
        public static string currentGame { get; private set; } = "";
        public static bool isLogging
        {
            get
            {
                return logFilePath != null;
            }
        }
        public static void StartLogging(string scene)
        {
            // Start Log file only if we are not already logging.
            if (isLogging)
            {
                return;
            }

            if (!Directory.Exists(directoryPath + "/applog"))
            {
                 Directory.CreateDirectory(directoryPath + "/applog");
            }

        // Not logging right now. Create a new one.
        logFilePath = directoryPath + $"/applog/log-{DateTime.Now:dd-MM-yyyy-HH-mm-ss}.log";
            if (!File.Exists(logFilePath))
            {
                using (File.Create(logFilePath)) { }
            }
            logWriter = new StreamWriter(logFilePath, true);
            currentScene = scene;
            LogInfo("Created PLUTO log file.");
        }

        public static void SetCurrentScene(string scene)
        {
            if (isLogging)
            {
                currentScene = scene;
            }
        }

        public static void SetCurrentMechanism(string movement)
        {
            if (isLogging)
            {
                currentMechanism = movement;
            }
        }

        public static void SetCurrentGame(string game)
        {
            if (isLogging)
            {
                currentGame = game;
            }
        }

        public static void StopLogging()
        {
            if (logWriter != null)
            {
                LogInfo("Closing log file.");
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
                    logWriter.WriteLine($"{DateTime.Now:dd-MM-yyyy HH:mm:ss} {logMsgType,-7} [{currentScene}] [{currentMechanism}] [{currentGame}] {message}");
                    logWriter.Flush();
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

