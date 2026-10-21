#if UNITY_EDITOR
using System.Collections.Generic;

namespace UIQualityLife.Editor
{
    // Note: [Serializable] is intentionally NOT used on these classes.
    // They are deserialized by Newtonsoft.Json (not JsonUtility) to support
    // the recursive UINodeSpec.children list without hitting JsonUtility's depth-10 limit.

    public class UIComponentSpec
    {
        public string name = "NewComponent";
        public string[] categories = new string[] { "General" };
        public string description = "";
        public string previewColorHex = "#2563EB";

        /// <summary>Primary root node. Use "root" in spec JSON.</summary>
        public UINodeSpec root;

        /// <summary>Alias accepted by the builder when "root" is not set. Agents may emit "rootNode" from SKILL.md docs.</summary>
        public UINodeSpec rootNode;

        /// <summary>Returns the effective root node, preferring "root" over "rootNode".</summary>
        public UINodeSpec ResolvedRoot => root ?? rootNode;
    }

    public class UINodeSpec
    {
        public string name = "Node";

        /// <summary>
        /// Node type: "Container" | "Image" | "Text" | "Button" | "InputField" | "Slider" | "Toggle" | "ProgressBar"
        /// Shorthand booleans (isButton, isProgressBar, etc.) are also supported and set this field automatically.
        /// </summary>
        public string type = "Container";

        // Shorthand type flags — set type implicitly when true
        public bool isButton;
        public bool isInputField;
        public bool isSlider;
        public bool isToggle;
        /// <summary>
        /// When true, builds a horizontal progress bar with Track + Fill sub-hierarchy.
        /// Set fillColorHex for the fill color, backgroundColorHex for the track, and value (0–1) for fill fraction.
        /// </summary>
        public bool isProgressBar;

        // Dimensions & Transform
        public float width = 160f;
        public float height = 44f;
        public float[] anchorMin;
        public float[] anchorMax;
        public float[] pivot;
        public float[] anchoredPosition;

        // Visual / Image styling
        /// <summary>Background color hex. Supports alpha: "#RRGGBBAA". Alias: colorHex.</summary>
        public string backgroundColorHex;
        /// <summary>Alias for backgroundColorHex. If both are set, backgroundColorHex takes priority.</summary>
        public string colorHex;
        public string spriteResource;

        /// <summary>Returns the effective background color, preferring backgroundColorHex over colorHex.</summary>
        public string ResolvedColorHex => !string.IsNullOrEmpty(backgroundColorHex) ? backgroundColorHex : colorHex;

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

        /// <summary>Progress bar fill value in range 0–1. Used when isProgressBar = true or type = "ProgressBar".</summary>
        public float value = 0f;

        // Toggle properties
        public bool toggleState = true;
        public string checkmarkColorHex = "#2563EB";

        // LayoutElement properties (optional overrides for children within LayoutGroups)
        public float minWidth = -1f;
        public float minHeight = -1f;
        public float preferredWidth = -1f;
        public float preferredHeight = -1f;
        public float flexibleWidth = -1f;
        public float flexibleHeight = -1f;
        public bool ignoreLayout = false;

        // Children hierarchy — Newtonsoft.Json handles this recursive type correctly
        public List<UINodeSpec> children = new List<UINodeSpec>();
    }

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
