using System;
using UnityEngine;

namespace UIQualityLife.Runtime.Demo.Studio
{
    /// <summary>
    /// Central static selection bus for the 3-panel UI Studio.
    /// Coordinates between the Canvas Viewport and the Left Inspector.
    /// </summary>
    public static class UIStudioSelection
    {
        private static GameObject s_Current;

        public static event Action<GameObject> OnSelectionChanged;

        public static GameObject Current => s_Current;

        public static void Select(GameObject target)
        {
            if (s_Current != target)
            {
                s_Current = target;
                OnSelectionChanged?.Invoke(s_Current);
            }
        }

        public static void Clear()
        {
            Select(null);
        }
    }
}
