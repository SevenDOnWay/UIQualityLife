#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[Obsolete("Deprecated: UIFigmaEditorWindow is superseded by UIAssetShelfWindow. Use Tools/UI Quality of Life/UI Asset Shelf instead.")]
public class UIFigmaEditorWindow : EditorWindow {
    private VisualElement canvas;
    private Label selectedLabel;
    private VisualElement selectedElement;

    [MenuItem("Tools/UI Quality of Life/Old Figma Editor (Deprecated)")]
    [MenuItem("Tools/UI Figma Editor (Deprecated)")]
    public static void Open() {
        var window = GetWindow<UIFigmaEditorWindow>();
        window.titleContent = new GUIContent("UI Figma (Deprecated)");
        window.minSize = new Vector2(800, 500);
    }

    public void CreateGUI() {
        rootVisualElement.Clear();
        rootVisualElement.style.flexDirection = FlexDirection.Column;

        // TOP: Deprecation Warning Banner
        var deprecationBanner = new VisualElement();
        deprecationBanner.style.backgroundColor = new Color(0.35f, 0.22f, 0.05f);
        deprecationBanner.style.borderBottomWidth = 1;
        deprecationBanner.style.borderBottomColor = new Color(0.6f, 0.4f, 0.1f);
        deprecationBanner.style.paddingTop = 8;
        deprecationBanner.style.paddingBottom = 8;
        deprecationBanner.style.paddingLeft = 12;
        deprecationBanner.style.paddingRight = 12;
        deprecationBanner.style.flexDirection = FlexDirection.Row;
        deprecationBanner.style.justifyContent = Justify.SpaceBetween;
        deprecationBanner.style.alignItems = Align.Center;

        var warningText = new Label("⚠️ DEPRECATED: This editor is superseded by the new UI Asset Shelf for UGUI.");
        warningText.style.color = new Color(1f, 0.85f, 0.4f);
        warningText.style.unityFontStyleAndWeight = FontStyle.Bold;

        var openShelfBtn = new Button(() => {
            EditorApplication.ExecuteMenuItem("Tools/UI Quality of Life/UI Asset Shelf");
        }) { text = "Open UI Asset Shelf →" };
        openShelfBtn.style.backgroundColor = new Color(0.24f, 0.45f, 0.85f);
        openShelfBtn.style.color = Color.white;

        deprecationBanner.Add(warningText);
        deprecationBanner.Add(openShelfBtn);
        rootVisualElement.Add(deprecationBanner);

        var mainLayout = new VisualElement();
        mainLayout.style.flexDirection = FlexDirection.Row;
        mainLayout.style.flexGrow = 1;

        // LEFT: Component palette
        var palette = new VisualElement();
        palette.style.width = 150;
        palette.style.paddingLeft = 8;
        palette.style.paddingRight = 8;

        palette.Add(new Label("Components"));

        var button = new Button(() => SpawnElement("Button"));
        button.text = "+ Button";
        palette.Add(button);

        var panel = new Button(() => SpawnElement("Panel"));
        panel.text = "+ Panel";
        palette.Add(panel);

        // CENTER: Design canvas
        canvas = new VisualElement();
        canvas.style.flexGrow = 1;
        canvas.style.position = Position.Relative;
        canvas.style.backgroundColor =
            new Color(0.16f, 0.16f, 0.16f);
        canvas.style.overflow = Overflow.Hidden;

        // RIGHT: Inspector
        var inspector = new VisualElement();
        inspector.style.width = 180;
        inspector.style.paddingLeft = 8;
        inspector.style.paddingRight = 8;

        inspector.Add(new Label("Inspector"));
        selectedLabel = new Label("Nothing selected");
        inspector.Add(selectedLabel);

        mainLayout.Add(palette);
        mainLayout.Add(canvas);
        mainLayout.Add(inspector);

        rootVisualElement.Add(mainLayout);
    }

    private void SpawnElement( string type ) {
        var element = new VisualElement();

        element.name = type;
        element.userData = type;

        element.style.position = Position.Absolute;
        element.style.left = 30;
        element.style.top = 30;
        element.style.width = type == "Panel" ? 180 : 120;
        element.style.height = type == "Panel" ? 120 : 44;

        element.style.backgroundColor = type == "Panel"
            ? new Color(0.25f, 0.45f, 0.7f)
            : new Color(0.2f, 0.65f, 0.4f);

        element.style.borderTopLeftRadius = 12;
        element.style.borderTopRightRadius = 12;
        element.style.borderBottomLeftRadius = 12;
        element.style.borderBottomRightRadius = 12;

        // Make the element selectable and draggable.
        element.RegisterCallback<PointerDownEvent>(OnPointerDown);
        element.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        element.RegisterCallback<PointerUpEvent>(OnPointerUp);

        canvas.Add(element);
        SelectElement(element);
    }

    private Vector2 pointerStart;
    private Vector2 elementStart;
    private int activePointerId = -1;

    private void OnPointerDown( PointerDownEvent evt ) {
        var element = (VisualElement)evt.currentTarget;

        if ( evt.button != 0 )
            return;

        SelectElement(element);

        // Convert pointer coordinates into canvas coordinates.
        pointerStart = canvas.WorldToLocal(evt.position);

        elementStart = new Vector2(
            element.resolvedStyle.left,
            element.resolvedStyle.top
        );

        activePointerId = evt.pointerId;
        element.CapturePointer(activePointerId);

        evt.StopPropagation();
    }

    private void OnPointerMove( PointerMoveEvent evt ) {
        var element = (VisualElement)evt.currentTarget;

        if ( activePointerId != evt.pointerId ||
            !element.HasPointerCapture(evt.pointerId) )
            return;

        Vector2 currentPointer = canvas.WorldToLocal(evt.position);
        Vector2 delta = currentPointer - pointerStart;

        element.style.left = elementStart.x + delta.x;
        element.style.top = elementStart.y + delta.y;
    }

    private void OnPointerUp( PointerUpEvent evt ) {
        var element = (VisualElement)evt.currentTarget;

        if ( activePointerId != evt.pointerId )
            return;

        if ( element.HasPointerCapture(evt.pointerId) )
            element.ReleasePointer(evt.pointerId);

        activePointerId = -1;
    }

    private void SelectElement( VisualElement element ) {
        selectedElement = element;

        selectedLabel.text =
            $"Selected: {element.name}\n" +
            $"Position: ({element.resolvedStyle.left:F0}, " +
            $"{element.resolvedStyle.top:F0})\n" +
            $"Size: ({element.resolvedStyle.width:F0}, " +
            $"{element.resolvedStyle.height:F0})";
    }
}
#endif
