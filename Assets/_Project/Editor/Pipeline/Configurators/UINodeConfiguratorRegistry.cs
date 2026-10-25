#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UIQualityLife.Editor.Configurators
{
    /// <summary>
    /// Central registry for UI node configurator strategies.
    /// Provides extensible resolution of node building strategies without modifying core pipeline code.
    /// </summary>
    public static class UINodeConfiguratorRegistry
    {
        private static readonly List<IUINodeConfigurator> s_Configurators = new List<IUINodeConfigurator>();
        private static readonly ContainerNodeConfigurator s_FallbackContainer = new ContainerNodeConfigurator();

        static UINodeConfiguratorRegistry()
        {
            // Register built-in widget configurators (order-sensitive: specific widgets first, container fallback last)
            Register(new ButtonNodeConfigurator());
            Register(new InputFieldNodeConfigurator());
            Register(new SliderNodeConfigurator());
            Register(new ToggleNodeConfigurator());
            Register(new ProgressBarNodeConfigurator());
            Register(new TextNodeConfigurator());
            Register(s_FallbackContainer);
        }

        public static void Register(IUINodeConfigurator configurator)
        {
            if (configurator != null && !s_Configurators.Contains(configurator))
            {
                s_Configurators.Add(configurator);
            }
        }

        public static IUINodeConfigurator Resolve(string resolvedType, UINodeSpec node)
        {
            for (int i = 0; i < s_Configurators.Count; i++)
            {
                if (s_Configurators[i].CanHandle(resolvedType, node))
                {
                    return s_Configurators[i];
                }
            }
            return s_FallbackContainer;
        }

        public static void ConfigureRectTransform(RectTransform rt, UINodeSpec node)
        {
            if (node.anchorMin != null && node.anchorMin.Length == 2)
                rt.anchorMin = new Vector2(node.anchorMin[0], node.anchorMin[1]);
            else
                rt.anchorMin = new Vector2(0.5f, 0.5f);

            if (node.anchorMax != null && node.anchorMax.Length == 2)
                rt.anchorMax = new Vector2(node.anchorMax[0], node.anchorMax[1]);
            else
                rt.anchorMax = new Vector2(0.5f, 0.5f);

            if (node.pivot != null && node.pivot.Length == 2)
                rt.pivot = new Vector2(node.pivot[0], node.pivot[1]);
            else
                rt.pivot = new Vector2(0.5f, 0.5f);

            if (node.anchoredPosition != null && node.anchoredPosition.Length == 2)
                rt.anchoredPosition = new Vector2(node.anchoredPosition[0], node.anchoredPosition[1]);
            else
                rt.anchoredPosition = Vector2.zero;

            rt.sizeDelta = new Vector2(node.width > 0 ? node.width : 160f, node.height > 0 ? node.height : 44f);
        }

        public static void ConfigureLayoutElement(GameObject go, UINodeSpec node)
        {
            bool hasLayoutOverride = node.minWidth >= 0f || node.minHeight >= 0f ||
                                     node.preferredWidth >= 0f || node.preferredHeight >= 0f ||
                                     node.flexibleWidth >= 0f || node.flexibleHeight >= 0f ||
                                     node.ignoreLayout;

            // Auto-guard against zero/negative width collapse inside LayoutGroups:
            // When placed inside a parent HorizontalOrVerticalLayoutGroup with childControlWidth=true,
            // children without preferredWidth or flexibleWidth collapse to 0 width.
            bool needsAutoPreferred = false;
            float autoWidth = -1f;
            float autoHeight = -1f;
            if (go.transform.parent != null)
            {
                var parentLg = go.transform.parent.GetComponent<HorizontalOrVerticalLayoutGroup>();
                if (parentLg != null)
                {
                    if (parentLg.childControlWidth && node.preferredWidth < 0f && node.flexibleWidth < 0f)
                    {
                        needsAutoPreferred = true;
                        autoWidth = node.width > 0f ? node.width : 160f;
                    }
                    if (parentLg.childControlHeight && node.preferredHeight < 0f && node.flexibleHeight < 0f)
                    {
                        needsAutoPreferred = true;
                        autoHeight = node.height > 0f ? node.height : 40f;
                    }
                }
            }

            if (hasLayoutOverride || needsAutoPreferred)
            {
                var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
                if (node.minWidth >= 0f) le.minWidth = node.minWidth;
                if (node.minHeight >= 0f) le.minHeight = node.minHeight;

                if (node.preferredWidth >= 0f) le.preferredWidth = node.preferredWidth;
                else if (autoWidth > 0f) le.preferredWidth = autoWidth;

                if (node.preferredHeight >= 0f) le.preferredHeight = node.preferredHeight;
                else if (autoHeight > 0f) le.preferredHeight = autoHeight;

                if (node.flexibleWidth >= 0f) le.flexibleWidth = node.flexibleWidth;
                if (node.flexibleHeight >= 0f) le.flexibleHeight = node.flexibleHeight;
                if (node.ignoreLayout) le.ignoreLayout = true;
            }
        }

        public static void ConfigureLayoutGroup(GameObject go, UILayoutSpec layout)
        {
            if (layout == null || string.IsNullOrEmpty(layout.type) || layout.type == "None") return;

            string t = layout.type.ToLowerInvariant();
            HorizontalOrVerticalLayoutGroup lg = null;

            if (t == "horizontal")
            {
                lg = go.AddComponent<HorizontalLayoutGroup>();
            }
            else if (t == "vertical")
            {
                lg = go.AddComponent<VerticalLayoutGroup>();
            }
            else if (t == "grid")
            {
                var grid = go.AddComponent<GridLayoutGroup>();
                grid.spacing = new Vector2(layout.spacing, layout.spacing);
                grid.padding = new RectOffset(layout.paddingLeft, layout.paddingRight, layout.paddingTop, layout.paddingBottom);
                grid.childAlignment = NodeFormattingUtils.ParseTextAnchor(layout.childAlignment);
                return;
            }

            if (lg != null)
            {
                lg.spacing = layout.spacing;
                lg.padding = new RectOffset(layout.paddingLeft, layout.paddingRight, layout.paddingTop, layout.paddingBottom);
                lg.childControlWidth = layout.childControlWidth;
                lg.childControlHeight = layout.childControlHeight;
                lg.childForceExpandWidth = layout.childForceExpandWidth;
                lg.childForceExpandHeight = layout.childForceExpandHeight;
                lg.childAlignment = NodeFormattingUtils.ParseTextAnchor(layout.childAlignment);
            }
        }
    }
}
#endif
