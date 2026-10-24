#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UIQualityLife.Styling.Editor
{
    [CustomEditor(typeof(UIClassAnimator))]
    [CanEditMultipleObjects]
    public class UIClassAnimatorEditor : UnityEditor.Editor
    {
        private UIClassAnimator m_Target;

        private void OnEnable()
        {
            m_Target = (UIClassAnimator)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🎮 CSS State Machine Live Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Test CSS states (:hover, :active, :focus, :disabled, :checked, custom) live in Edit Mode without running the game.", MessageType.Info);

            if (m_Target.AnimationClass == null)
            {
                EditorGUILayout.HelpBox("Assign a UIAnimationClass asset above to enable preview testing.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // 1. Motion Clips Section
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("🎬 Motion Clips Live Simulation", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool hasHover = m_Target.AnimationClass.HoverClip != null && m_Target.AnimationClass.HoverClip.enabled;
            bool hasPressed = m_Target.AnimationClass.PressedClip != null && m_Target.AnimationClass.PressedClip.enabled;
            bool hasEntrance = m_Target.AnimationClass.EntranceClip != null && m_Target.AnimationClass.EntranceClip.enabled;
            bool hasExit = m_Target.AnimationClass.ExitClip != null && m_Target.AnimationClass.ExitClip.enabled;

            EditorGUILayout.BeginHorizontal();
            if (hasHover)
            {
                if (GUILayout.Button("▶ Play Hover Motion (:hover)", EditorStyles.miniButton, GUILayout.Height(26)))
                {
                    m_Target.EditorPlayHover();
                }
            }
            if (hasPressed)
            {
                if (GUILayout.Button("▶ Play Pressed Motion (:active)", EditorStyles.miniButton, GUILayout.Height(26)))
                {
                    m_Target.EditorPlayPressed();
                }
            }
            if (hasEntrance)
            {
                if (GUILayout.Button("▶ Play Entrance Motion", EditorStyles.miniButton, GUILayout.Height(26)))
                {
                    m_Target.EditorPlayEntrance();
                }
            }
            if (hasExit)
            {
                if (GUILayout.Button("▶ Play Exit Motion", EditorStyles.miniButton, GUILayout.Height(26)))
                {
                    m_Target.EditorPlayExit();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("🔄 Reset To Rest", EditorStyles.miniButton, GUILayout.Height(24)))
            {
                m_Target.EditorReset();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("🎮 Static CSS Visual States (:hover, :pressed)", EditorStyles.boldLabel);

            var states = m_Target.AnimationClass.States;
            string currentState = m_Target.CurrentStateName;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Available States:", EditorStyles.miniBoldLabel);

            // Dynamic grid of state buttons
            int buttonsPerRow = 3;
            int count = states.Count;

            for (int i = 0; i < count; i += buttonsPerRow)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = 0; j < buttonsPerRow; j++)
                {
                    int index = i + j;
                    if (index < count)
                    {
                        var state = states[index];
                        bool isActive = string.Equals(currentState, state.stateName, System.StringComparison.OrdinalIgnoreCase);

                        if (isActive)
                        {
                            GUI.backgroundColor = UIQualityLife.Editor.UIElementStyleUtils.UnityAccentBlue;
                        }

                        if (GUILayout.Button(state.stateName, EditorStyles.miniButton, GUILayout.Height(22)))
                        {
                            m_Target.EditorPreviewState(state.stateName);
                        }

                        GUI.backgroundColor = Color.white;
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);

            // Reset Row
            if (GUILayout.Button("🔄 Reset To Rest (Normal)", EditorStyles.miniButton, GUILayout.Height(22)))
            {
                m_Target.EditorReset();
            }

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private void OnDisable()
        {
            if (m_Target != null && !Application.isPlaying)
            {
                m_Target.EditorReset();
            }
        }
    }
}
#endif
