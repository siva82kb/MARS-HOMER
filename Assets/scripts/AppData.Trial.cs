
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using UnityEngine;

/*
 * HOMER MARS Application Data Class.
 * Implements all the functions for running game trials.
 */
public partial class AppData
{
    // Trail Detials
    public float gameTime { get; set; } = 0;
    // public float gameSpeed { get => selectedGame.gameSpeed; }
    // Start a new trial.
    public void StartNewTrial()
    {
        trialStartTime = DateTime.Now;
        trialStopTime = null;
        selectedMovement.NextTrial();
        
        // Set the trial data files.
        StartRawDataLogging();

        // Write trial details to the log file.
        string _tdetails = string.Join(" | ",
            new string[] {
                $"Start Time: {trialStartTime:yyyy-MM-ddTHH:mm:ss}",
                $"Trial#Day: {selectedMovement.trialNumberDay}",
                $"Trial#Sess: {selectedMovement.trialNumberSession}",
                $"TrialRawDataFile: {trialRawDataFile.Split('/').Last()}"
        });
        AppLogger.LogInfo($"StartNewTrial | {_tdetails}");
    }

    public void StopTrial(int nTargets, int nSuccess, int nFailure)
    {
        trialStopTime = DateTime.Now;
        nTargets = (nTargets == 0) ? 1 : nTargets;
        successRate = Math.Clamp(100 * nSuccess / nTargets, 0, 100);

        // Compute the new speed based on the current performance.
        float adaptRate = (successRate < LOW_SUCCESS_RATE) ? SPEED_REDUCTION_FACTOR :
                          (successRate > HIGH_SUCCESS_RATE) ? SPEED_INCREASE_FACTOR : 1.0f;
        float currGameSpeed = selectedGame.gameSpeed;
        selectedGame.SetGameSpeed(adaptRate * currGameSpeed);

        // Update cummulative hits and misses.
        selectedGame.UpdateCummulativeHitsMisses(nTargets, nSuccess, nFailure);

        // Write trial information to the session details file.
        WriteTrialToSessionsFile();
       
        string _tdetails = string.Join(" | ",
            new string[] {
                $"Start Time: {trialStartTime:yyyy-MM-ddTHH:mm:ss}",
                $"Stop Time: {trialStopTime:yyyy-MM-ddTHH:mm:ss}",
                $"Trial#Day: {selectedMovement.trialNumberDay}",
                $"Trial#Sess: {selectedMovement.trialNumberSession}",
                $"NTargets: {nTargets}",
                $"NSuccess: {nSuccess}",
                $"NFailure: {nFailure}",
                $"Trial SR: {successRate} ",
                $"CuTargets: {selectedGame.cummulativeTargets} ",
                $"CuHits: {selectedGame.cummulativeHits} ",
                $"CuMisses: {selectedGame.cummulativeMisses} ",
                $"Current Game Speed: {currGameSpeed} ",
                $"New Game Speed: {selectedGame.gameSpeed} ",
                $"TrialRawDataFile: {trialRawDataFile.Split('/').Last()}"
        });
        AppLogger.LogInfo($"StopTrial | {_tdetails}");
        
        // Stop Raw and AAN real-time data logging.
        WriteTrialDataToRawDataFile();
        MarsComm.OnNewMarsData -= OnNewMarsDataDataLogging;
        trialRawDataFile = null;
    }

    private void WriteTrialToSessionsFile()
    {
        // Build the trial row.
        string[] trialRow = new string[] {
            $"{currentSessionNumber}",                              // SessionNumber
            startTime.ToString(DataManager.DATETIMEFORMAT),         // DateTime
            $"{selectedMovement.trialNumberDay}",                   // TrialNumberDay
            $"{selectedMovement.trialNumberSession}",               // TrialNumberSession
            trialStartTime.ToString(DataManager.DATETIMEFORMAT),    // TrialStartTime
            trialStopTime?.ToString(DataManager.DATETIMEFORMAT),    // TrialStopTime
            trialRawDataFile.Split("/data/")[1],                    // TrialRawDataFile
            $"{selectedMovement.name}",                             // Movement
            $"{userData.trainingPlaneAngle}",                       // TrainingPlaneAngle
            $"{selectedGame.name}",                                 // Game  
            null,                                                   // GameParameter
            $"{selectedGame.gameSpeed}",                            // GameSpeed
            $"{successRate}",                                       // SuccessRate
            Instance.gameTime.ToString(),                           // GameTime
            $"{selectedGame.cummulativeTargets}",                   // CummulativeTargets
            $"{selectedGame.cummulativeHits}",                      // CummulativeHits
            $"{selectedGame.cummulativeMisses}",                    // CummulativeMisses
            $"{trialRawDataFile.Split('/').Last()}"                 // RawDataFileName
        };

        // Write the trial row to the session file.
        using (StreamWriter sw = new StreamWriter(DataManager.sessionFile, true, Encoding.UTF8))
        {
            // Write the trial row to the session file.
            sw.WriteLine(string.Join(",", trialRow));
        }
    }

