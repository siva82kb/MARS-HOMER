
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/*
 * HOMER MARS Application Data Class.
 * Implements all the functions for running game trials.
 */
public partial class AppData
{
    //Trail Detials
    public float gameTime { get; set; } = 0;
    public float gameSpeed { get; set; }
    // Start a new trial.
    public void StartNewTrial()
    {
        trialStartTime = DateTime.Now;
        trialStopTime = null;
        selectedMovement.NextTrial();
        
        // Set the trial data files.
        StartRawAndAanExecDataLogging();

        // Write trial details to the log file.
        string _tdetails = string.Join(" | ",
            new string[] {
                $"Start Time: {trialStartTime:yyyy-MM-ddTHH:mm:ss}",
                $"Trial#Day: {selectedMovement.trialNumberDay}",
                $"Trial#Sess: {selectedMovement.trialNumberSession}",
                $"TrialType: ",
                $"Desired SR: ",
                $"Current CB: ",
                $"TrialRawDataFile: {trialRawDataFile.Split('/').Last()}"
        });
        AppLogger.LogInfo($"StartNewTrial | {_tdetails}");

        //Get GamelastSpeed of current Movement
        if (userData.dTableSession.Rows.Count == 0) return;
        var lastRowForMovement = userData.dTableSession.AsEnumerable()
                                .Where(r => r.Field<string>("Movement") == AppData.Instance.selectedMovement.name)
                                .LastOrDefault();

        if (lastRowForMovement != null)
        {
            gameSpeed = float.Parse(lastRowForMovement.Field<string>("GameSpeed"));
        }
    }

    public void StopTrial(int nTargets, int nSuccess, int nFailure)
    {
        trialStopTime = DateTime.Now;
        nTargets = (nTargets == 0) ? 1 : nTargets;
        successRate = 100 * nSuccess / nTargets;

        // Write trial information to the session details file.
        WriteTrialToSessionsFile();
       
        string _tdetails = string.Join(" | ",
            new string[] {
                $"Start Time: {trialStartTime:yyyy-MM-ddTHH:mm:ss}",
                $"Stop Time: {trialStopTime:yyyy-MM-ddTHH:mm:ss}",
                $"Trial#Day: {selectedMovement.trialNumberDay}",
                $"Trial#Sess: {selectedMovement.trialNumberSession}",
                $"TrialType: ",
                $"NTargets: {nTargets}",
                $"NSuccess: {nSuccess}",
                $"NFailure: {nFailure}",
                $"Desired SR: ",
                $"Trial SR:{successRate} ", 
                $"Current CB:",//CHANGE FOR MARS
                $"Next CB: ",//CHANGE FOR MARS
                $"TrialRawDataFile: {trialRawDataFile.Split('/').Last()}"
        });
        AppLogger.LogInfo($"StopTrial | {_tdetails}");
        // Stop Raw and AAN real-time data logging.
        WriteTrialDataToRawDataFile();
        MarsComm.OnNewMarsData -= OnNewMarsDataDataLogging;
        trialRawDataFile = null;
        //set to upload the data to the AWS
        //awsManager.changeUploadStatus(awsManager.status[0]);
    }

    private void WriteTrialToSessionsFile()
    {
        // Build the trial row.
        string[] trialRow = new string[] {
            // "SessionNumber"
            $"{currentSessionNumber}",
            // "DateTime"
            startTime.ToString(DataManager.DATETIMEFORMAT),
            // "TrialNumberDay"
            $"{selectedMovement.trialNumberDay}",
            // "TrialNumberSession"
            $"{selectedMovement.trialNumberSession}",
            // "TrialStartTime"
            trialStartTime.ToString(DataManager.DATETIMEFORMAT),
            // "TrialStopTime"
            trialStopTime?.ToString(DataManager.DATETIMEFORMAT),
            // "TrialRawDataFile"
            trialRawDataFile.Split("/data/")[1],
            // "Movement"
             $"{selectedMovement.name}",
            // "GameName"
            $"{selectedGame}",
            // "GameParameter"
            null,
            // "GameSpeed"
            $"{gameSpeed}",
            // "SuccessRate"
            $"{successRate}",
            //gameTime
            AppData.Instance.gameTime.ToString()
        };

        // Write the trial row to the session file.
        using (StreamWriter sw = new StreamWriter(DataManager.sessionFile, true, Encoding.UTF8))
        {
            // Write the trial row to the session file.
            sw.WriteLine(string.Join(",", trialRow));
        }
    }

