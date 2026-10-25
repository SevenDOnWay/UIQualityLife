using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIQualityLife.ProceduralUI;
using UIQualityLife.Styling;

namespace UIQualityLife.Runtime.Demo.Studio
{
    /// <summary>
    /// Left Panel: Runtime Inspector for the selected UI element on the Canvas.
    /// Lets visitors inspect and live-tweak transforms, procedural graphics, text, animations, and theme bindings.
    /// </summary>
    public class UIRuntimeInspector : MonoBehaviour
    {
        [Header("Scroll Container")]
        [SerializeField] private Transform m_FieldsContainer;
        [SerializeField] private TMP_Text m_SelectedTitleText;
        [SerializeField] private TMP_Text m_SelectedSubtitleText;

        [Header("Add Property Dropdown")]
        [SerializeField] private TMP_Dropdown m_AddPropertyDropdown;

        private GameObject m_InspectedObject;

        private void Start()
        {
            UIStudioSelection.OnSelectionChanged += Inspect;
            SetupAddPropertyMenu();
            Inspect(null);
        }

        private void OnDestroy()
        {
            UIStudioSelection.OnSelectionChanged -= Inspect;
        }

        public void Inspect(GameObject target)
        {
            m_InspectedObject = target;
            ClearFields();

            if (m_InspectedObject == null)
            {
                if (m_SelectedTitleText != null) m_SelectedTitleText.text = "Canvas Viewport";
                if (m_SelectedSubtitleText != null) m_SelectedSubtitleText.text = "Click any component on canvas to inspect";
                RenderCanvasDefaultInfo();
                return;
            }

            if (m_SelectedTitleText != null) m_SelectedTitleText.text = m_InspectedObject.name;
            if (m_SelectedSubtitleText != null)
            {
                var compTypes = new List<string>();
                if (m_InspectedObject.GetComponent<ProceduralImage>() != null) compTypes.Add("ProceduralUI");
                if (m_InspectedObject.GetComponent<Button>() != null) compTypes.Add("Button");
                if (m_InspectedObject.GetComponent<TMP_Text>() != null) compTypes.Add("TMP_Text");
                if (m_InspectedObject.GetComponent<UIClassAnimator>() != null) compTypes.Add("ClassAnimator");
                m_SelectedSubtitleText.text = string.Join(" • ", compTypes);
            }

            BuildInspectorFields();
        }

        private void ClearFields()
        {
            if (m_FieldsContainer == null) return;
            for (int i = m_FieldsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(m_FieldsContainer.GetChild(i).gameObject);
            }
        }

        private void RenderCanvasDefaultInfo()
        {
            if (m_FieldsContainer == null) return;

            UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Canvas Information");

            var infoGo = new GameObject("InfoHint");
            infoGo.transform.SetParent(m_FieldsContainer, false);
            var text = infoGo.AddComponent<TextMeshProUGUI>();
            text.text = "<b>Interactive Canvas</b>\n\n• Use <b>Interact Mode</b> to click buttons, drag sliders, and toggle accordion cards.\n\n• Use <b>Inspect Mode</b> to click any component and edit its live properties here in real time.\n\n• Or hold <b>Alt / Tab</b> to quick-toggle modes!";
            text.fontSize = 13;
            text.color = new Color(0.7f, 0.75f, 0.85f, 1f);
            var le = infoGo.AddComponent<LayoutElement>();
            le.preferredHeight = 150;
            le.flexibleWidth = 1;
        }

        private void BuildInspectorFields()
        {
            if (m_InspectedObject == null || m_FieldsContainer == null) return;

            // 1. RectTransform / Layout
            var rt = m_InspectedObject.GetComponent<RectTransform>();
            if (rt != null)
            {
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Transform & Dimensions");
                UIInspectorFieldFactory.CreateInputField(m_FieldsContainer, "Name", m_InspectedObject.name, n => m_InspectedObject.name = n);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Width", 20, 800, rt.sizeDelta.x, w => rt.sizeDelta = new Vector2(w, rt.sizeDelta.y), true);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Height", 20, 600, rt.sizeDelta.y, h => rt.sizeDelta = new Vector2(rt.sizeDelta.x, h), true);
            }

