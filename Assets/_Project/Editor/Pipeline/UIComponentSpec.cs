#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIQualityLife.Editor
{
    [Serializable]
    public class UIComponentSpec
    {
        public string name = "NewComponent";
        public string[] categories = new string[] { "General" };
        public string description = "";
        public string previewColorHex = "#2563EB";
        public UINodeSpec root;
    }

    [Serializable]
    public class UINodeSpec
    {
        public string name = "Node";
        public string type = "Container"; // Container, Image, Text, Button, InputField, Slider, Toggle

        // Dimensions & Transform
        public float width = 160f;
        public float height = 44f;
        public float[] anchorMin = new float[] { 0.5f, 0.5f };
        public float[] anchorMax = new float[] { 0.5f, 0.5f };
        public float[] pivot = new float[] { 0.5f, 0.5f };
        public float[] anchoredPosition = new float[] { 0f, 0f };

        // Visual / Image styling
        public string backgroundColorHex; // e.g. "#1E2025" or "#00000000"
        public string spriteResource;     // Optional Unity built-in sprite like "UISprite" or "Background"

        // Layout Group properties
        public UILayoutSpec layout;

        // Text properties (TextMeshPro)
        public string text;
        public float fontSize = 14f;
        public string fontStyle = "Normal"; // Normal, Bold, Italic, BoldItalic
        public string textColorHex = "#FFFFFF";
        public string alignment = "Center"; // Center, Left, Right, TopLeft, etc.
        public bool wrap = true;

        // Button properties
        public string normalColorHex = "#2563EB";
        public string highlightColorHex = "#3B82F6";
        public string pressedColorHex = "#1D4ED8";
        public string disabledColorHex = "#64748B";

        // Input Field properties
        public string placeholderText = "Enter text...";
        public string placeholderColorHex = "#94A3B8";

        // Slider properties
        public float sliderValue = 0.5f;
        public float sliderMinValue = 0f;
        public float sliderMaxValue = 1f;
        public string fillColorHex = "#2563EB";

        // Toggle properties
        public bool toggleState = true;
        public string checkmarkColorHex = "#2563EB";

        // Children hierarchy
        public List<UINodeSpec> children = new List<UINodeSpec>();
    }

    [Serializable]
    public class UILayoutSpec
    {
        public string type = "None"; // None, Horizontal, Vertical, Grid
        public float spacing = 8f;
        public int paddingLeft = 0;
        public int paddingRight = 0;
        public int paddingTop = 0;
        public int paddingBottom = 0;
        public bool childControlWidth = true;
        public bool childControlHeight = true;
        public bool childForceExpandWidth = false;
        public bool childForceExpandHeight = false;
        public string childAlignment = "MiddleCenter"; // UpperLeft, MiddleCenter, etc.
    }
}
#endif
