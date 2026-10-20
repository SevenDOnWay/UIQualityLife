#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIQualityLife.Editor
{
    public class LargeIconCardRenderer : IComponentCardRenderer
    {
        public UIPresetViewMode Mode => UIPresetViewMode.LargeIcon;

        public VisualElement Render(IUIComponentItem preset, Action<string> onCategorySelected)
        {
            var tile = new VisualElement();
            tile.style.width = 110;
            tile.style.height = 132;
            tile.style.marginRight = 8;
            tile.style.marginBottom = 8;
            tile.style.paddingTop = 8;
            tile.style.paddingBottom = 6;
            tile.style.paddingLeft = 6;
            tile.style.paddingRight = 6;
            tile.style.alignItems = Align.Center;
            tile.style.backgroundColor = UIElementStyleUtils.CardBg;
            UIElementStyleUtils.SetBorderRadius(tile, 6);
            UIElementStyleUtils.SetBorder(tile, 1, UIElementStyleUtils.BorderSubtle);

            // Thumbnail (82x82)
            var preview = UIElementStyleUtils.CreateThumbnailElement(82, preset.GetPreviewTexture(), preset.PreviewColor);
            preview.style.marginBottom = 6;
            tile.Add(preview);

            // Title
            var title = new Label(preset.DisplayName);
            title.style.fontSize = 11;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = UIElementStyleUtils.TextPrimary;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.whiteSpace = WhiteSpace.Normal;
            tile.Add(title);

            // Mini category tag
            var primaryTag = new Label(preset.PrimaryCategory);
            primaryTag.style.fontSize = 8;
            primaryTag.style.color = UIElementStyleUtils.TextMuted;
            primaryTag.style.marginTop = 2;
            tile.Add(primaryTag);

            return tile;
        }
    }
}
#endif
