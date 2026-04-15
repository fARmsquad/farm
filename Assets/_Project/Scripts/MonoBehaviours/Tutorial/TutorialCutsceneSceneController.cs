using FarmSimVR.Core.Story;
using FarmSimVR.MonoBehaviours.Cinematics;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    /// <summary>
    /// Displays a placeholder cutscene with a title and body text,
    /// then auto-advances to the next tutorial scene after a delay.
    /// </summary>
    public sealed class TutorialCutsceneSceneController : MonoBehaviour
    {
        private string _title;
        private string _body;
        private float _autoAdvanceDelay;
        private float _advanceAt = -1f;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _subtitleStyle;
        private bool _stylesReady;
        private StoryStoryboardShotSnapshot[] _storyboardShots = System.Array.Empty<StoryStoryboardShotSnapshot>();
        private RuntimeCutsceneShotHandle[] _runtimeShots = System.Array.Empty<RuntimeCutsceneShotHandle>();
        private string _currentSubtitle;
        private Texture2D _currentImage;
        private AudioSource _audioSource;
        private int _currentShotIndex = -1;
        private bool _completionHandled;

        public void Configure(string title, string body, float autoAdvanceDelay)
        {
            _title = title;
            _body = body;
            _autoAdvanceDelay = autoAdvanceDelay;
            _storyboardShots = System.Array.Empty<StoryStoryboardShotSnapshot>();
            _runtimeShots = System.Array.Empty<RuntimeCutsceneShotHandle>();
            _currentShotIndex = -1;
            _currentSubtitle = string.Empty;
            _currentImage = null;
            _advanceAt = -1f;
            _completionHandled = false;
        }

        public void ConfigureStoryboard(string title, StoryStoryboardShotSnapshot[] shots, float autoAdvanceDelay)
        {
            _title = title;
            _body = string.Empty;
            _autoAdvanceDelay = autoAdvanceDelay;
            _storyboardShots = shots ?? System.Array.Empty<StoryStoryboardShotSnapshot>();
            _runtimeShots = System.Array.Empty<RuntimeCutsceneShotHandle>();
            _currentShotIndex = -1;
            _currentSubtitle = string.Empty;
            _currentImage = null;
            _advanceAt = -1f;
            _completionHandled = false;
        }

        public void ConfigureRuntimeStoryboard(string title, RuntimeCutsceneShotHandle[] shots, float autoAdvanceDelay)
        {
            _title = title;
            _body = string.Empty;
            _autoAdvanceDelay = autoAdvanceDelay;
            _storyboardShots = System.Array.Empty<StoryStoryboardShotSnapshot>();
            _runtimeShots = shots ?? System.Array.Empty<RuntimeCutsceneShotHandle>();
            _currentShotIndex = -1;
            _currentSubtitle = string.Empty;
            _currentImage = null;
            _advanceAt = -1f;
            _completionHandled = false;
        }

        public void FastCompleteForDev()
        {
            if (_audioSource != null)
                _audioSource.Stop();

            CompleteScene();
        }

        private void Start()
        {
            if (HasStoryboard())
            {
                ResolveAudioSource();
                BeginStoryboardShot(0);
                return;
            }

            if (_autoAdvanceDelay > 0f)
                _advanceAt = Time.time + _autoAdvanceDelay;
        }

        private void Update()
        {
            if (HasStoryboard())
            {
                UpdateStoryboardPlayback();
                return;
            }

            if (_advanceAt < 0f || Time.time < _advanceAt)
                return;

            _advanceAt = -1f;
            CompleteScene();
        }

        private void OnGUI()
        {
            BuildStyles();

            if (HasStoryboard())
            {
                DrawStoryboard();
                return;
            }

            DrawTextCard();
        }

        private bool HasStoryboard()
        {
            return (_runtimeShots != null && _runtimeShots.Length > 0) ||
                   (_storyboardShots != null && _storyboardShots.Length > 0);
        }

        private void UpdateStoryboardPlayback()
        {
            if (_advanceAt < 0f || Time.time < _advanceAt)
                return;

            var nextIndex = _currentShotIndex + 1;
            var shotCount = _runtimeShots != null && _runtimeShots.Length > 0
                ? _runtimeShots.Length
                : (_storyboardShots != null ? _storyboardShots.Length : 0);
            if (nextIndex >= shotCount)
            {
                CompleteScene();
                return;
            }

            BeginStoryboardShot(nextIndex);
        }

        private void BeginStoryboardShot(int index)
        {
            _currentShotIndex = index;
            if (_runtimeShots != null && index >= 0 && index < _runtimeShots.Length)
            {
                var runtimeShot = _runtimeShots[index];
                _currentSubtitle = runtimeShot?.SubtitleText ?? string.Empty;
                _currentImage = runtimeShot?.Image;
                var runtimeAudioClip = runtimeShot?.AudioClip;
                PlayAudio(runtimeAudioClip);
                var runtimeShotDuration = runtimeShot == null
                    ? 0f
                    : Mathf.Max(runtimeShot.DurationSeconds, runtimeAudioClip != null ? runtimeAudioClip.length : 0f);
                _advanceAt = Time.time + Mathf.Max(runtimeShotDuration, 0.1f);
                return;
            }

            if (_storyboardShots == null || index < 0 || index >= _storyboardShots.Length)
                return;

            var shot = _storyboardShots[index];
            _currentSubtitle = shot?.SubtitleText ?? string.Empty;
            _currentImage = LoadResource<Texture2D>(shot?.ImageResourcePath);
            var audioClip = LoadResource<AudioClip>(shot?.AudioResourcePath);
            PlayAudio(audioClip);

            var shotDuration = shot == null ? 0f : Mathf.Max(shot.DurationSeconds, audioClip != null ? audioClip.length : 0f);
            _advanceAt = Time.time + Mathf.Max(shotDuration, 0.1f);
        }

        private void CompleteScene()
        {
            if (_completionHandled)
                return;

            _completionHandled = true;
            TutorialFlowController.Instance?.CompleteCurrentSceneAndLoadNext();
        }

        private void DrawTextCard()
        {
            GUI.color = new Color(0.04f, 0.05f, 0.04f, 0.85f);
            GUI.DrawTexture(new Rect(18f, 120f, 400f, 100f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (!string.IsNullOrEmpty(_title))
                GUI.Label(new Rect(32f, 128f, 372f, 28f), _title, _titleStyle);

            if (!string.IsNullOrEmpty(_body))
                GUI.Label(new Rect(32f, 158f, 372f, 52f), _body, _bodyStyle);
        }

        private void DrawStoryboard()
        {
            var screenRect = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.color = Color.white;

            if (_currentImage != null)
            {
                GUI.DrawTexture(screenRect, _currentImage, ScaleMode.ScaleAndCrop);
            }
            else
            {
                GUI.color = new Color(0.16f, 0.18f, 0.16f, 1f);
                GUI.DrawTexture(screenRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            GUI.color = new Color(0.03f, 0.04f, 0.03f, 0.7f);
            GUI.DrawTexture(new Rect(24f, 24f, 440f, 60f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(24f, Screen.height - 148f, Screen.width - 48f, 112f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (!string.IsNullOrWhiteSpace(_title))
                GUI.Label(new Rect(40f, 34f, 408f, 36f), _title, _titleStyle);

            if (!string.IsNullOrWhiteSpace(_currentSubtitle))
                GUI.Label(new Rect(44f, Screen.height - 132f, Screen.width - 88f, 84f), _currentSubtitle, _subtitleStyle);
        }

        private void BuildStyles()
        {
            if (_stylesReady)
                return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            _titleStyle.normal.textColor = Color.white;

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true
            };
            _bodyStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };
            _subtitleStyle.normal.textColor = Color.white;

            _stylesReady = true;
        }

        private AudioSource ResolveAudioSource()
        {
            if (_audioSource != null)
                return _audioSource;

            var existingSource = gameObject.GetComponent<AudioSource>();
            if (existingSource != null)
            {
                _audioSource = existingSource;
                return _audioSource;
            }

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            return _audioSource;
        }

        private void PlayAudio(AudioClip clip)
        {
            var audioSource = ResolveAudioSource();
            if (audioSource == null)
                return;

            audioSource.Stop();
            audioSource.clip = clip;
            if (clip != null)
                audioSource.Play();
        }

        private static T LoadResource<T>(string resourcePath) where T : Object
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
                return null;

            return Resources.Load<T>(resourcePath);
        }
    }
}
