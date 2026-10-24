#if UNITY_EDITOR
using System;
using UnityEngine;
using UIQualityLife.Styling;
using UIQualityLife.Editor.Styling.Builders;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Facade builder for Theme Palettes, Animation Classes, and Style Classes.
    /// Delegates specific asset construction tasks to dedicated single-responsibility builders:
    /// <see cref="UIThemeBuilder"/>, <see cref="UIAnimationClassBuilder"/>, and <see cref="UIStyleClassBuilder"/>.
    /// Preserves complete backward compatibility for external CLI and HTTP bridge callers.
    /// </summary>
    public static class UIStylingBuilder
    {
        /// <summary>
        /// Builds a <see cref="UIThemePalette"/> asset and 8x8 swatch thumbnail from a serialized ThemeBuildSpec JSON string.
        /// </summary>
        /// <param name="json">JSON specification string.</param>
        /// <returns>BuildResult detailing status and created asset paths.</returns>
        public static BuildResult BuildThemeFromJson(string json)
        {
            var res = UIThemeBuilder.BuildThemeFromJson(json);
            return ToBuildResult(res);
        }

        /// <summary>
        /// Creates or updates a <see cref="UIThemePalette"/> asset and generates its auto-framed 8x8 swatch thumbnail.
        /// </summary>
        /// <param name="spec">Specification containing color and float tokens.</param>
        /// <returns>BuildResult detailing status and created asset paths.</returns>
        public static BuildResult BuildTheme(ThemeBuildSpec spec)
        {
            var res = UIThemeBuilder.BuildTheme(spec);
            return ToBuildResult(res);
        }

        /// <summary>
        /// Builds a <see cref="UIAnimationClass"/> asset from serialized AnimationBuildSpec JSON.
        /// </summary>
        /// <param name="json">JSON specification string.</param>
        /// <returns>BuildResult detailing status and created asset paths.</returns>
        public static BuildResult BuildAnimationFromJson(string json)
        {
            var res = UIAnimationClassBuilder.BuildAnimationFromJson(json);
            return ToBuildResult(res);
        }

        /// <summary>
        /// Creates or updates a <see cref="UIAnimationClass"/> asset and generates its waveform thumbnail.
        /// </summary>
        /// <param name="spec">Specification containing visual states and keyframe loops.</param>
        /// <returns>BuildResult detailing status and created asset paths.</returns>
        public static BuildResult BuildAnimation(AnimationBuildSpec spec)
        {
            var res = UIAnimationClassBuilder.BuildAnimation(spec);
            return ToBuildResult(res);
        }

        /// <summary>
        /// Builds a compound <see cref="UIStyleClass"/> asset from serialized StyleBuildSpec JSON.
        /// </summary>
        /// <param name="json">JSON specification string.</param>
        /// <returns>BuildResult detailing status and created asset paths.</returns>
        public static BuildResult BuildStyleFromJson(string json)
        {
            var res = UIStyleClassBuilder.BuildStyleFromJson(json);
            return ToBuildResult(res);
        }

        /// <summary>
        /// Creates or updates a compound <see cref="UIStyleClass"/> asset bundling theme tokens, corner radii, and animation profiles.
        /// </summary>
        /// <param name="spec">Specification details.</param>
        /// <returns>BuildResult detailing status and created asset paths.</returns>
        public static BuildResult BuildStyle(StyleBuildSpec spec)
        {
            var res = UIStyleClassBuilder.BuildStyle(spec);
            return ToBuildResult(res);
        }

        /// <summary>
        /// Parses an easing identifier string into a corresponding <see cref="UIEaseType"/>.
        /// </summary>
        /// <param name="ease">Easing name (e.g. "linear", "easeInOut", "outQuad", "bounce").</param>
        /// <returns>Parsed UIEaseType enum value.</returns>
        public static UIEaseType ParseEaseType(string ease)
        {
            return UIAnimationClassBuilder.ParseEaseType(ease);
        }

        private static BuildResult ToBuildResult(StylingBuildResult res)
        {
            if (res == null) return null;
            return new BuildResult
            {
                success = res.success,
                name = res.name,
                assetPath = res.assetPath,
                thumbPath = res.thumbPath,
                error = res.error
            };
        }
    }
}
#endif
