using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UIQualityLife.ProceduralUI;

namespace UIQualityLife.Editor.ProceduralUI
{
    public static class ProceduralUIMenuItems
    {
        [MenuItem("GameObject/UI/Procedural UI/Card (Rounded & Gradient)", false, 10)]
        public static void CreateProceduralCard(MenuCommand menuCommand)
        {
            GameObject parent = GetParentCanvasGameObject(menuCommand);
            GameObject cardGO = new GameObject("ProceduralCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(ProceduralImage));
            GameObjectUtility.SetParentAndAlign(cardGO, parent);

            var rt = cardGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(320, 200);

            var img = cardGO.GetComponent<ProceduralImage>();
            img.cornerStyle = CornerStyle.Round;
            img.uniformCorners = true;
            img.uniformRadius = 18f;

            // Fill & Gradient
            img.fillMode = FillMode.LinearGradient;
            img.color = new Color(0.14f, 0.16f, 0.22f, 1f);
            img.gradientEndColor = new Color(0.08f, 0.09f, 0.12f, 1f);
            img.gradientAngle = 135f;

            // Border
            img.borderWidth = 1.5f;
            img.borderColor = new Color(0.3f, 0.35f, 0.45f, 0.6f);

            // Drop Shadow & Inner Shadow
            img.dropShadowSoftness = 18f;
            img.dropShadowOffset = new Vector2(0f, -6f);
            img.dropShadowColor = new Color(0f, 0f, 0f, 0.5f);
            img.innerShadowSoftness = 12f;
            img.innerShadowColor = new Color(1f, 1f, 1f, 0.08f); // subtle top highlight

            img.EnsureCanvasChannels();

            Undo.RegisterCreatedObjectUndo(cardGO, "Create Procedural Card");
            Selection.activeGameObject = cardGO;
        }

        [MenuItem("GameObject/UI/Procedural UI/Card (Octagonal / Chamfer)", false, 11)]
        public static void CreateOctagonalCard(MenuCommand menuCommand)
        {
            GameObject parent = GetParentCanvasGameObject(menuCommand);
            GameObject cardGO = new GameObject("ProceduralOctagonalCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(ProceduralImage));
            GameObjectUtility.SetParentAndAlign(cardGO, parent);

            var rt = cardGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 180);

            var img = cardGO.GetComponent<ProceduralImage>();
            img.cornerStyle = CornerStyle.Octagonal;
            img.uniformCorners = true;
            img.uniformRadius = 24f; // 45-degree chamfer cuts

            img.fillMode = FillMode.LinearGradient;
            img.color = new Color(0.10f, 0.14f, 0.20f, 1f);
            img.gradientEndColor = new Color(0.05f, 0.07f, 0.12f, 1f);
            img.gradientAngle = 90f;

            img.borderWidth = 2f;
            img.borderColor = new Color(0.15f, 0.65f, 0.95f, 0.7f); // Cyberpunk Cyan

            img.dropShadowSoftness = 16f;
            img.dropShadowOffset = new Vector2(0f, -4f);
            img.dropShadowColor = new Color(0.15f, 0.65f, 0.95f, 0.35f); // Cyan outer glow

            img.EnsureCanvasChannels();

            Undo.RegisterCreatedObjectUndo(cardGO, "Create Octagonal Card");
            Selection.activeGameObject = cardGO;
        }

        [MenuItem("GameObject/UI/Procedural UI/Card (Scooped / Inverted)", false, 12)]
        public static void CreateScoopedCard(MenuCommand menuCommand)
        {
            GameObject parent = GetParentCanvasGameObject(menuCommand);
            GameObject cardGO = new GameObject("ProceduralScoopedCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(ProceduralImage));
            GameObjectUtility.SetParentAndAlign(cardGO, parent);

            var rt = cardGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280, 160);

            var img = cardGO.GetComponent<ProceduralImage>();
            img.cornerStyle = CornerStyle.Scooped;
            img.uniformCorners = true;
            img.uniformRadius = 20f; // concave cutouts

            img.fillMode = FillMode.LinearGradient;
            img.color = new Color(0.18f, 0.12f, 0.24f, 1f);
            img.gradientEndColor = new Color(0.09f, 0.05f, 0.14f, 1f);
            img.gradientAngle = 45f;

            img.borderWidth = 1.5f;
            img.borderColor = new Color(0.75f, 0.35f, 0.95f, 0.6f);

            img.dropShadowSoftness = 14f;
            img.dropShadowOffset = new Vector2(0f, -4f);
            img.dropShadowColor = new Color(0f, 0f, 0f, 0.5f);

            img.EnsureCanvasChannels();

            Undo.RegisterCreatedObjectUndo(cardGO, "Create Scooped Card");
            Selection.activeGameObject = cardGO;
        }

        [MenuItem("GameObject/UI/Procedural UI/Health Bar (Progression)", false, 13)]
        public static void CreateProgressBar(MenuCommand menuCommand)
        {
            GameObject parent = GetParentCanvasGameObject(menuCommand);
            GameObject barGO = new GameObject("ProceduralHealthBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(ProceduralImage));
            GameObjectUtility.SetParentAndAlign(barGO, parent);

            var rt = barGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280, 28);

            var img = barGO.GetComponent<ProceduralImage>();
            img.cornerStyle = CornerStyle.Round;
            img.uniformCorners = true;
            img.uniformRadius = 14f; // Perfect pill

            img.fillMode = FillMode.LinearGradient;
            img.color = new Color(0.95f, 0.25f, 0.25f, 1f); // Vibrant Red
            img.gradientEndColor = new Color(0.98f, 0.65f, 0.20f, 1f); // Orange Gold
            img.gradientAngle = 0f;

            img.borderWidth = 2f;
            img.borderColor = new Color(0.3f, 0.3f, 0.35f, 0.8f);

            img.progressType = ProgressType.Horizontal;
            img.progressAmount = 0.72f;

            img.dropShadowSoftness = 10f;
            img.dropShadowOffset = new Vector2(0f, -3f);
            img.dropShadowColor = new Color(0f, 0f, 0f, 0.4f);

            img.EnsureCanvasChannels();

            Undo.RegisterCreatedObjectUndo(barGO, "Create Health Bar");
            Selection.activeGameObject = barGO;
        }

        [MenuItem("GameObject/UI/Procedural UI/Button (Glow)", false, 14)]
        public static void CreateProceduralButton(MenuCommand menuCommand)
        {
            GameObject parent = GetParentCanvasGameObject(menuCommand);
            GameObject btnGO = new GameObject("ProceduralButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(ProceduralImage), typeof(Button));
            GameObjectUtility.SetParentAndAlign(btnGO, parent);

            var rt = btnGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180, 48);

            var img = btnGO.GetComponent<ProceduralImage>();
            img.cornerStyle = CornerStyle.Round;
            img.uniformCorners = true;
            img.uniformRadius = 12f;

            img.fillMode = FillMode.LinearGradient;
            img.color = new Color(0.25f, 0.55f, 1.0f, 1f);
            img.gradientEndColor = new Color(0.12f, 0.35f, 0.85f, 1f);
            img.gradientAngle = 90f;

            img.borderWidth = 1.5f;
            img.borderColor = new Color(0.5f, 0.75f, 1f, 0.8f);

            img.dropShadowSoftness = 16f;
            img.dropShadowOffset = new Vector2(0f, -3f);
            img.dropShadowColor = new Color(0.15f, 0.45f, 0.95f, 0.5f);

            img.innerShadowSoftness = 8f;
            img.innerShadowColor = new Color(1f, 1f, 1f, 0.25f);

            img.EnsureCanvasChannels();

            var btn = btnGO.GetComponent<Button>();
            btn.targetGraphic = img;

            // Child Text
            GameObject textGO = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            GameObjectUtility.SetParentAndAlign(textGO, btnGO);
            var textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "Button";
            tmp.fontSize = 16;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            Undo.RegisterCreatedObjectUndo(btnGO, "Create Procedural Button");
            Selection.activeGameObject = btnGO;
        }

        private static GameObject GetParentCanvasGameObject(MenuCommand menuCommand)
        {
            GameObject context = menuCommand.context as GameObject;
            if (context != null && context.GetComponentInParent<Canvas>() != null)
            {
                return context;
            }

            Canvas canvas = CanvasService.GetOrCreateActiveCanvas();
            return canvas.gameObject;
        }
    }
}
