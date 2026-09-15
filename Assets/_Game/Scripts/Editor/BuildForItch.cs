using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Headless build entry points for shipping to itch.io via `butler`.
/// Uses whatever scenes are enabled in Build Settings (Tools ▸ Build Settings), in order.
///
/// Run from the command line (Unity must be closed first - only one instance may have the
/// project open):
///   Unity -batchmode -nographics -quit -projectPath &lt;repo&gt; -executeMethod BuildForItch.Mac
///   Unity -batchmode -nographics -quit -projectPath &lt;repo&gt; -executeMethod BuildForItch.WebGL
///   Unity -batchmode -nographics -quit -projectPath &lt;repo&gt; -executeMethod BuildForItch.Windows
///
/// Output lands in Builds/Mac, Builds/WebGL and Builds/Windows at the repo root (git-ignored -
/// these are build artifacts, not source).
/// </summary>
public static class BuildForItch
{
    private const string OutputRoot = "Builds";

    [MenuItem("Tools/Medieval Survival/Build For Itch/Mac")]
    public static void Mac()
    {
        string outDir = $"{OutputRoot}/Mac";
        Build(BuildTarget.StandaloneOSX, $"{outDir}/MedievalSurvival.app");
    }

    [MenuItem("Tools/Medieval Survival/Build For Itch/WebGL")]
    public static void WebGL()
    {
        string outDir = $"{OutputRoot}/WebGL";
        Build(BuildTarget.WebGL, outDir);
    }

    [MenuItem("Tools/Medieval Survival/Build For Itch/Windows")]
    public static void Windows()
    {
        string outDir = $"{OutputRoot}/Windows";
        Build(BuildTarget.StandaloneWindows64, $"{outDir}/MedievalSurvival.exe");
    }

    private static void Build(BuildTarget target, string locationPath)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("BuildForItch: no enabled scenes in Build Settings - aborting.");
            EditorApplication.Exit(1);
            return;
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = locationPath,
            target = target,
            options = BuildOptions.None,
        };

        Debug.Log($"[BuildForItch] Building {target} -> {locationPath}  ({scenes.Length} scene(s): {string.Join(", ", scenes)})");

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[BuildForItch] Build FAILED - result: {report.summary.result}, errors: {report.summary.totalErrors}");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[BuildForItch] Build SUCCEEDED - {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalTime}");
        EditorApplication.Exit(0);
    }
}
