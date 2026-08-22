using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.Threading.Tasks;
public static class awsManager
{
    public static string pythonScriptPath = @"C:/pythonscripts/uploadToAWSM.pyw";

    public static string pythonExecutionPath;//@"C:/Program Files/Python310/pythonw.exe"; for this laptop
    

    public static string filePathUploadStatus = @"C:/DeviceSetups/Mars"; //change according to the device
    public static string filePathAppsetups = @"C:/AppSetups/Mars"; //change according to the device

    public static string[] status = new string[] {"upload_needed","no_upload"};
    public static  string taskName ="AWSUploaderMarsTask"; //change according to the device
    public static string DeviceName = "Mars";//change according to the device
    public static string fullTaskAction = $"\\\"{pythonExecutionPath}\\\"\\\"{pythonScriptPath}\\\"";
    public static string message="";
    // Method to schedule the task using SCHTASKS
    public static void ScheduleTask()
    {
        
       
        string commandArguments = $"\"{pythonScriptPath}\"";
        string scheduleFrequency = "/SC MINUTE /MO 30";
        string command = $"schtasks /Create {scheduleFrequency} /TN \"{taskName}\" " +
                         $"/TR \"{fullTaskAction}\" /F ";
        RunCommand(command);
    }
  
  

public static void RunAWSpythonScript()
{
    if (!File.Exists(pythonScriptPath))
    {
        Debug.Log("File not found: Python script");
        return;
    }

    // Run off the main thread so Unity is not blocked while the upload runs.
    Task.Run(() =>
    {
        try
        {
            Process process = new Process();
            process.StartInfo.FileName = pythonExecutionPath;
            process.StartInfo.Arguments = pythonScriptPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            // Read stdout and stderr in parallel to avoid deadlock when either buffer fills.
            var stdoutTask = Task.Run(() => process.StandardOutput.ReadToEnd());
            var stderrTask = Task.Run(() => process.StandardError.ReadToEnd());
            process.WaitForExit();

            message = stdoutTask.Result;
            if (!string.IsNullOrEmpty(message)) Debug.Log(message);
            string error = stderrTask.Result;
            if (!string.IsNullOrEmpty(error)) Debug.LogWarning(error);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("An error occurred while running the Python script: " + ex.Message);
        }
    });
}
    public static string getmessage()
    {
        return message;
    }

    public static void changeUploadStatus(string status){
        string uploadFilePath = Path.Combine(filePathUploadStatus, "uploadStatus.txt");
        // You don't need `File.Create(...).Dispose()` manually � File.WriteAllText will create/write directly.
        Debug.Log($"{AppData.Instance.userData.hospNumber}-awsmanger:{AppData.Instance.userID}");
        File.WriteAllText(uploadFilePath, $"{Path.Combine(Application.dataPath,"data", AppData.Instance.userID)},{status},{DeviceName},{AppData.Instance.userID},{AppData.Instance.userData.GetDeviceLocation()}");
        
    }
    public static void AppSetups(string hospitalId, string location)
    {
        Directory.CreateDirectory(filePathAppsetups);
        string appInfoFilePath = Path.Combine(filePathAppsetups, "appInfo.txt");
        File.WriteAllText(appInfoFilePath, $"{Path.Combine(Application.dataPath, "data")},{hospitalId},{location},{DeviceName}");
    }

    //To create the uploadStatusFile
    public  static void createFile(string userID)
    {

        Directory.CreateDirectory(filePathUploadStatus);

        string uploadFilePath = Path.Combine(filePathUploadStatus, "uploadStatus.txt");

        File.WriteAllText(uploadFilePath, $"{Path.Combine(Application.dataPath,"data", AppData.Instance.userID)},{status[1]},{DeviceName},{userID},{AppData.Instance.userData.GetDeviceLocation()}");

    }
    // Method to check if the task is already scheduled
    public  static bool IsTaskScheduled(string taskName)
    {
        string command = $"schtasks /Query /TN \"{taskName}\"";
        var result = RunCommand(command);
        return result.Contains(taskName);
    }

    // Helper method to run a command in CMD
    public static string RunCommand(string command)
    {
        Process process = new Process();
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = $"/C {command}";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return output;
    }
}
