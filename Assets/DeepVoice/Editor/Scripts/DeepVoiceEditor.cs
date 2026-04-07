using UnityEditor;
using UnityEngine;
using System.Text;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using Unity.EditorCoroutines.Editor;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;



namespace AiKodexDeepVoice
{
    public class DeepVoiceEditor : EditorWindow
    {
        string text = "";
        public enum Model
        {
            DeepVoice_Neural,
            DeepVoice_Mono,
            DeepVoice_Multi,
            DeepVoice_Standard
        };
        public static Model model = Model.DeepVoice_Mono;
        public enum Voice
        {
            Jessie, Harry, Glinda, Clyde, Callum, Charlotte, Dave, Fin, Freya, Batman, Andrew, Hailey, Arthur , Anime_Girl, Valentina,Wayne,Jan, Noah, Lily, Lily_Narrator, Ethan, Sophia, Olivia, Ruby, Lucas, John
        };
        public static Voice voice = Voice.Jessie;
        public enum StandardVoice
        {
            Lotte, Maxim, Salli, Geraint, Miguel, Giorgio, Marlene, Ines, Zhiyu, Zeina, Karl, Gwyneth, Lucia, Cristiano, Astrid, Vicki, Mia, Vitoria, Bianca, Chantal, Raveena, Russell, Aditi, Dora, Enrique, Hans, Carmen, Ewa, Maja, Nicole, Filiz, Camila, Jacek, Celine, Ricardo, Mads, Mathieu, Lea, Tatyana, Penelope, Naja, Ruben, Takumi, Mizuki, Carla, Conchita, Jan, Liv, Lupe, Seoyeon
        }
        public static StandardVoice standardVoice = StandardVoice.Lotte;
        public enum NeuralVoice
        {
            Olivia, Emma, Amy, Brian, Arthur, Kajal, Aria, Ayanda, Salli, Kimberly, Kendra, Joanna, Ivy, Ruth, Kevin, Matthew, Justin, Joey, Stephen
        }
        public static NeuralVoice neuralVoice = NeuralVoice.Olivia;
        float variability = 0.3f, clarity = 0.75f;
        private bool initDone = false;
        private GUIStyle StatesLabel, styleError;
        public static bool running = false;
        private Vector2 mainScroll;
        string responseFromServer;
        float postProgress;
        bool postFlag;
        bool autoPath = true;
        string _directoryPath, fileName, bodyName = "Voice", voiceName = Voice.Noah.ToString(), take = "0";
        UnityEngine.Object currentAudioClip, lastAudioClip;
        Texture2D audioWaveForm, disabledWaveForm, audioSlider, previewClip;
        float scrubber = 0;
        bool updateScrubber;
        Texture button_play, button_pause, button_stop;
        float editorDeltaTime = 0f, lastTimeSinceStartup = 0f;
        bool fileExists;
        bool foldTrimmer = false, foldJoiner = false, foldEqualizer = false;
        AudioClip clipToTrim;
        float trimMin, trimMax;
        string trimmedClipFileName;
        bool trimFileExists;
        string combinedClipFileName;
        string invoice;
        bool combineFileExists;
        [SerializeField]
        List<AudioClip> audioJoinList = new List<AudioClip>();
        AudioClip clipToEqualize;
        float volume, pitch;
        float[] bandFreqs = { 100f, 230f, 910f, 3600f, 14000f, 18000f };
        List<float> gains = new List<float> { 0.7f, 0.7f, 0.7f, 0.7f, 0.7f, 0.7f };
        string equalizedClipFileName;
        bool equalizeFileExists;
        bool previewVoices = false, previewNeuralVoices = false, previewMonoMultiVoices = false, previewStandardVoices = false;
        int selGridNV = -1, selGridMV = -1, selGridSV = -1;
        int lastSelGridNV = -1, lastSelGridMV = -1, lastSelGridSV = -1;
        string[] previewNeuralVoicesString = { "Olivia", "Emma", "Amy", "Brian", "Arthur", "Kajal", "Aria", "Ayanda", "Salli", "Kimberly", "Kendra", "Joanna", "Ivy", "Ruth", "Kevin", "Matthew", "Justin", "Joey", "Stephen" };
        string[] previewMonoMultiVoicesString = { "Jessie","Harry","Glinda","Clyde","Freya","Fin","Dave","Charlotte","Callum", "Batman", "Andrew","Hailey","Arthur", "Anime_Girl","Valentina","Wayne","Jan", "Noah", "Lily", "Lily_Narrator", "Ethan", "Sophia", "Olivia", "Ruby", "Lucas", "John" };
        string[] previewStandardVoicesString = { "Lotte", "Maxim", "Salli", "Geraint", "Miguel", "Giorgio", "Marlene", "Ines", "Zhiyu", "Zeina", "Karl", "Gwyneth", "Lucia", "Cristiano", "Astrid", "Vicki", "Mia", "Vitoria", "Bianca", "Chantal", "Raveena", "Russell", "Aditi", "Dora", "Enrique", "Hans", "Carmen", "Ewa", "Maja", "Nicole", "Filiz", "Camila", "Jacek", "Celine", "Ricardo", "Mads", "Mathieu", "Lea", "Tatyana", "Penelope", "Naja", "Ruben", "Takumi", "Mizuki", "Carla", "Conchita", "Jan", "Liv", "Lupe", "Seoyeon" };


        void InitStyles()
        {
            initDone = true;
            StatesLabel = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(),
                padding = new RectOffset(),
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
        }


