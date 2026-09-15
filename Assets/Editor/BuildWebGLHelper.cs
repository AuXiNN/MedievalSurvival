using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildWebGLHelper
{
    [MenuItem("Tools/Build WebGL Now")]
    public static void Build()
    {
        string[] scenes = new string[] {
            "Assets/_Game/Scenes/MainMenu.unity",
            "Assets/_Game/Scenes/Gameplay.unity"
        };

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        UnityEngine.Debug.Log($"BuildWebGLHelper: build result = {report.summary.result}, total size = {report.summary.totalSize} bytes");
    }
}
