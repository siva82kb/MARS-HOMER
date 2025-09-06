
using System.Text;
using System;
using Debug = UnityEngine.Debug;
using UnityEngine;
using System.IO;

public partial class AppData
{

    private static readonly Lazy<AppData> _instance = new Lazy<AppData>(() => new AppData());

    public static AppData Instance => _instance.Value;


    static public readonly string COMPort = "COM4";

    /*
   * SESSION DETAILS
   */
    public int currentSessionNumber { get; set; }
    public DateTime startTime { get; private set; }
    public DateTime? stopTime { get; private set; }
    public DateTime trialStartTime { get; set; }
    public DateTime? trialStopTime { get; set; }

    /*
    * Logging file names.
    */
    public string trialRawDataFile { get; private set; } = null;
    static public string trialDataFileLocation;
    private StringBuilder rawDataString = null;
    private readonly object rawDataLock = new object();
    private StringBuilder aanExecDataString = null;
    public string userID { get; private set; } = null;
    public float successRate { get; private set; } = 0f;

    /* DO OBJECT CREATION HERE */
    public string selectedGame { get; private set; } = null;
    public MarsMovement selectedMovement { get; private set; }
    public MarsUserData userData;
    public MarsTransitionControl transitionControl { get; private set; }
    public string trainingSide => userData?.limb != null ? MarsComm.LIMBTYPE[userData.limb] : MarsComm.LIMBTYPE[0];

    public void Initialize(string scene)
    {
        // Set sesstion start time.
        startTime = DateTime.Now;

        // First check if this is a single user case.
        // Check if the base directory has only one folder.
        if (Directory.GetDirectories(DataManager.basePath).Length == 1)
        {
            // If so, set the user ID to the name of that folder.
            AppData.Instance.setUser(Path.GetFileName(Directory.GetDirectories(DataManager.basePath)[0]));
        }

        // Create file structure.
        DataManager.CreateFileStructure(AppData.Instance.userID);

        // Start logging.
        string _dtstr = AppLogger.StartLogging(scene);

        //Connect and init robot.
        InitializeRobotConnection(_dtstr);
     
        // Initialize the user data.
        userData = new MarsUserData(DataManager.configFile, DataManager.sessionFile, DataManager.limbParamFile, AppData.Instance.userID);
        transitionControl = new MarsTransitionControl();
        // Selected movement and game.
        selectedMovement = null;
        selectedGame = null;

        // Get current session number.
        currentSessionNumber = userData.dTableSession.Rows.Count > 0 ?
            Convert.ToInt32(userData.dTableSession.Rows[userData.dTableSession.Rows.Count - 1]["SessionNumber"]) + 1 : 1;
        AppLogger.LogInfo($"Session number set to {currentSessionNumber}.");
    }

    // Waiting for MarsComm
    private void InitializeRobotConnection(string datetimestr = null)
    {
        //Initialize the MARS Comm logger.
        if (datetimestr != null)
        {
            MarsCommLogger.StartLogging(datetimestr);
        }
        if (!ConnectToRobot.isMARS)
        {
            ConnectToRobot.Connect(COMPort);
        }
        MarsComm.getVersion();
        MarsComm.startSensorStream();

        // Check if the connection is successful.
        if (!ConnectToRobot.isConnected)
        {
            AppLogger.LogError($"Failed to connect to MARS @ {COMPort}.");
            throw new Exception($"Failed to connect to MARS @ {COMPort}.");
        }
        AppLogger.LogInfo($"Connected to MARS @ {COMPort}.");       
        AppLogger.LogInfo($"MARS SensorStream started.");
    }

    public void InitializeRobotDiagnostics()
    {
        ConnectToRobot.Connect(COMPort);
    }

    public void setUser(string user)
    {
        userID = user;
        AppLogger.LogInfo($"User ID set to {userID}.");
    }

    public void SetMovement(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            selectedMovement = null;
            //aanController = null;
            AppLogger.LogInfo($"Selected movment set to null.");
            return;
        }
        // Set the mechanism name.
        selectedMovement = new MarsMovement(name: name, side: trainingSide, sessno: currentSessionNumber);
        AppLogger.LogInfo($"Selected movement '{selectedMovement.name}'.");
        AppLogger.SetCurrentMovement(selectedMovement.name);
        AppLogger.LogInfo($"Trial numbers for ' {selectedMovement.name}' updated. Day: {selectedMovement.trialNumberDay}, Session: {selectedMovement.trialNumberSession}.");
    }

    public void SetGame(string game)
    {
        selectedGame = game;
    }
    
    // Check training side.
    public bool IsTrainingSide(string side) => string.Equals(trainingSide, side, StringComparison.OrdinalIgnoreCase);
}

public static class ConnectToRobot
{
    public static string _port;
    public static bool isMARS = false;
    public static bool isConnected = false;

    public static void Connect(string port)
    {
        _port = port;
        if (_port == null)
        {
            _port = "COM13";
            JediComm.InitSerialComm(_port);
        }
        else
        {
            JediComm.InitSerialComm(_port);
        }
        if (JediComm.serPort != null)
        {
            if (JediComm.serPort.IsOpen == false)
            {
                JediComm.Connect();
            }
            isConnected = JediComm.serPort.IsOpen;
        }
    }
    public static void disconnect()
    {
        ConnectToRobot.isMARS = false;
        JediComm.Disconnect();
    }
}



