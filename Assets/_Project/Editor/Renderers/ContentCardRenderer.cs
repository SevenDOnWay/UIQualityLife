#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIQualityLife.Editor
{
    public class ContentCardRenderer : IComponentCardRenderer
    {
        public UIPresetViewMode Mode => UIPresetViewMode.Content;

        public VisualElement Render(IUIComponentItem preset, Action<string> onCategorySelected)
        {
            var card = new VisualElement();
            card.style.height = 44;
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            card.style.paddingLeft = 8;
            card.style.paddingRight = 8;
            card.style.marginBottom = 4;
            card.style.backgroundColor = UIElementStyleUtils.CardBg;
            UIElementStyleUtils.SetBorderRadius(card, 4);
            UIElementStyleUtils.SetBorder(card, 1, UIElementStyleUtils.BorderSubtle);

            // Thumbnail swatch (34x34)
            var swatch = UIElementStyleUtils.CreateThumbnailElement(34, preset.GetPreviewTexture(), preset.PreviewColor);
            swatch.style.marginRight = 10;
            card.Add(swatch);

            // Information block
            var info = new VisualElement();
            info.style.flexGrow = 1;

            var title = new Label(preset.DisplayName);
            title.style.fontSize = 12;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = UIElementStyleUtils.TextPrimary;
            info.Add(title);

            if (!string.IsNullOrEmpty(preset.Description))
            {
                var desc = new Label(preset.Description);
                desc.style.fontSize = 10;
                desc.style.color = UIElementStyleUtils.TextSecondary;
                info.Add(desc);
            }
            card.Add(info);

            // Category tags
            if (preset.Categories != null)
            {
                var tagsRow = new VisualElement();
                tagsRow.style.flexDirection = FlexDirection.Row;
                tagsRow.style.alignItems = Align.Center;

                foreach (var cat in preset.Categories)
                {
                    if (string.IsNullOrWhiteSpace(cat)) continue;
                    var tag = UIElementStyleUtils.CreateCategoryTag(cat, () => onCategorySelected?.Invoke(cat));
                    tagsRow.Add(tag);
                }
                card.Add(tagsRow);
            }

            return card;
        }
    }
}
#endif
