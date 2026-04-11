using UnityEditor;
using UnityEngine;

namespace Febucci.UI.Core.Editors
{
    [CustomEditor(typeof(TypewriterCore), true)]
    class TypewriterCoreDrawer : Editor
    {
        SerializedProperty useTypewriter;
        SerializedProperty startTypewriterMode;
        SerializedProperty hideAppearancesOnSkip;
        SerializedProperty hideDisappearancesOnSkip;
        SerializedProperty triggerEventsOnSkip;
        SerializedProperty disappearanceOrientation;

        SerializedProperty onTextShowed;
        SerializedProperty onTypewriterStart;
        SerializedProperty onCharacterVisible;
        SerializedProperty onTextDisappeared;
        SerializedProperty onMessage;

        SerializedProperty resetTypingSpeedAtStartup;
        SerializedProperty waitForFullAppearance;
        SerializedProperty waitForFullDisappearance;

        string[] propertiesToExclude = new string[0];

        
        protected struct PropertyWithDifferentLabel
        {
            public SerializedProperty property;
            public GUIContent label;

            public PropertyWithDifferentLabel(SerializedObject obj, string property, string label)
            {
                this.property = obj.FindProperty(property);
                this.label = new GUIContent(label);
            }

            public void PropertyField()
            {
                EditorGUILayout.PropertyField(property, label);
            }
        }

        
        protected virtual string[] GetPropertiesToExclude()
        {
            return new string[] {
            "m_Script",
            "useTypeWriter",
            "startTypewriterMode",
            nameof(TypewriterCore.hideAppearancesOnSkip),
            nameof(TypewriterCore.hideDisappearancesOnSkip),
            "triggerEventsOnSkip",
            "onTextShowed",
            "onTypewriterStart",
            "onCharacterVisible",
            "resetTypingSpeedAtStartup",
            "onTextDisappeared",
            "disappearanceOrientation",
            "onMessage",
            nameof(TypewriterCore.triggerShowedAfterEffectsEnd),
            nameof(TypewriterCore.triggerDisappearedAfterEffectsEnd),
            };
        }

        protected virtual void OnEnable()
        {
            useTypewriter = serializedObject.FindProperty("useTypeWriter");
            startTypewriterMode = serializedObject.FindProperty("startTypewriterMode");
            hideAppearancesOnSkip = serializedObject.FindProperty("hideAppearancesOnSkip");
            hideDisappearancesOnSkip = serializedObject.FindProperty("hideDisappearancesOnSkip");
            triggerEventsOnSkip = serializedObject.FindProperty("triggerEventsOnSkip");
            disappearanceOrientation = serializedObject.FindProperty("disappearanceOrientation");


            onTextShowed = serializedObject.FindProperty("onTextShowed");
            onTypewriterStart = serializedObject.FindProperty("onTypewriterStart");
            onCharacterVisible = serializedObject.FindProperty("onCharacterVisible");
            onTextDisappeared = serializedObject.FindProperty("onTextDisappeared");
            onMessage = serializedObject.FindProperty("onMessage");

            resetTypingSpeedAtStartup = serializedObject.FindProperty("resetTypingSpeedAtStartup");
            waitForFullAppearance = serializedObject.FindProperty(nameof(TypewriterCore.triggerShowedAfterEffectsEnd));
            waitForFullDisappearance = serializedObject.FindProperty(nameof(TypewriterCore.triggerDisappearedAfterEffectsEnd));

            propertiesToExclude = GetPropertiesToExclude();
        }

        bool ButtonPlaymode(string label)
        {
            bool prevGUI = GUI.enabled;
            GUI.enabled = Application.isPlaying;

            bool value = GUILayout.Button(label, EditorStyles.miniButton, GUILayout.MaxWidth(70));

            GUI.enabled = prevGUI;
            return value;
        }

