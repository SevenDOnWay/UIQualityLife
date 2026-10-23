#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Service responsible for finding, configuring, or creating active Canvases and EventSystems.
    /// Follows Single Responsibility Principle (SRP).
    /// </summary>
    public static class CanvasService
    {
        /// <summary>
        /// Finds the primary active Canvas in the scene or creates a standard 1080p ScreenSpaceOverlay Canvas if none exists.
        /// </summary>
        /// <returns>Active Canvas in the scene.</returns>
        public static Canvas GetOrCreateActiveCanvas()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasGO.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");

                EnsureEventSystem();
            }
            return canvas;
        }

        /// <summary>
        /// Checks for an active <see cref="EventSystem"/> in the scene, creating one with the appropriate Input System module if missing.
        /// </summary>
        public static void EnsureEventSystem()
        {
            var es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();

                var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null)
                {
                    esGO.AddComponent(inputModuleType);
                }
                else
                {
                    esGO.AddComponent<StandaloneInputModule>();
                }

                Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
            }
        }
    }
}
#endif
