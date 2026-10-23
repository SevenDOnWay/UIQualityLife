#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Service responsible for instantiating UI component presets into the active Scene.
    /// Follows Single Responsibility Principle (SRP).
    /// </summary>
    public static class SceneSpawner
    {
        /// <summary>
        /// Instantiates a UI component preset into the active scene Canvas with automatic parenting, positioning, and Undo registration.
        /// </summary>
        /// <param name="preset">The preset item containing the prefab to instantiate.</param>
        /// <param name="customParent">Optional custom parent transform. If null, active selection or default Canvas is used.</param>
        /// <param name="worldPosition">Optional world position for drag-and-drop placement.</param>
        /// <returns>The newly created GameObject instance, or null if instantiation failed.</returns>
        public static GameObject SpawnPreset(IUIComponentItem preset, Transform customParent = null, Vector3? worldPosition = null)
        {
            if (preset == null || preset.Prefab == null)
                return null;

            Transform parent = customParent;
            if (parent == null)
            {
                // Ensure we are in the main scene stage when spawning into the active scene
                if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
                {
                    UnityEditor.SceneManagement.StageUtility.GoToMainStage();
                }

                if (Selection.activeTransform != null && Selection.activeTransform.GetComponent<RectTransform>() != null)
                {
                    // Check if Selection.activeTransform is an instance of the preset itself to prevent cyclic nesting
                    var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(Selection.activeGameObject);
                    if (prefabRoot != null && PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot) == preset.Prefab)
                    {
                        Canvas canvas = CanvasService.GetOrCreateActiveCanvas();
                        parent = canvas.transform;
                    }
                    else
                    {
                        parent = Selection.activeTransform;
                    }
                }
                else
                {
                    Canvas canvas = CanvasService.GetOrCreateActiveCanvas();
                    parent = canvas.transform;
                }
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(preset.Prefab, parent);
            instance.name = preset.DisplayName;

            if (worldPosition.HasValue)
            {
                instance.transform.position = worldPosition.Value;
                var rt = instance.GetComponent<RectTransform>();
                if (rt != null)
                {
                    var pos = rt.anchoredPosition3D;
                    pos.z = 0f;
                    rt.anchoredPosition3D = pos;
                }
            }
            else
            {
                var rt = instance.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = Vector2.zero;
                }
            }

            Undo.RegisterCreatedObjectUndo(instance, $"Spawn {preset.DisplayName}");
            Selection.activeGameObject = instance;
            return instance;
        }
    }
}
#endif
