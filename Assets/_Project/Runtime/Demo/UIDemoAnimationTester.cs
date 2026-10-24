using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIQualityLife.Styling;

namespace UIQualityLife.Runtime.Demo
{
    /// <summary>
    /// Interactive controller for the Animation Lab tab in the itch.io WebGL showcase demo.
    /// Lets visitors select any atomic animation profile, trigger hover/pressed/punch states,
    /// and observe multi-channel motion timing in real time.
    /// </summary>
    public class UIDemoAnimationTester : MonoBehaviour
    {
        [Header("Target Preview")]
        [SerializeField] private UIClassAnimator m_TargetAnimator;

        [Header("UI Controls")]
        [SerializeField] private TMP_Dropdown m_AnimationDropdown;
        [SerializeField] private Button m_PunchButton;
        [SerializeField] private Button m_ResetButton;

        [Header("Readout Labels")]
        [SerializeField] private TMP_Text m_AnimationTitleText;
        [SerializeField] private TMP_Text m_AnimationInfoText;

        private List<UIAnimationClass> m_AvailableAnimations = new List<UIAnimationClass>();

        private void Start()
        {
            LoadAnimations();
            PopulateDropdown();
            UpdateInfoDisplay();

            if (m_PunchButton != null)
                m_PunchButton.onClick.AddListener(TriggerPunch);

            if (m_ResetButton != null)
                m_ResetButton.onClick.AddListener(ResetTarget);
        }

        private void LoadAnimations()
        {
            m_AvailableAnimations.Clear();
            var loaded = Resources.LoadAll<UIAnimationClass>("Animations");
            if (loaded != null)
            {
                foreach (var anim in loaded)
                {
                    if (anim != null && !m_AvailableAnimations.Contains(anim))
                    {
                        m_AvailableAnimations.Add(anim);
                    }
                }
            }
        }

        private void PopulateDropdown()
        {
            if (m_AnimationDropdown == null) return;

            m_AnimationDropdown.ClearOptions();
            var options = new List<string>();

            int selectedIndex = 0;
            var current = m_TargetAnimator != null ? m_TargetAnimator.AnimationClass : null;

            for (int i = 0; i < m_AvailableAnimations.Count; i++)
            {
                var anim = m_AvailableAnimations[i];
                string name = string.IsNullOrEmpty(anim.ClassName) ? anim.name : anim.ClassName;
                options.Add(name);

                if (current != null && current == anim)
                {
                    selectedIndex = i;
                }
            }

            m_AnimationDropdown.AddOptions(options);
            m_AnimationDropdown.value = selectedIndex;
            m_AnimationDropdown.onValueChanged.AddListener(OnAnimationSelected);

            if (m_AvailableAnimations.Count > 0 && current == null && m_TargetAnimator != null)
            {
                SelectAnimationByIndex(0);
            }
        }

        public void SelectAnimationByIndex(int index)
        {
            if (index >= 0 && index < m_AvailableAnimations.Count && m_TargetAnimator != null)
            {
                m_TargetAnimator.AnimationClass = m_AvailableAnimations[index];
                UpdateInfoDisplay();
            }
        }

        private void OnAnimationSelected(int index)
        {
            SelectAnimationByIndex(index);
        }

        public void TriggerPunch()
        {
            if (m_TargetAnimator != null)
            {
                m_TargetAnimator.PlayPunch(new Vector3(0.2f, -0.2f, 0f), 0.25f, 5f);
            }
        }

        public void ResetTarget()
        {
            if (m_TargetAnimator != null)
            {
                m_TargetAnimator.ResetToRest();
            }
        }

        private void UpdateInfoDisplay()
        {
            var anim = m_TargetAnimator != null ? m_TargetAnimator.AnimationClass : null;
            if (anim == null)
            {
                if (m_AnimationTitleText != null) m_AnimationTitleText.text = "No Animation Selected";
                if (m_AnimationInfoText != null) m_AnimationInfoText.text = "Select an animation from the dropdown to test.";
                return;
            }

            string animName = string.IsNullOrEmpty(anim.ClassName) ? anim.name : anim.ClassName;
            if (m_AnimationTitleText != null)
                m_AnimationTitleText.text = $"Profile: <b>{animName}</b>";

            if (m_AnimationInfoText != null)
            {
                m_AnimationInfoText.text =
                    $"Loop Type: <b>{anim.LoopType}</b> (Duration: {anim.LoopDuration:F2}s)\n" +
                    $"Interactive States: Hover, Pressed, Focus, Disabled, Bounce Click\n" +
                    $"Interact with the card above using pointer hover, click, or test buttons below.";
            }
        }
    }
}
