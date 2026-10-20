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

            UIPresetItem preset = DragAndDrop.GetGenericData("UIPresetItem") as UIPresetItem;
            if (preset == null || preset.prefab == null)
                return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                Canvas canvas = UIPresetDatabase.GetOrCreateActiveCanvas();
                Transform parent = canvas.transform;

                // Position calculation on the Canvas plane
                Ray worldRay = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                Plane canvasPlane = new Plane(canvas.transform.forward, canvas.transform.position);

                Vector3 targetWorldPos = canvas.transform.position;
                if (canvasPlane.Raycast(worldRay, out float enter))
                {
                    targetWorldPos = worldRay.GetPoint(enter);
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(preset.prefab, parent);
                instance.name = preset.displayName;
                instance.transform.position = targetWorldPos;

                var rt = instance.GetComponent<RectTransform>();
                if (rt != null)
                {
                    // Snap Z to 0 so it stays flush with the canvas
                    var pos = rt.anchoredPosition3D;
                    pos.z = 0f;
                    rt.anchoredPosition3D = pos;
                }

                Undo.RegisterCreatedObjectUndo(instance, $"Drop {preset.displayName}");
                Selection.activeGameObject = instance;

                evt.Use();
            }
        }
    }
}
#endif
