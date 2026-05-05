using UnityEngine;

public static class SceneTransitionManager
{
    private static string assessmentReturnScene = "CHOOSEMOVE";

    public static void SetAssessmentReturnScene(string sceneName)
    {
        assessmentReturnScene = sceneName;
    }

    public static string GetAssessmentReturnScene()
    {
        return assessmentReturnScene;
    }

    public static void ResetAssessmentReturnScene()
    {
        assessmentReturnScene = "CHOOSEMOVE";
    }
}