        void Awake()
        {
#if UNITY_2022_1_OR_NEWER
            PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
#endif
            //Check all files names on startup to inform the user of a possible overwrite. 
            if (model == Model.DeepVoice_Standard)
                voiceName = standardVoice.ToString();
            else if (model == Model.DeepVoice_Neural)
                voiceName = neuralVoice.ToString();
            else if (model == Model.DeepVoice_Mono || model == Model.DeepVoice_Multi)
                voiceName = voice.ToString();
            fileName = $"{voiceName}_{bodyName}_{take}";
            fileExists = false;
            _directoryPath = "Assets/DeepVoice/Voices";
            var info = new DirectoryInfo(_directoryPath);
            var fileInfo = info.GetFiles();
            foreach (string file in System.IO.Directory.GetFiles(_directoryPath))
            {
                if ($"{_directoryPath}\\{fileName}.wav" == file.ToString())
                {
                    fileExists = true;
                }
            }
            invoice = PlayerPrefs.GetString("DeepVoice_Invoice");
        }
        // create menu item and window
        [MenuItem("Window/DeepVoice")]
        static void Init()
        {
            DeepVoiceEditor window = (DeepVoiceEditor)EditorWindow.GetWindow(typeof(DeepVoiceEditor));
            window.titleContent.text = "DeepVoice";
            window.minSize = new Vector2(350, 300);
            running = true;
        }
        void OnGUI()
        {
            mainScroll = EditorGUILayout.BeginScrollView(mainScroll);
            if (!initDone)
                InitStyles();
            GUIStyle style = new GUIStyle("WhiteLargeLabel");
            GUIStyle sectionTitle = style;
            GUIStyle subStyle = new GUIStyle("Label");
            subStyle.fontSize = 10;
            subStyle.normal.textColor = Color.white;
            sectionTitle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            GUIStyle headStyle = new GUIStyle("BoldLabel");
            headStyle.fontSize = 18;
            headStyle.normal.textColor = Color.white;
            EditorGUILayout.BeginHorizontal();
            Texture logo = (Texture)AssetDatabase.LoadAssetAtPath("Assets/DeepVoice/Editor/Resources/Logo.png", typeof(Texture));
            Texture infoToolTip = (Texture)AssetDatabase.LoadAssetAtPath("Assets/DeepVoice/Editor/Resources/Info.png", typeof(Texture));
            Texture2D disabledWaveForm = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/DeepVoice/Editor/Resources/DisabledWaveform.png", typeof(Texture));
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("            DeepVoice   ", headStyle);
            EditorGUILayout.LabelField("                Version 2.1.5", subStyle);
            EditorGUILayout.EndVertical();
            GUI.DrawTexture(new Rect(10, 3, 45, 45), logo, ScaleMode.StretchToFill, true, 10.0F);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
            GUILayout.BeginVertical("window");
            EditorGUILayout.LabelField("Voice Generator", sectionTitle);
            var tempCenter = GUILayoutUtility.GetLastRect().center.x;
            EditorGUILayout.Space(10);
            invoice = EditorGUILayout.TextField(new GUIContent("Invoice Number  ", infoToolTip, "Enter Invoice number. Invoice numbers start with \"IN\" and are 14 characters long. You can find them under Order History on the store. For a more detailed explaination, please refer to the documentation."), invoice);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("");
            if (GUILayout.Button("Verify", GUILayout.MaxWidth(48), GUILayout.MaxHeight(17)))
                this.StartCoroutine(Verify("http://50.19.203.25:5000/verify", "{\"invoice\":\"" + invoice + "\"}"));
            if (GUILayout.Button("Save", GUILayout.MaxWidth(48), GUILayout.MaxHeight(17)))
                SaveInvoice();
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Text", EditorStyles.boldLabel);
            EditorStyles.textArea.wordWrap = true;
            // Replacing newlines, quotes and backslashes that previously tipped off an error in generation
            text = Regex.Replace(text, @"\\(?!n|"")", "");
            text = Regex.Replace(text, "(?<!n)\n", "\\n");
            text = Regex.Replace(text, "(?<!\\\\)\"", "\\\"");
            text = EditorGUILayout.TextArea(text, EditorStyles.textArea, GUILayout.Height(80));
            EditorGUILayout.BeginHorizontal();
            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontSize = 10 };
            if (model == Model.DeepVoice_Mono)
                EditorGUILayout.LabelField($"Supports only English. Switch to Deep Voice_Multi for more langauges.", style, GUILayout.MaxWidth(500));
            else if (model == Model.DeepVoice_Multi)
                EditorGUILayout.LabelField($"Supports: EN, JA, DE, HI, FR, KO, PT, IT, ES, ID, NL, TR, FIL, PL, SV, BG, RO, AR, CS, EL, FI, HR, MS, SK, DA, TA, UK", style, GUILayout.MaxWidth(800));
            else
                EditorGUILayout.LabelField($"Supports only English. Switch to Deep Voice_Multi for more langauges.", style, GUILayout.MaxWidth(550));
            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
            styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
            styleError.normal.textColor = Color.red;
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (500 - text.Length >= 0)
                EditorGUILayout.LabelField($"{500 - text.Length} char", style, GUILayout.MaxWidth(80));
            else
                EditorGUILayout.LabelField($"{500 - text.Length} char", styleError);
            if (GUILayout.Button("Status", GUILayout.MaxWidth(48), GUILayout.MaxHeight(17)))
                this.StartCoroutine(Status(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cDovLzUwLjE5LjIwMy4yNTo1MDAwL3N0YXR1cw==")), "{\"invoice\":\"" + invoice + "\"}"));
            GUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Voice Model Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginChangeCheck();
            model = (Model)EditorGUILayout.EnumPopup(new GUIContent("Model", infoToolTip, "Select the text-to-speech (TTS) model file to use. The Multi and Mono models accept parameters such as variability and clarity to offer improved customization for the output. The standard and neural voices provide different English accents and intonations."), model);
            if (model == Model.DeepVoice_Standard)
                standardVoice = (StandardVoice)EditorGUILayout.EnumPopup(new GUIContent("Voice", infoToolTip, "Selects the Voice ID to use for the given model. Choose from a variety of different voices and find the best fit for your character."), standardVoice);
            else if (model == Model.DeepVoice_Neural)
                neuralVoice = (NeuralVoice)EditorGUILayout.EnumPopup(new GUIContent("Voice", infoToolTip, "Selects the Voice ID to use for the given model. Choose from a variety of different voices and find the best fit for your character."), neuralVoice);
            else if (model == Model.DeepVoice_Mono || model == Model.DeepVoice_Multi)
                voice = (Voice)EditorGUILayout.EnumPopup(new GUIContent("Voice", infoToolTip, "Selects the Voice ID to use for the given model. Choose from a variety of different voices and find the best fit for your character."), voice);
            if (EditorGUI.EndChangeCheck())
            {
                if (model == Model.DeepVoice_Standard)
                    voiceName = standardVoice.ToString();
                else if (model == Model.DeepVoice_Neural)
                    voiceName = neuralVoice.ToString();
                else if (model == Model.DeepVoice_Mono || model == Model.DeepVoice_Multi)
                    voiceName = voice.ToString();
                fileName = $"{voiceName}_{bodyName}_{take}";
            }
            EditorGUI.BeginDisabledGroup(model == Model.DeepVoice_Neural || model == Model.DeepVoice_Standard);
            variability = EditorGUILayout.Slider(new GUIContent("Variability", infoToolTip, "Sets a tone of the voice which allows for experimentation. Increasing variability can make speech more expressive with output varying between re-generations. However, it can also lead to instabilities."), variability, 0, 1);
            clarity = EditorGUILayout.Slider(new GUIContent("Clarity", infoToolTip, "High values boost overall voice clarity and target speaker similarity. Very high values can cause artifacts, so adjusting this setting to find the optimal value is encouraged."), clarity, 0, 1);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(10);
            previewVoices = FoldOuts.FoldOut("Preview Voices", previewVoices);
            if (previewVoices)
            {
                previewNeuralVoices = FoldOuts.FoldOut("Neural Voices", previewNeuralVoices);
                if (previewNeuralVoices)
                {
                    GUILayout.BeginHorizontal("Box");
                    selGridNV = GUILayout.SelectionGrid(selGridNV, previewNeuralVoicesString, 5, GUILayout.MinWidth(100));
                    if (selGridNV != lastSelGridNV)
                    {
                        StopAllClips();
                        PlayClip((AudioClip)AssetDatabase.LoadAssetAtPath("Assets/DeepVoice/Voices/Preview Voices/Neural/" + previewNeuralVoicesString[selGridNV] + ".wav", typeof(AudioClip)),0,false);
                        lastSelGridNV = selGridNV;
                    }
                    GUILayout.EndHorizontal();
                }
                previewMonoMultiVoices = FoldOuts.FoldOut("Mono/Multi Voices", previewMonoMultiVoices);
                if (previewMonoMultiVoices)
                {
                    GUILayout.BeginHorizontal("Box");
                    selGridMV = GUILayout.SelectionGrid(selGridMV, previewMonoMultiVoicesString, 5, GUILayout.MinWidth(100));
                    if (selGridMV != lastSelGridMV)
                    {
                        StopAllClips();
                        PlayClip((AudioClip)AssetDatabase.LoadAssetAtPath("Assets/DeepVoice/Voices/Preview Voices/MonoMulti/" + previewMonoMultiVoicesString[selGridMV]+ ".wav", typeof(AudioClip)),0,false);
                        lastSelGridMV = selGridMV;
                    }
                    GUILayout.EndHorizontal();
                }
                previewStandardVoices = FoldOuts.FoldOut("Standard Voices", previewStandardVoices);
                if (previewStandardVoices)
                {
                    GUILayout.BeginHorizontal("Box");
                    selGridSV = GUILayout.SelectionGrid(selGridSV, previewStandardVoicesString, 5, GUILayout.MinWidth(100));
                    if (selGridSV != lastSelGridSV)
                    {
                        StopAllClips();
                        PlayClip((AudioClip)AssetDatabase.LoadAssetAtPath("Assets/DeepVoice/Voices/Preview Voices/Standard/" + previewStandardVoicesString[selGridSV]+ ".wav", typeof(AudioClip)),0,false);
                        lastSelGridSV = selGridSV;
                    }
                    GUILayout.EndHorizontal();
                }


            }
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            voiceName = EditorGUILayout.TextField(new GUIContent("File Name", infoToolTip, "Automatically assigns the file name based on the selected voice. Additionally, increments the take field by +1 upon voice processing"), voiceName);
            bodyName = EditorGUILayout.TextField(bodyName);
            take = EditorGUILayout.TextField(take);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{voiceName}_{bodyName}_{take}.wav", style);
            fileName = $"{voiceName}_{bodyName}_{take}";
            if (EditorGUI.EndChangeCheck())
            {
                //Check all files for name existence

                fileExists = false;
                var info = new DirectoryInfo(_directoryPath);
                var fileInfo = info.GetFiles();
                foreach (string file in System.IO.Directory.GetFiles(_directoryPath))
                {
                    if ($"{_directoryPath}\\{fileName}.wav" == file.ToString())
                    {
                        fileExists = true;
                    }
                }
            }

            if (fileExists)
            {
                styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
                styleError.normal.textColor = Color.red;
                EditorGUILayout.LabelField(new GUIContent("[Overwrite Name]", infoToolTip, "This file name already exists. Clicking on generate will overwrite and replace the current file. Proceed with precaution."), styleError, GUILayout.Width(100));
            }
            else
            {
                styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
                styleError.normal.textColor = Color.green;
                EditorGUILayout.LabelField(new GUIContent("[Available Name]", infoToolTip, "This file name is available to use."), styleError, GUILayout.Width(100));
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(autoPath == true);
            if (autoPath)
                _directoryPath = EditorGUILayout.TextField("Voices Folder", "Assets/DeepVoice/Voices");
            else
                _directoryPath = EditorGUILayout.TextField("Voices Folder", _directoryPath);
            if (GUILayout.Button(". . /", GUILayout.MaxWidth(50)))
                _directoryPath = EditorUtility.OpenFolderPanel("", "", "");
            EditorGUI.EndDisabledGroup();
            autoPath = EditorGUILayout.ToggleLeft("Auto", autoPath, GUILayout.MaxWidth(50));

            EditorGUILayout.EndHorizontal();
            EditorGUI.BeginDisabledGroup(text == "");
            if (GUILayout.Button("Generate Voice", GUILayout.Height(30)))
            {
                postFlag = true;
                postProgress = 0;
                if (model == Model.DeepVoice_Mono || model == Model.DeepVoice_Multi)
                    this.StartCoroutine(Post("http://50.19.203.25:5000/invoice", "{\"text\":\"" + $"{text}" + "\",\"model\":\"" + $"{model}" + "\",\"invoice\":\"" + $"{invoice}" + "\",\"name\":\"" + $"{voice}" + "\",\"variability\":\"" + $"{variability}" + "\",\"clarity\":\"" + $"{clarity}" + "\"}"));
                else if (model == Model.DeepVoice_Standard)
                    this.StartCoroutine(Post("http://50.19.203.25:5000/invoice", "{\"text\":\"" + $"{text}" + "\",\"model\":\"" + $"{model}" + "\",\"invoice\":\"" + $"{invoice}" + "\",\"name\":\"" + $"{standardVoice}" + "\",\"variability\":\"" + "0.0" + "\",\"clarity\":\"" + "0.0" + "\"}"));
                else if (model == Model.DeepVoice_Neural)
                    this.StartCoroutine(Post("http://50.19.203.25:5000/invoice", "{\"text\":\"" + $"{text}" + "\",\"model\":\"" + $"{model}" + "\",\"invoice\":\"" + $"{invoice}" + "\",\"name\":\"" + $"{neuralVoice}" + "\",\"variability\":\"" + "0.0" + "\",\"clarity\":\"" + "0.0" + "\"}"));

            }
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(10);
            Rect loading = GUILayoutUtility.GetRect(9, 9);
            if (postFlag)
            {
                Repaint();
                EditorGUI.ProgressBar(loading, Mathf.Sqrt(++postProgress) * 0.009f, "");
            }
            GUILayout.EndVertical();
            GUILayout.Space(10);
            currentAudioClip = Selection.activeObject;
            EditorGUI.BeginDisabledGroup(Selection.activeObject == null || !Selection.activeObject.GetType().Equals(typeof(AudioClip)) || currentAudioClip == null);
            GUILayout.BeginVertical("window");
            EditorGUILayout.LabelField(new GUIContent("Preview", infoToolTip, "The preview section helps you preview the sound files without leaving this interface. To access it, single-click on a file in the project and hover over this panel. You will see this section enabled. Scrub the playhead to preview different sections of the audio."), sectionTitle);
            GUILayout.Space(100);


            if (Selection.activeObject != null && Selection.activeObject.GetType().Equals(typeof(AudioClip)) && lastAudioClip != currentAudioClip)
            {
                AudioClip sound = (AudioClip)Selection.activeObject;
                audioWaveForm = PaintWaveformSpectrum(sound, Screen.width / 4, 100, new Color(1, 0.55f, 0), false, 0);

                scrubber = 0;
                audioSlider = PaintWaveformSpectrum(sound, Screen.width / 4, 100, new Color(1, 1, 1), true, scrubber / sound.length);
            }

            lastAudioClip = currentAudioClip;


            if (Selection.activeObject != null && Selection.activeObject.GetType().Equals(typeof(AudioClip)) && currentAudioClip != null)
            {
                GUI.DrawTexture(new Rect(tempCenter-Screen.width * 0.3f, GUILayoutUtility.GetLastRect().y, Screen.width * 0.6f, 100), audioWaveForm, ScaleMode.StretchToFill, true, 1);
                GUI.DrawTexture(new Rect(tempCenter-Screen.width * 0.3f, GUILayoutUtility.GetLastRect().y, Screen.width * 0.6f, 100), audioSlider, ScaleMode.StretchToFill, true, 1);
                AudioClip sound = (AudioClip)Selection.activeObject;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(sound.name.ToString() + ", " + sound.frequency.ToString() + "Hz, " + sound.length.ToString().Substring(0, 4) + "s", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                this.button_play = (Texture)AssetDatabase.LoadAssetAtPath("Assets/DeepVoice/Editor/Resources/Play.png", typeof(Texture));
                this.button_pause = (Texture)AssetDatabase.LoadAssetAtPath("Assets/DeepVoice/Editor/Resources/Pause.png", typeof(Texture));
                this.button_stop = (Texture)AssetDatabase.LoadAssetAtPath("Assets/DeepVoice/Editor/Resources/Stop.png", typeof(Texture));

                if (GUILayout.Button(new GUIContent(button_play), GUILayout.Width(25), GUILayout.Height(25)))
                {
                    if (!updateScrubber)
                    {
                        PlayClip((AudioClip)currentAudioClip, Mathf.CeilToInt((scrubber / sound.length) * sound.samples), false);
                        if (Mathf.Approximately(scrubber, sound.length))
                            scrubber = 0;
                    }
                    updateScrubber = true;
                }
                if (GUILayout.Button(new GUIContent(button_pause), GUILayout.Width(25), GUILayout.Height(25)))
                {
                    StopAllClips();
                    updateScrubber = false;
                }
                if (GUILayout.Button(new GUIContent(button_stop), GUILayout.Width(25), GUILayout.Height(25)))
                {
                    StopAllClips();
                    updateScrubber = false;
                    scrubber = 0;
                    audioSlider = PaintWaveformSpectrum(sound, Screen.width / 4, 100, new Color(1, 1, 1), true, scrubber / sound.length);
                }
                if (!updateScrubber)
                    lastTimeSinceStartup = 0f;
                if (scrubber > sound.length)
                {
                    updateScrubber = false;
                }
                if (updateScrubber)
                {
                    if (lastTimeSinceStartup == 0f)
                        lastTimeSinceStartup = (float)EditorApplication.timeSinceStartup;
                    editorDeltaTime = (float)EditorApplication.timeSinceStartup - lastTimeSinceStartup;
                    lastTimeSinceStartup = (float)EditorApplication.timeSinceStartup;
                    scrubber += editorDeltaTime;
                    audioSlider = PaintWaveformSpectrum(sound, Screen.width / 4, 100, new Color(1, 1, 1), true, scrubber / sound.length);

                    //Prevent edge case for the sound clip looping if the scrubber is played while it is at the end of the playhead (~16000 samples)
                    if (sound.samples - Mathf.CeilToInt((scrubber / sound.length) * sound.samples) < 100)
                        StopAllClips();

                    Repaint();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

            }

            else
            {
                GUI.DrawTexture(new Rect(tempCenter-100, GUILayoutUtility.GetLastRect().y, 200, 100), disabledWaveForm, ScaleMode.ScaleToFit, true, 1.5f);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select an audio file from the project", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                this.button_play = (Texture)AssetDatabase.LoadAssetAtPath("Assets/DeepVoice/Editor/Resources/Play.png", typeof(Texture));
                this.button_pause = (Texture)AssetDatabase.LoadAssetAtPath("Assets/DeepVoice/Editor/Resources/Pause.png", typeof(Texture));
                this.button_stop = (Texture)AssetDatabase.LoadAssetAtPath("Assets/DeepVoice/Editor/Resources/Stop.png", typeof(Texture));
                GUILayout.Button(new GUIContent(button_play), GUILayout.Width(25), GUILayout.Height(25));
                GUILayout.Button(new GUIContent(button_pause), GUILayout.Width(25), GUILayout.Height(25));
                GUILayout.Button(new GUIContent(button_stop), GUILayout.Width(25), GUILayout.Height(25));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();


            }
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Playhead");

            if (Selection.activeObject != null && Selection.activeObject.GetType().Equals(typeof(AudioClip)))
            {
                AudioClip clip = (AudioClip)Selection.activeObject;
                EditorGUI.BeginChangeCheck();
                scrubber = EditorGUILayout.Slider(scrubber, 0, clip.length, GUILayout.Width(120));
                if (EditorGUI.EndChangeCheck())
                {
                    audioSlider = PaintWaveformSpectrum(clip, Screen.width / 4, 100, new Color(1, 1, 1), true, scrubber / clip.length);
                    updateScrubber = false;
                    StopAllClips();
                }
            }
            else
                scrubber = EditorGUILayout.Slider(scrubber, 0, 1, GUILayout.Width(120));
            GUILayout.Label("s");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.EndVertical();
            GUILayout.Space(10);
            EditorGUI.EndDisabledGroup();
            GUILayout.BeginVertical("window");
            EditorGUILayout.LabelField("Audio Utility", sectionTitle);
            GUILayout.Space(10);

            GUILayout.Space(10);

            foldTrimmer = FoldOuts.FoldOut("Audio Trimmer", foldTrimmer);
            if (foldTrimmer)
            {
                EditorGUI.BeginChangeCheck();
                GUILayout.BeginHorizontal();
                clipToTrim = (AudioClip)EditorGUILayout.ObjectField(new GUIContent("Clip To Trim", infoToolTip, "Select an audio file you wish to trim. Once selected, use the slider to cut portions of the audio. When satisfied, save the audio by entering a valid name for the audio file. Click on \"Active Clip\" button to select the clip active in the project. To remove the selection, simply click on the x button on the right side of the clip selection field."), clipToTrim, typeof(AudioClip), true);

                if (GUILayout.Button("Active Clip", GUILayout.MaxWidth(80)) && Selection.activeObject != null && Selection.activeObject.GetType().Equals(typeof(AudioClip)))
                {
                    clipToTrim = (AudioClip)Selection.activeObject;
                    trimMax = 1;
                }

                if (GUILayout.Button("×", EditorStyles.boldLabel, GUILayout.MaxWidth(20)))
                {
                    clipToTrim = null;
                    previewClip = null;
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(120);
                if (EditorGUI.EndChangeCheck() && clipToTrim != null)
                {
                    previewClip = PaintWaveformSpectrum(clipToTrim, Screen.width / 4, 100, new Color(1, 0.55f, 0), false, 0);
                }
                EditorGUI.BeginDisabledGroup(previewClip == null);
                if (previewClip != null)
                {
                    float trimLastRectY = GUILayoutUtility.GetLastRect().y + 20;
                    GUI.DrawTexture(new Rect(tempCenter - Screen.width * 0.3f, trimLastRectY, Screen.width * 0.6f, 100), previewClip, ScaleMode.StretchToFill, true, 1);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.MinMaxSlider(ref trimMin, ref trimMax, 0, 1, GUILayout.MaxWidth(Screen.width * 0.705f));
                    GUILayout.FlexibleSpace();
                    GUI.Box(new Rect(Screen.width * 0.04f, trimLastRectY, Screen.width * trimMin * 0.6f, 100), "");
                    GUI.Box(new Rect(Screen.width * trimMax * 0.6f + Screen.width * 0.04f, trimLastRectY, Screen.width * 0.6f * (1 - trimMax), 100), "");
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Start:");
                    EditorGUILayout.FloatField(trimMin * clipToTrim.length, GUILayout.MaxWidth(50));
                    GUILayout.Label("s");
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("End:");
                    EditorGUILayout.FloatField(trimMax * clipToTrim.length, GUILayout.MaxWidth(50));
                    GUILayout.Label("s");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent(button_play), GUILayout.Width(25), GUILayout.Height(25)))
                    {
                        AudioClip ac = AudioClip.Create("Temp", Mathf.CeilToInt(trimMax * clipToTrim.samples) - Mathf.CeilToInt(trimMin * clipToTrim.samples), clipToTrim.channels, clipToTrim.frequency, false);
                        float[] samples = new float[Mathf.CeilToInt(trimMax * clipToTrim.samples) - Mathf.CeilToInt(trimMin * clipToTrim.samples)];
                        clipToTrim.GetData(samples, Mathf.CeilToInt(trimMin * clipToTrim.samples));
                        ac.SetData(samples, 0);
                        if (!Directory.Exists(_directoryPath + "/Temp_data")) Directory.CreateDirectory(_directoryPath + "/Temp_data");
                        WaveUtils.Save("TempTrim", ac, _directoryPath + "/Temp_data", false);
                        AssetDatabase.Refresh();

                        AudioClip temp = (AudioClip)AssetDatabase.LoadAssetAtPath(_directoryPath + "/Temp_data/TempTrim.wav", typeof(AudioClip));
                        //System.Reflection cannot play audio samples stored in local variables hence the method above 
                        PlayClip(temp, 0, false);
                    }
                    if (GUILayout.Button(new GUIContent(button_stop), GUILayout.Width(25), GUILayout.Height(25)))
                    {
                        StopAllClips();
                    }
                    if (GUILayout.Button("Reset", GUILayout.Width(50), GUILayout.Height(25)))
                    {
                        trimMin = 0;
                        trimMax = 1;
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    EditorGUI.BeginChangeCheck();
                    trimmedClipFileName = EditorGUILayout.TextField("File Name", trimmedClipFileName);
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{trimmedClipFileName}.wav", style);
                    if (EditorGUI.EndChangeCheck())
                    {
                        //Check all files for name existence
                        fileName = $"{trimmedClipFileName}";

                        trimFileExists = false;
                        var info = new DirectoryInfo(_directoryPath);
                        var fileInfo = info.GetFiles();
                        foreach (string file in System.IO.Directory.GetFiles(_directoryPath))
                        {
                            if ($"{_directoryPath}\\{fileName}.wav" == file.ToString())
                            {
                                trimFileExists = true;
                            }
                        }
                    }

                    if (trimFileExists)
                    {
                        styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
                        styleError.normal.textColor = Color.red;
                        EditorGUILayout.LabelField($"[Overwrite Name]", styleError, GUILayout.Width(100));
                    }
                    else
                    {
                        styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
                        if (trimmedClipFileName == "" || trimmedClipFileName == null)
                        {
                            styleError.normal.textColor = Color.red;
                            EditorGUILayout.LabelField($"[Cannot be Empty]", styleError, GUILayout.Width(100));
                        }
                        else
                        {
                            styleError.normal.textColor = Color.green;
                            EditorGUILayout.LabelField($"[Available Name]", styleError, GUILayout.Width(100));
                        }
                    }
                    GUILayout.EndHorizontal();
                    EditorGUI.BeginDisabledGroup(trimmedClipFileName == "" || trimmedClipFileName == null);
                    if (GUILayout.Button("Save Trimmed Audio", GUILayout.Height(30)))
                    {
                        AudioClip ac = AudioClip.Create("Temp", Mathf.CeilToInt(trimMax * clipToTrim.samples) - Mathf.CeilToInt(trimMin * clipToTrim.samples), clipToTrim.channels, clipToTrim.frequency, false);
                        float[] samples = new float[Mathf.CeilToInt(trimMax * clipToTrim.samples) - Mathf.CeilToInt(trimMin * clipToTrim.samples)];
                        clipToTrim.GetData(samples, Mathf.CeilToInt(trimMin * clipToTrim.samples));
                        ac.SetData(samples, 0);
                        WaveUtils.Save(trimmedClipFileName, ac, _directoryPath, false);
                        AssetDatabase.Refresh();
                    }
                    EditorGUI.EndDisabledGroup();
                    GUILayout.Space(10);

                }
                else
                {
                    GUI.DrawTexture(new Rect(tempCenter-Screen.width * 0.25f, GUILayoutUtility.GetLastRect().y + 20, Screen.width * 0.5f, 100), disabledWaveForm, ScaleMode.ScaleToFit, true, 1.5f);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.MinMaxSlider(ref trimMin, ref trimMax, 0, 1, GUILayout.MaxWidth(Screen.width * 0.725f));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Start:");
                    EditorGUILayout.FloatField(trimMin * 0, GUILayout.MaxWidth(50));
                    GUILayout.Label("s");
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("End:");
                    EditorGUILayout.FloatField(trimMax * 1, GUILayout.MaxWidth(50));
                    GUILayout.Label("s");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Button(new GUIContent(button_play), GUILayout.Width(25), GUILayout.Height(25));
                    GUILayout.Button(new GUIContent(button_stop), GUILayout.Width(25), GUILayout.Height(25));
                    GUILayout.Button("Reset", GUILayout.Width(50), GUILayout.Height(25));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.Button("Save Trimmed Audio", GUILayout.Height(30));
                }

                EditorGUI.EndDisabledGroup();

            }
            foldJoiner = FoldOuts.FoldOut("Audio Joiner", foldJoiner);
            if (foldJoiner)
            {
                string[] selectionOfAudioClips = Selection.assetGUIDs;
                EditorGUI.BeginDisabledGroup(selectionOfAudioClips.Length < 2);
                ScriptableObject target = this;
                SerializedObject so = new SerializedObject(target);
                SerializedProperty stringsProperty = so.FindProperty("audioJoinList");
                GUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(stringsProperty, new GUIContent("Audio Clips to Join", infoToolTip, "Select two or more audio files you wish to combine. Select the audio files from the project and click on \"Set Selected\" to auto populate the queue with the selected files. Please note that you cannot manually assign clips using the editor, you may only use the Set Selected Button to assign clips in this version of the asset. You can rearrange the audio clips in the hierarchy by dragging the clips. Once satisfied with the arrangement of the clips, enter a suitable name and save the file. You can clear the queue using the x button on the right of the Set Selected Button."), true, GUILayout.MaxWidth(300));
                so.ApplyModifiedProperties();
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();

                if (GUILayout.Button(new GUIContent("Set Selected", null, "Select two or more clips in the project to enable button")))
                {
                    audioJoinList.Clear();
                    for (int i = 0; i < selectionOfAudioClips.Length; i++)
                    {
                        audioJoinList.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(selectionOfAudioClips[i])));
                    }

                }
                EditorGUI.EndDisabledGroup();
                bool clearList = false;
                if (GUILayout.Button("×", EditorStyles.boldLabel, GUILayout.MaxWidth(20)))
                {
                    clearList = true;
                }
                GUILayout.EndHorizontal();
                bool anyClipNull = false;
                float combinedTime = 0;
                for (int i = 0; i < audioJoinList.Count; i++)
                {
                    if (audioJoinList[i] == null)
                        anyClipNull = true;
                }
                if (!anyClipNull)
                {
                    for (int i = 0; i < audioJoinList.Count; i++)
                    {
                        combinedTime += audioJoinList[i].length;
                        GUILayout.Label(audioJoinList[i].length.ToString() + "s", GUILayout.Height(18));
                    }
                }
                EditorGUI.BeginDisabledGroup(stringsProperty.arraySize == 0);
                GUILayout.Label(combinedTime.ToString().Length > 5 ? combinedTime.ToString().Substring(0, 5) + "s [Total]" : "Total Time", EditorStyles.boldLabel, GUILayout.Height(18));
                EditorGUI.EndDisabledGroup();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                if (clearList)
                    audioJoinList.Clear();

                EditorGUI.BeginDisabledGroup(audioJoinList.Count < 2 || anyClipNull);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(button_play), GUILayout.Width(25), GUILayout.Height(25)))
                {
                    int totalSamplesCount = 0;

                    for (int i = 0; i < audioJoinList.Count; i++)
                        totalSamplesCount += audioJoinList[i].samples;


                    AudioClip ac = AudioClip.Create("Temp", totalSamplesCount, audioJoinList[0].channels, audioJoinList[0].frequency, false);

                    float[] concatenatedSamples = audioJoinList.Select(audioClip =>
                    {
                        float[] samples = new float[audioClip.samples * audioClip.channels];
                        audioClip.GetData(samples, 0);
                        return samples;
                    })
                    .SelectMany(x => x)
                    .ToArray();

                    ac.SetData(concatenatedSamples, 0);
                    if (!Directory.Exists(_directoryPath + "/Temp_data")) Directory.CreateDirectory(_directoryPath + "/Temp_data");
                    WaveUtils.Save("TempCombine", ac, _directoryPath + "/Temp_data", false);
                    AssetDatabase.Refresh();

                    AudioClip temp = (AudioClip)AssetDatabase.LoadAssetAtPath(_directoryPath + "/Temp_data/TempCombine.wav", typeof(AudioClip));
                    PlayClip(temp, 0, false);

                }
                if (GUILayout.Button(new GUIContent(button_stop), GUILayout.Width(25), GUILayout.Height(25)))
                {
                    StopAllClips();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();


                EditorGUI.BeginChangeCheck();
                combinedClipFileName = EditorGUILayout.TextField("File Name", combinedClipFileName);
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{combinedClipFileName}.wav", style);
                if (EditorGUI.EndChangeCheck())
                {
                    //Check all files for name existence
                    fileName = $"{combinedClipFileName}";

                    combineFileExists = false;
                    var info = new DirectoryInfo(_directoryPath);
                    var fileInfo = info.GetFiles();
                    foreach (string file in System.IO.Directory.GetFiles(_directoryPath))
                    {
                        if ($"{_directoryPath}\\{fileName}.wav" == file.ToString())
                        {
                            combineFileExists = true;
                        }
                    }
                }

                if (combineFileExists)
                {
                    styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
                    styleError.normal.textColor = Color.red;
                    EditorGUILayout.LabelField($"[Overwrite Name]", styleError, GUILayout.Width(100));
                }
                else
                {
                    styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
                    if (combinedClipFileName == "" || combinedClipFileName == null)
                    {
                        styleError.normal.textColor = Color.red;
                        EditorGUILayout.LabelField($"[Cannot be Empty]", styleError, GUILayout.Width(100));
                    }
                    else
                    {
                        styleError.normal.textColor = Color.green;
                        EditorGUILayout.LabelField($"[Available Name]", styleError, GUILayout.Width(100));
                    }
                }
                GUILayout.EndHorizontal();



                EditorGUI.BeginDisabledGroup(combinedClipFileName == "" || combinedClipFileName == null);
                if (GUILayout.Button("Save Combined Audio", GUILayout.Height(30)))
                {
                    int totalSamplesCount = 0;

                    for (int i = 0; i < audioJoinList.Count; i++)
                        totalSamplesCount += audioJoinList[i].samples;


                    AudioClip ac = AudioClip.Create("Temp", totalSamplesCount, audioJoinList[0].channels, audioJoinList[0].frequency, false);

                    float[] concatenatedSamples = audioJoinList.Select(audioClip =>
                    {
                        float[] samples = new float[audioClip.samples * audioClip.channels];
                        audioClip.GetData(samples, 0);
                        return samples;
                    })
                    .SelectMany(x => x)
                    .ToArray();

                    ac.SetData(concatenatedSamples, 0);
                    WaveUtils.Save(combinedClipFileName, ac, _directoryPath, false);
                    AssetDatabase.Refresh();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndDisabledGroup();
                GUILayout.Space(10);

            }


            foldEqualizer = FoldOuts.FoldOut("Audio Equalizer", foldEqualizer);
            if (foldEqualizer)
            {
                GUILayout.BeginHorizontal();
                clipToEqualize = (AudioClip)EditorGUILayout.ObjectField(new GUIContent("Clip to Equalize", infoToolTip, "Select an audio file you wish to equalize. You can adjust the sliders to make the voice loud, low, bassy or shrill. Once satisfied with the changes, enter a suitable name and save the file. You can reset the settings for the equalizer using the reset button."), clipToEqualize, typeof(AudioClip), true);

                if (GUILayout.Button("Active Clip", GUILayout.MaxWidth(80)) && Selection.activeObject != null && Selection.activeObject.GetType().Equals(typeof(AudioClip)))
                {
                    clipToEqualize = (AudioClip)Selection.activeObject;
                }

                if (GUILayout.Button("×", EditorStyles.boldLabel, GUILayout.MaxWidth(20)))
                {
                    clipToEqualize = null;
                }

                GUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(clipToEqualize == null);
                volume = EditorGUILayout.Slider("Gain Volume", volume, -10, 10, GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField("dB", style, GUILayout.MaxWidth(20));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                pitch = EditorGUILayout.Slider("Pitch", pitch, -12, 12);
                EditorGUILayout.LabelField("ST", style, GUILayout.MaxWidth(20));
                EditorGUILayout.EndHorizontal();
                sectionTitle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Parametric EQ", sectionTitle);
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                for (int i = 0; i < 6; i++)
                {
                    GUILayout.BeginVertical();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(" ", GUILayout.MaxWidth((bandFreqs[i].ToString().Length - 2) * (bandFreqs[i].ToString().Length - 1))); // Done for allignment purposes
                    gains[5 - i] = GUILayout.VerticalSlider(gains[5 - i], 0.2f, 1.2f, GUILayout.Height(100));
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Label(bandFreqs[i].ToString());
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(button_play), GUILayout.Width(25), GUILayout.Height(25)))
                {

                    float[] samples = new float[clipToEqualize.samples * clipToEqualize.channels];
                    clipToEqualize.GetData(samples, 0);

                    int numBands = 6;
                    float[] bandQs = { 1f, 1f, 1f, 1f, 1f, 1f };

                    for (int i = 0; i < numBands; i++)
                    {
                        float freq = bandFreqs[i];
                        float q = bandQs[i];
                        float gain = gains[i];

                        float w0 = 2 * Mathf.PI * freq / clipToEqualize.frequency;
                        float alpha = Mathf.Sin(w0) / (2 * q);

                        float b0 = 1 + alpha * gain;
                        float b1 = -2 * Mathf.Cos(w0);
                        float b2 = 1 - alpha * gain;
                        float a0 = 1 + alpha / gain;
                        float a1 = -2 * Mathf.Cos(w0);
                        float a2 = 1 - alpha / gain;

                        float x1 = 0;
                        float x2 = 0;
                        float y1 = 0;
                        float y2 = 0;

                        for (int j = 0; j < samples.Length; j++)
                        {
                            float x0 = samples[j];
                            float y0 = (b0 / a0) * x0 + (b1 / a0) * x1 + (b2 / a0) * x2
                                       - (a1 / a0) * y1 - (a2 / a0) * y2;

                            x2 = x1;
                            x1 = x0;
                            y2 = y1;
                            y1 = y0;

                            samples[j] = y0;
                        }
                    }

                    for (int i = 0; i < samples.Length; i++)
                    {
                        samples[i] *= Mathf.Pow(10, volume / 20); //Decibel Conversion
                    }

                    // Apply the pitch shift to the sample data
                    float[] pitchedSamples = new float[Mathf.CeilToInt(samples.Length * Mathf.Pow(2, -pitch / 12))];
                    for (int i = 0; i < pitchedSamples.Length; i++)
                    {
                        float oldIndex = (float)i / Mathf.Pow(2, -pitch / 12); //Semitone Conversion
                        int index = Mathf.FloorToInt(oldIndex);
                        float t = oldIndex - index;

                        if (index >= samples.Length - 1)
                        {
                            pitchedSamples[i] = samples[samples.Length - 1];
                        }
                        else
                        {
                            pitchedSamples[i] = Mathf.Lerp(samples[index], samples[index + 1], t);
                        }
                    }


                    AudioClip ac = AudioClip.Create("Temp", pitchedSamples.Length, clipToEqualize.channels, clipToEqualize.frequency, false);
                    ac.SetData(pitchedSamples, 0);
                    if (!Directory.Exists(_directoryPath + "/Temp_data")) Directory.CreateDirectory(_directoryPath + "/Temp_data");
                    WaveUtils.Save("TempEqualize", ac, _directoryPath + "/Temp_data", false);
                    AssetDatabase.Refresh();
                    AudioClip temp = (AudioClip)AssetDatabase.LoadAssetAtPath(_directoryPath + "/Temp_data/TempEqualize.wav", typeof(AudioClip));
                    PlayClip(temp, 0, false);

                }
                if (GUILayout.Button(new GUIContent(button_stop), GUILayout.Width(25), GUILayout.Height(25)))
                {
                    StopAllClips();
                }
                if (GUILayout.Button("Reset", GUILayout.Width(50), GUILayout.Height(25)))
                {
                    volume = 0;
                    pitch = 0;
                    for (int i = 0; i < 6; i++)
                        gains[i] = 1;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();


                EditorGUI.BeginChangeCheck();
                equalizedClipFileName = EditorGUILayout.TextField("File Name", equalizedClipFileName);
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{equalizedClipFileName}.wav", style);
                if (EditorGUI.EndChangeCheck())
                {
                    //Check all files for name existence
                    fileName = $"{equalizedClipFileName}";

                    equalizeFileExists = false;
                    var info = new DirectoryInfo(_directoryPath);
                    var fileInfo = info.GetFiles();
                    foreach (string file in System.IO.Directory.GetFiles(_directoryPath))
                    {
                        if ($"{_directoryPath}\\{fileName}.wav" == file.ToString())
                        {
                            equalizeFileExists = true;
                        }
                    }
                }

                if (equalizeFileExists)
                {
                    styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
                    styleError.normal.textColor = Color.red;
                    EditorGUILayout.LabelField($"[Overwrite Name]", styleError, GUILayout.Width(100));
                }
                else
                {
                    styleError = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 10 };
                    if (equalizedClipFileName == "" || equalizedClipFileName == null)
                    {
                        styleError.normal.textColor = Color.red;
                        EditorGUILayout.LabelField($"[Cannot be Empty]", styleError, GUILayout.Width(100));
                    }
                    else
                    {
                        styleError.normal.textColor = Color.green;
                        EditorGUILayout.LabelField($"[Available Name]", styleError, GUILayout.Width(100));
                    }
                }
                GUILayout.EndHorizontal();



                EditorGUI.BeginDisabledGroup(equalizedClipFileName == "" || equalizedClipFileName == null);
                if (GUILayout.Button("Save Equalized Audio", GUILayout.Height(30)))
                {
                    float[] samples = new float[clipToEqualize.samples * clipToEqualize.channels];
                    clipToEqualize.GetData(samples, 0);

                    int numBands = 6;
                    float[] bandQs = { 1f, 1f, 1f, 1f, 1f, 1f };

                    for (int i = 0; i < numBands; i++)
                    {
                        float freq = bandFreqs[i];
                        float q = bandQs[i];
                        float gain = gains[i];

                        float w0 = 2 * Mathf.PI * freq / clipToEqualize.frequency;
                        float alpha = Mathf.Sin(w0) / (2 * q);

                        float b0 = 1 + alpha * gain;
                        float b1 = -2 * Mathf.Cos(w0);
                        float b2 = 1 - alpha * gain;
                        float a0 = 1 + alpha / gain;
                        float a1 = -2 * Mathf.Cos(w0);
                        float a2 = 1 - alpha / gain;

                        float x1 = 0;
                        float x2 = 0;
                        float y1 = 0;
                        float y2 = 0;

                        for (int j = 0; j < samples.Length; j++)
                        {
                            float x0 = samples[j];
                            float y0 = (b0 / a0) * x0 + (b1 / a0) * x1 + (b2 / a0) * x2
                                       - (a1 / a0) * y1 - (a2 / a0) * y2;

                            x2 = x1;
                            x1 = x0;
                            y2 = y1;
                            y1 = y0;

                            samples[j] = y0;
                        }
                    }

                    for (int i = 0; i < samples.Length; i++)
                    {
                        samples[i] *= Mathf.Pow(10, volume / 20); //Decibel Conversion
                    }

                    // Apply the pitch shift to the sample data
                    float[] pitchedSamples = new float[Mathf.CeilToInt(samples.Length * Mathf.Pow(2, -pitch / 12))];
                    for (int i = 0; i < pitchedSamples.Length; i++)
                    {
                        float oldIndex = (float)i / Mathf.Pow(2, -pitch / 12); //Semitone Conversion
                        int index = Mathf.FloorToInt(oldIndex);
                        float t = oldIndex - index;

                        if (index >= samples.Length - 1)
                        {
                            pitchedSamples[i] = samples[samples.Length - 1];
                        }
                        else
                        {
                            pitchedSamples[i] = Mathf.Lerp(samples[index], samples[index + 1], t);
                        }
                    }


                    AudioClip ac = AudioClip.Create("Temp", pitchedSamples.Length, clipToEqualize.channels, clipToEqualize.frequency, false);
                    ac.SetData(pitchedSamples, 0);
                    WaveUtils.Save(equalizedClipFileName, ac, _directoryPath, false);
                    AssetDatabase.Refresh();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndDisabledGroup();
                GUILayout.Space(10);

            }
            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();



        }

        private void SaveInvoice()
        {
            PlayerPrefs.SetString("DeepVoice_Invoice",invoice);
        }

        public Texture2D PaintWaveformSpectrum(AudioClip audio, int width, int height, Color col, bool slider, float sliderValue)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            float[] samples = new float[audio.samples];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);
            int packSize = (audio.samples / width) + 1;
            int s = 0;
            for (int i = 0; i < audio.samples; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[i]);
                s++;
            }


