#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Encapsulates interaction behavior (hover highlight, drag-and-drop to Scene/Hierarchy,
    /// double-click instant spawn, and contextual menu) for any UI component card.
    /// Follows Single Responsibility Principle (SRP).
    /// </summary>
    public class PresetItemManipulator : PointerManipulator
    {
        private readonly IUIComponentItem preset;
        private readonly Action<IUIComponentItem> onDoubleClicked;
        private Vector2 dragStartPos;
        private bool isPointerDown;
        private Color originalBg;

        public PresetItemManipulator(IUIComponentItem preset, Action<IUIComponentItem> onDoubleClicked = null)
        {
            this.preset = preset;
            this.onDoubleClicked = onDoubleClicked;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            originalBg = target.style.backgroundColor.value;

            target.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            target.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);

            // Context menu
            target.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseEnterEvent>(OnMouseEnter);
            target.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        }

        private void OnMouseEnter(MouseEnterEvent evt)
        {
            target.style.backgroundColor = UIElementStyleUtils.CardHoverBg;
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            target.style.backgroundColor = originalBg;
            isPointerDown = false;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 0) // Left click
            {
                dragStartPos = evt.position;
                isPointerDown = true;
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (isPointerDown && Vector2.Distance(dragStartPos, evt.position) > 4f)
            {
                isPointerDown = false;
                StartDrag();
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            isPointerDown = false;
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.clickCount == 2 && evt.button == 0)
            {
                onDoubleClicked?.Invoke(preset);
            }
        }

        private void StartDrag()
        {
            if (preset == null)
                return;

            UnityEngine.Object dragObj = null;
            if (preset.Prefab != null) dragObj = preset.Prefab;
            else if (preset is UIQualityLife.Styling.Editor.UIThemeShelfItem th) dragObj = th.Palette;
            else if (preset is UIQualityLife.Styling.Editor.UIAnimationShelfItem an) dragObj = an.AnimClass;
            else if (preset is UIQualityLife.Styling.Editor.UIStyleClassShelfItem st) dragObj = st.StyleClass;
            else if (preset is UnityEngine.Object uo) dragObj = uo;

            if (dragObj == null) return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new UnityEngine.Object[] { dragObj };
            DragAndDrop.SetGenericData("IUIComponentItem", preset);
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            DragAndDrop.StartDrag(preset.DisplayName);
        }

        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (preset is UIPresetItem)
            {
                evt.menu.AppendAction("Spawn in Canvas", action => onDoubleClicked?.Invoke(preset));
            }
            else if (preset is UIQualityLife.Styling.Editor.UIThemeShelfItem th)
            {
                evt.menu.AppendAction("Set as Active Project Theme", action =>
                {
                    UIQualityLife.Styling.UIThemeManager.ActiveTheme = th.Palette;
                    Debug.Log($"[UI Shelf] Active theme set to '{th.DisplayName}'.");
                });
                evt.menu.AppendAction("Apply Theme to Selected UI", action => onDoubleClicked?.Invoke(preset));
                evt.menu.AppendAction("Open in CSS Studio", action => UIQualityLife.Styling.Editor.UIThemeStudioWindow.Open());
            }
            else if (preset is UIQualityLife.Styling.Editor.UIAnimationShelfItem)
            {
                evt.menu.AppendAction("Apply Animation to Selected UI", action => onDoubleClicked?.Invoke(preset));
                evt.menu.AppendAction("Open in CSS Studio", action => UIQualityLife.Styling.Editor.UIThemeStudioWindow.Open());
            }
            else if (preset is UIQualityLife.Styling.Editor.UIStyleClassShelfItem)
            {
                evt.menu.AppendAction("Apply Style Class to Selected UI", action => onDoubleClicked?.Invoke(preset));
                evt.menu.AppendAction("Open in CSS Studio", action => UIQualityLife.Styling.Editor.UIThemeStudioWindow.Open());
            }

            if (preset.Prefab != null)
            {
                evt.menu.AppendAction("Select Prefab in Project", action =>
                {
                    EditorGUIUtility.PingObject(preset.Prefab);
                    Selection.activeObject = preset.Prefab;
                });
            }

            UnityEngine.Object targetAsset = null;
            if (preset is UnityEngine.Object uo) targetAsset = uo;
            else if (preset is UIQualityLife.Styling.Editor.UIThemeShelfItem thItem) targetAsset = thItem.Palette;
            else if (preset is UIQualityLife.Styling.Editor.UIAnimationShelfItem anItem) targetAsset = anItem.AnimClass;
            else if (preset is UIQualityLife.Styling.Editor.UIStyleClassShelfItem stItem) targetAsset = stItem.StyleClass;

            if (targetAsset != null)
            {
                evt.menu.AppendAction("Select Asset in Project", action =>
                {
                    EditorGUIUtility.PingObject(targetAsset);
                    Selection.activeObject = targetAsset;
                });
            }
        }
    }
}
#endif
