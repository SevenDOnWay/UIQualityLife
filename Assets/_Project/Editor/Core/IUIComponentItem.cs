#if UNITY_EDITOR
using UnityEngine;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Common abstraction for any component item in the UI library.
    /// Supports polymorphic extension to UGUI prefabs and future UI Builder (UXML) templates.
    /// </summary>
    public interface IUIComponentItem
    {
        string DisplayName { get; }
        string[] Categories { get; }
        string PrimaryCategory { get; }
        string Description { get; }
        Color PreviewColor { get; }
        UISystemType SystemType { get; }
        GameObject Prefab { get; }

        Texture2D GetPreviewTexture();
        bool HasCategory(string category);
        bool MatchesSearch(string query);
    }
}
#endif
