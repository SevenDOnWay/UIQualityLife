using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Automated build utility for creating itch.io-ready WebGL packages.
    /// Configures decompression fallback, builds to Builds/WebGL, and generates a valid itch.io zip package.
    /// </summary>
    public static class BuildDemoWebGL
    {
        private const string OutputDir = "Builds/WebGL";
        private const string ZipOutputFile = "Builds/UIQualityLife-itch-webgl.zip";

        [MenuItem("Tools/UI Quality of Life/Build itch.io WebGL Demo", priority = 100)]
        public static void BuildWebGL()
        {
            Debug.Log("[BuildDemoWebGL] Starting itch.io WebGL Demo Build...");

            // 1. Configure itch.io WebGL Settings
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.runInBackground = true;

            // 2. Discover scenes
            string demoScenePath = "Assets/Scenes/DemoScene.unity";
            string fallbackScenePath = "Assets/Scenes/SampleScene.unity";

            string sceneToBuild = File.Exists(demoScenePath) ? demoScenePath : fallbackScenePath;
            if (!File.Exists(sceneToBuild))
            {
                Debug.LogError($"[BuildDemoWebGL] No scene found to build! Checked {demoScenePath} and {fallbackScenePath}");
                return;
            }

            string[] scenes = new string[] { sceneToBuild };
            Debug.Log($"[BuildDemoWebGL] Target scene: {sceneToBuild}");

            // 3. Ensure output directory
            if (Directory.Exists(OutputDir))
            {
                try { Directory.Delete(OutputDir, true); } catch { /* ignore */ }
            }
            Directory.CreateDirectory(OutputDir);

            // 4. Build Player
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildDemoWebGL] WebGL Build Succeeded! Size: {summary.totalSize / (1024 * 1024):F2} MB. Duration: {summary.totalTime.TotalSeconds:F1}s");

                // 5. Package for itch.io (root of zip must contain index.html)
                PackageForItchIo();
            }
            else
            {
                Debug.LogError($"[BuildDemoWebGL] WebGL Build Failed: {summary.result}. Total Errors: {summary.totalErrors}");
            }
        }

        public static void PackageForItchIo()
        {
            if (!Directory.Exists(OutputDir))
            {
                Debug.LogError($"[BuildDemoWebGL] Cannot package: {OutputDir} does not exist.");
                return;
            }

            string indexHtml = Path.Combine(OutputDir, "index.html");
            if (!File.Exists(indexHtml))
            {
                Debug.LogWarning($"[BuildDemoWebGL] Warning: index.html not found directly in {OutputDir}. Checking subdirectories...");
            }

            if (File.Exists(ZipOutputFile))
            {
                File.Delete(ZipOutputFile);
            }

            string zipDir = Path.GetDirectoryName(ZipOutputFile);
            if (!string.IsNullOrEmpty(zipDir) && !Directory.Exists(zipDir))
            {
                Directory.CreateDirectory(zipDir);
            }

            Debug.Log($"[BuildDemoWebGL] Compressing {OutputDir} to {ZipOutputFile}...");
            ZipFile.CreateFromDirectory(OutputDir, ZipOutputFile, CompressionLevel.Optimal, false);

            Debug.Log($"[BuildDemoWebGL] itch.io package ready at: {Path.GetFullPath(ZipOutputFile)}");
        }
    }
}
