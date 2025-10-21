
using System.Text;
using System;
using Debug = UnityEngine.Debug;
using UnityEngine;
using System.IO;

public partial class AppData
{
    private static readonly Lazy<AppData> _instance = new Lazy<AppData>(() => new AppData());
    public static AppData Instance => _instance.Value;

    static public readonly string COMPort = "COM5"; //1-35//2-30//3-32//4-50

    // Robot Connection Alive Variables.
    static public float MARS_WATCHDOG_TIMEOUT = 2.0f; //seconds
    static private bool _isMARSConnectionAlive = false;
    static public bool isMARSConnectionAlive
    {
        get { return _isMARSConnectionAlive; }
        set
        {
            if (value == false && _isMARSConnectionAlive == true)
            {
                AppLogger.LogWarning("MARS connection lost! Stopping session.");
            }
            else if (value == true && _isMARSConnectionAlive == false)
            {
                AppLogger.LogInfo("MARS connection restored.");
            }
            _isMARSConnectionAlive = value;
        }
    }

    /*
     * GAME ADAPTATION CONSTANTS
     */
    public const float LOW_SUCCESS_RATE = 80;
    public const float HIGH_SUCCESS_RATE = 90;
    private const float SPEED_REDUCTION_FACTOR_MIN = 0.975f; // Reduce by 2.5%
    private const float SPEED_REDUCTION_FACTOR_MAX = 0.995f; // Reduce by 0.5%
    private const float SPEED_INCREASE_FACTOR_MIN = 1.005f; // Increase by 0.5%
    private const float SPEED_INCREASE_FACTOR_MAX = 1.025f; // Increase by 2.5%
    public float GetReachSpeedAdaptationRate(float successRate, float currentReachSpeed)
    {
        float _normspeed = (currentReachSpeed - MarsGameDefs.MIN_REACH_SPEED) / (MarsGameDefs.MAX_REACH_SPEED - MarsGameDefs.MIN_REACH_SPEED);
        if (successRate < LOW_SUCCESS_RATE)
        {
            // Compute speed reduction factor.
            return SPEED_REDUCTION_FACTOR_MIN + (SPEED_REDUCTION_FACTOR_MAX - SPEED_REDUCTION_FACTOR_MIN) * _normspeed;
        }
        else if (successRate > HIGH_SUCCESS_RATE)
        {
            // Compute speed increase factor.
            return SPEED_INCREASE_FACTOR_MAX - (SPEED_INCREASE_FACTOR_MAX - SPEED_INCREASE_FACTOR_MIN) * _normspeed;
        }
        return 1.0f;
    }

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
    public string trialAromDataFile { get; private set; } = null;
    public string trialArmWeightDataFile { get; private set; } = null;
    static public string trialDataFileLocation;
    private StringBuilder rawDataString = null;
    private readonly object rawDataLock = new object();
    private StringBuilder aanExecDataString = null;
    public string userID { get; private set; } = null;
    public float successRate { get; private set; } = 0f;

    /* DO OBJECT CREATION HERE */
    // public string selectedGame { get; private set; } = null;
    public MarsGame selectedGame { get; private set; } = null;
    public MarsMovement selectedMovement { get; private set; } = null;
    // public MarsArom currentArom { get; private set; } = null;
    public MarsUserData userData;
    public string trainingSide => userData?.limb != null ? MarsComm.LIMBTYPE[userData.limb] : MarsComm.LIMBTYPE[0];

    // An annotation integer for scenes to set annotation it the raw data.
    public string annotation = "";

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
        userData = new MarsUserData(DataManager.configFile, DataManager.sessionFile, AppData.Instance.userID);

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
            selectedGame = null;
            AppLogger.LogInfo($"Selected movment set to null.");
            return;
        }
        // Set the mechanism name.
        // Check if the movement ROM file exists
        selectedMovement = new MarsMovement(name: name, side: trainingSide, sessno: currentSessionNumber);
        AppLogger.LogInfo($"Selected movement '{selectedMovement.name}'.");
        AppLogger.SetCurrentMovement(selectedMovement.name);
        AppLogger.LogInfo($"Trial numbers for '{selectedMovement.name}' updated. Day: {selectedMovement.trialNumberDay}, Session: {selectedMovement.trialNumberSession}.");
        // Reset the selected game.
        selectedGame = null;
        AppLogger.LogInfo($"Selected game reset to null.");
        AppLogger.SetCurrentGame("");
    }

    public void SetGame(string game)
    {
        //Read the game speed from the session data.
        float rSpeed = Instance.userData.readReachSpeedForGameMovement(game, selectedMovement?.name);

        // Read the cummulative hits and misses from the session data.
        int[] cuScores = Instance.userData.readCummulativeHitsMissesForGameMovement(game, selectedMovement?.name);
        
        // Set the selected game.
        selectedGame = new MarsGame(gName: game,
                                    mName: selectedMovement?.name,
                                    rSpeed: rSpeed,
                                    gDuration: MarsGameDefs.GAMEDURATION[game],
                                    arom: selectedMovement?.currentArom,
                                    gCuTargets: cuScores[0],
                                    gCuHits: cuScores[1],
                                    gCuMisses: cuScores[2]);
        AppLogger.SetCurrentGame(selectedGame.name);
        AppLogger.LogInfo($"Selected game '{selectedGame.name}'. Reach speed: {selectedGame.reachSpeed}m/s, Cummulative targets: {selectedGame.cummulativeTargets}, Cummulative hits: {selectedGame.cummulativeHits}, Cummulative misses: {selectedGame.cummulativeMisses}.");
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