    public void StartRawAndAanExecDataLogging()
    {
        //// Set the file name.
        trialRawDataFile = DataManager.GetTrialRawDataFileName(
            currentSessionNumber,
            selectedMovement.trialNumberDay,
            Instance.selectedGame,
            Instance.selectedMovement.name);

        //// Initialize the string builders.
        rawDataString = new StringBuilder();
        // Write pre-header and header information
        rawDataString.AppendLine($":Device: MARS");
        rawDataString.AppendLine($":Location: {userData.GetDeviceLocation()}");
        rawDataString.AppendLine($":Movement: {selectedMovement.name}");
        rawDataString.AppendLine($":Game: {selectedGame}");
        rawDataString.AppendLine($":TrialType: ");
        rawDataString.AppendLine($":TrialStartTime: {trialStartTime:yyyy-MM-ddTHH:mm:ss}");
        rawDataString.AppendLine($":TrialNumberDay: {selectedMovement.trialNumberDay}");
        // rawDataString.AppendLine($":FWS-ROM: X-[{selectedMovement.CurrentArom[0]:F3},{selectedMovement.CurrentArom[1]:F3}],Y-[{selectedMovement.CurrentArom[2]:F3},{selectedMovement.CurrentArom[3]:F3}]");
        rawDataString.AppendLine($":DesiredSuccessRate: ");
        rawDataString.AppendLine($":ControlBound: ");
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
            rawDataString.Append($"{MarsComm.runTime},");
            rawDataString.Append($"{MarsComm.packetNumber},");
            rawDataString.Append($"{MarsComm.status},");
            rawDataString.Append($"{MarsComm.controlType},");
            rawDataString.Append($"{MarsComm.errorStatus},");
            rawDataString.Append($"{MarsComm.limb},");
            rawDataString.Append($"{MarsComm.calibration},");
            rawDataString.Append($"{MarsComm.angle1},");
            rawDataString.Append($"{MarsComm.angle2},");
            rawDataString.Append($"{MarsComm.angle3},");
            rawDataString.Append($"{MarsComm.angle4},");
            rawDataString.Append($"{MarsComm.imuAngle1},");
            rawDataString.Append($"{MarsComm.imuAngle2},");
            rawDataString.Append($"{MarsComm.imuAngle3},");
            rawDataString.Append($"{MarsComm.imuAngle4},");
            rawDataString.Append($"{MarsComm.force},");
            rawDataString.Append($"{MarsComm.target},");
            rawDataString.Append($"{MarsComm.desired},");
            rawDataString.Append($"{MarsComm.control},");
            rawDataString.Append($"{MarsComm.buttonState},");
            rawDataString.Append($"{MarsComm.epPos.x},");
            rawDataString.Append($"{MarsComm.epPos.y},");
            rawDataString.Append($"{MarsComm.epPos.z},");
            rawDataString.Append($"{MarsComm.epPosInThePlane.y},");
            rawDataString.Append($"{MarsComm.epPosInThePlane.z},");
            rawDataString.Append($"{MarsComm.errP},");
            rawDataString.Append($"{MarsComm.errD},");
            rawDataString.Append($"{MarsComm.errI},");
            rawDataString.Append($"{_playerPos.x},");
            rawDataString.Append($"{_playerPos.y},");
            rawDataString.Append($"{_targetPos.x},");
            rawDataString.Append($"{_targetPos.y},");
            rawDataString.Append($"{GetGameState()}");
            rawDataString.Append($"{AppData.Instance.annotation}");
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
    public void StartRawDataArmWeightDataLogging(string movement, string datetime)
    {
        // Set the file name.
        trialArmWeightDataFile = DataManager.GetArmWeightRawFileName(datetime);
        Debug.Log(trialArmWeightDataFile);

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

    public void StopRawDataArmWeightDataLogging()
    {
        AppLogger.LogInfo($"Writing Arm Weight raw data to {trialArmWeightDataFile}");
        string _dir = Path.GetDirectoryName(trialArmWeightDataFile);

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
        trialArmWeightDataFile = null;
    }

    //CHECK FOR MARS
    private Vector3 GetGamePlayerPosition()
    {
        // Get the game target X position.
        if (selectedGame == "space_shooter_home")
        {
            return spaceShooterGameContoller.Instance.playerPosition;
        }
        else if (selectedGame == "pong_game")
        {
            return pongGameController.Instance.playerPosition;
        }

        else if (selectedGame == "Whack_WelcomeScene")
        {

            return WAMGameController.Instance.playerPosition;

        }
        return Vector3.zero;
    }

    private string GetGameTargetPosition()
    {
        //// Get the game target X position.
        if (selectedGame == "space_shooter_home")
        {
            if (spaceShooterGameContoller.Instance.targetPosition.HasValue)
            {
                return $"{spaceShooterGameContoller.Instance.targetPosition.Value.x:F3},{spaceShooterGameContoller.Instance.targetPosition.Value.y:F3}";
            }
        }
        else if (selectedGame == "pong_game")
        {
            if (pongGameController.Instance.targetPosition.HasValue) return $"{pongGameController.Instance.targetPosition.Value.x:F3},{pongGameController.Instance.targetPosition.Value.y:F3}";
        }

        else if (selectedGame == "Whack_WelcomeScene")
        {
            if (WAMGameController.Instance.targetPosition.HasValue)
            {
                return $"{WAMGameController.Instance.targetPosition.Value.x:F3},{WAMGameController.Instance.targetPosition.Value.y:F3}";
            }
        }
        return ",";
    }

    private string GetGameState()
    {
        //// Get the game state.
        if (selectedGame == "space_shooter_home")
        {
            return $"{spaceShooterGameContoller.Instance.gameState}";
        }
        else if (selectedGame == "pong_game")
        {
            return $"{pongGameController.Instance.gameState}";
        }
       
        else if(selectedGame == "Whack_WelcomeScene")
        {
            return $"{WAMGameController.Instance.gameState}";
        }
            return "";
    }
    public void updateSessionDetails()
    {
        AppData.Instance.userData.readParseSessionData(DataManager.sessionFile);
       
    }
}
