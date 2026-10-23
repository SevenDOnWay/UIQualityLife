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

            // Micro-Bounce & Loop Row
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("💥 Click Bounce", EditorStyles.miniButton, GUILayout.Height(22)))
            {
                m_Target.EditorTriggerClickBounce();
            }

            bool loopActive = m_Target.IsEditorLoopActive;
            GUI.backgroundColor = loopActive ? UIQualityLife.Editor.UIElementStyleUtils.UnityAccentBlue : Color.white;
            string loopName = m_Target.AnimationClass.LoopType != UILoopAnimationType.None
                ? $"✨ Loop ({m_Target.AnimationClass.LoopType})"
                : "✨ Loop (None)";
            if (GUILayout.Button(loopName, EditorStyles.miniButton, GUILayout.Height(22)))
            {
                m_Target.EditorToggleLoop();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

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
