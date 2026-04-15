using System;
using System.Collections;
using System.IO;
using FarmSimVR.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    internal static class ArtifactPreloader
    {
        public static IEnumerator PreloadTurn(
            string baseUrl,
            GenerativePlayableTurnEnvelope envelope,
            Action<PreloadedGenerativeTurnAssets, string> onComplete)
        {
            if (envelope == null)
            {
                onComplete?.Invoke(null, "Generated turn envelope is missing.");
                yield break;
            }

            string cacheRoot = Path.Combine(
                Application.persistentDataPath,
                "generative-runtime-cache",
                envelope.session_id ?? string.Empty,
                envelope.turn_id ?? string.Empty);
            Directory.CreateDirectory(cacheRoot);

            var assets = new PreloadedGenerativeTurnAssets(envelope.session_id, envelope.turn_id, cacheRoot);
            var artifacts = envelope.artifacts ?? Array.Empty<GenerativeArtifactDescriptor>();
            for (int i = 0; i < artifacts.Length; i++)
            {
                var artifact = artifacts[i];
                if (artifact == null || string.IsNullOrWhiteSpace(artifact.asset_id))
                    continue;

                string extension = ResolveExtension(artifact);
                string localPath = Path.Combine(cacheRoot, $"{artifact.asset_id}{extension}");
                if (!File.Exists(localPath))
                {
                    using var request = UnityWebRequest.Get(GenerativeRuntimeClient.BuildArtifactContentUrl(baseUrl, artifact.asset_id));
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        onComplete?.Invoke(null, request.error ?? $"Artifact download failed for '{artifact.asset_id}'.");
                        yield break;
                    }

                    File.WriteAllBytes(localPath, request.downloadHandler.data);
                }

                switch (artifact.artifact_type)
                {
                    case "image":
                        var bytes = File.ReadAllBytes(localPath);
                        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (!texture.LoadImage(bytes))
                        {
                            UnityEngine.Object.Destroy(texture);
                            onComplete?.Invoke(null, $"Image artifact '{artifact.asset_id}' could not be decoded.");
                            yield break;
                        }
                        assets.RegisterImage(artifact.asset_id, texture, localPath);
                        break;
                    case "audio":
                        using (var audioRequest = UnityWebRequestMultimedia.GetAudioClip(new Uri(localPath).AbsoluteUri, AudioType.MPEG))
                        {
                            yield return audioRequest.SendWebRequest();
                            if (audioRequest.result != UnityWebRequest.Result.Success)
                            {
                                onComplete?.Invoke(null, audioRequest.error ?? $"Audio artifact '{artifact.asset_id}' could not be decoded.");
                                yield break;
                            }

                            var clip = DownloadHandlerAudioClip.GetContent(audioRequest);
                            if (clip == null)
                            {
                                onComplete?.Invoke(null, $"Audio artifact '{artifact.asset_id}' was empty.");
                                yield break;
                            }

                            assets.RegisterAudio(artifact.asset_id, clip, localPath);
                        }
                        break;
                    case "alignment":
                        assets.RegisterAlignment(artifact.asset_id, File.ReadAllText(localPath), localPath);
                        break;
                }
            }

            onComplete?.Invoke(assets, string.Empty);
        }

        private static string ResolveExtension(GenerativeArtifactDescriptor artifact)
        {
            if (artifact == null)
                return ".bin";

            return artifact.artifact_type switch
            {
                "image" => ".png",
                "audio" => ".mp3",
                "alignment" => ".json",
                _ => ".bin",
            };
        }
    }
}
