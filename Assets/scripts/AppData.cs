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

static class AppData
{
    static public readonly string comPort = "COM6";

    static public string selectedMovement;
     
    public static int useHand;
    public static int lu;//lenght of upperarm
    public static int lf;//length of forearm
    public static float[] dataSendToRobot = new float []{ 0.0f, 0.0f, 0.0f, 0.0f };  //parameters send Back to robot

    //Training movements
    public static class MarsDefs
    {
        public static readonly string[] Movements = new string[] { "SFE","SABDU","ELFE"};

        public static int getMovementIndex(string Movement)
        {
            return Array.IndexOf(Movements, Movement);
        }
    }
    public static string[] selectedGame = { "FlappyGame", "space_shooter_home", "Draw_Pong" };

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

    public  static void InitializeRobot()
    {
        DataManager.createFileStructure();
        JediComm.ConnectToRobot(comPort);
        UserData.readAllUserData();
        dataSendToRobot = new float[] { (float)useHand, 0.0f, 1998.0f, 0.0f };
    }
    public static void sendToRobot()
    {
        byte[] _data = new byte[16];
        Buffer.BlockCopy(dataSendToRobot, 0, _data, 0, _data.Length);
        JediComm.SendMessage(_data);
    }

    public static class UserData
    {
        public static DataTable dTableConfig = null;
        public static DataTable dTableSession = null;
        public static DataTable dTableAssessment = null;
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
        public static float totalMoveTimeRemaining  
        {
            get
            {
                float _total = 0f;
                foreach (string movement in MarsDefs.Movements)
                {
                    _total += movementMoveTimePrsc[movement] - SessionDataHandler.movementMoveTimePrev[movement] - movementMoveTimeCurr[movement];
                }
                return _total;
            }
        }

        // Function to read all the user data.
        public static void readAllUserData()
        {
            
            dTableConfig = DataManager.loadCSV(DataManager.filePathConfigData);
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

        public static float getTodayMoveTimeForMovement(string movement)
        {
            return SessionDataHandler.movementMoveTimePrev[movement] + movementMoveTimeCurr[movement];
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

   
  





