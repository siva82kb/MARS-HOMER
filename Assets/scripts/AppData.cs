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

static class AppData
{
    static public readonly string comPort = "COM7";
    static public string selectedMovement;
   //parameter send Back to robot
    public static float[] PCParam = new float []{ 0.0f, 0.0f, 0.0f, 0.0f };

    //Taining movements
    public static class MarsDefs
    {
        public static readonly string[] Movements = new string[] { "SFE","SABDU","ELFE"};

        public static int getMovementIndex(string Movement)
        {
            return Array.IndexOf(Movements, Movement);
        }
    }
    //File headers
    static public string movement = "Movement";
    static public string moveTime = "MoveTime";
    static public String dateTime = "DateTime";
    static public string dateTimeFormat = "dd-MM-yyyy HH:mm:ss";
    static public string dateFormat = "dd-MM-yyyy";
    static public string hosno = "hospno";
    static public string startDateH = "startdate";

    public  static void InitializeRobot()
    {
        DataManager.createFileStructure();
        JediComm.ConnectToRobot(comPort);
        UserData.readAllUserData();
    }
    public static class UserData
    {
        public static DataTable dTableConfig = null;
        public static DataTable dTableSession = null;
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
                    // Add all entries of the mechanism move time dictionary
                    return movementMoveTimePrsc.Values.Sum();

                }
            }
        }
        public static float totalMoveTimeRemaining // Retuns the 
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
            hospNumber = lastRow.Field<string>(hosno);
            startDate = DateTime.ParseExact(lastRow.Field<string>(startDateH), dateFormat, CultureInfo.InvariantCulture);

            //parse the prescribed movement time for training
            for (int i = 0; i < MarsDefs.Movements.Length; i++)
            {
                movementMoveTimePrsc[MarsDefs.Movements[i]] = float.Parse(lastRow.Field<string>(MarsDefs.Movements[i]));
            }
        }
    }

    public static class Miscellaneous
    {
        public static string GetAbbreviatedDayName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek.ToString().Substring(0, 3);
        }
    }
}

   
  





