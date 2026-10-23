#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIQualityLife.Editor
{
    public class UISavePresetWizard : EditorWindow
    {
        private GameObject targetGameObject;
        private TextField nameField;
        private TextField categoryInput;
        private VisualElement categorySection;
        private VisualElement tagBox;
        private VisualElement activeTagsContainer;
        private VisualElement suggestionsBox;
        private VisualElement suggestionsChipsContainer;
        private Label suggestHeaderLabel;

        private TextField descField;
        private ColorField colorField;
        private VisualElement previewSwatch;

        private List<string> selectedCategories = new List<string>();

        [MenuItem("GameObject/UI Quality of Life/Save Selection as Preset", false, 0)]
        public static void OpenFromHierarchy()
        {
            var go = Selection.activeGameObject;
            if (go == null || go.GetComponent<RectTransform>() == null)
            {
                EditorUtility.DisplayDialog("UI Quality of Life", "Please select a UI GameObject (with a RectTransform) to save as a preset.", "OK");
                return;
            }

            Open(go);
        }

        [MenuItem("GameObject/UI Quality of Life/Save Selection as Preset", true)]
        public static bool ValidateOpenFromHierarchy()
        {
            return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<RectTransform>() != null;
        }

        public static void Open(GameObject go)
        {
            var window = CreateInstance<UISavePresetWizard>();
            window.targetGameObject = go;
            window.titleContent = new GUIContent("Save UI Preset");
            window.minSize = new Vector2(380, 390);
            window.maxSize = new Vector2(500, 500);
            window.ShowUtility();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.paddingTop = 14;
            rootVisualElement.style.paddingBottom = 14;
            rootVisualElement.style.paddingLeft = 14;
            rootVisualElement.style.paddingRight = 14;
            rootVisualElement.style.backgroundColor = UIElementStyleUtils.WindowBg;

            // Title
            var title = new Label("💾 Save UI Preset");
            title.style.fontSize = 15;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = UIElementStyleUtils.TextPrimary;
            rootVisualElement.Add(title);

            var subtitle = new Label($"Exporting: '{(targetGameObject != null ? targetGameObject.name : "None")}'");
            subtitle.style.fontSize = 11;
            subtitle.style.color = UIElementStyleUtils.TextSecondary;
            subtitle.style.marginBottom = 12;
            rootVisualElement.Add(subtitle);

            // Preset Name
            string defaultName = targetGameObject != null ? targetGameObject.name : "New Component";
            nameField = new TextField("Preset Name") { value = defaultName };
            nameField.style.marginBottom = 10;
            rootVisualElement.Add(nameField);

            // =========================================================================
            // MULTI-CATEGORY TAG INPUT WITH TYPE / CLICK SUGGESTIONS
            // =========================================================================
            categorySection = new VisualElement();
            categorySection.style.marginBottom = 10;

            var categoryHeaderRow = new VisualElement();
            categoryHeaderRow.style.flexDirection = FlexDirection.Row;
            categoryHeaderRow.style.justifyContent = Justify.SpaceBetween;
            categoryHeaderRow.style.alignItems = Align.Center;

            var categoryLabel = new Label("Categories / Tags");
            categoryLabel.style.fontSize = 12;
            categoryLabel.style.color = UIElementStyleUtils.TextPrimary;
            categoryHeaderRow.Add(categoryLabel);

            var hintLabel = new Label("Click or type for suggestions");
            hintLabel.style.fontSize = 9;
            hintLabel.style.color = UIElementStyleUtils.TextMuted;
            categoryHeaderRow.Add(hintLabel);

            categorySection.Add(categoryHeaderRow);

            // Initial category setup
            string initialCategory = GuessCategory(targetGameObject);
            selectedCategories.Clear();
            if (!string.IsNullOrEmpty(initialCategory))
                selectedCategories.Add(initialCategory);

            // Tag input box (holds active tags + inline text field)
            tagBox = new VisualElement();
            tagBox.style.flexDirection = FlexDirection.Row;
            tagBox.style.flexWrap = Wrap.Wrap;
            tagBox.style.alignItems = Align.Center;
            tagBox.style.backgroundColor = UIElementStyleUtils.PanelBg;
            SetBorder(tagBox, 1, UIElementStyleUtils.BorderDefault);
            SetBorderRadius(tagBox, 3);
            tagBox.style.paddingLeft = 4;
            tagBox.style.paddingRight = 4;
            tagBox.style.paddingTop = 3;
            tagBox.style.paddingBottom = 3;
            tagBox.style.minHeight = 32;
            tagBox.style.marginTop = 4;

            activeTagsContainer = new VisualElement();
            activeTagsContainer.style.flexDirection = FlexDirection.Row;
            activeTagsContainer.style.flexWrap = Wrap.Wrap;
            activeTagsContainer.style.alignItems = Align.Center;
            tagBox.Add(activeTagsContainer);

            categoryInput = new TextField();
            categoryInput.style.flexGrow = 1;
            categoryInput.style.minWidth = 80;
            categoryInput.style.backgroundColor = Color.clear;
            SetBorder(categoryInput, 0, Color.clear);
            categoryInput.style.marginLeft = 2;
            categoryInput.style.paddingLeft = 2;
            categoryInput.style.paddingTop = 1;
            categoryInput.style.paddingBottom = 1;

            tagBox.Add(categoryInput);
            categorySection.Add(tagBox);

            // Suggestions Dropdown (Only shown when user clicks or types!)
            suggestionsBox = new VisualElement();
            suggestionsBox.style.display = DisplayStyle.None;
            suggestionsBox.style.backgroundColor = UIElementStyleUtils.PanelBg;
            suggestionsBox.style.borderBottomWidth = 1;
            suggestionsBox.style.borderLeftWidth = 1;
            suggestionsBox.style.borderRightWidth = 1;
            suggestionsBox.style.borderBottomColor = UIElementStyleUtils.BorderDefault;
            suggestionsBox.style.borderLeftColor = UIElementStyleUtils.BorderDefault;
            suggestionsBox.style.borderRightColor = UIElementStyleUtils.BorderDefault;
            suggestionsBox.style.borderBottomLeftRadius = 3;
            suggestionsBox.style.borderBottomRightRadius = 3;
            suggestionsBox.style.paddingLeft = 6;
            suggestionsBox.style.paddingRight = 6;
            suggestionsBox.style.paddingTop = 6;
            suggestionsBox.style.paddingBottom = 6;
            suggestionsBox.style.maxHeight = 110;

            var suggestHeader = new VisualElement();
            suggestHeader.style.flexDirection = FlexDirection.Row;
            suggestHeader.style.justifyContent = Justify.SpaceBetween;
            suggestHeader.style.alignItems = Align.Center;
            suggestHeader.style.marginBottom = 4;

            suggestHeaderLabel = new Label("Suggestions (click to add):");
            suggestHeaderLabel.style.fontSize = 9;
            suggestHeaderLabel.style.color = UIElementStyleUtils.TextSecondary;
            suggestHeader.Add(suggestHeaderLabel);

            var closeSuggestBtn = new Button(HideSuggestions) { text = "Done ✕" };
            closeSuggestBtn.style.height = 16;
            closeSuggestBtn.style.fontSize = 9;
            closeSuggestBtn.style.paddingLeft = 4;
            closeSuggestBtn.style.paddingRight = 4;
            closeSuggestBtn.style.backgroundColor = Color.clear;
            SetBorder(closeSuggestBtn, 0, Color.clear);
            closeSuggestBtn.style.color = UIElementStyleUtils.TextMuted;
            suggestHeader.Add(closeSuggestBtn);

            suggestionsBox.Add(suggestHeader);

            var suggestionsScroll = new ScrollView(ScrollViewMode.Vertical);
            suggestionsScroll.style.maxHeight = 85;

            suggestionsChipsContainer = new VisualElement();
            suggestionsChipsContainer.style.flexDirection = FlexDirection.Row;
            suggestionsChipsContainer.style.flexWrap = Wrap.Wrap;
            suggestionsScroll.Add(suggestionsChipsContainer);
            suggestionsBox.Add(suggestionsScroll);

            categorySection.Add(suggestionsBox);
            rootVisualElement.Add(categorySection);

            // Setup listeners for click & type to trigger suggestions
            tagBox.RegisterCallback<PointerDownEvent>(evt =>
            {
                ShowSuggestions();
                categoryInput.Focus();
                evt.StopPropagation();
            });

            categoryInput.RegisterCallback<FocusInEvent>(evt =>
            {
                ShowSuggestions();
            });

            categoryInput.RegisterValueChangedCallback(evt =>
            {
                ShowSuggestions();
                UpdateSuggestionsList(evt.newValue);
            });

            categoryInput.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.character == ',')
                {
                    string typed = categoryInput.value?.Replace(",", "").Trim();
                    if (!string.IsNullOrEmpty(typed))
                    {
                        AddCategory(typed);
                        categoryInput.SetValueWithoutNotify("");
                        UpdateSuggestionsList("");
                    }
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.Backspace && string.IsNullOrEmpty(categoryInput.value))
                {
                    if (selectedCategories.Count > 0)
                    {
                        selectedCategories.RemoveAt(selectedCategories.Count - 1);
                        RenderActiveTags();
                        UpdateSuggestionsList("");
                    }
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    HideSuggestions();
                    evt.StopPropagation();
                }
            });

            // Click outside category section closes suggestions
            rootVisualElement.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (!categorySection.worldBound.Contains(evt.position))
                {
                    HideSuggestions();
                }
            });

            RenderActiveTags();

            // =========================================================================
            // DESCRIPTION & COLOR FIELDS
            // =========================================================================
            descField = new TextField("Description") { multiline = true };
            descField.style.height = 42;
            descField.style.marginBottom = 8;
            rootVisualElement.Add(descField);

            // Color Field
            Color initialColor = new Color(0.14f, 0.45f, 0.95f);
            if (targetGameObject != null)
            {
                var img = targetGameObject.GetComponent<UnityEngine.UI.Image>();
                if (img != null && img.color.a > 0.1f)
                    initialColor = img.color;
            }

            colorField = new ColorField("Theme Color") { value = initialColor };
            colorField.style.marginBottom = 10;
            colorField.RegisterValueChangedCallback(evt =>
            {
                if (previewSwatch != null)
                    previewSwatch.style.backgroundColor = evt.newValue;
            });
            rootVisualElement.Add(colorField);

            // Preview Swatch
            var previewRow = new VisualElement();
            previewRow.style.flexDirection = FlexDirection.Row;
            previewRow.style.alignItems = Align.Center;
            previewRow.style.marginBottom = 14;
            previewRow.style.paddingLeft = 110;

            previewSwatch = new VisualElement();
            previewSwatch.style.width = 40;
            previewSwatch.style.height = 24;
            previewSwatch.style.backgroundColor = initialColor;
            SetBorderRadius(previewSwatch, 4);
            previewSwatch.style.marginRight = 8;
            previewRow.Add(previewSwatch);

            var previewLabel = new Label("Card indicator color");
            previewLabel.style.fontSize = 10;
            previewLabel.style.color = UIElementStyleUtils.TextSecondary;
            previewRow.Add(previewLabel);
            rootVisualElement.Add(previewRow);

            // Spacer
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            rootVisualElement.Add(spacer);

            // Action Buttons
            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.FlexEnd;

            var cancelBtn = new Button(Close) { text = "Cancel" };
            cancelBtn.style.width = 80;
            cancelBtn.style.marginRight = 8;

            var saveBtn = new Button(SavePreset) { text = "Save Preset" };
            saveBtn.style.width = 110;
            saveBtn.style.backgroundColor = UIElementStyleUtils.UnityAccentBlue;
            saveBtn.style.color = Color.white;
            saveBtn.style.unityFontStyleAndWeight = FontStyle.Bold;

            btnRow.Add(cancelBtn);
            btnRow.Add(saveBtn);
            rootVisualElement.Add(btnRow);
        }

        private void ShowSuggestions()
        {
            if (suggestionsBox == null) return;
            suggestionsBox.style.display = DisplayStyle.Flex;
            UpdateSuggestionsList(categoryInput?.value ?? "");
        }

        private void HideSuggestions()
        {
            if (suggestionsBox == null) return;
            suggestionsBox.style.display = DisplayStyle.None;
        }

        private void RenderActiveTags()
        {
            if (activeTagsContainer == null) return;
            activeTagsContainer.Clear();

            foreach (var cat in selectedCategories)
            {
                string currentCat = cat;
                var tag = new VisualElement();
                tag.style.flexDirection = FlexDirection.Row;
                tag.style.alignItems = Align.Center;
                tag.style.backgroundColor = UIElementStyleUtils.UnityAccentBlue;
                SetBorderRadius(tag, 3);
                tag.style.paddingLeft = 7;
                tag.style.paddingRight = 6;
                tag.style.paddingTop = 2;
                tag.style.paddingBottom = 2;
                tag.style.marginRight = 4;
                tag.style.marginBottom = 2;

                var tagText = new Label(currentCat);
                tagText.style.fontSize = 10;
                tagText.style.color = Color.white;
                tagText.style.unityFontStyleAndWeight = FontStyle.Bold;
                tag.Add(tagText);

                var removeBtn = new Label(" ✕");
                removeBtn.style.fontSize = 9;
                removeBtn.style.color = UIElementStyleUtils.TextPrimary;
                removeBtn.style.marginLeft = 2;
                removeBtn.RegisterCallback<PointerDownEvent>(e =>
                {
                    e.StopPropagation();
                    selectedCategories.Remove(currentCat);
                    RenderActiveTags();
                    UpdateSuggestionsList(categoryInput.value);
                });
                tag.Add(removeBtn);

                activeTagsContainer.Add(tag);
            }
        }

        private void UpdateSuggestionsList(string query)
        {
            if (suggestionsChipsContainer == null) return;
            suggestionsChipsContainer.Clear();

            var allKnown = UIPresetDatabase.GetAllCategories();
            allKnown.Remove("All");

            string[] defaults = { "Buttons", "Cards", "Modals", "Bars", "HUD", "Navigation", "Inputs", "Primary", "Danger", "Secondary", "Form" };
            foreach (var d in defaults)
            {
                if (!allKnown.Contains(d))
                    allKnown.Add(d);
            }

            // Exclude already selected categories
            var available = new List<string>();
            foreach (var cat in allKnown)
            {
                if (!selectedCategories.Contains(cat))
                    available.Add(cat);
            }

            string cleanQuery = query?.Trim().ToLowerInvariant() ?? "";

            // If user typed something specific, filter suggestions
            List<string> matches = string.IsNullOrEmpty(cleanQuery)
                ? available
                : available.FindAll(c => c.ToLowerInvariant().Contains(cleanQuery));

            // If user typed a custom category that doesn't exist, show [+ Create "query"]
            if (!string.IsNullOrEmpty(cleanQuery))
            {
                bool exists = allKnown.Exists(c => string.Equals(c, cleanQuery, StringComparison.OrdinalIgnoreCase)) ||
                              selectedCategories.Exists(c => string.Equals(c, cleanQuery, StringComparison.OrdinalIgnoreCase));

                if (!exists)
                {
                    string customName = query.Trim();
                    var createChip = new Button(() =>
                    {
                        AddCategory(customName);
                        categoryInput.SetValueWithoutNotify("");
                        UpdateSuggestionsList("");
                    })
                    {
                        text = $"[+ Add \"{customName}\"]"
                    };
                    createChip.style.height = 18;
                    createChip.style.fontSize = 9;
                    createChip.style.marginRight = 4;
                    createChip.style.marginBottom = 4;
                    createChip.style.backgroundColor = UIElementStyleUtils.UnityAccentBlueHover;
                    createChip.style.color = Color.white;
                    SetBorderRadius(createChip, 3);
                    suggestionsChipsContainer.Add(createChip);
                }
            }

            if (matches.Count == 0 && string.IsNullOrEmpty(cleanQuery))
            {
                var allSelectedLabel = new Label("All known categories added.");
                allSelectedLabel.style.fontSize = 9;
                allSelectedLabel.style.color = UIElementStyleUtils.TextMuted;
                suggestionsChipsContainer.Add(allSelectedLabel);
                return;
            }

            foreach (var cat in matches)
            {
                string catName = cat;
                var chip = new Button(() =>
                {
                    AddCategory(catName);
                    categoryInput.SetValueWithoutNotify("");
                    UpdateSuggestionsList("");
                })
                {
                    text = $"+ {catName}"
                };
                chip.style.height = 18;
                chip.style.fontSize = 9;
                chip.style.marginRight = 4;
                chip.style.marginBottom = 4;
                chip.style.backgroundColor = UIElementStyleUtils.UnitySubtleChip;
                chip.style.color = UIElementStyleUtils.TextPrimary;
                SetBorderRadius(chip, 3);

                suggestionsChipsContainer.Add(chip);
            }
        }

        private void AddCategory(string cat)
        {
            if (string.IsNullOrWhiteSpace(cat)) return;
            string clean = cat.Trim();
            if (!selectedCategories.Contains(clean))
            {
                selectedCategories.Add(clean);
                RenderActiveTags();
            }
        }

        private void SavePreset()
        {
            if (targetGameObject == null)
            {
                Close();
                return;
            }

            // Check if user left uncommitted text in the category input
            string uncommitted = categoryInput.value?.Trim();
            if (!string.IsNullOrEmpty(uncommitted))
            {
                AddCategory(uncommitted);
            }

            string pName = nameField.value?.Trim();
            if (string.IsNullOrEmpty(pName))
                pName = targetGameObject.name;

            var catsList = new List<string>();
            foreach (var c in selectedCategories)
            {
                if (!string.IsNullOrWhiteSpace(c))
                    catsList.Add(c.Trim());
            }

            if (catsList.Count == 0)
                catsList.Add("General");

            string desc = descField.value?.Trim() ?? "";
            Color color = colorField.value;

            var preset = UIPresetDatabase.CreatePresetFromGameObject(targetGameObject, pName, catsList.ToArray(), desc, color);

            // Refresh Asset Shelf if open
            if (HasOpenInstances<UIAssetShelfWindow>())
            {
                var shelf = GetWindow<UIAssetShelfWindow>();
                shelf.CreateGUI();
            }

            EditorGUIUtility.PingObject(preset);
            Selection.activeObject = preset;

            Close();
        }

        private static string GuessCategory(GameObject go)
        {
            if (go == null) return "Buttons";

            string n = go.name.ToLowerInvariant();
            if (n.Contains("btn") || n.Contains("button")) return "Buttons";
            if (n.Contains("card") || n.Contains("slot")) return "Cards";
            if (n.Contains("dialog") || n.Contains("modal") || n.Contains("popup")) return "Modals";
            if (n.Contains("bar") || n.Contains("slider") || n.Contains("progress") || n.Contains("hp")) return "Bars";
            if (n.Contains("tab") || n.Contains("nav") || n.Contains("menu")) return "Navigation";
            if (n.Contains("input") || n.Contains("field")) return "Inputs";

            return "Buttons";
        }

        private static void SetBorder(VisualElement elem, float width, Color color) => UIElementStyleUtils.SetBorder(elem, width, color);

        private static void SetBorderRadius(VisualElement elem, float radius) => UIElementStyleUtils.SetBorderRadius(elem, radius);
    }
}
#endif