    public void StartRawDataLogging()
    {
        //// Set the file name.
        trialRawDataFile = DataManager.GetTrialRawDataFileName(
            currentSessionNumber,
            selectedMovement.trialNumberDay,
            Instance.selectedGame.name,
            Instance.selectedMovement.name);

        //// Initialize the string builders.
        rawDataString = new StringBuilder();
        // Write pre-header and header information
        rawDataString.AppendLine($":Device: MARS");
        rawDataString.AppendLine($":Location: {userData.GetDeviceLocation()}");
        rawDataString.AppendLine($":Movement: {selectedMovement.name}");
        rawDataString.AppendLine($":Game: {selectedGame.name}");
        rawDataString.AppendLine($":TrialType: ");
        rawDataString.AppendLine($":TrialStartTime: {trialStartTime:yyyy-MM-ddTHH:mm:ss}");
        rawDataString.AppendLine($":TrialNumberDay: {selectedMovement.trialNumberDay}");
        // Screen/Robot limits string
        float[] _screenLimits = MarsGame.GetGameScreenLimits(selectedGame.name);
        string _limitstr = string.Join(",", new string[] {
            $"{_screenLimits[0]:F6}",
            $"{_screenLimits[1]:F6}",
            $"{_screenLimits[2]:F6}",
            $"{_screenLimits[3]:F6}"
        });
        rawDataString.AppendLine($":ScreenLimits: {_limitstr}");
        _limitstr = string.Join(",", new string[] {
            $"{Instance.selectedMovement.currentArom.leftAdjusted.x:F6}",
            $"{Instance.selectedMovement.currentArom.rightAdjusted.x:F6}",
            $"{Instance.selectedMovement.currentArom.bottomAdjusted.y:F6}",
            $"{Instance.selectedMovement.currentArom.topAdjusted.y:F6}"
        });
        rawDataString.AppendLine($":RobotLimits: {_limitstr}");
        rawDataString.AppendLine(string.Join(",", DataManager.RAWFILEHEADER));

        // Attach the event handler for data logging.
        MarsComm.OnNewMarsData += OnNewMarsDataDataLogging;
    }
 
