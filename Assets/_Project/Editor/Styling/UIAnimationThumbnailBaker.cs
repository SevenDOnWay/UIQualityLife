#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UIQualityLife.Styling.Editor
{
    /// <summary>
    /// Generates and provides default fallback thumbnails for UI Animation Classes.
    /// </summary>
    public static class UIAnimationThumbnailBaker
    {
        private const string DefaultAnimThumbPath = "Assets/_Project/Styling/Thumbnails/AnimationDefault_thumb.png";
        private static readonly Color DefaultDarkBg = new Color(0.118f, 0.125f, 0.145f, 1f); // #1E2025
        private static readonly Color AccentColor = new Color(0.55f, 0.34f, 0.96f, 1f); // Modern Purple

        /// <summary>
        /// Generates or retrieves the default smooth sine wave thumbnail representing CSS animation and easing.
        /// </summary>
        /// <param name="size">Square texture dimension in pixels.</param>
        /// <returns>Default animation Texture2D asset.</returns>
        public static Texture2D GetOrCreateDefaultAnimationThumbnail(int size = 256)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultAnimThumbPath);
            if (existing != null) return existing;

            string folder = Path.GetDirectoryName(DefaultAnimThumbPath).Replace('\\', '/');
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = DefaultDarkBg;
            }

            // Draw a smooth Sine Wave / Spring Curve to visually symbolize CSS Motion & Transition
            float centerY = size * 0.5f;
            float amplitude = size * 0.22f;
            float waveStart = size * 0.2f;
            float waveEnd = size * 0.8f;
            float thickness = 7f;

            for (int x = (int)waveStart; x <= (int)waveEnd; x++)
            {
                float t = (x - waveStart) / (waveEnd - waveStart);
                float yVal = centerY + Mathf.Sin(t * Mathf.PI * 2f) * amplitude;

                int minY = Mathf.Clamp((int)(yVal - thickness), 0, size - 1);
                int maxY = Mathf.Clamp((int)(yVal + thickness), 0, size - 1);

                for (int y = minY; y <= maxY; y++)
                {
                    float dist = Mathf.Abs(y - yVal);
                    float alpha = Mathf.Clamp01(1f - (dist / thickness));
                    Color c = Color.Lerp(DefaultDarkBg, AccentColor, alpha);
                    pixels[y * size + x] = c;
                }
            }

            // Draw a bounce dot at the wave peak
            int dotCenterX = (int)(waveStart + (waveEnd - waveStart) * 0.25f);
            int dotCenterY = (int)(centerY + amplitude);
            int dotRadius = 14;

            for (int y = dotCenterY - dotRadius; y <= dotCenterY + dotRadius; y++)
            {
                for (int x = dotCenterX - dotRadius; x <= dotCenterX + dotRadius; x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(dotCenterX, dotCenterY));
                        if (dist <= dotRadius)
                        {
                            pixels[y * size + x] = Color.white;
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            File.WriteAllBytes(DefaultAnimThumbPath, png);
            AssetDatabase.ImportAsset(DefaultAnimThumbPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(DefaultAnimThumbPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.GUI;
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultAnimThumbPath);
        }
    }
}
#endif
