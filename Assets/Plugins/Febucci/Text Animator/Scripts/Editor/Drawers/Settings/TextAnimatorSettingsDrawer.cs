using System;
using UnityEditor;
using UnityEngine;

namespace Febucci.UI
{
    [CustomEditor(typeof(TextAnimatorSettings))]
    public class TextAnimatorSettingsDrawer : Editor
    {
        bool extraSettings = false;

        SerializedProperty behaviors;
        SerializedProperty appearances;
        SerializedProperty styles;
        SerializedProperty actions;
        SerializedProperty controlTags;
        SerializedProperty disappearanceMiddleSymbol;


        void OnEnable()
        {
            behaviors = serializedObject.FindProperty(nameof(TextAnimatorSettings.behaviors));
            appearances = serializedObject.FindProperty(nameof(TextAnimatorSettings.appearances));
            styles = serializedObject.FindProperty(nameof(TextAnimatorSettings.defaultStyleSheet));
            actions = serializedObject.FindProperty(nameof(TextAnimatorSettings.actions));
            controlTags = serializedObject.FindProperty(nameof(TextAnimatorSettings.controlTags));
            disappearanceMiddleSymbol = serializedObject.FindProperty(nameof(TextAnimatorSettings.disappearancesMiddleSymbol));
        }

        void DrawEffects()
        {
            EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(behaviors);
            EditorGUILayout.PropertyField(appearances);
            EditorGUILayout.PropertyField(disappearanceMiddleSymbol);
            EditorGUI.indentLevel--;
        }

        void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(actions);
            EditorGUI.indentLevel--;
        }

        void DrawStyles()
        {
            EditorGUILayout.LabelField("Styles", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(styles);
            EditorGUI.indentLevel--;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox($"For this to work, make sure that it's named {TextAnimatorSettings.expectedName} and it's placed inside the Resources folder.", MessageType.Info);
            EditorGUILayout.Space();

            DrawEffects();
            EditorGUILayout.Space();

            DrawActions();
            EditorGUILayout.Space();

            DrawStyles();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(controlTags);
            EditorGUILayout.Space();

            extraSettings = EditorGUILayout.Foldout(extraSettings, "Extra Settings", EditorStyles.foldoutHeader);
            if (extraSettings)
            {
                if (GUILayout.Button("Reset Default Effects and Actions"))
                {
                    if (EditorUtility.DisplayDialog("Text Animator",
                            "Are you sure you want to reset the default effects and actions?", "Yes", "No"))
                    {
                        TextAnimatorSetupWindow.ResetToBuiltIn();
                    }
                }
            }

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }
    }
}