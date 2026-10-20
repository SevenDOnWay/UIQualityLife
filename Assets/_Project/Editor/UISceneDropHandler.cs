#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UIQualityLife.Editor
{
    [InitializeOnLoad]
    public static class UISceneDropHandler
    {
        static UISceneDropHandler()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            Event evt = Event.current;
            if (evt == null)
                return;

            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
                return;

            IUIComponentItem preset = DragAndDrop.GetGenericData("UIPresetItem") as IUIComponentItem;
            if (preset == null || preset.Prefab == null)
                return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                Canvas canvas = CanvasService.GetOrCreateActiveCanvas();
                Ray worldRay = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                Plane canvasPlane = new Plane(canvas.transform.forward, canvas.transform.position);

                Vector3 targetWorldPos = canvas.transform.position;
                if (canvasPlane.Raycast(worldRay, out float enter))
                {
                    targetWorldPos = worldRay.GetPoint(enter);
                }

                SceneSpawner.SpawnPreset(preset, canvas.transform, targetWorldPos);
                evt.Use();
            }
        }
    }
}
#endif
