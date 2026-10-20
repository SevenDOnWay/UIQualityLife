#if UNITY_EDITOR
using System.Collections.Generic;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Registry managing view mode renderers. Follows Open/Closed Principle (OCP).
    /// New renderers can be registered dynamically for custom view modes or UI Builder extensions.
    /// </summary>
    public class CardRendererRegistry
    {
        private readonly Dictionary<UIPresetViewMode, IComponentCardRenderer> renderers = new Dictionary<UIPresetViewMode, IComponentCardRenderer>();
        private readonly IComponentCardRenderer fallbackRenderer;

        public CardRendererRegistry()
        {
            fallbackRenderer = new ContentCardRenderer();

            Register(new ListCardRenderer());
            Register(fallbackRenderer);
            Register(new SmallIconCardRenderer());
            Register(new LargeIconCardRenderer());
        }

        public void Register(IComponentCardRenderer renderer)
        {
            if (renderer != null)
            {
                renderers[renderer.Mode] = renderer;
            }
        }

        public IComponentCardRenderer GetRenderer(UIPresetViewMode mode)
        {
            if (renderers.TryGetValue(mode, out var renderer))
            {
                return renderer;
            }
            return fallbackRenderer;
        }
    }
}
#endif