    public void OnNewMarsDataDataLogging()
    {
        lock (rawDataLock)
        {
            if (rawDataString == null)
            {
                Debug.LogWarning("rawDataString is null, skipping logging.");
                return;
            }
            Vector3 _playerPos = GetGamePlayerPosition();
            Vector3 _targetPos = GetGamePlayerPosition();
            rawDataString.Append($"{MarsComm.runTime},");               // DeviceRunTime
            rawDataString.Append($"{MarsComm.packetNumber},");          // PacketNumber
            rawDataString.Append($"{MarsComm.status},");                // Status
            rawDataString.Append($"{MarsComm.controlType},");           // ControlType
            rawDataString.Append($"{MarsComm.errorStatus},");           // ErrorStatus
            rawDataString.Append($"{MarsComm.limb},");                  // Limb
            rawDataString.Append($"{MarsComm.calibration},");           // Calibration
            rawDataString.Append($"{MarsComm.angle1},");                // MarsAngle1
            rawDataString.Append($"{MarsComm.angle2},");                // MarsAngle2
            rawDataString.Append($"{MarsComm.angle3},");                // MarsAngle3
            rawDataString.Append($"{MarsComm.angle4},");                // MarsAngle4
            rawDataString.Append($"{MarsComm.imuAngle1},");             // ImuMarsAngle1
            rawDataString.Append($"{MarsComm.imuAngle2},");             // ImuMarsAngle2
            rawDataString.Append($"{MarsComm.imuAngle3},");             // ImuMarsAngle3
            rawDataString.Append($"{MarsComm.imuAngle4},");             // ImuMarsAngle4
            rawDataString.Append($"{MarsComm.force},");                 // Force
            rawDataString.Append($"{MarsComm.target},");                // Target
            rawDataString.Append($"{MarsComm.desired},");               // Desired
            rawDataString.Append($"{MarsComm.control},");               // Control
            rawDataString.Append($"{MarsComm.buttonState},");           // Button
            rawDataString.Append($"{MarsComm.epPos.x},");               // EndPointX
            rawDataString.Append($"{MarsComm.epPos.y},");               // EndPointY
            rawDataString.Append($"{MarsComm.epPos.z},");               // EndPointZ
            rawDataString.Append($"{MarsComm.epPosInThePlane.y},");     // EndPointYPlaneY
            rawDataString.Append($"{MarsComm.epPosInThePlane.z},");     // EndPointZPlaneZ
            rawDataString.Append($"{MarsComm.errP},");                  // Error
            rawDataString.Append($"{MarsComm.errD},");                  // ErrorDiff
            rawDataString.Append($"{MarsComm.errI},");                  // ErrorSum
            rawDataString.Append($"{_playerPos.x},");                   // GamePlayerX
            rawDataString.Append($"{_playerPos.y},");                   // GamePlayerY
            rawDataString.Append($"{_targetPos.x},");                   // GameTargetX
            rawDataString.Append($"{_targetPos.y},");                   // GameTargetY
            rawDataString.Append($"{GetGameState()},");                 // GameState
            rawDataString.Append($"{AppData.Instance.annotation}");     // Annotation
            rawDataString.Append("\n");
        }
    }

    private void WriteTrialDataToRawDataFile()
    {
        AppLogger.LogInfo($"Writing to: {trialRawDataFile}");
        AppLogger.LogInfo($"File exists before write? {File.Exists(trialRawDataFile)}");

        string _dir = Path.GetDirectoryName(trialRawDataFile);
        if (!Directory.Exists(_dir)) Directory.CreateDirectory(_dir);

        lock (rawDataLock)  // locking
        {
            using (StreamWriter sw = new StreamWriter(trialRawDataFile, false, Encoding.UTF8))
            {
                sw.Write(rawDataString.ToString());
            }
            rawDataString.Clear();
            rawDataString = null;
        }
        AppLogger.LogInfo($"File exists before write? {File.Exists(trialRawDataFile)}");
    }

    // AROM assessment raw data logging function.
    public void StartRawDataAromDataLogging(string movement, string datetime)
    {
        // Set the file name.
        trialAromDataFile = DataManager.GetRomRawFileName(movement, datetime);
        Debug.Log(trialAromDataFile);

        // Initialize the string builders.
        rawDataString = new StringBuilder();
        // Write pre-header and header information
        rawDataString.AppendLine($":Device: MARS");
        rawDataString.AppendLine($":Location: {userData.GetDeviceLocation()}");
        rawDataString.AppendLine($":Movement: {selectedMovement.name}");
        rawDataString.AppendLine(string.Join(",", DataManager.RAWFILEHEADER));

        // Attach the event handler for data logging.
        MarsComm.OnNewMarsData += OnNewMarsDataDataLogging;
    }

    public void StopRawDataAromDataLogging()
    {
        AppLogger.LogInfo($"Writing AROM raw data to {trialAromDataFile}");
        string _dir = Path.GetDirectoryName(trialAromDataFile);

        // Create directory if it doesn't exist.
        if (!Directory.Exists(_dir)) Directory.CreateDirectory(_dir);

        // Write the raw data to file.
        lock (rawDataLock)  // locking
        {
            using (StreamWriter sw = new StreamWriter(trialAromDataFile, false, Encoding.UTF8))
            {
                sw.Write(rawDataString.ToString());
            }
            rawDataString.Clear();
            rawDataString = null;
        }
        MarsComm.OnNewMarsData -= OnNewMarsDataDataLogging;
        trialAromDataFile = null;
    }
    
