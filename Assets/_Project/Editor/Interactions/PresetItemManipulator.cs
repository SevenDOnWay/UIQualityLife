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
            if (preset == null || preset.Prefab == null)
                return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new UnityEngine.Object[] { preset.Prefab };
            DragAndDrop.SetGenericData("UIPresetItem", preset);
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            DragAndDrop.StartDrag(preset.DisplayName);
        }

        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Spawn in Canvas", action =>
            {
                onDoubleClicked?.Invoke(preset);
            });

            if (preset.Prefab != null)
            {
                evt.menu.AppendAction("Select Prefab in Project", action =>
                {
                    EditorGUIUtility.PingObject(preset.Prefab);
                    Selection.activeObject = preset.Prefab;
                });
            }

            if (preset is UnityEngine.Object uo)
            {
                evt.menu.AppendAction("Select Preset Asset", action =>
                {
                    EditorGUIUtility.PingObject(uo);
                    Selection.activeObject = uo;
                });
            }
        }
    }
}
#endif
