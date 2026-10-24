using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIQualityLife.ProceduralUI;

namespace UIQualityLife.Runtime.Demo
{
    /// <summary>
    /// Interactive controller for the Procedural Shape Studio in the itch.io demo.
    /// Lets users dynamically adjust corner radius, borders, and shadows with live code preview.
    /// </summary>
    public class UIDemoProceduralTester : MonoBehaviour
    {
        [Header("Target Graphic")]
        [SerializeField] private ProceduralImage m_TargetImage;

        [Header("UI Controls")]
        [SerializeField] private Slider m_RadiusSlider;
        [SerializeField] private Slider m_BorderWidthSlider;
        [SerializeField] private Slider m_ShadowSoftnessSlider;
        [SerializeField] private Slider m_GradientAngleSlider;
        [SerializeField] private TMP_Dropdown m_CornerStyleDropdown;

        [Header("Readout Labels")]
        [SerializeField] private TMP_Text m_RadiusLabel;
        [SerializeField] private TMP_Text m_BorderWidthLabel;
        [SerializeField] private TMP_Text m_ShadowLabel;
        [SerializeField] private TMP_Text m_AngleLabel;
        [SerializeField] private TMP_Text m_CodeSnippetLabel;

        private void Start()
        {
            if (m_TargetImage == null) return;

            if (m_RadiusSlider != null)
            {
                m_RadiusSlider.value = m_TargetImage.uniformRadius;
                m_RadiusSlider.onValueChanged.AddListener(OnRadiusChanged);
            }

            if (m_BorderWidthSlider != null)
            {
                m_BorderWidthSlider.value = m_TargetImage.borderWidth;
                m_BorderWidthSlider.onValueChanged.AddListener(OnBorderWidthChanged);
            }

            if (m_ShadowSoftnessSlider != null)
            {
                m_ShadowSoftnessSlider.value = m_TargetImage.dropShadowSoftness;
                m_ShadowSoftnessSlider.onValueChanged.AddListener(OnShadowSoftnessChanged);
            }

            if (m_GradientAngleSlider != null)
            {
                m_GradientAngleSlider.value = m_TargetImage.gradientAngle;
                m_GradientAngleSlider.onValueChanged.AddListener(OnGradientAngleChanged);
            }

            if (m_CornerStyleDropdown != null)
            {
                m_CornerStyleDropdown.value = (int)m_TargetImage.cornerStyle;
                m_CornerStyleDropdown.onValueChanged.AddListener(OnCornerStyleChanged);
            }

            UpdateLabelsAndSnippet();
        }

        public void OnRadiusChanged(float value)
        {
            if (m_TargetImage == null) return;
            m_TargetImage.uniformCorners = true;
            m_TargetImage.uniformRadius = value;
            UpdateLabelsAndSnippet();
        }

        public void OnBorderWidthChanged(float value)
        {
            if (m_TargetImage == null) return;
            m_TargetImage.borderWidth = value;
            UpdateLabelsAndSnippet();
        }

        public void OnShadowSoftnessChanged(float value)
        {
            if (m_TargetImage == null) return;
            m_TargetImage.dropShadowSoftness = value;
            UpdateLabelsAndSnippet();
        }

        public void OnGradientAngleChanged(float value)
        {
            if (m_TargetImage == null) return;
            m_TargetImage.fillMode = FillMode.LinearGradient;
            m_TargetImage.gradientAngle = value;
            UpdateLabelsAndSnippet();
        }

        public void OnCornerStyleChanged(int styleIndex)
        {
            if (m_TargetImage == null) return;
            m_TargetImage.cornerStyle = (CornerStyle)styleIndex;
            UpdateLabelsAndSnippet();
        }

        public void SetShapePresetSquare()
        {
            if (m_TargetImage == null) return;
            m_TargetImage.cornerStyle = CornerStyle.Round;
            m_TargetImage.uniformRadius = 0f;
            if (m_RadiusSlider != null) m_RadiusSlider.value = 0f;
            UpdateLabelsAndSnippet();
        }

        public void SetShapePresetRounded()
        {
            if (m_TargetImage == null) return;
            m_TargetImage.cornerStyle = CornerStyle.Round;
            m_TargetImage.uniformRadius = 24f;
            if (m_RadiusSlider != null) m_RadiusSlider.value = 24f;
            UpdateLabelsAndSnippet();
        }

        public void SetShapePresetPill()
        {
            if (m_TargetImage == null) return;
            m_TargetImage.cornerStyle = CornerStyle.Round;
            m_TargetImage.uniformRadius = 999f;
            if (m_RadiusSlider != null) m_RadiusSlider.value = 100f;
            UpdateLabelsAndSnippet();
        }

        public void SetShapePresetOctagon()
        {
            if (m_TargetImage == null) return;
            m_TargetImage.cornerStyle = CornerStyle.Octagonal;
            m_TargetImage.uniformRadius = 20f;
            if (m_CornerStyleDropdown != null) m_CornerStyleDropdown.value = (int)CornerStyle.Octagonal;
            if (m_RadiusSlider != null) m_RadiusSlider.value = 20f;
            UpdateLabelsAndSnippet();
        }

        private void UpdateLabelsAndSnippet()
        {
            if (m_TargetImage == null) return;

            if (m_RadiusLabel != null)
                m_RadiusLabel.text = $"{m_TargetImage.uniformRadius:F0} px";

            if (m_BorderWidthLabel != null)
                m_BorderWidthLabel.text = $"{m_TargetImage.borderWidth:F1} px";

            if (m_ShadowLabel != null)
                m_ShadowLabel.text = $"{m_TargetImage.dropShadowSoftness:F0} px";

            if (m_AngleLabel != null)
                m_AngleLabel.text = $"{m_TargetImage.gradientAngle:F0}°";

            if (m_CodeSnippetLabel != null)
            {
                m_CodeSnippetLabel.text =
                    $"// Generated Procedural Shape\n" +
                    $"proceduralImage.cornerStyle = CornerStyle.{m_TargetImage.cornerStyle};\n" +
                    $"proceduralImage.uniformRadius = {m_TargetImage.uniformRadius:F0}f;\n" +
                    $"proceduralImage.borderWidth = {m_TargetImage.borderWidth:F1}f;\n" +
                    $"proceduralImage.dropShadowSoftness = {m_TargetImage.dropShadowSoftness:F0}f;";
            }
        }
    }
}
