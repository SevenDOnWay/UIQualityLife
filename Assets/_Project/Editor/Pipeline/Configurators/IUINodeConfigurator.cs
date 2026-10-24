#if UNITY_EDITOR
using UnityEngine;

namespace UIQualityLife.Editor.Configurators
{
    /// <summary>
    /// Strategy interface for configuring specific UI node widget types from a UINodeSpec.
    /// Follows the Open/Closed Principle (OCP).
    /// </summary>
    public interface IUINodeConfigurator
    {
        /// <summary>
        /// Determines if this configurator can handle the given node type string or spec flags.
        /// </summary>
        /// <param name="resolvedType">Normalized node type (e.g. "button", "text", "container").</param>
        /// <param name="node">Node specification.</param>
        /// <returns>True if this configurator handles the node.</returns>
        bool CanHandle(string resolvedType, UINodeSpec node);

        /// <summary>
        /// Configures the GameObject with components, layout, and visual parameters matching the spec.
        /// </summary>
        /// <param name="go">Target GameObject.</param>
        /// <param name="node">Node specification.</param>
        void Configure(GameObject go, UINodeSpec node);
    }
}
#endif