        public override void OnInspectorGUI()
        {

            {
                EditorGUILayout.LabelField("Main Settings", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(useTypewriter);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            //Typewriter settings

            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Typewriter", EditorStyles.boldLabel);

                if (useTypewriter.boolValue)
                {
                    if (ButtonPlaymode("Start"))
                    {
                        ((TypewriterCore)target).StartShowingText(true);
                    }
                    if (ButtonPlaymode("Stop"))
                    {
                        ((TypewriterCore)target).StopShowingText();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            if (useTypewriter.boolValue)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(startTypewriterMode);

                EditorGUILayout.PropertyField(resetTypingSpeedAtStartup);

                EditorGUILayout.BeginHorizontal(); 
                EditorGUILayout.LabelField("Typewriter Skip & Events", EditorStyles.boldLabel);


                if (ButtonPlaymode("Skip"))
                {
                    ((TypewriterCore)target).SkipTypewriter();
                }
                EditorGUILayout.EndHorizontal();


                EditorGUILayout.LabelField("Appearing");
                
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(hideAppearancesOnSkip);
                EditorGUILayout.PropertyField(triggerEventsOnSkip);
                EditorGUILayout.PropertyField(waitForFullAppearance);
                EditorGUI.indentLevel--;
                
                EditorGUILayout.LabelField("Disappearing");
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(hideDisappearancesOnSkip);
                EditorGUILayout.PropertyField(waitForFullDisappearance);
                EditorGUI.indentLevel--;

                EditorGUI.indentLevel--;

            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.LabelField("The typewriter is disabled");
                GUI.enabled = true;
            }

            EditorGUILayout.Space();

            //Events
            {
                EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);

                // foldoutEvents = EditorGUILayout.Foldout(foldoutEvents, "Events");

                //if (foldoutEvents)
                {
                    EditorGUILayout.PropertyField(onTextShowed);
                    EditorGUILayout.PropertyField(onTextDisappeared);

                    //GUI.enabled = showLettersDinamically.boolValue;

                    if (useTypewriter.boolValue)
                    {

                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(onTypewriterStart);
                        EditorGUILayout.PropertyField(onCharacterVisible);
                        EditorGUILayout.PropertyField(onMessage);

                        EditorGUI.indentLevel--;
                    }

                    //GUI.enabled = true;
                }

            }

            EditorGUILayout.Space();

            //Typewriter
            {
                EditorGUILayout.LabelField("Typewriter Wait", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                GUI.enabled = false;
                if(!useTypewriter.boolValue) EditorGUILayout.LabelField("[!] 'Use Typewriter' option is disabled, so these settings might not apply", EditorStyles.wordWrappedMiniLabel);
                GUI.enabled = true;
                OnTypewriterSectionGUI();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            //Disappearance
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Disappearances", EditorStyles.boldLabel);

                if (ButtonPlaymode("Start"))
                {
                    ((TypewriterCore)target).StartDisappearingText();
                }
                if (ButtonPlaymode("Stop"))
                {
                    ((TypewriterCore)target).StopDisappearingText();
                }

                EditorGUILayout.EndHorizontal();
                

                EditorGUI.indentLevel++;
                GUI.enabled = false;
                if(!useTypewriter.boolValue) EditorGUILayout.LabelField("[!] 'Use Typewriter' option is disabled, so these settings might not apply", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("To start disappearances, please call the 'StartDisappearingText()' method. See the docs for more.", EditorStyles.wordWrappedMiniLabel);
                GUI.enabled = true;

                EditorGUILayout.PropertyField(disappearanceOrientation);

                OnDisappearanceSectionGUI();

                EditorGUI.indentLevel--;
            }

            //Draws parent without the children (so, TanimPlayerBase can have a custom inspector)
            DrawPropertiesExcluding(serializedObject, propertiesToExclude);


            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }

        }

        protected virtual void OnTypewriterSectionGUI()
        {

        }

        protected virtual void OnDisappearanceSectionGUI()
        {

        }
    }
}