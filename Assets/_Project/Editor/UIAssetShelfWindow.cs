#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UIQualityLife.Styling;
using UIQualityLife.Styling.Editor;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// UI Asset Shelf Window.
    /// Manages the top-level UI, category selection, and search filtering.
    /// View mode rendering is delegated to IComponentCardRenderer strategies,
    /// and user interactions are delegated to PresetItemManipulator (SOLID architecture).
    /// </summary>
    public class UIAssetShelfWindow : EditorWindow
    {
        private const string PrefsViewModeKey = "UIQualityLife_Shelf_ViewMode";
        private const string PrefsCategoryKey = "UIQualityLife_Shelf_Category";

        private List<IUIComponentItem> allPresets;
        private string currentSearchQuery = "";
        private string selectedCategory = "All";
        private UIPresetViewMode currentViewMode = UIPresetViewMode.Content;

        private ToolbarSearchField searchField;
        private ToolbarMenu categoryMenu;
        private ToolbarMenu viewModeMenu;
        private ScrollView suggestionScrollView;
        private VisualElement suggestionBar;
        private VisualElement itemsContainer;
        private Label statusCountLabel;
        private SliderInt viewModeSlider;

        // Card Renderer Strategy Registry (OCP)
        private readonly CardRendererRegistry rendererRegistry = new CardRendererRegistry();

        [MenuItem("Tools/UI Quality of Life/UI Asset Shelf", false, 0)]
        [MenuItem("Window/UI Quality of Life/UI Asset Shelf", false, 100)]
        public static void Open()
        {
            var window = GetWindow<UIAssetShelfWindow>();
            window.titleContent = new GUIContent("UI Shelf", EditorGUIUtility.IconContent("d_Prefab Icon").image);
            window.minSize = new Vector2(320, 360);
        }

        private void OnEnable()
        {
            currentViewMode = (UIPresetViewMode)EditorPrefs.GetInt(PrefsViewModeKey, (int)UIPresetViewMode.Content);
            selectedCategory = EditorPrefs.GetString(PrefsCategoryKey, "All");
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void OnFocus()
        {
            ReloadAndRefresh();
        }

        private void OnProjectChanged()
        {
            ReloadAndRefresh();
        }

        public void ReloadAndRefresh()
        {
            LoadPresets();
            if (categoryMenu != null) BuildCategoryMenu();
            UpdateSuggestionBar();
            RefreshList();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.backgroundColor = UIElementStyleUtils.WindowBg;

            // --- 1. TOP TOOLBAR ---
            var topToolbar = new Toolbar();
            topToolbar.style.height = 28;
            topToolbar.style.paddingLeft = 6;
            topToolbar.style.paddingRight = 6;
            topToolbar.style.flexDirection = FlexDirection.Row;
            topToolbar.style.alignItems = Align.Center;

            // Search Field (dynamically expands/adapts to window width)
            searchField = new ToolbarSearchField();
            searchField.style.flexGrow = 1;
            searchField.style.minWidth = 100;
            searchField.RegisterValueChangedCallback(evt =>
            {
                currentSearchQuery = evt.newValue;
                UpdateSuggestionBar();
                RefreshList();
            });
            topToolbar.Add(searchField);

            topToolbar.Add(new ToolbarSpacer());

            // Category Filter Dropdown
            categoryMenu = new ToolbarMenu();
            categoryMenu.text = $"Category: {selectedCategory}";
            categoryMenu.style.flexShrink = 0;
            topToolbar.Add(categoryMenu);

            // View Mode Dropdown
            viewModeMenu = new ToolbarMenu();
            viewModeMenu.text = GetViewModeLabel(currentViewMode);
            viewModeMenu.style.flexShrink = 0;
            topToolbar.Add(viewModeMenu);

            // "+ Save Selection" button
            var saveSelectionBtn = new ToolbarButton(() =>
            {
                var go = Selection.activeGameObject;
                if (go != null && go.GetComponent<RectTransform>() != null)
                {
                    UISavePresetWizard.Open(go);
                }
                else
                {
                    EditorUtility.DisplayDialog("UI Quality of Life",
                        "Select any UI GameObject with a RectTransform in your Hierarchy first, then click '+ Save Selection' to convert it into a preset!",
                        "OK");
                }
            })
            {
                text = "+ Save Selection"
            };
            saveSelectionBtn.style.flexShrink = 0;
            topToolbar.Add(saveSelectionBtn);

            rootVisualElement.Add(topToolbar);

            // --- 2. CATEGORY SUGGESTION STRIP (Search & Suggest) ---
            suggestionScrollView = new ScrollView(ScrollViewMode.Horizontal);
            suggestionScrollView.style.height = 34;
            suggestionScrollView.style.backgroundColor = UIElementStyleUtils.PanelBg;
            suggestionScrollView.style.borderBottomWidth = 1;
            suggestionScrollView.style.borderBottomColor = UIElementStyleUtils.BorderDefault;
            suggestionScrollView.style.paddingLeft = 8;
            suggestionScrollView.style.paddingRight = 8;
            suggestionScrollView.style.paddingTop = 5;
            suggestionScrollView.style.paddingBottom = 5;
            suggestionScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            suggestionBar = new VisualElement();
            suggestionBar.style.flexDirection = FlexDirection.Row;
            suggestionBar.style.alignItems = Align.Center;
            suggestionScrollView.Add(suggestionBar);
            rootVisualElement.Add(suggestionScrollView);

            // --- 3. SCROLLABLE PRESETS CONTAINER ---
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.style.paddingTop = 6;
            scrollView.style.paddingBottom = 6;
            scrollView.style.paddingLeft = 6;
            scrollView.style.paddingRight = 6;

            itemsContainer = new VisualElement();
            scrollView.Add(itemsContainer);
            rootVisualElement.Add(scrollView);

            // --- 4. BOTTOM STATUS TOOLBAR ---
            var bottomToolbar = new Toolbar();
            bottomToolbar.style.height = 22;
            bottomToolbar.style.paddingLeft = 8;
            bottomToolbar.style.paddingRight = 8;
            bottomToolbar.style.justifyContent = Justify.SpaceBetween;

            statusCountLabel = new Label("0 items");
            statusCountLabel.style.fontSize = 11;
            statusCountLabel.style.color = UIElementStyleUtils.TextSecondary;
            bottomToolbar.Add(statusCountLabel);

            var sliderContainer = new VisualElement();
            sliderContainer.style.flexDirection = FlexDirection.Row;
            sliderContainer.style.alignItems = Align.Center;

            var sliderLabel = new Label("View:");
            sliderLabel.style.fontSize = 10;
            sliderLabel.style.color = UIElementStyleUtils.TextSecondary;
            sliderLabel.style.marginRight = 4;
            sliderContainer.Add(sliderLabel);

            viewModeSlider = new SliderInt(0, 3);
            viewModeSlider.value = (int)currentViewMode;
            viewModeSlider.style.width = 90;
            viewModeSlider.RegisterValueChangedCallback(evt =>
            {
                SetViewMode((UIPresetViewMode)evt.newValue);
            });
            sliderContainer.Add(viewModeSlider);

            bottomToolbar.Add(sliderContainer);
            rootVisualElement.Add(bottomToolbar);

            // Load & Initialize
            LoadPresets();
            BuildCategoryMenu();
            BuildViewModeMenu();
            ApplyContainerLayout();
            UpdateSuggestionBar();
            RefreshList();
        }

        private void LoadPresets()
        {
            allPresets = UIPresetDatabase.GetAllLibraryItems();
        }

        private void BuildCategoryMenu()
        {
            categoryMenu.menu.MenuItems().Clear();

            var counts = UIPresetDatabase.GetCategoryCounts();
            var categories = UIPresetDatabase.GetAllCategories();

            foreach (var cat in categories)
            {
                int count = counts.TryGetValue(cat, out int c) ? c : 0;
                string label = $"{cat} ({count})";

                categoryMenu.menu.AppendAction(label, action =>
                {
                    SelectCategory(cat);
                },
                action => selectedCategory == cat ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
        }

        private void SelectCategory(string cat)
        {
            selectedCategory = cat;
            categoryMenu.text = $"Category: {selectedCategory}";
            EditorPrefs.SetString(PrefsCategoryKey, selectedCategory);
            LoadPresets();
            UpdateSuggestionBar();
            RefreshList();
        }

        private void UpdateSuggestionBar()
        {
            if (suggestionBar == null) return;
            suggestionBar.Clear();

            var counts = UIPresetDatabase.GetCategoryCounts();
            string query = currentSearchQuery?.Trim().ToLowerInvariant() ?? "";

            if (!string.IsNullOrEmpty(query))
            {
                var matches = UIPresetDatabase.GetMatchingCategories(query);

                var tip = new Label("Tags:");
                tip.style.fontSize = 11;
                tip.style.color = UIElementStyleUtils.TextMuted;
                tip.style.marginRight = 6;
                suggestionBar.Add(tip);

                if (matches.Count == 0)
                {
                    var none = new Label("(No category tags matched)");
                    none.style.fontSize = 11;
                    none.style.color = UIElementStyleUtils.TextMuted;
                    suggestionBar.Add(none);
                }
                else
                {
                    foreach (var cat in matches)
                    {
                        int count = counts.TryGetValue(cat, out int c) ? c : 0;
                        bool isActive = string.Equals(selectedCategory, cat, System.StringComparison.OrdinalIgnoreCase);

                        var chip = new Button(() => SelectCategory(cat))
                        {
                            text = $"🏷️ {cat} ({count})"
                        };
                        StyleChip(chip, isActive);
                        suggestionBar.Add(chip);
                    }
                }
            }
            else
            {
                var categories = UIPresetDatabase.GetAllCategories();
                foreach (var cat in categories)
                {
                    int count = counts.TryGetValue(cat, out int c) ? c : 0;
                    bool isActive = string.Equals(selectedCategory, cat, System.StringComparison.OrdinalIgnoreCase);

                    var chip = new Button(() => SelectCategory(cat))
                    {
                        text = cat == "All" ? $"All ({count})" : $"{cat} ({count})"
                    };
                    StyleChip(chip, isActive);
                    suggestionBar.Add(chip);
                }
            }
        }

        private void StyleChip(Button chip, bool isActive)
        {
            chip.style.height = 20;
            chip.style.fontSize = 11;
            chip.style.paddingLeft = 8;
            chip.style.paddingRight = 8;
            chip.style.marginRight = 6;
            chip.style.borderTopWidth = 1;
            chip.style.borderBottomWidth = 1;
            chip.style.borderLeftWidth = 1;
            chip.style.borderRightWidth = 1;
            UIElementStyleUtils.SetBorderRadius(chip, 3);

            if (isActive)
            {
                chip.style.backgroundColor = UIElementStyleUtils.UnityAccentBlue;
                chip.style.borderTopColor = UIElementStyleUtils.BorderFocus;
                chip.style.borderBottomColor = UIElementStyleUtils.BorderFocus;
                chip.style.borderLeftColor = UIElementStyleUtils.BorderFocus;
                chip.style.borderRightColor = UIElementStyleUtils.BorderFocus;
                chip.style.color = Color.white;
                chip.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                chip.style.backgroundColor = UIElementStyleUtils.UnitySubtleChip;
                chip.style.borderTopColor = UIElementStyleUtils.BorderDefault;
                chip.style.borderBottomColor = UIElementStyleUtils.BorderDefault;
                chip.style.borderLeftColor = UIElementStyleUtils.BorderDefault;
                chip.style.borderRightColor = UIElementStyleUtils.BorderDefault;
                chip.style.color = UIElementStyleUtils.TextPrimary;
            }
        }

        private void BuildViewModeMenu()
        {
            viewModeMenu.menu.MenuItems().Clear();

            foreach (UIPresetViewMode mode in System.Enum.GetValues(typeof(UIPresetViewMode)))
            {
                string label = GetViewModeLabel(mode);
                viewModeMenu.menu.AppendAction(label, action =>
                {
                    SetViewMode(mode);
                },
                action => currentViewMode == mode ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
        }

        private void SetViewMode(UIPresetViewMode mode)
        {
            currentViewMode = mode;
            EditorPrefs.SetInt(PrefsViewModeKey, (int)currentViewMode);

            if (viewModeSlider != null && viewModeSlider.value != (int)mode)
                viewModeSlider.SetValueWithoutNotify((int)mode);

            if (viewModeMenu != null)
                viewModeMenu.text = GetViewModeLabel(mode);

            ApplyContainerLayout();
            RefreshList();
        }

        private string GetViewModeLabel(UIPresetViewMode mode)
        {
            switch (mode)
            {
                case UIPresetViewMode.List: return "List";
                case UIPresetViewMode.Content: return "Content";
                case UIPresetViewMode.SmallIcon: return "Small Icon";
                case UIPresetViewMode.LargeIcon: return "Large Icon";
                default: return mode.ToString();
            }
        }

        private void ApplyContainerLayout()
        {
            if (itemsContainer == null)
                return;

            if (currentViewMode == UIPresetViewMode.SmallIcon || currentViewMode == UIPresetViewMode.LargeIcon)
            {
                itemsContainer.style.flexDirection = FlexDirection.Row;
                itemsContainer.style.flexWrap = Wrap.Wrap;
                itemsContainer.style.justifyContent = Justify.FlexStart;
                itemsContainer.style.alignItems = Align.FlexStart;
            }
            else
            {
                itemsContainer.style.flexDirection = FlexDirection.Column;
                itemsContainer.style.flexWrap = Wrap.NoWrap;
                itemsContainer.style.justifyContent = Justify.FlexStart;
                itemsContainer.style.alignItems = Align.Stretch;
            }
        }

        private void RefreshList()
        {
            if (itemsContainer == null)
                return;

            itemsContainer.Clear();
            if (allPresets == null || allPresets.Count == 0)
            {
                statusCountLabel.text = "0 items";
                var emptyLabel = new Label("No presets found.\nRight-click any UI in Hierarchy -> 'Save Selection as Preset' to add one!");
                emptyLabel.style.color = UIElementStyleUtils.TextMuted;
                emptyLabel.style.marginTop = 20;
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                emptyLabel.style.alignSelf = Align.Center;
                itemsContainer.Add(emptyLabel);
                return;
            }

            int visibleCount = 0;
            var renderer = rendererRegistry.GetRenderer(currentViewMode);

            foreach (var preset in allPresets)
            {
                if (!preset.HasCategory(selectedCategory))
                    continue;

                if (!preset.MatchesSearch(currentSearchQuery))
                    continue;

                VisualElement itemElem = renderer.Render(preset, SelectCategory);
                itemElem.AddManipulator(new PresetItemManipulator(preset, p => HandleItemDoubleClicked(p)));

                itemsContainer.Add(itemElem);
                visibleCount++;
            }

            statusCountLabel.text = $"{visibleCount} {(visibleCount == 1 ? "item" : "items")}";
        }

        private void HandleItemDoubleClicked(IUIComponentItem item)
        {
            if (item is UIPresetItem p)
            {
                SceneSpawner.SpawnPreset(p);
            }
            else if (item is UIThemeShelfItem th)
            {
                UIThemeManager.ActiveTheme = th.Palette;
                var selectedGOs = Selection.gameObjects;
                if (selectedGOs.Length > 0)
                {
                    foreach (var go in selectedGOs)
                    {
                        var binder = go.GetComponent<UIThemeBinder>();
                        if (binder == null) binder = Undo.AddComponent<UIThemeBinder>(go);
                        binder.ApplyTheme();
                    }
                    Debug.Log($"[UI Shelf] Applied theme '{th.DisplayName}' to {selectedGOs.Length} selected object(s).");
                }
                else
                {
                    Debug.Log($"[UI Shelf] Set active project theme to '{th.DisplayName}'.");
                }
            }
            else if (item is UIAnimationShelfItem an)
            {
                var selectedGOs = Selection.gameObjects;
                if (selectedGOs.Length > 0)
                {
                    foreach (var go in selectedGOs)
                    {
                        var anim = go.GetComponent<UIClassAnimator>();
                        if (anim == null) anim = Undo.AddComponent<UIClassAnimator>(go);
                        anim.AnimationClass = an.AnimClass;
                    }
                    Debug.Log($"[UI Shelf] Applied animation '{an.DisplayName}' to {selectedGOs.Length} selected object(s).");
                }
                else
                {
                    EditorUtility.DisplayDialog("Apply Animation", "Select one or more UI GameObjects in the scene hierarchy first, then double-click to apply this animation class.", "OK");
                }
            }
            else if (item is UIStyleClassShelfItem st)
            {
                var selectedGOs = Selection.gameObjects;
                if (selectedGOs.Length > 0)
                {
                    foreach (var go in selectedGOs)
                    {
                        var binder = go.GetComponent<UIStyleClassBinder>();
                        if (binder == null) binder = Undo.AddComponent<UIStyleClassBinder>(go);
                        binder.StyleClass = st.StyleClass;
                        binder.ApplyStyleClass();
                    }
                    Debug.Log($"[UI Shelf] Applied style class '{st.DisplayName}' to {selectedGOs.Length} selected object(s).");
                }
                else
                {
                    EditorUtility.DisplayDialog("Apply Style Class", "Select one or more UI GameObjects in the scene hierarchy first, then double-click to apply this style class.", "OK");
                }
            }
        }
    }
}
#endif
