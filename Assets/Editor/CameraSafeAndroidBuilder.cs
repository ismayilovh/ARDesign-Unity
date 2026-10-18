using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

internal static class CameraSafeAndroidBuilder
{
    // Uses explicit BuildOptions so active Build Profile diagnostics cannot leak into the APK.
    private const string OutputPath = "Build/ARDesign.apk";

    [MenuItem("Tools/AR/Build and Run Camera-Safe Android %#k")]
    private static void BuildAndRun()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            throw new BuildFailedException(
                "Switch the active build target to Android before using the camera-safe builder.");
        }

        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new BuildFailedException("No enabled scenes were found in Build Settings.");
        }

        // Development/profiler builds currently prevent ARCore sensor registration on
        // affected Samsung devices running Android 16. Keep these settings aligned with
        // the explicit non-development BuildOptions below.
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;
        EditorUserBuildSettings.buildWithDeepProfilingSupport = false;

        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            throw new BuildFailedException("Could not resolve the Unity project root.");
        }

        var absoluteOutputPath = Path.Combine(projectRoot, OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absoluteOutputPath)!);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = absoluteOutputPath,
            target = BuildTarget.Android,
            options = BuildOptions.AutoRunPlayer,
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException(
                $"Camera-safe Android build failed with result {report.summary.result}.");
        }
    }
}
