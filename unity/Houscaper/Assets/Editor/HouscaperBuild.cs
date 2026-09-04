using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Houscaper.EditorTools
{
    /// <summary>
    /// WebGL build entry point. Run headless with:
    ///
    ///   Unity -quit -batchmode -nographics -projectPath unity/Houscaper \
    ///         -executeMethod Houscaper.EditorTools.HouscaperBuild.BuildWebGL \
    ///         -logFile -
    ///
    /// Output goes to public/unity so Next.js serves it from /unity.
    /// </summary>
    public static class HouscaperBuild
    {
        const string DefaultOutput = "../../public/unity";

        [MenuItem("Houscaper/Build WebGL")]
        public static void BuildWebGL()
        {
            string output = ArgumentOr("-houscaperOutput", DefaultOutput);
            string absolute = Path.GetFullPath(Path.Combine(Application.dataPath, "..", output));

            ApplySettings();
            HouscaperSceneSetup.RegisterInBuildSettings();

            Directory.CreateDirectory(absolute);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { HouscaperSceneSetup.ScenePath },
                locationPathName = absolute,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            Debug.Log($"Houscaper: WebGL build {summary.result} -> {absolute} ({summary.totalSize} bytes)");

            if (summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        static void ApplySettings()
        {
            PlayerSettings.companyName = "Houscaper";
            PlayerSettings.productName = "Houscaper";
            // Gamma keeps the hand-picked pastel hex values looking as authored.
            PlayerSettings.colorSpace = ColorSpace.Gamma;

            PlayerSettings.WebGL.template = "PROJECT:Houscaper";
            // Uncompressed keeps the build servable from any plain static host (Vercel, next start)
            // without Content-Encoding headers. Switch to Brotli once the host sets them.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Low);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.runInBackground = false;

            // Nothing in the scene uses realtime lights or shadows.
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.antiAliasing = 4;
        }

        static string ArgumentOr(string flag, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == flag) return args[i + 1];
            }
            return fallback;
        }
    }
}
