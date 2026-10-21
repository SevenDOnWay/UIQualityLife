#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UIQualityLife.Styling.Editor
{
    /// <summary>
    /// Procedurally bakes a symmetrical, evenly spaced square color swatch thumbnail
    /// for any UIThemePalette asset.
    /// Shows all colors in an even square grid, clamped to a maximum of 64 colors (8x8 grid).
    /// </summary>
    public static class UIThemeThumbnailBaker
    {
        private static readonly Color DefaultDarkBg = new Color(0.118f, 0.125f, 0.145f, 1f); // #1E2025

        public static Texture2D BakePaletteThumbnail(UIThemePalette palette, int size = 256)
        {
            if (palette == null) return null;

            string assetPath = AssetDatabase.GetAssetPath(palette);
            string folder = !string.IsNullOrEmpty(assetPath) ? Path.GetDirectoryName(assetPath).Replace('\\', '/') : "Assets/_Project/Styling";
            string thumbPath = $"{folder}/{palette.name}_thumb.png";

            // Extract all colors from the palette up to 64 maximum (8x8 grid limit)
            var colorsList = new List<Color>();
            if (palette.Colors != null)
            {
                for (int i = 0; i < palette.Colors.Count && colorsList.Count < 64; i++)
                {
                    colorsList.Add(palette.Colors[i].color);
                }
            }

            // Fallback if empty
            if (colorsList.Count == 0)
            {
                colorsList.Add(new Color(0.231f, 0.510f, 0.965f, 1f));
                colorsList.Add(new Color(0.549f, 0.337f, 0.965f, 1f));
                colorsList.Add(new Color(0.961f, 0.620f, 0.043f, 1f));
                colorsList.Add(new Color(0.133f, 0.773f, 0.369f, 1f));
            }

            int count = colorsList.Count;

            // Determine grid columns and rows (up to 8x8)
            int cols = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(count)), 1, 8);
            int rows = Mathf.Clamp(Mathf.CeilToInt(count / (float)cols), 1, 8);

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            // Fill background with standard dark neutral (#1E2025)
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = DefaultDarkBg;
            }

            int maxDim = Mathf.Max(cols, rows);
            int gap = (maxDim <= 2) ? 14 : (maxDim <= 4) ? 8 : (maxDim <= 6) ? 5 : 3;
            int availableSize = size - 44; // ~22px base padding on each side
            int tileSize = (availableSize - (maxDim - 1) * gap) / maxDim;

            int totalGridW = cols * tileSize + (cols - 1) * gap;
            int totalGridH = rows * tileSize + (rows - 1) * gap;

            int globalStartX = (size - totalGridW) / 2;
            int globalStartY = (size - totalGridH) / 2;
            float cornerRadius = Mathf.Max(2f, tileSize * 0.14f);

            int colorIdx = 0;
            for (int r = 0; r < rows; r++)
            {
                int remainingColors = count - colorIdx;
                int itemsInRow = Mathf.Min(cols, remainingColors);
                if (itemsInRow <= 0) break;

                // Center row horizontally if it has fewer items than cols
                int rowWidth = itemsInRow * tileSize + (itemsInRow - 1) * gap;
                int rowStartX = globalStartX + (totalGridW - rowWidth) / 2;

                // Texture coordinates: r=0 is top row, so it gets higher Y
                int yMin = globalStartY + (rows - 1 - r) * (tileSize + gap);
                int yMax = yMin + tileSize;

                for (int c = 0; c < itemsInRow; c++)
                {
                    Color tileColor = colorsList[colorIdx++];
                    int xMin = rowStartX + c * (tileSize + gap);
                    int xMax = xMin + tileSize;

                    for (int y = yMin; y < yMax; y++)
                    {
                        for (int x = xMin; x < xMax; x++)
                        {
                            // Rounded corner distance calculation
                            float dx = 0;
                            if (x < xMin + cornerRadius) dx = (xMin + cornerRadius) - x;
                            else if (x > xMax - 1 - cornerRadius) dx = x - (xMax - 1 - cornerRadius);

                            float dy = 0;
                            if (y < yMin + cornerRadius) dy = (yMin + cornerRadius) - y;
                            else if (y > yMax - 1 - cornerRadius) dy = y - (yMax - 1 - cornerRadius);

                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            if (dist <= cornerRadius)
                            {
                                float alpha = Mathf.Clamp01(cornerRadius - dist + 0.5f);
                                pixels[y * size + x] = Color.Lerp(DefaultDarkBg, tileColor, alpha);
                            }
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            File.WriteAllBytes(thumbPath, png);
            AssetDatabase.ImportAsset(thumbPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(thumbPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.GUI;
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(thumbPath);
        }
    }
}
#endif
