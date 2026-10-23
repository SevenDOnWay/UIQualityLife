#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UIQualityLife.Styling;

namespace UIQualityLife.Editor
{
    public class TokenImportReport
    {
        public int colorsImported;
        public int dimensionsImported;
        public int rulesImported;
        public List<string> warnings = new List<string>();

        public string Summary => $"Imported: {colorsImported} Colors, {dimensionsImported} Dimensions/Radii, {rulesImported} Style Rules. Warnings: {warnings.Count}";
    }

    /// <summary>
    /// Parses W3C Design Tokens JSON, Figma Variables, and Tokens Studio exports
    /// and populates UIStyleSheet assets with reactive tokens and CSS class rules.
    /// </summary>
    public static class FigmaTokenImporter
    {
        /// <summary>
        /// Parses W3C Design Tokens JSON, Figma Variables, or Tokens Studio export data and populates the target <see cref="UIStyleSheet"/>.
        /// Automatically triggers reactive UI updates for all bound scene elements.
        /// </summary>
        /// <param name="jsonText">Raw JSON text from Figma or Tokens Studio.</param>
        /// <param name="targetSheet">Target StyleSheet asset to populate.</param>
        /// <returns>Import report with counts of imported tokens and any warnings.</returns>
        public static TokenImportReport ImportTokensJson(string jsonText, UIStyleSheet targetSheet)
        {
            var report = new TokenImportReport();
            if (string.IsNullOrWhiteSpace(jsonText) || targetSheet == null)
            {
                report.warnings.Add("Empty JSON text or missing target StyleSheet.");
                return report;
            }

            try
            {
                var root = JObject.Parse(jsonText);
                TraverseJObject(root, "", targetSheet, report);
                EditorUtility.SetDirty(targetSheet);
                AssetDatabase.SaveAssets();

                // Trigger reactive update across all subscribed scene elements
                targetSheet.NotifyVariableChanged("*");
            }
            catch (Exception ex)
            {
                report.warnings.Add($"JSON Parsing error: {ex.Message}");
            }

            return report;
        }

        private static void TraverseJObject(JObject obj, string currentPath, UIStyleSheet targetSheet, TokenImportReport report)
        {
            foreach (var prop in obj.Properties())
            {
                string key = prop.Name;
                string newPath = string.IsNullOrEmpty(currentPath) ? key : $"{currentPath}.{key}";

                if (prop.Value is JObject childObj)
                {
                    // Check if this child object is a token leaf (has 'value' or '$value')
                    var valToken = childObj["value"] ?? childObj["$value"];
                    var typeToken = childObj["type"] ?? childObj["$type"];

                    if (valToken != null)
                    {
                        ProcessTokenLeaf(newPath, valToken.ToString(), typeToken?.ToString(), childObj["description"]?.ToString(), targetSheet, report);
                    }
                    else if (key.StartsWith(".") || key.StartsWith("#") || currentPath.EndsWith("components", StringComparison.OrdinalIgnoreCase))
                    {
                        // Component-level rule (e.g. .btn-danger, #submit-btn)
                        ProcessComponentRule(key, childObj, targetSheet, report);
                    }
                    else
                    {
                        // Sub-group navigation
                        TraverseJObject(childObj, newPath, targetSheet, report);
                    }
                }
                else if (prop.Value is JValue val)
                {
                    // Direct key-value pair (e.g. "danger": "#ef4444")
                    string strVal = val.ToString();
                    ProcessDirectValue(newPath, strVal, targetSheet, report);
                }
            }
        }

