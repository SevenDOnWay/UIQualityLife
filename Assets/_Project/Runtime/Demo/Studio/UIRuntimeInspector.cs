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
    /// Dynamically reads all components on the raycasted GameObject and provides live tweaking,
    /// hierarchy breadcrumb navigation, and property injection.
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
                if (m_SelectedSubtitleText != null) m_SelectedSubtitleText.text = "Click any element on canvas to inspect via Raycast";
                RenderCanvasDefaultInfo();
                return;
            }

            var allComponents = m_InspectedObject.GetComponents<Component>();
            int compCount = allComponents != null ? allComponents.Length : 0;

            if (m_SelectedTitleText != null)
                m_SelectedTitleText.text = m_InspectedObject.name;

            if (m_SelectedSubtitleText != null)
            {
                string parentName = m_InspectedObject.transform.parent != null ? m_InspectedObject.transform.parent.name : "Root";
                m_SelectedSubtitleText.text = $"{parentName} | {compCount} Components | {m_InspectedObject.transform.childCount} Children";
            }

            BuildInspectorFields(allComponents);
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
            text.text = "<b>Raycast UI Inspector</b>\n\n- In <b>Inspect Mode</b>, click anywhere on the canvas viewport: raycast directly captures the exact GameObject under your cursor.\n\n- All attached components on that GameObject are dynamically read and displayed here.\n\n- Use the <b>Hierarchy Breadcrumb</b> to navigate upwards to parent containers or down to child elements.\n\n- In <b>Interact Mode</b>, right-click any element to inspect it instantly!";
            text.fontSize = 13;
            text.color = new Color(0.7f, 0.75f, 0.85f, 1f);
            var le = infoGo.AddComponent<LayoutElement>();
            le.preferredHeight = 160;
            le.flexibleWidth = 1;
        }

        private void BuildInspectorFields(Component[] allComponents)
        {
            if (m_InspectedObject == null || m_FieldsContainer == null) return;

            // 1. Hierarchy & Breadcrumbs Navigation
            BuildHierarchyNavigation();

            // 2. GameObject Basic Properties
            UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Game Object");
            UIInspectorFieldFactory.CreateInputField(m_FieldsContainer, "Name", m_InspectedObject.name, n =>
            {
                m_InspectedObject.name = n;
                if (m_SelectedTitleText != null) m_SelectedTitleText.text = n;
            });
            UIInspectorFieldFactory.CreateToggleField(m_FieldsContainer, "Active", m_InspectedObject.activeSelf, a => m_InspectedObject.SetActive(a));

            // Set of components already handled with custom UI
            var handledComponents = new HashSet<Component>();

            // 3. RectTransform
            var rt = m_InspectedObject.GetComponent<RectTransform>();
            if (rt != null)
            {
                handledComponents.Add(rt);
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "RectTransform");
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Width", 10, 800, rt.sizeDelta.x, w => rt.sizeDelta = new Vector2(w, rt.sizeDelta.y), true);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Height", 10, 600, rt.sizeDelta.y, h => rt.sizeDelta = new Vector2(rt.sizeDelta.x, h), true);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Pos X", -400, 400, rt.anchoredPosition.x, x =>
                {
                    rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
                    var anim = rt.GetComponent<UIQualityLife.Styling.UIClassAnimator>();
                    if (anim != null) anim.BaseAnchoredPosition = rt.anchoredPosition;
                }, true);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Pos Y", -400, 400, rt.anchoredPosition.y, y =>
                {
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
                    var anim = rt.GetComponent<UIQualityLife.Styling.UIClassAnimator>();
                    if (anim != null) anim.BaseAnchoredPosition = rt.anchoredPosition;
                }, true);
            }

            // 4. ProceduralImage (Procedural Graphic)
            var pImg = m_InspectedObject.GetComponent<ProceduralImage>();
            if (pImg != null)
            {
                handledComponents.Add(pImg);
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Procedural Graphic");
                var styles = new List<string> { "Round", "Octagonal", "Scooped" };
                UIInspectorFieldFactory.CreateDropdownField(m_FieldsContainer, "Corner Style", styles, (int)pImg.cornerStyle, s => pImg.cornerStyle = (CornerStyle)s);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Radius", 0, 80, pImg.uniformRadius, r => { pImg.uniformCorners = true; pImg.uniformRadius = r; }, true);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Border Width", 0, 20, pImg.borderWidth, b => pImg.borderWidth = b);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Drop Shadow", 0, 40, pImg.dropShadowSoftness, s => pImg.dropShadowSoftness = s, true);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Shadow Offset Y", -30, 30, pImg.dropShadowOffset.y, y => pImg.dropShadowOffset = new Vector2(pImg.dropShadowOffset.x, y), true);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Gradient Angle", 0, 360, pImg.gradientAngle, a => { pImg.fillMode = FillMode.LinearGradient; pImg.gradientAngle = a; }, true);
                UIInspectorFieldFactory.CreateColorPresetsRow(m_FieldsContainer, "Tint Color", pImg.color, c => pImg.color = c);
            }

            // 5. Standard Image (if not ProceduralImage)
            var img = m_InspectedObject.GetComponent<Image>();
            if (img != null && pImg == null)
            {
                handledComponents.Add(img);
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "UI Image");
                UIInspectorFieldFactory.CreateToggleField(m_FieldsContainer, "Raycast Target", img.raycastTarget, r => img.raycastTarget = r);
                UIInspectorFieldFactory.CreateColorPresetsRow(m_FieldsContainer, "Image Color", img.color, c => img.color = c);
            }

            // 6. RawImage
            var rawImg = m_InspectedObject.GetComponent<RawImage>();
            if (rawImg != null)
            {
                handledComponents.Add(rawImg);
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Raw Image");
                UIInspectorFieldFactory.CreateToggleField(m_FieldsContainer, "Raycast Target", rawImg.raycastTarget, r => rawImg.raycastTarget = r);
                UIInspectorFieldFactory.CreateColorPresetsRow(m_FieldsContainer, "Color", rawImg.color, c => rawImg.color = c);
            }

            // 7. TextMeshProUGUI / TMP_Text
            var tmp = m_InspectedObject.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                handledComponents.Add(tmp);
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Typography (TMP)");
                UIInspectorFieldFactory.CreateInputField(m_FieldsContainer, "Text", tmp.text, t => tmp.text = t);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Font Size", 8, 64, tmp.fontSize, s => tmp.fontSize = s, true);
                UIInspectorFieldFactory.CreateColorPresetsRow(m_FieldsContainer, "Text Color", tmp.color, c => tmp.color = c);
            }

            // 8. Legacy Text
            var legacyText = m_InspectedObject.GetComponent<Text>();
            if (legacyText != null)
            {
                handledComponents.Add(legacyText);
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Legacy Text");
                UIInspectorFieldFactory.CreateInputField(m_FieldsContainer, "Text", legacyText.text, t => legacyText.text = t);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Font Size", 8, 64, legacyText.fontSize, s => legacyText.fontSize = (int)s, true);
                UIInspectorFieldFactory.CreateColorPresetsRow(m_FieldsContainer, "Color", legacyText.color, c => legacyText.color = c);
            }

            // 9. Button
            var btn = m_InspectedObject.GetComponent<Button>();
            if (btn != null)
            {
                handledComponents.Add(btn);
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "UI Button");
                UIInspectorFieldFactory.CreateToggleField(m_FieldsContainer, "Interactable", btn.interactable, i => btn.interactable = i);
                UIInspectorFieldFactory.CreateActionButton(m_FieldsContainer, "Simulate Button Click", new Color(0.12f, 0.40f, 0.70f, 1f), () => btn.onClick.Invoke());
            }

            // 10. Slider
            var slider = m_InspectedObject.GetComponent<Slider>();
            if (slider != null)
            {
                handledComponents.Add(slider);
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "UI Slider");
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Value", slider.minValue, slider.maxValue, slider.value, v => slider.value = v, slider.wholeNumbers);
                UIInspectorFieldFactory.CreateToggleField(m_FieldsContainer, "Whole Numbers", slider.wholeNumbers, w => slider.wholeNumbers = w);
                UIInspectorFieldFactory.CreateToggleField(m_FieldsContainer, "Interactable", slider.interactable, i => slider.interactable = i);
            }

            // 11. Toggle
            var toggle = m_InspectedObject.GetComponent<Toggle>();
            if (toggle != null)
            {
                handledComponents.Add(toggle);
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "UI Toggle");
                UIInspectorFieldFactory.CreateToggleField(m_FieldsContainer, "Is On", toggle.isOn, o => toggle.isOn = o);
                UIInspectorFieldFactory.CreateToggleField(m_FieldsContainer, "Interactable", toggle.interactable, i => toggle.interactable = i);
            }

            // 12. TMP_InputField
            var tmpInput = m_InspectedObject.GetComponent<TMP_InputField>();
            if (tmpInput != null)
            {
                handledComponents.Add(tmpInput);
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "TMP Input Field");
                UIInspectorFieldFactory.CreateInputField(m_FieldsContainer, "Text", tmpInput.text, t => tmpInput.text = t);
                UIInspectorFieldFactory.CreateToggleField(m_FieldsContainer, "Read Only", tmpInput.readOnly, r => tmpInput.readOnly = r);
            }

            // 13. UIClassAnimator
            var animator = m_InspectedObject.GetComponent<UIClassAnimator>();
            if (animator != null)
            {
                handledComponents.Add(animator);
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Class Animator");
                string animName = animator.AnimationClass != null ? animator.AnimationClass.name : "None";
                UIInspectorFieldFactory.CreateInputField(m_FieldsContainer, "Profile", animName, null);
                UIInspectorFieldFactory.CreateActionButton(m_FieldsContainer, "Trigger Click Punch", new Color(0.12f, 0.40f, 0.70f, 1f), () => animator.TriggerClickPunch());
                UIInspectorFieldFactory.CreateActionButton(m_FieldsContainer, "Reset to Rest", new Color(0.20f, 0.25f, 0.32f, 1f), () => animator.ResetToRest());
            }

            // 14. CanvasGroup
            var cg = m_InspectedObject.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                handledComponents.Add(cg);
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Canvas Group");
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Alpha", 0f, 1f, cg.alpha, a => cg.alpha = a);
                UIInspectorFieldFactory.CreateToggleField(m_FieldsContainer, "Interactable", cg.interactable, i => cg.interactable = i);
                UIInspectorFieldFactory.CreateToggleField(m_FieldsContainer, "Blocks Raycasts", cg.blocksRaycasts, b => cg.blocksRaycasts = b);
            }

            // 15. Layout Element
            var le = m_InspectedObject.GetComponent<LayoutElement>();
            if (le != null)
            {
                handledComponents.Add(le);
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Layout Element");
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Pref Width", 0, 800, Mathf.Max(0, le.preferredWidth), w => le.preferredWidth = w, true);
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Pref Height", 0, 600, Mathf.Max(0, le.preferredHeight), h => le.preferredHeight = h, true);
                UIInspectorFieldFactory.CreateToggleField(m_FieldsContainer, "Ignore Layout", le.ignoreLayout, ig => le.ignoreLayout = ig);
            }

            // 16. Layout Groups
            var hlg = m_InspectedObject.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (hlg != null)
            {
                handledComponents.Add(hlg);
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, $"Layout Group ({hlg.GetType().Name})");
                UIInspectorFieldFactory.CreateSliderField(m_FieldsContainer, "Spacing", 0, 50, hlg.spacing, s => hlg.spacing = s, true);
            }

            // 17. Content Size Fitter
            var csf = m_InspectedObject.GetComponent<ContentSizeFitter>();
            if (csf != null)
            {
                handledComponents.Add(csf);
                UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Content Size Fitter");
                var fitModes = new List<string> { "Unconstrained", "MinSize", "PreferredSize" };
                UIInspectorFieldFactory.CreateDropdownField(m_FieldsContainer, "Horizontal", fitModes, (int)csf.horizontalFit, h => csf.horizontalFit = (ContentSizeFitter.FitMode)h);
                UIInspectorFieldFactory.CreateDropdownField(m_FieldsContainer, "Vertical", fitModes, (int)csf.verticalFit, v => csf.verticalFit = (ContentSizeFitter.FitMode)v);
            }

            // 18. Generic Readers for other components
            if (allComponents != null)
            {
                foreach (var comp in allComponents)
                {
                    if (comp == null || handledComponents.Contains(comp)) continue;

                    string compTypeName = comp.GetType().Name;
                    UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, $"Component: {compTypeName}");

                    if (comp is Behaviour b)
                    {
                        UIInspectorFieldFactory.CreateToggleField(m_FieldsContainer, "Enabled", b.enabled, en => b.enabled = en);
                    }
                }
            }

            // 19. Actions / Deletion
            UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Element Actions");
            UIInspectorFieldFactory.CreateActionButton(m_FieldsContainer, "Duplicate GameObject", new Color(0.18f, 0.40f, 0.60f, 1f), () =>
            {
                if (m_InspectedObject != null)
                {
                    var dup = Instantiate(m_InspectedObject, m_InspectedObject.transform.parent);
                    dup.name = m_InspectedObject.name + "_Copy";
                    UIStudioSelection.Select(dup);
                }
            });

            UIInspectorFieldFactory.CreateActionButton(m_FieldsContainer, "Delete From Canvas", new Color(0.65f, 0.15f, 0.18f, 1f), () =>
            {
                var toDestroy = m_InspectedObject;
                UIStudioSelection.Clear();
                if (toDestroy != null) Destroy(toDestroy);
            });
        }

        private void BuildHierarchyNavigation()
        {
            if (m_InspectedObject == null || m_FieldsContainer == null) return;

            UIInspectorFieldFactory.CreateSectionHeader(m_FieldsContainer, "Hierarchy Navigation");

            // Build ancestor chain
            var chain = new List<Transform>();
            var curr = m_InspectedObject.transform;
            while (curr != null)
            {
                chain.Insert(0, curr);
                // Stop if we reach Artboard container
                if (curr.name == "ArtboardContainer") break;
                curr = curr.parent;
            }

            // Breadcrumb row
            var breadcrumbRow = new GameObject("BreadcrumbRow");
            breadcrumbRow.transform.SetParent(m_FieldsContainer, false);
            var bRt = breadcrumbRow.AddComponent<RectTransform>();
            bRt.sizeDelta = new Vector2(0, 28);
            var bLayout = breadcrumbRow.AddComponent<HorizontalLayoutGroup>();
            bLayout.childControlWidth = false;
            bLayout.childControlHeight = true;
            bLayout.spacing = 4f;

            var bLe = breadcrumbRow.AddComponent<LayoutElement>();
            bLe.preferredHeight = 28;
            bLe.flexibleWidth = 1;

            for (int i = 0; i < chain.Count; i++)
            {
                var node = chain[i];
                bool isCurrent = (node.gameObject == m_InspectedObject);
                string nodeName = node.name;
                if (nodeName.Length > 12) nodeName = nodeName.Substring(0, 10) + "..";

                var targetGo = node.gameObject;
                UIInspectorFieldFactory.CreatePillButton(breadcrumbRow.transform, nodeName, isCurrent, () =>
                {
                    UIStudioSelection.Select(targetGo);
                });

                if (i < chain.Count - 1)
                {
                    var arrowGo = new GameObject("Sep");
                    arrowGo.transform.SetParent(breadcrumbRow.transform, false);
                    var arrowText = arrowGo.AddComponent<TextMeshProUGUI>();
                    arrowText.text = ">";
                    arrowText.fontSize = 11;
                    arrowText.alignment = TextAlignmentOptions.Center;
                    arrowText.color = new Color(0.5f, 0.6f, 0.7f, 0.8f);
                    var aLe = arrowGo.AddComponent<LayoutElement>();
                    aLe.preferredWidth = 12;
                }
            }

            // Direct children row if any
            int childCount = m_InspectedObject.transform.childCount;
            if (childCount > 0)
            {
                var childRow = new GameObject("ChildrenRow");
                childRow.transform.SetParent(m_FieldsContainer, false);
                var cRt = childRow.AddComponent<RectTransform>();
                cRt.sizeDelta = new Vector2(0, 26);
                var cLayout = childRow.AddComponent<HorizontalLayoutGroup>();
                cLayout.childControlWidth = false;
                cLayout.childControlHeight = true;
                cLayout.spacing = 4f;

                var cLe = childRow.AddComponent<LayoutElement>();
                cLe.preferredHeight = 26;
                cLe.flexibleWidth = 1;

                // Children label
                var cLbl = new GameObject("Label");
                cLbl.transform.SetParent(childRow.transform, false);
                var lblText = cLbl.AddComponent<TextMeshProUGUI>();
                lblText.text = $"Children ({childCount}):";
                lblText.fontSize = 11;
                lblText.alignment = TextAlignmentOptions.MidlineLeft;
                lblText.color = new Color(0.6f, 0.7f, 0.8f, 1f);
                var clLe = cLbl.AddComponent<LayoutElement>();
                clLe.preferredWidth = 75;

                for (int i = 0; i < Mathf.Min(childCount, 6); i++)
                {
                    var child = m_InspectedObject.transform.GetChild(i);
                    var targetChild = child.gameObject;
                    string cName = child.name;
                    if (cName.Length > 10) cName = cName.Substring(0, 8) + "..";

                    UIInspectorFieldFactory.CreatePillButton(childRow.transform, cName, false, () =>
                    {
                        UIStudioSelection.Select(targetChild);
                    });
                }
            }
        }

        private void SetupAddPropertyMenu()
        {
            if (m_AddPropertyDropdown == null) return;

            m_AddPropertyDropdown.ClearOptions();
            var options = new List<string>
            {
                "+ Add Property / Modifier...",
                "+ Add ProceduralImage",
                "+ Add Border (2px)",
                "+ Add Drop Shadow",
                "+ Add Motion Animator",
                "+ Add Gradient Fill",
                "+ Add Button",
                "+ Add CanvasGroup",
                "+ Add LayoutElement",
                "+ Add ContentSizeFitter",
                "+ Add TextMeshPro",
                "Duplicate GameObject",
                "Delete GameObject"
            };
            m_AddPropertyDropdown.AddOptions(options);
            m_AddPropertyDropdown.onValueChanged.AddListener(OnAddPropertySelected);
        }

        private void OnAddPropertySelected(int index)
        {
            if (m_InspectedObject == null || index <= 0) return;

            switch (index)
            {
                case 1: // Add ProceduralImage
                    var pi = m_InspectedObject.GetComponent<ProceduralImage>();
                    if (pi == null) pi = m_InspectedObject.AddComponent<ProceduralImage>();
                    pi.color = new Color(0.12f, 0.16f, 0.24f, 1f);
                    pi.uniformRadius = 8f;
                    break;
                case 2: // Add Border
                    var pImg = m_InspectedObject.GetComponent<ProceduralImage>();
                    if (pImg == null) pImg = m_InspectedObject.AddComponent<ProceduralImage>();
                    pImg.borderWidth = 2f;
                    pImg.borderColor = new Color(0f, 0.85f, 1f, 0.8f);
                    break;
                case 3: // Add Drop Shadow
                    var pImgShadow = m_InspectedObject.GetComponent<ProceduralImage>();
                    if (pImgShadow == null) pImgShadow = m_InspectedObject.AddComponent<ProceduralImage>();
                    pImgShadow.dropShadowSoftness = 16f;
                    pImgShadow.dropShadowOffset = new Vector2(0, -6);
                    break;
                case 4: // Add Motion Animator
                    if (m_InspectedObject.GetComponent<UIClassAnimator>() == null)
                    {
                        var anim = m_InspectedObject.AddComponent<UIClassAnimator>();
                        var bounce = Resources.Load<UIAnimationClass>("Animations/AnimClass_ButtonBounce");
                        if (bounce != null) anim.AnimationClass = bounce;
                    }
                    break;
                case 5: // Add Gradient Fill
                    var pImgGrad = m_InspectedObject.GetComponent<ProceduralImage>();
                    if (pImgGrad != null)
                    {
                        pImgGrad.fillMode = FillMode.LinearGradient;
                        pImgGrad.gradientAngle = 45f;
                        pImgGrad.gradientEndColor = new Color(0.1f, 0.8f, 0.9f, 1f);
                    }
                    break;
                case 6: // Add Button
                    if (m_InspectedObject.GetComponent<Button>() == null)
                    {
                        m_InspectedObject.AddComponent<Button>();
                    }
                    break;
                case 7: // Add CanvasGroup
                    if (m_InspectedObject.GetComponent<CanvasGroup>() == null)
                    {
                        m_InspectedObject.AddComponent<CanvasGroup>();
                    }
                    break;
                case 8: // Add LayoutElement
                    if (m_InspectedObject.GetComponent<LayoutElement>() == null)
                    {
                        m_InspectedObject.AddComponent<LayoutElement>();
                    }
                    break;
                case 9: // Add ContentSizeFitter
                    if (m_InspectedObject.GetComponent<ContentSizeFitter>() == null)
                    {
                        var csf = m_InspectedObject.AddComponent<ContentSizeFitter>();
                        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    }
                    break;
                case 10: // Add TextMeshPro
                    var childText = new GameObject("Text");
                    childText.transform.SetParent(m_InspectedObject.transform, false);
                    var ctRt = childText.AddComponent<RectTransform>();
                    ctRt.sizeDelta = new Vector2(160, 30);
                    var tmp = childText.AddComponent<TextMeshProUGUI>();
                    tmp.text = "New Text";
                    tmp.fontSize = 14;
                    tmp.alignment = TextAlignmentOptions.Center;
                    break;
                case 11: // Duplicate GameObject
                    var dup = Instantiate(m_InspectedObject, m_InspectedObject.transform.parent);
                    dup.name = m_InspectedObject.name + "_Copy";
                    UIStudioSelection.Select(dup);
                    return;
                case 12: // Delete GameObject
                    var toDel = m_InspectedObject;
                    UIStudioSelection.Clear();
                    if (toDel != null) Destroy(toDel);
                    return;
            }

            m_AddPropertyDropdown.value = 0;
            Inspect(m_InspectedObject);
        }
    }
}