    // Arm Weight assessment raw data logging function.
    public void StartRawDataArmWeightDataLogging(string datetime)
    {
        // Set the file name.
        trialArmWeightDataFile = DataManager.GetArmWeightRawFileName(datetime);
        Debug.Log(trialArmWeightDataFile);

        // Initialize the string builders.
        rawDataString = new StringBuilder();
        // Write pre-header and header information
        rawDataString.AppendLine($":Device: MARS");
        rawDataString.AppendLine($":Location: {userData.GetDeviceLocation()}");
        rawDataString.AppendLine($":Movement: MLAP");
        rawDataString.AppendLine(string.Join(",", DataManager.RAWFILEHEADER));

        // Attach the event handler for data logging.
        MarsComm.OnNewMarsData += OnNewMarsDataDataLogging;
    }

    public void StopRawDataArmWeightDataLogging()
    {
        AppLogger.LogInfo($"Writing Arm Weight raw data to {trialArmWeightDataFile}");
        string _dir = Path.GetDirectoryName(trialArmWeightDataFile);

        // Create directory if it doesn't exist.
        if (!Directory.Exists(_dir)) Directory.CreateDirectory(_dir);

        // Write the raw data to file.
        lock (rawDataLock)  // locking
        {
            using (StreamWriter sw = new StreamWriter(trialArmWeightDataFile, false, Encoding.UTF8))
            {
                sw.Write(rawDataString.ToString());
            }
            rawDataString.Clear();
            rawDataString = null;
        }
        MarsComm.OnNewMarsData -= OnNewMarsDataDataLogging;
        trialArmWeightDataFile = null;
    }

    //CHECK FOR MARS
    //private Vector3 GetGamePlayerPosition()
    //{
    //    // Get the game target X position.
    //    if (selectedGame.name == "SS")
    //    {
    //        return SpaceShooterGameContoller.Instance.playerPosition;
    //    }
    //    else if (selectedGame.name == "PP")
    //    {
    //        return pongGameController.Instance.playerPosition;
    //    }
    //    else if (selectedGame.name == "CD")
    //    {
    //        return DCGameController.Instance.playerPosition;
    //    }
    //    return Vector3.zero;
    //}
    private Vector3 GetGamePlayerPosition()
    {
      

        switch (selectedGame.name)
        {
            case "SS":
                return SpaceShooterGameContoller.Instance != null
                    ? SpaceShooterGameContoller.Instance.playerPosition
                    : Vector3.zero;

            case "PP":
                return pongGameController.Instance != null
                    ? pongGameController.Instance.playerPosition
                    : Vector3.zero;

            case "CD":
                return DCGameController.Instance != null
                    ? DCGameController.Instance.playerPosition
                    : Vector3.zero;

            default:
                return Vector3.zero;
        }
    }


    private string GetGameTargetPosition()
    {
        //// Get the game target X position.
        if (selectedGame.name == "SS")
        {
            if (SpaceShooterGameContoller.Instance.targetPosition.HasValue)
            {
                return $"{SpaceShooterGameContoller.Instance.targetPosition.Value.x:F3},{SpaceShooterGameContoller.Instance.targetPosition.Value.y:F3}";
            }
        }
        else if (selectedGame.name == "PP")
        {
            if (pongGameController.Instance.targetPosition.HasValue) return $"{pongGameController.Instance.targetPosition.Value.x:F3},{pongGameController.Instance.targetPosition.Value.y:F3}";
        }

        else if (selectedGame.name == "CD")
        {
            if (DCGameController.Instance.targetPosition.HasValue)
            {
                return $"{DCGameController.Instance.targetPosition.Value.x:F3},{DCGameController.Instance.targetPosition.Value.y:F3}";
            }
        }
        return ",";
    }

    private string GetGameState()
    {
        //// Get the game state.
        if (selectedGame.name == "SS")
        {
            return SpaceShooterGameContoller.Instance!=null? SpaceShooterGameContoller.Instance.gameState.ToString():"";
        }
        else if (selectedGame.name == "PP")
        {
            return pongGameController.Instance!=null?pongGameController.Instance.gameState.ToString():"";
        }

        else if (selectedGame.name == "CD")
        {
            return DCGameController.Instance != null ? DCGameController.Instance.gameState.ToString() : "";
        }
        return "";
    }

    public void reloadSessionDetails() => Instance.userData.readParseSessionData(DataManager.sessionFile);
}