            // 2. Procedural Image Graphic
            var pImg = m_InspectedObject.GetComponent<ProceduralImage>();
            if (pImg != null)
            {
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Procedural Graphic");

                var styles = new List<string> { "Round", "Octagonal", "Scooped" };
                UIInspectorFieldFactory.CreateDropdownField(m_FieldsContainer, "Corner Style", styles, (int)pImg.cornerStyle, s => pImg.cornerStyle = (CornerStyle)s);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Corner Radius", 0, 100, pImg.uniformRadius, r => { pImg.uniformCorners = true; pImg.uniformRadius = r; }, true);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Border Width", 0, 20, pImg.borderWidth, b => pImg.borderWidth = b);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Drop Shadow", 0, 40, pImg.dropShadowSoftness, s => pImg.dropShadowSoftness = s, true);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Gradient Angle", 0, 360, pImg.gradientAngle, a => { pImg.fillMode = FillMode.LinearGradient; pImg.gradientAngle = a; }, true);
            }

            // 3. TextMeshProUGUI
            var tmp = m_InspectedObject.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Typography");
                UIInspectorFieldFactory.CreateInputField(m_FieldsContainer, "Text", tmp.text, t => tmp.text = t);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Font Size", 8, 48, tmp.fontSize, s => tmp.fontSize = s, true);
            }

            // 4. Motion / Animator
            var animator = m_InspectedObject.GetComponent<UIClassAnimator>();
            if (animator != null)
            {
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Motion & Interaction");
                UIInspectorFieldFactory.CreateActionButton(m_FieldsContainer, "Trigger Click Punch", new Color(0.12f, 0.40f, 0.70f, 1f), () => animator.TriggerClickPunch());
                UIInspectorFieldFactory.CreateActionButton(m_FieldsContainer, "Reset to Rest", new Color(0.20f, 0.25f, 0.32f, 1f), () => animator.ResetToRest());
            }

            // 5. Actions / Deletion
            UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Element Actions");
            UIInspectorFieldFactory.CreateActionButton(m_FieldsContainer, "Delete From Canvas", new Color(0.65f, 0.15f, 0.18f, 1f), () =>
            {
                var toDestroy = m_InspectedObject;
                UIStudioSelection.Clear();
                if (toDestroy != null) Destroy(toDestroy);
            });
        }

        private void SetupAddPropertyMenu()
        {
            if (m_AddPropertyDropdown == null) return;

            m_AddPropertyDropdown.ClearOptions();
            var options = new List<string>
            {
                "+ Add Property / Modifier...",
                "+ Add Border (2px)",
                "+ Add Drop Shadow",
                "+ Add Motion Animator",
                "+ Add Gradient Fill",
                "+ Theme Primary Tint",
                "Delete Element"
            };
            m_AddPropertyDropdown.AddOptions(options);
            m_AddPropertyDropdown.onValueChanged.AddListener(OnAddPropertySelected);
        }

        private void OnAddPropertySelected(int index)
        {
            if (m_InspectedObject == null || index <= 0) return;

            switch (index)
            {
                case 1: // Add Border
                    var pImg = m_InspectedObject.GetComponent<ProceduralImage>();
                    if (pImg == null) pImg = m_InspectedObject.AddComponent<ProceduralImage>();
                    pImg.borderWidth = 2f;
                    pImg.borderColor = new Color(1f, 1f, 1f, 0.8f);
                    break;
                case 2: // Add Drop Shadow
                    var pImgShadow = m_InspectedObject.GetComponent<ProceduralImage>();
                    if (pImgShadow == null) pImgShadow = m_InspectedObject.AddComponent<ProceduralImage>();
                    pImgShadow.dropShadowSoftness = 16f;
                    pImgShadow.dropShadowOffset = new Vector2(0, -6);
                    break;
                case 3: // Add Motion Animator
                    if (m_InspectedObject.GetComponent<UIClassAnimator>() == null)
                    {
                        var anim = m_InspectedObject.AddComponent<UIClassAnimator>();
                        var bounce = Resources.Load<UIAnimationClass>("Animations/AnimClass_ButtonBounce");
                        if (bounce != null) anim.AnimationClass = bounce;
                    }
                    break;
                case 4: // Add Gradient Fill
                    var pImgGrad = m_InspectedObject.GetComponent<ProceduralImage>();
                    if (pImgGrad != null)
                    {
                        pImgGrad.fillMode = FillMode.LinearGradient;
                        pImgGrad.gradientAngle = 45f;
                        pImgGrad.gradientEndColor = new Color(0.1f, 0.8f, 0.9f, 1f);
                    }
                    break;
                case 5: // Theme Primary Tint
                    var themeImg = m_InspectedObject.GetComponent<ProceduralImage>();
                    if (themeImg != null)
                    {
                        themeImg.color = UIThemeManager.GetColor("primary", new Color(0f, 0.85f, 1f, 1f));
                    }
                    break;
                case 6: // Delete Element
                    var toDel = m_InspectedObject;
                    UIStudioSelection.Clear();
                    if (toDel != null) Destroy(toDel);
                    break;
            }

            m_AddPropertyDropdown.value = 0;
            Inspect(m_InspectedObject);
        }
    }
}
