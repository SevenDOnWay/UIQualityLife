using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TMPro;

namespace UIQualityLife.Styling
{
    public enum TokenType
    {
        Color = 0,
        Dimension = 1,     // Radii, border widths, spacing, paddings
        Typography = 2,    // Font, size, style
        Shadow = 3         // Offset, blur, color
    }

    [Serializable]
    public class TypographyToken
    {
        public string id;
        public TMP_FontAsset fontAsset;
        public float fontSize = 16f;
        public FontStyles fontStyle = FontStyles.Normal;
        public Color textColor = Color.white;
        public string description;

        public TypographyToken() { }
        public TypographyToken(string id, float fontSize, Color textColor, string description = "")
        {
            this.id = id;
            this.fontSize = fontSize;
            this.textColor = textColor;
            this.description = description;
        }
    }

    [Serializable]
    public class ShadowToken
    {
        public string id;
        public Color color = new Color(0f, 0f, 0f, 0.4f);
        public Vector2 offset = new Vector2(0f, -4f);
        public float softness = 8f;
        public string description;

        public ShadowToken() { }
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
        public static bool IsAlias(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            string trimmed = value.Trim();
            return trimmed.StartsWith("{") && trimmed.EndsWith("}");
        }

        public static string ExtractAliasTarget(string value)
        {
            if (!IsAlias(value)) return value;
            string trimmed = value.Trim();
            return trimmed.Substring(1, trimmed.Length - 2).Trim();
        }

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
