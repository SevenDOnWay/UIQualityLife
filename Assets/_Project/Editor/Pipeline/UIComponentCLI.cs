#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Dedicated Command-Line Interface (CLI) entry point runner for headless Unity execution.
    /// Follows Single Responsibility Principle (SRP) by isolating process lifecycle and argument parsing from builder logic.
    /// </summary>
    public static class UIComponentCLI
    {
        /// <summary>
        /// Entry point invoked via Unity command-line: -executeMethod UIQualityLife.Editor.UIComponentCLI.BuildFromCommandLine
        /// </summary>
        public static void BuildFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            string specPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-componentSpec" && i + 1 < args.Length)
                {
                    specPath = args[i + 1];
                    break;
                }
            }

            if (string.IsNullOrEmpty(specPath) || !File.Exists(specPath))
            {
                Debug.LogError("[UI Pipeline CLI] Invalid or missing -componentSpec path argument.");
                EditorApplication.Exit(1);
                return;
            }

            string json = File.ReadAllText(specPath);
            UIComponentBuilder.BuildResult res = UIComponentBuilder.BuildFromJson(json);

            if (res.success)
            {
                Debug.Log($"[UI Pipeline CLI] Success! Generated preset: {res.presetName} at {res.prefabPath}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[UI Pipeline CLI] Error: {res.error}");
                EditorApplication.Exit(2);
            }
        }
    }
}
#endif
