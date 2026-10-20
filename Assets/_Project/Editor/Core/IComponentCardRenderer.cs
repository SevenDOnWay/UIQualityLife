#if UNITY_EDITOR
using System;
using UnityEngine.UIElements;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Strategy interface for rendering a component item into a UI Toolkit element.
    /// Follows the Open/Closed Principle (OCP): new view modes or layouts can be added
    /// by creating a new renderer without modifying the main shelf window.
    /// </summary>
    public interface IComponentCardRenderer
    {
        UIPresetViewMode Mode { get; }

        VisualElement Render(IUIComponentItem item, Action<string> onCategorySelected);
    }
}
#endif
