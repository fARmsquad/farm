using System.Collections.Generic;
using FarmSimVR.Core;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    public sealed class RuntimeCutsceneShotHandle
    {
        public RuntimeCutsceneShotHandle(
            string shotId,
            string subtitleText,
            string narrationText,
            float durationSeconds,
            Texture2D image,
            AudioClip audioClip,
            string alignmentJson)
        {
            ShotId = shotId ?? string.Empty;
            SubtitleText = subtitleText ?? string.Empty;
            NarrationText = narrationText ?? string.Empty;
            DurationSeconds = durationSeconds;
            Image = image;
            AudioClip = audioClip;
            AlignmentJson = alignmentJson ?? string.Empty;
        }

        public string ShotId { get; }
        public string SubtitleText { get; }
        public string NarrationText { get; }
        public float DurationSeconds { get; }
        public Texture2D Image { get; }
        public AudioClip AudioClip { get; }
        public string AlignmentJson { get; }
    }

    public sealed class PreloadedGenerativeTurnAssets
    {
        private readonly Dictionary<string, Texture2D> _imagesByAssetId = new();
        private readonly Dictionary<string, AudioClip> _audioByAssetId = new();
        private readonly Dictionary<string, string> _alignmentJsonByAssetId = new();
        private readonly Dictionary<string, string> _localPathsByAssetId = new();

        public PreloadedGenerativeTurnAssets(string sessionId, string turnId, string cacheRootPath)
        {
            SessionId = sessionId ?? string.Empty;
            TurnId = turnId ?? string.Empty;
            CacheRootPath = cacheRootPath ?? string.Empty;
        }

        public string SessionId { get; }
        public string TurnId { get; }
        public string CacheRootPath { get; }
        public IReadOnlyDictionary<string, Texture2D> ImagesByAssetId => _imagesByAssetId;
        public IReadOnlyDictionary<string, AudioClip> AudioByAssetId => _audioByAssetId;

        public void RegisterImage(string assetId, Texture2D image, string localPath)
        {
            if (string.IsNullOrWhiteSpace(assetId) || image == null)
                return;

            _imagesByAssetId[assetId] = image;
            _localPathsByAssetId[assetId] = localPath ?? string.Empty;
        }

        public void RegisterAudio(string assetId, AudioClip clip, string localPath)
        {
            if (string.IsNullOrWhiteSpace(assetId) || clip == null)
                return;

            _audioByAssetId[assetId] = clip;
            _localPathsByAssetId[assetId] = localPath ?? string.Empty;
        }

        public void RegisterAlignment(string assetId, string alignmentJson, string localPath)
        {
            if (string.IsNullOrWhiteSpace(assetId))
                return;

            _alignmentJsonByAssetId[assetId] = alignmentJson ?? string.Empty;
            _localPathsByAssetId[assetId] = localPath ?? string.Empty;
        }

        public bool TryGetImage(string assetId, out Texture2D image) => _imagesByAssetId.TryGetValue(assetId, out image);
        public bool TryGetAudio(string assetId, out AudioClip clip) => _audioByAssetId.TryGetValue(assetId, out clip);
        public bool TryGetAlignment(string assetId, out string alignmentJson) => _alignmentJsonByAssetId.TryGetValue(assetId, out alignmentJson);
        public bool TryGetLocalPath(string assetId, out string localPath) => _localPathsByAssetId.TryGetValue(assetId, out localPath);
    }

    public static class GenerativeTurnRuntimeState
    {
        private static GenerativePlayableTurnEnvelope _preparedTurn;
        private static PreloadedGenerativeTurnAssets _preparedAssets;

        public static bool HasPreparedTurn => _preparedTurn != null && _preparedAssets != null;
        public static GenerativePlayableTurnEnvelope PreparedTurn => _preparedTurn;
        public static PreloadedGenerativeTurnAssets PreparedAssets => _preparedAssets;

        public static void SetPreparedTurn(
            GenerativePlayableTurnEnvelope envelope,
            PreloadedGenerativeTurnAssets assets)
        {
            if (!IsPlayableEnvelope(envelope, out _) || assets == null)
            {
                Clear();
                return;
            }

            _preparedTurn = envelope;
            _preparedAssets = assets;
        }

        public static bool IsPlayableEnvelope(
            GenerativePlayableTurnEnvelope envelope,
            out string errorMessage)
        {
            if (envelope == null)
            {
                errorMessage = "Generated runtime envelope was missing.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(envelope.entry_scene_name))
            {
                errorMessage = "Generated runtime envelope is missing an entry scene.";
                return false;
            }

            if (envelope.cutscene == null)
            {
                errorMessage = "Generated runtime envelope is missing a cutscene.";
                return false;
            }

            if (envelope.minigame == null || string.IsNullOrWhiteSpace(envelope.minigame.scene_name))
            {
                errorMessage = "Generated runtime envelope is missing a minigame contract.";
                return false;
            }

            var shots = envelope.cutscene.shots;
            if (shots == null || shots.Length < 3 || shots.Length > 6)
            {
                errorMessage = "Generated runtime cutscene must contain 3 to 6 shots.";
                return false;
            }

            for (var i = 0; i < shots.Length; i++)
            {
                var shot = shots[i];
                if (shot == null)
                {
                    errorMessage = $"Generated runtime shot {i + 1} was missing.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(shot.subtitle_text) ||
                    string.IsNullOrWhiteSpace(shot.narration_text))
                {
                    errorMessage = $"Generated runtime shot '{shot.shot_id}' is missing subtitle or narration text.";
                    return false;
                }

                if (!string.Equals(shot.subtitle_text.Trim(), shot.narration_text.Trim(), System.StringComparison.Ordinal))
                {
                    errorMessage = $"Generated runtime shot '{shot.shot_id}' narration must exactly match subtitle text.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(shot.image_asset_id) ||
                    string.IsNullOrWhiteSpace(shot.audio_asset_id) ||
                    string.IsNullOrWhiteSpace(shot.alignment_asset_id))
                {
                    errorMessage = $"Generated runtime shot '{shot.shot_id}' is missing required artifact ids.";
                    return false;
                }

                if (shot.duration_seconds <= 0f)
                {
                    errorMessage = $"Generated runtime shot '{shot.shot_id}' has an invalid duration.";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        public static void Clear()
        {
            _preparedTurn = null;
            _preparedAssets = null;
        }

        public static bool TryGetCutscene(
            string sceneName,
            out string title,
            out RuntimeCutsceneShotHandle[] shots)
        {
            title = string.Empty;
            shots = System.Array.Empty<RuntimeCutsceneShotHandle>();
            if (!HasPreparedTurn || _preparedTurn.cutscene == null)
                return false;

            if (!string.Equals(_preparedTurn.cutscene.scene_name, sceneName, System.StringComparison.Ordinal))
                return false;

            title = _preparedTurn.cutscene.display_name ?? string.Empty;
            shots = BuildRuntimeShotHandles(_preparedTurn.cutscene, _preparedAssets);
            return shots.Length > 0;
        }

        public static bool TryGetMinigameContract(
            string sceneName,
            out GenerativeMinigameContract minigame)
        {
            minigame = null;
            if (!HasPreparedTurn || _preparedTurn.minigame == null)
                return false;

            if (!string.Equals(_preparedTurn.minigame.scene_name, sceneName, System.StringComparison.Ordinal))
                return false;

            minigame = _preparedTurn.minigame;
            return minigame != null;
        }

        private static RuntimeCutsceneShotHandle[] BuildRuntimeShotHandles(
            GenerativeCutsceneContract cutscene,
            PreloadedGenerativeTurnAssets assets)
        {
            if (cutscene == null || cutscene.shots == null || assets == null)
                return System.Array.Empty<RuntimeCutsceneShotHandle>();

            var runtimeShots = new RuntimeCutsceneShotHandle[cutscene.shots.Length];
            for (int i = 0; i < cutscene.shots.Length; i++)
            {
                var shot = cutscene.shots[i];
                assets.TryGetImage(shot.image_asset_id, out var image);
                assets.TryGetAudio(shot.audio_asset_id, out var audioClip);
                assets.TryGetAlignment(shot.alignment_asset_id, out var alignmentJson);
                runtimeShots[i] = new RuntimeCutsceneShotHandle(
                    shot.shot_id,
                    shot.subtitle_text,
                    shot.narration_text,
                    shot.duration_seconds,
                    image,
                    audioClip,
                    alignmentJson);
            }

            return runtimeShots;
        }
    }
}