        private static void ProcessTokenLeaf(string tokenPath, string rawValue, string tokenType, string description, UIStyleSheet targetSheet, TokenImportReport report)
        {
            string cleanId = SanitizeTokenId(tokenPath);
            string type = (tokenType ?? "").ToLowerInvariant();

            // 1. Color Token
            if (type.Contains("color") || rawValue.StartsWith("#") || rawValue.StartsWith("rgb") || TokenResolverUtils.TryParseHexOrRgba(rawValue, out _))
            {
                if (TokenResolverUtils.TryParseHexOrRgba(rawValue, out var color))
                {
                    targetSheet.SetColorToken(cleanId, color, description ?? tokenPath);
                    report.colorsImported++;
                    return;
                }
                else if (TokenResolverUtils.IsAlias(rawValue))
                {
                    // Alias color reference: resolve target or set fallback
                    string aliasKey = SanitizeTokenId(TokenResolverUtils.ExtractAliasTarget(rawValue));
                    if (targetSheet.TryGetColor(aliasKey, out var aliasColor))
                    {
                        targetSheet.SetColorToken(cleanId, aliasColor, $"Alias to {aliasKey}");
                        report.colorsImported++;
                    }
                    else
                    {
                        targetSheet.SetColorToken(cleanId, Color.white, $"Unresolved alias to {rawValue}");
                        report.warnings.Add($"Color token '{cleanId}' points to alias '{rawValue}' which is not yet resolved.");
                    }
                    return;
                }
            }

            // 2. Dimension / Radius Token
            if (type.Contains("radius") || type.Contains("dimension") || type.Contains("spacing") || type.Contains("border") ||
                TokenResolverUtils.TryParseDimension(rawValue, out _))
            {
                if (TokenResolverUtils.TryParseDimension(rawValue, out float floatVal))
                {
                    targetSheet.SetFloatToken(cleanId, floatVal, description ?? tokenPath);
                    report.dimensionsImported++;
                    return;
                }
                else if (TokenResolverUtils.IsAlias(rawValue))
                {
                    string aliasKey = SanitizeTokenId(TokenResolverUtils.ExtractAliasTarget(rawValue));
                    if (targetSheet.TryGetFloat(aliasKey, out float aliasVal))
                    {
                        targetSheet.SetFloatToken(cleanId, aliasVal, $"Alias to {aliasKey}");
                        report.dimensionsImported++;
                    }
                    else
                    {
                        targetSheet.SetFloatToken(cleanId, 0f, $"Unresolved alias to {rawValue}");
                        report.warnings.Add($"Dimension token '{cleanId}' points to alias '{rawValue}' which is not yet resolved.");
                    }
                    return;
                }
            }
        }

        private static void ProcessDirectValue(string tokenPath, string rawValue, UIStyleSheet targetSheet, TokenImportReport report)
        {
            string cleanId = SanitizeTokenId(tokenPath);

            if (TokenResolverUtils.TryParseHexOrRgba(rawValue, out var color))
            {
                targetSheet.SetColorToken(cleanId, color, tokenPath);
                report.colorsImported++;
            }
            else if (TokenResolverUtils.TryParseDimension(rawValue, out float dim))
            {
                targetSheet.SetFloatToken(cleanId, dim, tokenPath);
                report.dimensionsImported++;
            }
        }

        private static void ProcessComponentRule(string selector, JObject ruleObj, UIStyleSheet targetSheet, TokenImportReport report)
        {
            string cleanSelector = selector.StartsWith(".") || selector.StartsWith("#") ? selector : "." + selector;
            var rule = new UIStyleRule(cleanSelector, $"Imported rule from Figma: {cleanSelector}");

            foreach (var prop in ruleObj.Properties())
            {
                string pName = prop.Name.ToLowerInvariant();
                string pVal = prop.Value?.ToString() ?? "";
                if (prop.Value is JObject innerObj)
                {
                    var v = innerObj["value"] ?? innerObj["$value"];
                    if (v != null) pVal = v.ToString();
                }

                if (pName.Contains("bg") || pName.Contains("background") || pName.Contains("fill"))
                {
                    rule.overrideBackground = true;
                    rule.backgroundColorToken = SanitizeTokenId(TokenResolverUtils.ExtractAliasTarget(pVal));
                }
                else if (pName.Contains("radius") || pName.Contains("corner"))
                {
                    rule.overrideCornerRadius = true;
                    rule.cornerRadiusToken = SanitizeTokenId(TokenResolverUtils.ExtractAliasTarget(pVal));
                }
                else if (pName.Contains("bordercolor") || pName.Contains("border-color"))
                {
                    rule.overrideBorder = true;
                    rule.borderColorToken = SanitizeTokenId(TokenResolverUtils.ExtractAliasTarget(pVal));
                }
                else if (pName.Contains("borderwidth") || pName.Contains("border-width"))
                {
                    rule.overrideBorder = true;
                    rule.borderWidthToken = SanitizeTokenId(TokenResolverUtils.ExtractAliasTarget(pVal));
                }
                else if (pName.Contains("text") || pName.Contains("fontcolor") || pName.Contains("font-color"))
                {
                    rule.overrideTypography = true;
                    rule.textColorToken = SanitizeTokenId(TokenResolverUtils.ExtractAliasTarget(pVal));
                }
            }

            targetSheet.AddOrUpdateRule(rule);
            report.rulesImported++;
        }

        public static string SanitizeTokenId(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            // Transform "global.colors.danger" -> "colors-danger" or "danger"
            string clean = path.Replace('/', '-').Replace('.', '-').Trim();
            if (clean.StartsWith("global-", StringComparison.OrdinalIgnoreCase))
                clean = clean.Substring(7);
            return clean;
        }
    }
}
#endif
