#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIQualityLife.Editor
{
    public class ListCardRenderer : IComponentCardRenderer
    {
        public UIPresetViewMode Mode => UIPresetViewMode.List;

        public VisualElement Render(IUIComponentItem preset, Action<string> onCategorySelected)
        {
            var row = new VisualElement();
            row.style.height = 24;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 6;
            row.style.paddingRight = 6;
            row.style.marginBottom = 1;
            UIElementStyleUtils.SetBorderRadius(row, 3);

            // Thumbnail (16x16)
            var thumb = UIElementStyleUtils.CreateThumbnailElement(16, preset.GetPreviewTexture(), preset.PreviewColor);
            thumb.style.marginRight = 8;
            row.Add(thumb);

            // Label
            var label = new Label(preset.DisplayName);
            label.style.fontSize = 12;
            label.style.color = UIElementStyleUtils.TextPrimary;
            label.style.flexGrow = 1;
            row.Add(label);

            // Category tags
            if (preset.Categories != null)
            {
                foreach (var cat in preset.Categories)
                {
                    if (string.IsNullOrWhiteSpace(cat)) continue;
                    var tag = UIElementStyleUtils.CreateCategoryTag(cat, () => onCategorySelected?.Invoke(cat));
                    row.Add(tag);
                }
            }

            var badge = new Label(preset.SystemType == UISystemType.UGUI ? "TMP" : "UXML");
            badge.style.fontSize = 9;
            badge.style.color = UIElementStyleUtils.BorderFocus;
            row.Add(badge);

            return row;
        }
    }
}
#endif
