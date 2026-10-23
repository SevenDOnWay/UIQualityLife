using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TMPro;

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Category types for design tokens stored in stylesheets.
    /// </summary>
    public enum TokenType
    {
        /// <summary>Color token (RGB/RGBA).</summary>
        Color = 0,
        /// <summary>Dimension token (radii, border widths, spacing, paddings).</summary>
        Dimension = 1,
        /// <summary>Typography token (font, size, style, text color).</summary>
        Typography = 2,
        /// <summary>Drop shadow token (offset, blur, color).</summary>
        Shadow = 3
    }

    /// <summary>
    /// Design token bundling typography specifications (Font, Size, Style, Color).
    /// </summary>
    [Serializable]
    public class TypographyToken
    {
        /// <summary>Unique token identifier.</summary>
        public string id;
        /// <summary>Associated TextMeshPro font asset.</summary>
        public TMP_FontAsset fontAsset;
        /// <summary>Font size in points/pixels.</summary>
        public float fontSize = 16f;
        /// <summary>Font styles (Bold, Italic, Underline, etc.).</summary>
        public FontStyles fontStyle = FontStyles.Normal;
        /// <summary>Default font text color.</summary>
        public Color textColor = Color.white;
        /// <summary>Documentation or design usage notes.</summary>
        public string description;

        /// <summary>Default constructor.</summary>
        public TypographyToken() { }

        /// <summary>
        /// Creates a typography token with specified parameters.
        /// </summary>
        public TypographyToken(string id, float fontSize, Color textColor, string description = "")
        {
            this.id = id;
            this.fontSize = fontSize;
            this.textColor = textColor;
            this.description = description;
        }
    }

    /// <summary>
    /// Design token bundling drop shadow and glow properties.
    /// </summary>
    [Serializable]
    public class ShadowToken
    {
        /// <summary>Unique shadow token identifier.</summary>
        public string id;
        /// <summary>Shadow color and opacity.</summary>
        public Color color = new Color(0f, 0f, 0f, 0.4f);
        /// <summary>Shadow directional offset in pixels.</summary>
        public Vector2 offset = new Vector2(0f, -4f);
        /// <summary>Shadow blur / softness radius in pixels.</summary>
        public float softness = 8f;
        /// <summary>Documentation or design usage notes.</summary>
        public string description;

        /// <summary>Default constructor.</summary>
        public ShadowToken() { }

        /// <summary>
        /// Creates a shadow token with specified parameters.
        /// </summary>
        public ShadowToken(string id, Color color, Vector2 offset, float softness, string description = "")
        {
            this.id = id;
            this.color = color;
            this.offset = offset;
            this.softness = softness;
            this.description = description;
        }
    }

    /// <summary>
    /// Utility class for parsing and resolving token values and aliases (e.g. "{colors.brand.primary}").
    /// </summary>
    public static class TokenResolverUtils
    {
        /// <summary>
        /// Checks whether a token value string is an alias reference (wrapped in curly braces, e.g. "{colors.primary}").
        /// </summary>
        /// <param name="value">String to test.</param>
        /// <returns>True if the string is formatted as an alias; otherwise false.</returns>
        public static bool IsAlias(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            string trimmed = value.Trim();
            return trimmed.StartsWith("{") && trimmed.EndsWith("}");
        }

        /// <summary>
        /// Strips surrounding curly braces from an alias string, returning the raw target identifier.
        /// </summary>
        /// <param name="value">Alias string (e.g. "{radii.md}").</param>
        /// <returns>Unwrapped token key (e.g. "radii.md").</returns>
        public static string ExtractAliasTarget(string value)
        {
            if (!IsAlias(value)) return value;
            string trimmed = value.Trim();
            return trimmed.Substring(1, trimmed.Length - 2).Trim();
        }

        /// <summary>
        /// Parses HTML hex colors (#RGB, #RRGGBB, #RRGGBBAA) or CSS rgb()/rgba() syntax into a Unity Color.
        /// </summary>
        /// <param name="input">Color string to parse.</param>
        /// <param name="color">Output parsed Color.</param>
        /// <returns>True if parsing succeeded; otherwise false.</returns>
        public static bool TryParseHexOrRgba(string input, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrWhiteSpace(input)) return false;
            string s = input.Trim();

            // Support #RRGGBB, #RRGGBBAA, #RGB
            if (s.StartsWith("#"))
            {
                return ColorUtility.TryParseHtmlString(s, out color);
            }

            // Support rgba(r, g, b, a) or rgb(r, g, b)
            if (s.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            {
                int start = s.IndexOf('(');
                int end = s.IndexOf(')');
                if (start >= 0 && end > start)
                {
                    string inner = s.Substring(start + 1, end - start - 1);
                    string[] parts = inner.Split(',');
                    if (parts.Length >= 3)
                    {
                        if (float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float r) &&
                            float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float g) &&
                            float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float b))
                        {
                            float a = 1f;
                            if (parts.Length >= 4)
                            {
                                float.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out a);
                            }

                            // If RGB values are in 0-255 range
                            if (r > 1f || g > 1f || b > 1f)
                            {
                                r /= 255f;
                                g /= 255f;
                                b /= 255f;
                            }

                            color = new Color(r, g, b, a);
                            return true;
                        }
                    }
                }
            }

            return ColorUtility.TryParseHtmlString(s, out color);
        }

        /// <summary>
        /// Parses CSS dimension units (px, rem, pt, or raw float) into numeric pixel values.
        /// Assumes standard web typography base of 1rem = 16px.
        /// </summary>
        /// <param name="input">Dimension string (e.g. "24px", "1.5rem", "18").</param>
        /// <param name="value">Output pixel float value.</param>
        /// <returns>True if parsing succeeded; otherwise false.</returns>
        public static bool TryParseDimension(string input, out float value)
        {
            value = 0f;
            if (string.IsNullOrWhiteSpace(input)) return false;
            string s = input.Trim().ToLowerInvariant();

            // Strip units: px, rem, pt
            if (s.EndsWith("px")) s = s.Substring(0, s.Length - 2).Trim();
            else if (s.EndsWith("rem"))
            {
                s = s.Substring(0, s.Length - 3).Trim();
                if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float rem))
                {
                    value = rem * 16f; // Standard 1rem = 16px
                    return true;
                }
            }
            else if (s.EndsWith("pt")) s = s.Substring(0, s.Length - 2).Trim();

            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
