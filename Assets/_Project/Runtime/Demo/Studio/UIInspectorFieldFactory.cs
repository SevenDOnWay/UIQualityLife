using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIQualityLife.ProceduralUI;

namespace UIQualityLife.Runtime.Demo.Studio
{
    /// <summary>
    /// Factory for generating standardized, responsive property controls in the Runtime Inspector.
    /// </summary>
    public static class UIInspectorFieldFactory
    {
        public static GameObject CreateSectionHeader(Transform parent, string title)
        {
            var headerGo = new GameObject("Section_" + title);
            headerGo.transform.SetParent(parent, false);
            var rt = headerGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 32);

            var text = headerGo.AddComponent<TextMeshProUGUI>();
            text.text = $"<color=#00E5FF><b>{title.ToUpperInvariant()}</b></color>";
            text.fontSize = 12;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = new Color(0.7f, 0.85f, 1f, 1f);

            var le = headerGo.AddComponent<LayoutElement>();
            le.preferredHeight = 32;
            le.flexibleWidth = 1;

            return headerGo;
        }

        public static GameObject CreateSliderField(Transform parent, string labelText, float min, float max, float current, Action<float> onChanged, bool isInteger = false)
        {
            var rowGo = new GameObject("Row_" + labelText);
            rowGo.transform.SetParent(parent, false);
            var rt = rowGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 34);

            var le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = 34;
            le.flexibleWidth = 1;

