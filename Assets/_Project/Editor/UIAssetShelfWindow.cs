#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIQualityLife.Editor
{
    public class UIAssetShelfWindow : EditorWindow
    {
        private const string PrefsViewModeKey = "UIQualityLife_Shelf_ViewMode";
        private const string PrefsCategoryKey = "UIQualityLife_Shelf_Category";

        private List<UIPresetItem> allPresets;
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

        // Drag tracking
        private Vector2 dragStartPos;
        private bool isPointerDown;
        private UIPresetItem activeDragPreset;

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
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

            // --- 1. TOP TOOLBAR (Minimal, Native) ---
            var topToolbar = new Toolbar();
            topToolbar.style.height = 24;
            topToolbar.style.paddingLeft = 4;
            topToolbar.style.paddingRight = 4;

            // Search Field
            searchField = new ToolbarSearchField();
            searchField.style.flexGrow = 1;
            searchField.style.maxWidth = 200;
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
            categoryMenu.style.minWidth = 100;
            topToolbar.Add(categoryMenu);

            // View Mode Dropdown
            viewModeMenu = new ToolbarMenu();
            viewModeMenu.text = GetViewModeLabel(currentViewMode);
            viewModeMenu.style.minWidth = 90;
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
            topToolbar.Add(saveSelectionBtn);

            rootVisualElement.Add(topToolbar);

            // --- 2. CATEGORY SUGGESTION STRIP (Search & Suggest) ---
            suggestionScrollView = new ScrollView(ScrollViewMode.Horizontal);
            suggestionScrollView.style.height = 26;
            suggestionScrollView.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            suggestionScrollView.style.borderBottomWidth = 1;
            suggestionScrollView.style.borderBottomColor = new Color(0.23f, 0.23f, 0.23f);
            suggestionScrollView.style.paddingLeft = 6;
            suggestionScrollView.style.paddingRight = 6;
            suggestionScrollView.style.paddingTop = 3;
            suggestionScrollView.style.paddingBottom = 3;

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
            statusCountLabel.style.color = new Color(0.65f, 0.65f, 0.65f);
            bottomToolbar.Add(statusCountLabel);

            var sliderContainer = new VisualElement();
            sliderContainer.style.flexDirection = FlexDirection.Row;
            sliderContainer.style.alignItems = Align.Center;

            var sliderLabel = new Label("View:");
            sliderLabel.style.fontSize = 10;
            sliderLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
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
            allPresets = UIPresetDatabase.GetPresets();
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
                // When searching: Show matching category suggestions (Category as Search Tag Suggestion)
                var matches = UIPresetDatabase.GetMatchingCategories(query);

                var tip = new Label("Tags:");
                tip.style.fontSize = 9;
                tip.style.color = new Color(0.55f, 0.55f, 0.55f);
                tip.style.marginRight = 4;
                suggestionBar.Add(tip);

                if (matches.Count == 0)
                {
                    var none = new Label("(No category tags matched)");
                    none.style.fontSize = 9;
                    none.style.color = new Color(0.45f, 0.45f, 0.45f);
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
                // When search is empty: Quick Category Filter Pills
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
            chip.style.height = 18;
            chip.style.fontSize = 9;
            chip.style.paddingLeft = 7;
            chip.style.paddingRight = 7;
            chip.style.marginRight = 4;
            chip.style.borderTopLeftRadius = 9;
            chip.style.borderTopRightRadius = 9;
            chip.style.borderBottomLeftRadius = 9;
            chip.style.borderBottomRightRadius = 9;

            if (isActive)
            {
                chip.style.backgroundColor = new Color(0.18f, 0.45f, 0.85f);
                chip.style.color = Color.white;
                chip.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                chip.style.backgroundColor = new Color(0.24f, 0.24f, 0.24f);
                chip.style.color = new Color(0.75f, 0.75f, 0.75f);
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
                emptyLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
                emptyLabel.style.marginTop = 20;
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                emptyLabel.style.alignSelf = Align.Center;
                itemsContainer.Add(emptyLabel);
                return;
            }

            int visibleCount = 0;

            foreach (var preset in allPresets)
            {
                // Multi-category filtering
                if (!preset.HasCategory(selectedCategory))
                    continue;

                // Search query matching (matches Name, Description, and ALL Categories as tags!)
                if (!preset.MatchesSearch(currentSearchQuery))
                    continue;

                VisualElement itemElem = RenderPresetItem(preset, currentViewMode);
                AttachCommonInteractions(itemElem, preset);
                itemsContainer.Add(itemElem);
                visibleCount++;
            }

            statusCountLabel.text = $"{visibleCount} {(visibleCount == 1 ? "preset" : "presets")}";
        }

        // =========================================================================
        // VIEW MODE RENDERERS
        // =========================================================================

        private VisualElement RenderPresetItem(UIPresetItem preset, UIPresetViewMode mode)
        {
            switch (mode)
            {
                case UIPresetViewMode.List:
                    return RenderListRow(preset);
                case UIPresetViewMode.Content:
                    return RenderContentRow(preset);
                case UIPresetViewMode.SmallIcon:
                    return RenderSmallIconTile(preset);
                case UIPresetViewMode.LargeIcon:
                    return RenderLargeIconTile(preset);
                default:
                    return RenderContentRow(preset);
            }
        }

        // 1. LIST VIEW
        private VisualElement RenderListRow(UIPresetItem preset)
        {
            var row = new VisualElement();
            row.style.height = 24;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 6;
            row.style.paddingRight = 6;
            row.style.marginBottom = 1;
            SetBorderRadius(row, 3);

            var dot = new VisualElement();
            dot.style.width = 16;
            dot.style.height = 16;
            dot.style.backgroundColor = new Color(0.12f, 0.125f, 0.145f);
            SetBorderRadius(dot, 3);
            dot.style.marginRight = 8;
            Texture2D listTex = preset.GetPreviewTexture();
            if (listTex != null)
            {
                dot.style.backgroundImage = new StyleBackground(listTex);
            }
            else
            {
                dot.style.backgroundColor = preset.previewColor;
            }
            row.Add(dot);

            var label = new Label(preset.displayName);
            label.style.fontSize = 12;
            label.style.color = new Color(0.85f, 0.85f, 0.85f);
            label.style.flexGrow = 1;
            row.Add(label);

            // Render clickable category tags
            if (preset.categories != null)
            {
                foreach (var cat in preset.categories)
                {
                    if (string.IsNullOrWhiteSpace(cat)) continue;
                    var catTag = new Button(() => SelectCategory(cat)) { text = cat };
                    catTag.style.fontSize = 9;
                    catTag.style.height = 16;
                    catTag.style.paddingLeft = 4;
                    catTag.style.paddingRight = 4;
                    catTag.style.marginRight = 4;
                    catTag.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
                    catTag.style.color = new Color(0.6f, 0.6f, 0.6f);
                    SetBorderRadius(catTag, 3);
                    row.Add(catTag);
                }
            }

            var badge = new Label("TMP");
            badge.style.fontSize = 9;
            badge.style.color = new Color(0.4f, 0.75f, 1f);
            row.Add(badge);

            return row;
        }

        // 2. CONTENT VIEW
        private VisualElement RenderContentRow(UIPresetItem preset)
        {
            var card = new VisualElement();
            card.style.height = 44;
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            card.style.paddingLeft = 8;
            card.style.paddingRight = 8;
            card.style.marginBottom = 4;
            card.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            SetBorderRadius(card, 4);
            SetBorder(card, 1, new Color(0.27f, 0.27f, 0.27f));

            var swatch = new VisualElement();
            swatch.style.width = 34;
            swatch.style.height = 34;
            swatch.style.backgroundColor = new Color(0.12f, 0.125f, 0.145f);
            SetBorderRadius(swatch, 4);
            SetBorder(swatch, 1, new Color(0.22f, 0.22f, 0.22f));
            swatch.style.marginRight = 10;

            Texture2D previewTex = preset.GetPreviewTexture();
            if (previewTex != null)
            {
                swatch.style.backgroundImage = new StyleBackground(previewTex);
            }
            else
            {
                swatch.style.backgroundColor = preset.previewColor;
            }

            card.Add(swatch);

            var info = new VisualElement();
            info.style.flexGrow = 1;

            var title = new Label(preset.displayName);
            title.style.fontSize = 12;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.9f, 0.9f, 0.9f);
            info.Add(title);

            if (!string.IsNullOrEmpty(preset.description))
            {
                var desc = new Label(preset.description);
                desc.style.fontSize = 10;
                desc.style.color = new Color(0.6f, 0.6f, 0.6f);
                info.Add(desc);
            }
            card.Add(info);

            // Clickable category badges row
            if (preset.categories != null)
            {
                var tagsRow = new VisualElement();
                tagsRow.style.flexDirection = FlexDirection.Row;
                tagsRow.style.alignItems = Align.Center;

                foreach (var cat in preset.categories)
                {
                    if (string.IsNullOrWhiteSpace(cat)) continue;
                    var tag = new Button(() => SelectCategory(cat)) { text = cat };
                    tag.style.fontSize = 9;
                    tag.style.height = 17;
                    tag.style.paddingLeft = 5;
                    tag.style.paddingRight = 5;
                    tag.style.marginRight = 3;
                    tag.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
                    tag.style.color = new Color(0.65f, 0.65f, 0.65f);
                    SetBorderRadius(tag, 3);
                    tagsRow.Add(tag);
                }
                card.Add(tagsRow);
            }

            return card;
        }

        // 3. SMALL ICON VIEW
        private VisualElement RenderSmallIconTile(UIPresetItem preset)
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
            tile.style.backgroundColor = new Color(0.21f, 0.21f, 0.21f);
            SetBorderRadius(tile, 4);
            SetBorder(tile, 1, new Color(0.26f, 0.26f, 0.26f));

            var preview = new VisualElement();
            preview.style.width = 52;
            preview.style.height = 52;
            preview.style.backgroundColor = new Color(0.12f, 0.125f, 0.145f);
            SetBorderRadius(preview, 4);
            SetBorder(preview, 1, new Color(0.22f, 0.22f, 0.22f));
            preview.style.marginBottom = 5;

            Texture2D previewTex = preset.GetPreviewTexture();
            if (previewTex != null)
            {
                preview.style.backgroundImage = new StyleBackground(previewTex);
            }
            else
            {
                preview.style.backgroundColor = preset.previewColor;
            }

            tile.Add(preview);

            var title = new Label(preset.displayName);
            title.style.fontSize = 10;
            title.style.color = new Color(0.85f, 0.85f, 0.85f);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.whiteSpace = WhiteSpace.Normal;
            tile.Add(title);

            return tile;
        }

        // 4. LARGE ICON VIEW
        private VisualElement RenderLargeIconTile(UIPresetItem preset)
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
            tile.style.backgroundColor = new Color(0.21f, 0.21f, 0.21f);
            SetBorderRadius(tile, 6);
            SetBorder(tile, 1, new Color(0.27f, 0.27f, 0.27f));

            var canvas = new VisualElement();
            canvas.style.width = 82;
            canvas.style.height = 82;
            canvas.style.backgroundColor = new Color(0.12f, 0.125f, 0.145f);
            SetBorderRadius(canvas, 4);
            SetBorder(canvas, 1, new Color(0.22f, 0.22f, 0.22f));
            canvas.style.justifyContent = Justify.Center;
            canvas.style.alignItems = Align.Center;
            canvas.style.marginBottom = 6;

            Texture2D previewTex = preset.GetPreviewTexture();
            if (previewTex != null)
            {
                canvas.style.backgroundImage = new StyleBackground(previewTex);
            }
            else
            {
                var miniBtn = new VisualElement();
                miniBtn.style.width = 62;
                miniBtn.style.height = 22;
                miniBtn.style.backgroundColor = preset.previewColor;
                SetBorderRadius(miniBtn, 3);
                miniBtn.style.justifyContent = Justify.Center;
                miniBtn.style.alignItems = Align.Center;

                var miniBtnText = new Label("UI");
                miniBtnText.style.fontSize = 9;
                miniBtnText.style.color = Color.white;
                miniBtnText.style.unityFontStyleAndWeight = FontStyle.Bold;
                miniBtn.Add(miniBtnText);

                canvas.Add(miniBtn);
            }

            tile.Add(canvas);

            var title = new Label(preset.displayName);
            title.style.fontSize = 11;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.9f, 0.9f, 0.9f);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.whiteSpace = WhiteSpace.Normal;
            tile.Add(title);

            // Mini category tag
            var primaryTag = new Label(preset.PrimaryCategory);
            primaryTag.style.fontSize = 8;
            primaryTag.style.color = new Color(0.5f, 0.5f, 0.5f);
            primaryTag.style.marginTop = 2;
            tile.Add(primaryTag);

            return tile;
        }

        // =========================================================================
        // COMMON DRAG & DROP & SPAWN INTERACTIONS
        // =========================================================================

        private void AttachCommonInteractions(VisualElement element, UIPresetItem preset)
        {
            Color defaultBg = element.style.backgroundColor.value;

            // Hover state
            element.RegisterCallback<MouseEnterEvent>(evt =>
            {
                element.style.backgroundColor = new Color(0.28f, 0.28f, 0.28f);
            });

            element.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                element.style.backgroundColor = defaultBg;
                isPointerDown = false;
            });

            // Drag handling
            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0) // Left click
                {
                    dragStartPos = evt.position;
                    isPointerDown = true;
                    activeDragPreset = preset;
                }
            });

            element.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (isPointerDown && activeDragPreset == preset)
                {
                    if (Vector2.Distance(dragStartPos, evt.position) > 4f)
                    {
                        StartDrag(preset);
                        isPointerDown = false;
                        activeDragPreset = null;
                    }
                }
            });

            element.RegisterCallback<PointerUpEvent>(evt =>
            {
                isPointerDown = false;
                activeDragPreset = null;
            });

            // Double Click to instant spawn in Scene
            element.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2 && evt.button == 0)
                {
                    UIPresetDatabase.SpawnPresetInScene(preset);
                }
            });

            // Context Menu (Right Click)
            element.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Spawn in Canvas", action =>
                {
                    UIPresetDatabase.SpawnPresetInScene(preset);
                });

                if (preset.prefab != null)
                {
                    evt.menu.AppendAction("Select Prefab in Project", action =>
                    {
                        EditorGUIUtility.PingObject(preset.prefab);
                        Selection.activeObject = preset.prefab;
                    });
                }

                evt.menu.AppendAction("Select Preset Asset", action =>
                {
                    EditorGUIUtility.PingObject(preset);
                    Selection.activeObject = preset;
                });
            }));
        }

        private void StartDrag(UIPresetItem preset)
        {
            if (preset == null || preset.prefab == null)
                return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new UnityEngine.Object[] { preset.prefab };
            DragAndDrop.SetGenericData("UIPresetItem", preset);
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            DragAndDrop.StartDrag(preset.displayName);
        }

        private static void SetBorder(VisualElement elem, float width, Color color)
        {
            elem.style.borderTopWidth = width;
            elem.style.borderBottomWidth = width;
            elem.style.borderLeftWidth = width;
            elem.style.borderRightWidth = width;

            elem.style.borderTopColor = color;
            elem.style.borderBottomColor = color;
            elem.style.borderLeftColor = color;
            elem.style.borderRightColor = color;
        }

        private static void SetBorderRadius(VisualElement elem, float radius)
        {
            elem.style.borderTopLeftRadius = radius;
            elem.style.borderTopRightRadius = radius;
            elem.style.borderBottomLeftRadius = radius;
            elem.style.borderBottomRightRadius = radius;
        }
    }
}
#endif
