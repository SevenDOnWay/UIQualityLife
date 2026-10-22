#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Global project configuration for UI Quality of Life.
    /// Persisted in ProjectSettings/UIQualityLifeSettings.asset so user projects
    /// can customize preset, theme, animation, and queue folders without code modification.
    /// </summary>
    [FilePath("ProjectSettings/UIQualityLifeSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class UIQualityLifeSettings : ScriptableSingleton<UIQualityLifeSettings>
    {
        [Header("Preset & Library Folders")]
        [SerializeField] private string m_PresetsFolder = "Assets/_Project/Presets";
        [SerializeField] private string m_ThemesFolder = "Assets/_Project/Styling/Themes";
        [SerializeField] private string m_AnimationsFolder = "Assets/_Project/Styling/Animations";
        [SerializeField] private string m_StylesFolder = "Assets/_Project/Styling/Styles";

        [Header("Pipeline & Automation Bridge")]
        [SerializeField] private string m_QueueFolder = "Assets/_Project/Pipeline/Queue";
        [SerializeField] private string m_FinishedFolder = "Assets/_Project/Pipeline/Finished";
        [SerializeField] private int m_BridgePort = 8765;
        [SerializeField] private bool m_AutoStartBridge = true;

        public static string PresetsFolder => NormalizePath(instance?.m_PresetsFolder, "Assets/_Project/Presets");
        public static string ThemesFolder => NormalizePath(instance?.m_ThemesFolder, "Assets/_Project/Styling/Themes");
        public static string AnimationsFolder => NormalizePath(instance?.m_AnimationsFolder, "Assets/_Project/Styling/Animations");
        public static string StylesFolder => NormalizePath(instance?.m_StylesFolder, "Assets/_Project/Styling/Styles");
        public static string QueueFolder => NormalizePath(instance?.m_QueueFolder, "Assets/_Project/Pipeline/Queue");
        public static string FinishedFolder => NormalizePath(instance?.m_FinishedFolder, "Assets/_Project/Pipeline/Finished");
        public static int BridgePort => (instance != null && instance.m_BridgePort > 0) ? instance.m_BridgePort : 8765;
        public static bool AutoStartBridge => instance == null || instance.m_AutoStartBridge;

        public void SaveSettings()
        {
            Save(true);
        }

        public void ResetToDefaults()
        {
            m_PresetsFolder = "Assets/_Project/Presets";
            m_ThemesFolder = "Assets/_Project/Styling/Themes";
            m_AnimationsFolder = "Assets/_Project/Styling/Animations";
            m_StylesFolder = "Assets/_Project/Styling/Styles";
            m_QueueFolder = "Assets/_Project/Pipeline/Queue";
            m_FinishedFolder = "Assets/_Project/Pipeline/Finished";
            m_BridgePort = 8765;
            m_AutoStartBridge = true;
            SaveSettings();
        }

        private static string NormalizePath(string path, string fallback)
        {
            if (string.IsNullOrWhiteSpace(path)) return fallback;
            string normalized = path.Replace('\\', '/').Trim();
            if (normalized.EndsWith("/")) normalized = normalized.Substring(0, normalized.Length - 1);
            return normalized;
        }
    }
}
#endif