            // Label
            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(rowGo.transform, false);
            var lblRt = lblGo.AddComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0, 0);
            lblRt.anchorMax = new Vector2(0.40f, 1);
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = Vector2.zero;

            var lbl = lblGo.AddComponent<TextMeshProUGUI>();
            lbl.text = labelText;
            lbl.fontSize = 13;
            lbl.alignment = TextAlignmentOptions.MidlineLeft;
            lbl.color = new Color(0.85f, 0.88f, 0.92f, 1f);

            // Value label
            var valGo = new GameObject("Val");
            valGo.transform.SetParent(rowGo.transform, false);
            var valRt = valGo.AddComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0.82f, 0);
            valRt.anchorMax = new Vector2(1f, 1);
            valRt.offsetMin = Vector2.zero;
            valRt.offsetMax = Vector2.zero;

            var valText = valGo.AddComponent<TextMeshProUGUI>();
            valText.text = isInteger ? $"{current:F0}" : $"{current:F1}";
            valText.fontSize = 12;
            valText.alignment = TextAlignmentOptions.MidlineRight;
            valText.color = new Color(0.6f, 0.8f, 1f, 1f);

            // Slider
            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(rowGo.transform, false);
            var sRt = sliderGo.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.42f, 0.5f);
            sRt.anchorMax = new Vector2(0.80f, 0.5f);
            sRt.sizeDelta = new Vector2(0, 16);

            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = isInteger;
            slider.value = current;

            var bg = new GameObject("Background");
            bg.transform.SetParent(sliderGo.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            SetFullStretch(bgRt);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.18f, 0.22f, 0.30f, 1f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var faRt = fillArea.AddComponent<RectTransform>();
            SetFullStretch(faRt);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fRt = fill.AddComponent<RectTransform>();
            fRt.sizeDelta = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.00f, 0.85f, 1.00f, 1f);
            slider.fillRect = fRt;

            slider.onValueChanged.AddListener(v =>
            {
                valText.text = isInteger ? $"{v:F0}" : $"{v:F1}";
                onChanged?.Invoke(v);
            });

            return rowGo;
        }

        public static GameObject CreateInputField(Transform parent, string labelText, string currentText, Action<string> onChanged)
        {
            var rowGo = new GameObject("Row_" + labelText);
            rowGo.transform.SetParent(parent, false);
            var rt = rowGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 36);

            var le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = 36;
            le.flexibleWidth = 1;

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(rowGo.transform, false);
            var lblRt = lblGo.AddComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0, 0);
            lblRt.anchorMax = new Vector2(0.35f, 1);
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = Vector2.zero;

            var lbl = lblGo.AddComponent<TextMeshProUGUI>();
            lbl.text = labelText;
            lbl.fontSize = 13;
            lbl.alignment = TextAlignmentOptions.MidlineLeft;
            lbl.color = new Color(0.85f, 0.88f, 0.92f, 1f);

            // Input field background
            var inputGo = new GameObject("InputField");
            inputGo.transform.SetParent(rowGo.transform, false);
            var inRt = inputGo.AddComponent<RectTransform>();
            inRt.anchorMin = new Vector2(0.37f, 0.1f);
            inRt.anchorMax = new Vector2(1f, 0.9f);
            inRt.offsetMin = Vector2.zero;
            inRt.offsetMax = Vector2.zero;

            var inBg = inputGo.AddComponent<ProceduralImage>();
            inBg.color = new Color(0.12f, 0.15f, 0.20f, 1f);
            inBg.uniformRadius = 4f;
            inBg.borderWidth = 1f;
            inBg.borderColor = new Color(1f, 1f, 1f, 0.15f);

            var inputField = inputGo.AddComponent<TMP_InputField>();

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(inputGo.transform, false);
            var tRt = textGo.AddComponent<RectTransform>();
            SetFullStretch(tRt);
            tRt.offsetMin = new Vector2(8, 2);
            tRt.offsetMax = new Vector2(-8, -2);

            var tmpText = textGo.AddComponent<TextMeshProUGUI>();
            tmpText.text = currentText;
            tmpText.fontSize = 13;
            tmpText.alignment = TextAlignmentOptions.MidlineLeft;
            tmpText.color = Color.white;

            inputField.textComponent = tmpText;
            inputField.text = currentText;
            inputField.onValueChanged.AddListener(s => onChanged?.Invoke(s));

            return rowGo;
        }

        public static GameObject CreateDropdownField(Transform parent, string labelText, List<string> options, int current, Action<int> onChanged)
        {
            var rowGo = new GameObject("Row_" + labelText);
            rowGo.transform.SetParent(parent, false);
            var rt = rowGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 36);

            var le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = 36;
            le.flexibleWidth = 1;

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(rowGo.transform, false);
            var lblRt = lblGo.AddComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0, 0);
            lblRt.anchorMax = new Vector2(0.35f, 1);
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = Vector2.zero;

            var lbl = lblGo.AddComponent<TextMeshProUGUI>();
            lbl.text = labelText;
            lbl.fontSize = 13;
            lbl.alignment = TextAlignmentOptions.MidlineLeft;
            lbl.color = new Color(0.85f, 0.88f, 0.92f, 1f);

            // Dropdown container
            var ddGo = new GameObject("Dropdown");
            ddGo.transform.SetParent(rowGo.transform, false);
            var ddRt = ddGo.AddComponent<RectTransform>();
            ddRt.anchorMin = new Vector2(0.37f, 0.1f);
            ddRt.anchorMax = new Vector2(1f, 0.9f);
            ddRt.offsetMin = Vector2.zero;
            ddRt.offsetMax = Vector2.zero;

            var ddImg = ddGo.AddComponent<ProceduralImage>();
            ddImg.color = new Color(0.14f, 0.18f, 0.25f, 1f);
            ddImg.uniformRadius = 4f;
            ddImg.borderWidth = 1f;
            ddImg.borderColor = new Color(1f, 1f, 1f, 0.15f);

            var dropdown = ddGo.AddComponent<TMP_Dropdown>();

            var labelItem = new GameObject("Label");
            labelItem.transform.SetParent(ddGo.transform, false);
            var lRt = labelItem.AddComponent<RectTransform>();
            SetFullStretch(lRt);
            lRt.offsetMin = new Vector2(8, 2);
            lRt.offsetMax = new Vector2(-8, -2);
            var lText = labelItem.AddComponent<TextMeshProUGUI>();
            lText.fontSize = 13;
            lText.alignment = TextAlignmentOptions.MidlineLeft;
            lText.color = Color.white;
            dropdown.captionText = lText;

            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.value = Mathf.Clamp(current, 0, Mathf.Max(0, options.Count - 1));
            dropdown.onValueChanged.AddListener(i => onChanged?.Invoke(i));

            return rowGo;
        }

        public static GameObject CreateActionButton(Transform parent, string labelText, Color bgCol, Action onClick)
        {
            var btnGo = new GameObject("Btn_" + labelText);
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 36);

            var le = btnGo.AddComponent<LayoutElement>();
            le.preferredHeight = 36;
            le.flexibleWidth = 1;

            var img = btnGo.AddComponent<ProceduralImage>();
            img.color = bgCol;
            img.uniformRadius = 6f;
            img.borderWidth = 1f;
            img.borderColor = new Color(1f, 1f, 1f, 0.2f);

            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(btnGo.transform, false);
            var tRt = txtGo.AddComponent<RectTransform>();
            SetFullStretch(tRt);

            var txt = txtGo.AddComponent<TextMeshProUGUI>();
            txt.text = labelText;
            txt.fontSize = 13;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;

            return btnGo;
        }

        private static void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
