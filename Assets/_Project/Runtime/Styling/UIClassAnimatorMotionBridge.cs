#if UNITY_EDITOR
using System;

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Decoupled motion bridge delegate registry connecting runtime <see cref="UIClassAnimator"/>
    /// to the editor-only animation driver without creating tight coupling or cyclic dependencies.
    /// </summary>
    public static class UIClassAnimatorMotionBridge
    {
        /// <summary>Delegate triggering smooth state preview in the Editor.</summary>
        public static Action<UIClassAnimator, string> PreviewStateHandler;

        /// <summary>Delegate triggering entrance clip simulation in the Editor.</summary>
        public static Action<UIClassAnimator> PlayEntranceHandler;

        /// <summary>Delegate triggering hover clip simulation in the Editor.</summary>
        public static Action<UIClassAnimator> PlayHoverHandler;

        /// <summary>Delegate triggering pressed clip simulation in the Editor.</summary>
        public static Action<UIClassAnimator> PlayPressedHandler;

        /// <summary>Delegate triggering exit clip simulation in the Editor.</summary>
        public static Action<UIClassAnimator> PlayExitHandler;

        /// <summary>Delegate resetting the animator to resting state in the Editor.</summary>
        public static Action<UIClassAnimator> ResetHandler;
    }
}
#endif
