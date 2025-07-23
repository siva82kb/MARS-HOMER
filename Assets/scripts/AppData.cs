using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.IO.Ports;
using System.Data;
using System.Globalization;
using UnityEngine.XR;
using UnityEngine.UIElements;
using UnityEngine;
using System.Runtime.InteropServices.ComTypes;
using TMPro;
using static UnityEngine.Rendering.SupportedOnRenderPipelineAttribute;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.VisualScripting;


public partial class AppData
{
    // Singleton
    private static readonly Lazy<AppData> _instance = new Lazy<AppData>(() => new AppData());
    public static AppData Instance => _instance.Value;

    /*
     * CONSTANT FIXED VARIABLES.
     */
    // COM Port for the device
    public const string COMPort = "COM17"; //"COM13";

    // static public readonly string comPort = "COM12";
    static public string selectedMovement;
    static public string selectedGame;
    public static int useHand;
    public static int lu;//lenght of upperarm
    public static int lf;//length of forearm
    public static float[] dataSendToRobot;  //parameters send  to robot

    //Training movements
    public static class MarsDefs
    {
        public static readonly string[] Movements = new string[] { "SFE","SABDU","ELFE"};

        public static int getMovementIndex(string Movement)
        {
            return Array.IndexOf(Movements, Movement);
        }
    }
    public static string[] selectGame = { "FlappyGame", "space_shooter_home", "pong_menu" };
    static public int currentSessionNumber;
    static public string trialDataFileLocation;
    //File headers
    static public string movement = "Mechanism";
    static public string moveTime = "MoveTime";
    static public String dateTime = "DateTime";
    static public string dateTimeFormat = "dd-MM-yyyy HH:mm:ss";
    static public string dateFormat = "dd-MM-yyyy";
    static public string hosno = "hospno";
    static public string startDateH = "startdate";
    static public string useHandHeader = "useHand";
    static public string forearmLength = "forearmLength";
    static public string upperarmLength = "upperarmLength";
    static public string maxx="Max_x", minx="Min_x", maxy="Max_y",miny="Min_y";
    // public  static void InitializeRobot()
    // {
    //     DataManager.createFileStructure();
    //     ConnectToRobot(comPort);
    //     UserData.readAllUserData();
    //     dataSendToRobot = new float[] { (float)useHand, 0.0f, 1998.0f, 0.0f };
    //     sendToRobot(dataSendToRobot);
    // }
    // public static void sendToRobot(float[] data)
    // {
    //     byte[] _data = new byte[16];
    //     Buffer.BlockCopy(data, 0, _data, 0, _data.Length);
    //     Debug.Log(_data.Length+"length");
    //     JediComm.SendMessage(_data);
    // }

    public static void writeAssessmentData(String Header, String Data,String FileName,String DirPath)
    {
        String file = $"{DirPath}/{FileName}";
        if (!Directory.Exists(DirPath))
        {
            Directory.CreateDirectory(DirPath);
        }
        if (!File.Exists(file))
        {
            File.Create(file).Dispose();
        }
        try
        {

            if (File.Exists(file) && File.ReadAllLines(file).Length == 0)
            {

                File.WriteAllText(file, Header + "\n"); // Add a new line after the header
                Debug.Log("Header written successfully.");
                
                File.AppendAllText(file, Data + "\n");
                AppLogger.LogInfo($"{FileName}Assessment data writtern successfully");
            }
            else
            {
               
                File.AppendAllText(file, Data + "\n");
                AppLogger.LogInfo($"{FileName}Assessent data writtern successfully");
            }
        }
        catch (Exception ex)
        {
            // Catch any other generic exceptions
            Debug.LogError("An error occurred while writing the data: " + ex.Message);
            AppLogger.LogError("An error occurred while writing the data: " + ex.Message);
        }
    }
   
   
  
    public static class ArmSupportController
    {
        //To change the support Gradually
        public class CoroutineRunner : MonoBehaviour
        {
            private static CoroutineRunner _instance;