            for (int i = 1; i < waveform.Length; i++)
            {
                var start = (i - 2 > 0 ? i - 2 : 0);
                var end = (i + 2 < waveform.Length ? i + 2 : waveform.Length);

                float sum = 0;

                for (int j = start; j < end; j++)
                {
                    sum += waveform[j];
                }

                var avg = sum / (end - start);
                waveform[i] = avg;

            }


            //Transparent BG
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
            if (!slider)
            {
                for (int x = 0; x < waveform.Length; x = x + 2)
                {
                    for (int y = 0; y <= waveform[x] * height; y++)
                    {
                        tex.SetPixel(x, (height / 2) + y, col);
                        tex.SetPixel(x, (height / 2) - y, col);
                    }
                }
            }
            else
            {
                for (int x = 0; x < waveform.Length; x = x + 2)
                {
                    for (int y = 0; y <= waveform[x] * height; y++)
                    {
                        if (x < waveform.Length * sliderValue)
                        {
                            tex.SetPixel(x, (height / 2) + y, col);
                            tex.SetPixel(x, (height / 2) - y, col);
                        }
                    }
                }
            }
            tex.Apply();

            return tex;
        }

        public static void PlayClip(AudioClip clip, int startSample = 0, bool loop = false)
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod(
                "PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null
            );

