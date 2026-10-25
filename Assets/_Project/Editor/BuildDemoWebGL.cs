using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UIQualityLife.Styling;
using UIQualityLife.Runtime.Demo;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Automated build utility for creating itch.io-ready WebGL packages.
    /// Synchronizes presets, themes, thumbnails, and animations, configures decompression fallback,
    /// builds to Builds/WebGL, and generates an itch.io compliant zip package.
    /// </summary>
    public static class BuildDemoWebGL
    {
        private const string OutputDir = "Builds/WebGL";
        private const string ZipOutputFile = "Builds/UIQualityLife-itch-webgl.zip";

        [MenuItem("Tools/UI Quality of Life/Build itch.io WebGL Demo", priority = 100)]
        public static void BuildWebGL()
        {
            Debug.Log("[BuildDemoWebGL] Starting itch.io WebGL Demo Build...");

            // 0. Synchronize latest demo assets (themes, animations, thumbnails, presets)
            SyncAllDemoAssets();

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

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
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

        [MenuItem("Tools/UI Quality of Life/Sync Demo Assets (Presets, Themes, Animations)", priority = 90)]
        public static void SyncAllDemoAssets()
        {
            SyncThemes();
            SyncAnimations();
            SyncThumbnails();
            SyncPresetCatalog();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BuildDemoWebGL] All demo assets synchronized for WebGL runtime.");
        }

        private static void SyncThemes()
        {
            string targetDir = "Assets/Resources/Themes";
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string[] guids = AssetDatabase.FindAssets("t:UIThemePalette");
            foreach (string guid in guids)
            {
                string sourcePath = AssetDatabase.GUIDToAssetPath(guid);
                if (sourcePath.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase)) continue;

                string fileName = Path.GetFileName(sourcePath);
                string destPath = Path.Combine(targetDir, fileName);

                if (!File.Exists(destPath))
                {
                    AssetDatabase.CopyAsset(sourcePath, destPath);
                    Debug.Log($"[BuildDemoWebGL] Mirrored theme to Resources: {fileName}");
                }
            }
        }

        private static void SyncAnimations()
        {
            string targetDir = "Assets/Resources/Animations";
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string[] guids = AssetDatabase.FindAssets("t:UIAnimationClass");
            foreach (string guid in guids)
            {
                string sourcePath = AssetDatabase.GUIDToAssetPath(guid);
                if (sourcePath.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase)) continue;

                string fileName = Path.GetFileName(sourcePath);
                string destPath = Path.Combine(targetDir, fileName);

                if (!File.Exists(destPath))
                {
                    AssetDatabase.CopyAsset(sourcePath, destPath);
                    Debug.Log($"[BuildDemoWebGL] Mirrored animation to Resources: {fileName}");
                }
            }
        }

        private static void SyncThumbnails()
        {
            string targetDir = "Assets/Resources/Thumbnails";
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Packages/com.SevenDOnWay.ui-quality-life/Runtime/Presets", "Assets/_Project/Presets" });
            foreach (string guid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                string thumbSource = prefabPath.Replace(".prefab", "_thumb.png");
                if (!File.Exists(thumbSource)) continue;

                string fileName = Path.GetFileName(thumbSource);
                string destPath = Path.Combine(targetDir, fileName).Replace('\\', '/');

                if (!File.Exists(destPath))
                {
                    AssetDatabase.CopyAsset(thumbSource, destPath);
                }

                // Ensure destPath is imported as Sprite
                TextureImporter importer = AssetImporter.GetAtPath(destPath) as TextureImporter;
                if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void SyncPresetCatalog()
        {
            string targetDir = "Assets/Resources/Demo";
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string catalogPath = Path.Combine(targetDir, "UIPresetCatalog.asset");
            UIPresetCatalog catalog = AssetDatabase.LoadAssetAtPath<UIPresetCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<UIPresetCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            // Find all prefab presets in the project
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Packages/com.SevenDOnWay.ui-quality-life/Runtime/Presets", "Assets/_Project/Presets" });
            var addedItems = new HashSet<string>();

            // Clear old items via reflection or rebuild
            var catalogItemsField = typeof(UIPresetCatalog).GetField("m_Items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            List<PresetCatalogItem> list = new List<PresetCatalogItem>();

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                string prefabName = prefab.name;
                if (addedItems.Contains(prefabName)) continue;

                // Determine category from folder path
                string category = "General";
                string[] parts = path.Split('/');
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (parts[i] == "Presets" && i + 1 < parts.Length - 1)
                    {
                        category = parts[i + 1];
                        break;
                    }
                }

                PresetCatalogItem item = new PresetCatalogItem
                {
                    Name = prefabName,
                    Category = category,
                    Description = $"Interactive UI component preset ({category}): {prefabName}",
                    Prefab = prefab
                };

                // Check for thumbnail sprite / texture in Resources/Thumbnails or adjacent
                string resThumbPath = $"Assets/Resources/Thumbnails/{prefabName}_thumb.png";
                string directThumbPath = path.Replace(".prefab", "_thumb.png");

                string targetThumb = File.Exists(resThumbPath) ? resThumbPath : directThumbPath;

                if (File.Exists(targetThumb))
                {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(targetThumb);
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(targetThumb);

                    item.Thumbnail = sprite;
                    item.ThumbnailTexture = tex;
                }

                list.Add(item);
                addedItems.Add(prefabName);
            }

            if (catalogItemsField != null)
            {
                catalogItemsField.SetValue(catalog, list);
            }

            EditorUtility.SetDirty(catalog);
            Debug.Log($"[BuildDemoWebGL] Updated UIPresetCatalog with {list.Count} presets.");
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
            ZipFile.CreateFromDirectory(OutputDir, ZipOutputFile, System.IO.Compression.CompressionLevel.Optimal, false);

            Debug.Log($"[BuildDemoWebGL] itch.io package ready at: {Path.GetFullPath(ZipOutputFile)}");
        }
    }
}
