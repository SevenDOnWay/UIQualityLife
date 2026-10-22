#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UIQualityLife.Editor
{
    public static class UIThumbnailBaker
    {
        public static string ThumbnailsFolder => $"{UIQualityLifeSettings.PresetsFolder}/Thumbnails";
        private static readonly Color NeutralDarkBg = new Color(0.12f, 0.125f, 0.145f, 1f); // Subtle dark neutral contrast (#1E2025)

        /// <summary>
        /// Renders an off-screen, auto-framed 256x256 snapshot of any UI GameObject or Prefab.
        /// Accurately scales and centers compound UI elements so nothing is clipped on the left, right, top, or bottom.
        /// Saves directly into the component's folder when outputFolder is supplied.
        /// </summary>
        public static Texture2D BakeThumbnail(GameObject targetPrefab, string assetName, int size = 256, string outputFolder = null)
        {
            if (targetPrefab == null)
                return null;

            string cleanName = SanitizeFileName(string.IsNullOrEmpty(assetName) ? targetPrefab.name : assetName);
            string targetDir = !string.IsNullOrEmpty(outputFolder) ? outputFolder.Replace('\\', '/') : ThumbnailsFolder;
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string relativePath = $"{targetDir}/{cleanName}_thumb.png";
            string fullPath = Path.GetFullPath(relativePath);

            // 1. Setup isolated preview sandbox far off-screen
            GameObject sandboxRoot = new GameObject("UIThumbnail_Sandbox") { hideFlags = HideFlags.HideAndDontSave };
            sandboxRoot.transform.position = new Vector3(9999f, 9999f, 0f);

            RenderTexture rtTex = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rtTex.antiAliasing = 8;

            Camera cam = sandboxRoot.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = NeutralDarkBg;
            cam.orthographic = true;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.allowHDR = false;
            cam.allowMSAA = true;
            cam.targetTexture = rtTex;
            cam.aspect = 1.0f; // Fixed square 1:1 aspect ratio

            // 2. Setup ScreenSpaceCamera Canvas tied to the preview camera
            GameObject canvasGO = new GameObject("PreviewCanvas") { hideFlags = HideFlags.HideAndDontSave };
            canvasGO.transform.SetParent(sandboxRoot.transform, false);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            // Intermediate container centered in canvas
            GameObject container = new GameObject("Container", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
            container.transform.SetParent(canvas.transform, false);
            RectTransform containerRt = container.GetComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.pivot = new Vector2(0.5f, 0.5f);
            containerRt.anchoredPosition = Vector2.zero;

            // 3. Instantiate target UI component under the container
            GameObject uiInstance = Object.Instantiate(targetPrefab, container.transform, false);
            uiInstance.hideFlags = HideFlags.HideAndDontSave;

            // 4. Force layout pass and TextMeshPro mesh regeneration
            Canvas.ForceUpdateCanvases();
            var textMeshes = uiInstance.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var tmp in textMeshes)
            {
                tmp.ForceMeshUpdate(true, true);
            }
            Canvas.ForceUpdateCanvases();

            // 5. Compute compound visual bounds relative to container
            Bounds localBounds = CalculateLocalBounds(container.transform, uiInstance);

            // Shift uiInstance so its visual center is at container origin (0, 0)
            uiInstance.transform.localPosition -= localBounds.center;

            // Fit into 256x256 with ~24% comfortable padding (content occupies ~76% of square frame, ~30px padding on all sides)
            float maxDim = Mathf.Max(localBounds.size.x, localBounds.size.y, 10f);
            float targetContentSize = size * 0.76f; // ~195px inside 256px square
            float scale = targetContentSize / maxDim;

            container.transform.localScale = new Vector3(scale, scale, 1f);

            Canvas.ForceUpdateCanvases();

            // 6. Render and extract pixels
            cam.Render();

            RenderTexture.active = rtTex;
            Texture2D snapshot = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            snapshot.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            snapshot.Apply();

            // Cleanup sandbox immediately
            cam.targetTexture = null;
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rtTex);
            Object.DestroyImmediate(sandboxRoot);

            // Save PNG to disk
            byte[] pngBytes = snapshot.EncodeToPNG();
            Object.DestroyImmediate(snapshot);
            File.WriteAllBytes(fullPath, pngBytes);

            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);

            // Configure texture import settings
            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.GUI;
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
        }

        /// <summary>
        /// Calculates compound bounding box covering the root and all child Graphics and CanvasRenderers
        /// in the coordinate space of the reference root transform.
        /// </summary>
        private static Bounds CalculateLocalBounds(Transform referenceRoot, GameObject root)
        {
            var rects = root.GetComponentsInChildren<RectTransform>(false);
            if (rects.Length == 0)
            {
                return new Bounds(Vector3.zero, new Vector3(160f, 44f, 0f));
            }

            Bounds bounds = new Bounds();
            bool hasBounds = false;
            Vector3[] corners = new Vector3[4];

            foreach (var rect in rects)
            {
                if (!rect.gameObject.activeInHierarchy) continue;

                bool isRoot = (rect.gameObject == root);
                Graphic graphic = rect.GetComponent<Graphic>();
                CanvasRenderer cr = rect.GetComponent<CanvasRenderer>();

                if (isRoot || (graphic != null && graphic.enabled) || (cr != null && cr.cull == false))
                {
                    rect.GetWorldCorners(corners);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 localPoint = referenceRoot.InverseTransformPoint(corners[i]);
                        if (!hasBounds)
                        {
                            bounds = new Bounds(localPoint, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(localPoint);
                        }
                    }
                }
            }

            if (!hasBounds || bounds.size.x <= 0f || bounds.size.y <= 0f)
            {
                bounds = new Bounds(Vector3.zero, new Vector3(160f, 44f, 0f));
            }

            return bounds;
        }

        private static void EnsureThumbnailsFolder()
        {
            if (!Directory.Exists(ThumbnailsFolder))
            {
                Directory.CreateDirectory(ThumbnailsFolder);
                AssetDatabase.Refresh();
            }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Trim();
        }
    }
}
#endif
