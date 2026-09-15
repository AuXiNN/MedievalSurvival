using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildStandaloneHelper
{
    private static readonly string[] Scenes = new string[] {
        "Assets/_Game/Scenes/MainMenu.unity",
        "Assets/_Game/Scenes/Gameplay.unity"
    };

    [MenuItem("Tools/Build Mac Now")]
    public static void BuildMac()
    {
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = "Builds/Mac/MedievalSurvival.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        UnityEngine.Debug.Log($"BuildStandaloneHelper: Mac build result = {report.summary.result}, size = {report.summary.totalSize} bytes");
    }

    [MenuItem("Tools/Build Windows Now")]
    public static void BuildWindows()
    {
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = "Builds/Windows/MedievalSurvival.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        UnityEngine.Debug.Log($"BuildStandaloneHelper: Windows build result = {report.summary.result}, size = {report.summary.totalSize} bytes");
    }
}