            method.Invoke(
                null,
                new object[] { clip, startSample, loop }
            );
        }

        public static void StopAllClips()
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod(
                "StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { },
                null
            );

            method.Invoke(
                null,
                new object[] { }
            );
        }


        IEnumerator Post(string url, string bodyJsonString)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            postProgress = 1;
            postFlag = false;
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("There was an error in generating the voice. Please check your invoice/order number and try again or check the documentation for more information.");
                if(request.responseCode == 400)
                {
                    Debug.Log("Error in text field: Please check your prompt for quotes (\"\") and line breaks at the end of the prompt. There could also be special formatting in your text. Please remove any special formatting by pasting as plain text in a notepad and then pasting the text here. Inclusion of any special formatting or illegal characters will result in an error such as this. For best results, please use a combination of letters, periods and commas and make sure there are no line breaks in between or at the end. If you must use quotes or line breaks, please prepend them with a backslash. Please do not press enter in the text field before clicking on generate.");
                }
            }
            else
            {
                if(request.responseCode == 400)
                {
                    Debug.Log("Error in text field: Please check your prompt for quotes (\"\") and line breaks at the end of the prompt. There could also be special formatting in your text. Please remove any special formatting by pasting as plain text in a notepad and then pasting the text here. Inclusion of any special formatting or illegal characters will result in an error such as this. For best results, please use a combination of letters, periods and commas and make sure there are no line breaks in between or at the end. If you must use quotes or line breaks, please prepend them with a backslash. Please do not press enter in the text field before clicking on generate.");
                }
                if (request.downloadHandler.text == "Invalid Response")
                    Debug.Log("Invalid Invoice/Order Number. Please check your invoice/order number and try again.");
                else if(request.downloadHandler.text == "Limit Reached")
                    Debug.Log("It seems that you may have reached the limit. To check your character usage, please click on the Status button. Please wait until 30th/31st of the month to get a renewed character count. Thank you for using DeepVoice.");
                else
                {
                    byte[] soundBytes = System.Convert.FromBase64String(request.downloadHandler.text);
                    File.WriteAllBytes($"{_directoryPath}/{fileName}.mp3", soundBytes);
                    AssetDatabase.Refresh();
                    AudioClip audioFile = (AudioClip)AssetDatabase.LoadMainAssetAtPath($"{_directoryPath}/{fileName}.mp3");
                    WaveUtils.Save($"{fileName}", audioFile, _directoryPath, false);
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath($"{_directoryPath}/{fileName}.wav");
                    File.Delete($"{_directoryPath}/{fileName}.mp3");
                    File.Delete($"{_directoryPath}/{fileName}.mp3.meta");
                    AssetDatabase.Refresh();
                    Selection.activeObject = (AudioClip)AssetDatabase.LoadMainAssetAtPath($"{_directoryPath}/{fileName}.wav");
                    Debug.Log($"<color=green>Inference Successful: </color>Please find the audio clip named {fileName} in {_directoryPath}");
                    take = (int.Parse(take) + 1).ToString();
                }
            }

            request.Dispose();
        }
        IEnumerator Status(string url, string bodyJsonString)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            postProgress = 1;
            postFlag = false;
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
            }
            else
            {
                if (request.downloadHandler.text == "Invalid Invoice Number")
                    Debug.Log("You do not have any generations or your invoice/order number is invalid. Please click on verify to verify your purchase.");
                else
                    Debug.Log("You have used " + request.downloadHandler.text + " characters.");
            }

            request.Dispose();
        }
        IEnumerator Verify(string url, string bodyJsonString)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            postProgress = 1;
            postFlag = false;
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
            }
            else
            {
                if (request.downloadHandler.text == "Not Verified")
                    Debug.Log("Invoice/Order number verification unsuccessful. Please check your invoice/order number and try again or contact the publisher on the email given in the documentation.");
                else
                {
                    if(invoice.Length == 13)
                        Debug.Log("Your order number is verified, however, please use your invoice number to access the voice generator. Your invoice number typically gets generated in 2-4 hours after your purchase. It will be listed beside your Order number. An invoice number looks like IN010X0XXXXXXX, a 14 digit number you can find on the \"My Orders\" page on the asset store page.");
                    else
                    Debug.Log("Your invoice is verified. Thank you for choosing DeepVoice!");
                }
            }
            request.Dispose();
        }
    }
}
