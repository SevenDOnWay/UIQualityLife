using UnityEngine;

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Standalone reusable ScriptableObject defining an atomic multi-channel motion profile.
    /// Can be shared across multiple UI components or referenced by <see cref="UIAnimationClass"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMotionProfile", menuName = "UI Quality of Life/Styling/Motion Profile")]
    public class UIMotionProfile : ScriptableObject
    {
        [Tooltip("Human-readable title or CSS animation name (e.g. 'SlideIn_Bottom', 'CardFlip_Y', 'Swing_Pendulum').")]
        [SerializeField] private string m_ProfileName = "CustomMotion";

        [TextArea(2, 4)]
        [SerializeField] private string m_Description = "Composable multi-channel motion clip.";

        [SerializeField] private UIMotionClip m_Clip = new UIMotionClip();

        #region Properties
        public string ProfileName => m_ProfileName;
        public string Description => m_Description;
        public UIMotionClip Clip => m_Clip;
        #endregion

        /// <summary>
        /// Creates a deep copy of this profile's motion clip for runtime evaluation.
        /// </summary>
        public UIMotionClip CloneClip()
        {
            // JsonUtility provides clean deep cloning of Serializable structures
            string json = JsonUtility.ToJson(m_Clip);
            return JsonUtility.FromJson<UIMotionClip>(json);
        }
    }
}
