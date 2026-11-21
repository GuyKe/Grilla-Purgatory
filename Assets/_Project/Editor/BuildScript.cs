// Headless Quest APK build.
// Usage: Unity -batchmode -quit -projectPath . -executeMethod GrillaPurgatory.Setup.BuildScript.BuildQuestApk -buildOutput Builds/GrillaPurgatory.apk
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GrillaPurgatory.Setup
{
    public static class BuildScript
    {
        const string ScenePath = "Assets/_Project/Scenes/Field.unity";

        public static void BuildQuestApk()
        {
            string output = GetArg("-buildOutput") ?? "Builds/GrillaPurgatory.apk";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));

            // OpenXR's package settings asset is normally created when the XR Plug-in Management UI is opened.
            // Create and register it up front (it can't be registered once a build has started).
            var openXrSettings = Type.GetType("UnityEditor.XR.OpenXR.OpenXRPackageSettings, Unity.XR.OpenXR.Editor");
            openXrSettings?.GetMethod("GetOrCreateInstance",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.Invoke(null, null);

            // XR Plug-in Management normally creates its per-platform settings when its Project Settings page is
            // opened. Do the same here so the OpenXR loader gets assigned and packaged.
            var perTarget = typeof(UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget)
                .GetMethod("GetOrCreate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.Invoke(null, null) as UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget;
            if (perTarget != null)
            {
                foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
                {
                    if (!perTarget.HasSettingsForBuildTarget(group)) perTarget.CreateDefaultSettingsForBuildTarget(group);
                    if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
                }
                EditorUtility.SetDirty(perTarget);
                AssetDatabase.SaveAssets();
            }

            // Run twice: the first pass creates XR loader/feature settings, the second one enables them.
            ProjectSetup.ConfigureProject();
            ProjectSetup.ConfigureProject();
            // Always regenerate so the scene matches the current ProjectSetup rig.
            ProjectSetup.CreateHelloScene();
            AssetDatabase.SaveAssets();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[Grilla Purgatory] Build result: {report.summary.result}, {report.summary.totalSize} bytes -> {output}");
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