            public static CoroutineRunner Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CoroutineRunner");
                        _instance = go.AddComponent<CoroutineRunner>();
                        GameObject.DontDestroyOnLoad(go);
                    }
                    return _instance;
                }
            }
        }

        public static int SEND_ARM_WEIGHT = 2004;
        public static int MARS_ACTIVATED = 2005;
        public static int ROBOT_ACTIVE_WITH_MARS = 2006;

        public enum supportState
        {
            NOWIGHTSUPPORT = 0,
            FULLWEIGHTSUPPORT = 1,
            HALFWEIGHTSUPPORT = 2

        }
        public static supportState setsupportstate;
        //Arm weight calibration value
        public static float b1, b2;
        //gain value
        public static string value;
        private static Coroutine supportCoroutine;


        public static float  decayTime = 5.0f, supporti, startSupport, startDecay = 0, endSupport;
        public static void  initiate()
        {
            // NEEDS CHANGE
            // if (MarsComm.desThree >= MARS_ACTIVATED)
            //     return;
            getSupportCalibrationData();


            if (b1 == float.NaN && b2 == float.NaN)
            {
                SceneManager.LoadScene("weightCalibration");
                return;
            }
                
            // NEEDS CHANGE
            // if (MarsComm.desThree <= calibrationSceneHandler.SENT_SUCCESSFULLY)
            // {
            //     AppData.dataSendToRobot = new float[] { b1, (float)MarsComm.thetades1, SEND_ARM_WEIGHT, (float)MarsComm.controlStatus };
            //     // AppData.sendToRobot(AppData.dataSendToRobot);
            // }
            // if (MarsComm.desThree == SEND_ARM_WEIGHT)
            // {
            //     AppData.dataSendToRobot = new float[] { b2, (float)MarsComm.thetades1, MARS_ACTIVATED, (float)MarsComm.controlStatus };
            //     // AppData.sendToRobot(AppData.dataSendToRobot);

            // }
         
        }
        public static string getGain()
        {
            // NEEDS CHANGE
            // value = (MarsComm.desOne * 100).ToString("F0");
        
            return value;
        }
        public static void getSupportCalibrationData()
        {
            UserData.dTableSupportConfig = DataManager.loadCSV($"{DataManager.directoryAssessmentData}/{DataManager.SupportCalibrationFileName}");
          
            DataRow lastRow = UserData.dTableSupportConfig.Rows[UserData.dTableSupportConfig.Rows.Count - 1];
            b1 = float.Parse((lastRow.Field<string>("B1")));
            b2 = float.Parse((lastRow.Field<string>("B2")));
            Debug.Log($"{b1},{b2}b1values");
        }
        public static void setSupport(float supportCode)
        {
            // NEEDS CHANGE
            // if (MarsComm.desThree < MARS_ACTIVATED)
            //     return;

            MarsComm.controlStatus = MarsComm.CONTROL_STATUS_CODE[1];
            MarsComm.SUPPORT = supportCode;
            AppData.dataSendToRobot = new float[] { supportCode, 0.0f, ROBOT_ACTIVE_WITH_MARS, MarsComm.controlStatus };
            // AppData.sendToRobot(AppData.dataSendToRobot);

        }
        public static void UseFullWeightSupport(int ReadyToChangeSupport = 1)
        {

            setsupportstate = supportState.FULLWEIGHTSUPPORT;
            changeSupportGradully(ReadyToChangeSupport, MarsComm.SUPPORT_CODE[1]);
           

        }
        public static void UseHalfWeightSupport(int ReadyToChangeSupport = 1)
        {
            setsupportstate = supportState.HALFWEIGHTSUPPORT;
            changeSupportGradully(ReadyToChangeSupport, MarsComm.SUPPORT_CODE[2]);
            
        }
        public static void UseNoWeightSupport(int ReadyToChangeSupport = 1)
        {
            setsupportstate = supportState.NOWIGHTSUPPORT;
            changeSupportGradully(ReadyToChangeSupport, MarsComm.SUPPORT_CODE[0]);
           
        }
        public static void changeSupportGradully(int initiate, float targetSupport)
        {
            if (supportCoroutine != null)
                CoroutineRunner.Instance.StopCoroutine(supportCoroutine);
            startDecay = initiate;
            endSupport = targetSupport;
            startSupport = MarsComm.SUPPORT;
            supportCoroutine =  CoroutineRunner.Instance.StartCoroutine(GradualSupportCoroutine(initiate, targetSupport));
        }

        private static IEnumerator GradualSupportCoroutine(int initiate, float targetSupport)
        {
            // NEEDS CHANGE
            // if (MarsComm.desThree < MARS_ACTIVATED)
            //     yield break;

            float elapsedTime = 0f;
            float initialSupport = MarsComm.SUPPORT;

            float sendInterval = 0.1f;         // Send data every 0.1s (10 Hz)
            float sendTimer = 0f;

            float lastSentSupport = -999f;     // Arbitrary impossible initial value

            while (elapsedTime < decayTime && startDecay == initiate)
            {
                elapsedTime += Time.deltaTime;
                sendTimer += Time.deltaTime;

                float t = elapsedTime / decayTime;
                t = t * t * (3f - 2f * t);     // SmoothStep easing
                supporti = Mathf.Lerp(initialSupport, targetSupport, t);

                // Only send if change is significant and send interval passed
                if (sendTimer >= sendInterval && Mathf.Abs(supporti - lastSentSupport) > 0.01f)
                {
                    MarsComm.controlStatus = MarsComm.CONTROL_STATUS_CODE[1];
                    MarsComm.SUPPORT = supporti;

                    setSupport(MarsComm.SUPPORT);
                    lastSentSupport = supporti;
                    sendTimer = 0f;
                }

                yield return null;
            }

            // Ensure final target support is sent at the end
            MarsComm.SUPPORT = targetSupport;
            setSupport(targetSupport);
            startDecay = 0;
        }


    }
    public static class UserData
    {
        public static bool isExceeded { get; private set; }
        public static DataTable dTableConfig = null;
        public static DataTable dTableSession = null;
        public static DataTable dTableAssessment = null;
        public static DataTable dTableSupportConfig = null;
        public static string hospNumber;
        public static DateTime startDate;
        public static Dictionary<string, float> movementMoveTimePrsc { get; private set; } // Prescribed movement time
        public static Dictionary<string, float> movementMoveTimeCurr { get; private set; } // Current movement time

        // Total movement times.
        public static float totalMoveTimePrsc
        {
            get
            {
                if (movementMoveTimePrsc == null)
                {
                    return -1f;
                }
                else
                {
                    // Add all entries of the movement move time dictionary
                    return movementMoveTimePrsc.Values.Sum();

                }
            }
        }
        public static int totalMoveTimeRemaining  
        {
            get
            {
                float _total = 0f;
                float _Prsc = 0f;
                foreach (string movement in MarsDefs.Movements)
                {
                    _Prsc += movementMoveTimePrsc[movement];
                    _total +=  SessionDataHandler.movementMoveTimePrev[movement] - movementMoveTimeCurr[movement];
                }
                Debug.Log(_Prsc+"prescribed");
                Debug.Log(_total+ "done");
                if (_Prsc < _total)
                {
                     isExceeded = true;
                    _total = (_total - _Prsc);
                    return (int)_total;
                }
                else
                {
                    isExceeded = false;
                    _total = (_Prsc -_total);
                    return (int)_total;
                }
               
            }
        }

        // Function to read all the user data.
        public static void readAllUserData()
        {
            
            dTableConfig = DataManager.loadCSV(DataManager.filePathforConfig);
            dTableSession = DataManager.loadCSV(DataManager.filePathSessionData);
            //dTableAssessment = DataManager.loadCSV(DataManager.filePathAssessmentData);
           
            
            parseTherapyConfigData();
            
           

            SessionDataHandler.parseMovementMoveTimePrev();
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

        public static int getTodayMoveTimeForMovement(string movement)
        {
            return (int)SessionDataHandler.movementMoveTimePrev[movement] + (int)movementMoveTimeCurr[movement];
        }

        public static int getCurrentDayOfTraining()
        {
            TimeSpan duration = DateTime.Now - startDate;
            return (int)duration.TotalDays;
        }

        private static void parseTherapyConfigData()
        {
            //create th dictionary
            movementMoveTimeCurr = createMoveTimeDictionary();
            movementMoveTimePrsc = createMoveTimeDictionary();

            DataRow lastRow = dTableConfig.Rows[dTableConfig.Rows.Count - 1];
            //patient data
            hospNumber = lastRow.Field<string>(hosno);
            startDate = DateTime.ParseExact(lastRow.Field<string>(startDateH), dateFormat, CultureInfo.InvariantCulture);
            useHand = int.Parse( lastRow.Field<string>(useHandHeader));
            lu = int.Parse(lastRow.Field<string>(upperarmLength));
            lf = int.Parse(lastRow.Field<string>(forearmLength));

           
            //parse the prescribed movement time for training
            for (int i = 0; i < MarsDefs.Movements.Length; i++)
            {
                movementMoveTimePrsc[MarsDefs.Movements[i]] = float.Parse(lastRow.Field<string>(MarsDefs.Movements[i]));
            }
        }
    }
   
    public static int returnLastAssesment()
    {
        DataTable assessmentdata = DataManager.loadCSV(DataManager.filePathAssessmentData);
        DataRow lastRow = assessmentdata.Rows[assessmentdata.Rows.Count - 1];
        DateTime  lastAssessmentDate = DateTime.ParseExact(lastRow.Field<string>(startDateH), dateTimeFormat, CultureInfo.InvariantCulture);
        TimeSpan duration = DateTime.Now - lastAssessmentDate;
        Debug.Log((int)duration.TotalDays);
        return (int)duration.TotalDays;
       
    }
    public static class Miscellaneous
    {
        public static string GetAbbreviatedDayName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek.ToString().Substring(0, 3);
        }
    }
}
public static class gameData
{
    //game
    public static bool isGameLogging;
    public static string game;
    public static int gameScore;
    public static int reps;
    public static int playerScore;
    public static int enemyScore;
    public static string TargetPos = "0";
    public static string playerPos = "0";
    public static string enemyPos = "0";
    public static string playerHit = "0";
    public static string enemyHit = "0";
    public static string wallBounce = "0";
    public static string enemyFail = "0";
    public static string playerFail = "0";
    public static int winningScore = 3;
    public static float moveTime;
    public static readonly string[] instructions = new string[] { "1 - moving,2 - collidedWithcolumn,3 - Passedsuccessfully\n", "1 - moving,2 - AstroidHit,3 - playerHit\n", "1 - moving,2 - enemyHit,3 - playerHit\n" };
    public static int events ;
 
