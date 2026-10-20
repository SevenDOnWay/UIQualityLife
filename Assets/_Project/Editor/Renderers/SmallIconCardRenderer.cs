#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIQualityLife.Editor
{
    public class SmallIconCardRenderer : IComponentCardRenderer
    {
        public UIPresetViewMode Mode => UIPresetViewMode.SmallIcon;

        public VisualElement Render(IUIComponentItem preset, Action<string> onCategorySelected)
        {
            var tile = new VisualElement();
            tile.style.width = 78;
            tile.style.height = 88;
            tile.style.marginRight = 6;
            tile.style.marginBottom = 6;
            tile.style.paddingTop = 5;
            tile.style.paddingBottom = 4;
            tile.style.paddingLeft = 4;
            tile.style.paddingRight = 4;
            tile.style.alignItems = Align.Center;
            tile.style.backgroundColor = UIElementStyleUtils.CardBg;
            UIElementStyleUtils.SetBorderRadius(tile, 4);
            UIElementStyleUtils.SetBorder(tile, 1, UIElementStyleUtils.BorderSubtle);

            // Thumbnail (52x52)
            var preview = UIElementStyleUtils.CreateThumbnailElement(52, preset.GetPreviewTexture(), preset.PreviewColor);
            preview.style.marginBottom = 5;
            tile.Add(preview);

            // Title label
            var title = new Label(preset.DisplayName);
            title.style.fontSize = 10;
            title.style.color = UIElementStyleUtils.TextPrimary;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.whiteSpace = WhiteSpace.Normal;
            tile.Add(title);

            return tile;
        }
    }
}
#endif
