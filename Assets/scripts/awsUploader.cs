using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;
public class awsUploader : MonoBehaviour
{
    private string pythonScriptPath = @"C:/pythonscripts/uploadToAWS.pyw";
    private string pythonExecutionPath = @"C:/Users/CMC/AppData/Local/Programs/Python/Python313/pythonw.exe";
    private static string filePathUploadStatus = @"C:/DeviceSetup/Mars";
    string[] status = new string[] {"upload_needed","no_upload"};
    string taskName ="AWSUploaderMarsTask";
    string DeviceName = "MarsData";
    void Start()
    {

        if (!IsTaskScheduled(taskName))
        {
            ScheduleTask();
            createFile();
        }
        else
        {
            //downloadConfigFile();
        }

    }

    // Method to schedule the task using SCHTASKS
    void ScheduleTask()
    {
        
       
        string commandArguments = $"\"{pythonScriptPath}\"";
        string scheduleFrequency = "/SC MINUTE /MO 30";
        string command = $"schtasks /Create {scheduleFrequency} /TN \"{taskName}\" " +
                         $"/TR \"{pythonExecutionPath} {commandArguments}\" /F ";
        RunCommand(command);
        AppLogger.LogInfo("Task scheduled For AWSuploaderTask Successfully");
    }
  
    public void downloadConfigFile()
    {
        AppLogger.LogInfo("Downloading config file");
       
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
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            //Debug.Log(output);
            //Debug.LogError(error);
            AppLogger.LogInfo(output);
            AppLogger.LogError(error);
        }
        catch (System.Exception ex)
        {
            Debug.Log("An error occurred while running the Python script: " + ex.Message);
            AppLogger.LogError("An error occurred while running the Python script: " + ex.Message);
          
        }
    }

    //To create the uploadStatusFile
    void createFile()
    {
        Directory.CreateDirectory(filePathUploadStatus);  
        File.Create(filePathUploadStatus + "/uploadStatus.txt").Dispose();//+"uploadStatus.txt"
        File.WriteAllText(filePathUploadStatus,$"{Application.dataPath},{status[1]},{DeviceName}");
    }
    // Method to check if the task is already scheduled
    bool IsTaskScheduled(string taskName)
    {
        string command = $"schtasks /Query /TN \"{taskName}\"";
        var result = RunCommand(command);
        return result.Contains(taskName);
    }

    // Helper method to run a command in CMD
    string RunCommand(string command)
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
        AppLogger.LogInfo(output);
        AppLogger.LogWarning(error);
        return output;
    }
}