    private static DataLogger dataLog;
    private static string[] gameHeader = new string[] {
        "time","buttonState","angle1","angle2","angle3","angle4","force1","force2","des1","des2","des3"
        ,"playerPos","events","playerScore"
    };
    public static bool isLogging { get; private set; }
    static public void StartDataLog(string fname)
    {
        if (dataLog != null)
        {
            StopLogging();
            Debug.Log("datalognull");
        }
        // Start new logger
        if (fname != "")
        {
            string instructionLine = instructions[Array.IndexOf(AppData.selectGame,AppData.selectedGame)];
            string headerWithInstructions = instructionLine + String.Join(", ", gameHeader) + "\n";
            dataLog = new DataLogger(fname, headerWithInstructions);
            isLogging = true;
            Debug.Log("logging file name is notempty");
        }
        else
        {
            Debug.Log("file name is empty");
            dataLog = null;
            isLogging = false;
        }
    }
    static public void StopLogging()
    {
        if (dataLog != null)
        {
            Debug.Log("Null log not");
            dataLog.stopDataLog(true);
            dataLog = null;
            isLogging = false;
        }
        else
            Debug.Log("Null log");
    }

    static public void LogData()
    {
            string[] _data = new string[] {
               MarsComm.currentTime.ToString(),
               MarsComm.buttonState.ToString(),
               MarsComm.angle1.ToString("G17"),
               MarsComm.angle2.ToString("G17"),
               MarsComm.angle3.ToString("G17"),
               MarsComm.angle4.ToString("G17"),
               MarsComm.force.ToString("G17"),
               MarsComm.calibButton.ToString("G17"),
               // NEEDS CHANGE
               //    MarsComm.desOne.ToString("G17"),
               //    MarsComm.desTwo.ToString("G17"),
               //    MarsComm.desThree.ToString("G17"),
               playerPos,
               gameData.events.ToString("F2"),
               gameData.playerScore.ToString("F2"),
              
            };
            string _dstring = String.Join(", ", _data);
            _dstring += "\n";
            
            dataLog.logData(_dstring);
        
        Debug.Log("workign data log");
        
    }
}
public class DataLogger
{
    public string currFileName { get; private set; }
    public StringBuilder fileData;
    public bool stillLogging
    {
        get { return (fileData != null); }
    }

    public DataLogger(string filename, string header)
    {
        currFileName = filename;

        fileData = new StringBuilder(header);
    }

    public void stopDataLog(bool log = true)
    {
        if (log)
        {
            File.AppendAllText(currFileName, fileData.ToString());
        }
        currFileName = "";
        fileData = null;
    }

    public void logData(string data)
    {
        if (fileData != null)
        {
            fileData.Append(data);
        }
    }
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
                UnityEngine.Debug.Log(_port);
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