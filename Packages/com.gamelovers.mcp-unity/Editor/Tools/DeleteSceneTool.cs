using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using McpUnity.Unity;
using McpUnity.Utils;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for deleting a Unity scene and removing it from Build Settings
    /// </summary>
    public class DeleteSceneTool : McpToolBase
    {
        public DeleteSceneTool()
        {
            Name = "delete_scene";
            Description = "Deletes a scene by path or name and removes it from Build Settings";
        }

        /// <summary>
        /// Execute the DeleteScene tool with the provided parameters
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject</param>
        public override JObject Execute(JObject parameters)
        {
            string scenePath = parameters["scenePath"]?.ToObject<string>();
            string sceneName = parameters["sceneName"]?.ToObject<string>();
            string folderPath = parameters["folderPath"]?.ToObject<string>();

            if (string.IsNullOrEmpty(scenePath))
            {
                if (string.IsNullOrEmpty(sceneName))
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        "Provide either 'scenePath' or 'sceneName'",
                        "validation_error"
                    );
                }

                // Resolve scene path by name (optionally within folderPath)
                string filter = $"{sceneName} t:Scene";
                string[] searchInFolders = null;
                if (!string.IsNullOrEmpty(folderPath))
                {
                    // Ensure folder exists
                    if (!AssetDatabase.IsValidFolder(folderPath))
                    {
                        return McpUnitySocketHandler.CreateErrorResponse(
                            $"Folder '{folderPath}' does not exist",
                            "not_found_error"
                        );
                    }
                    searchInFolders = new[] { folderPath };
                }

                var guids = AssetDatabase.FindAssets(filter, searchInFolders);
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                    {
                        scenePath = path;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(scenePath))
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        $"Scene named '{sceneName}' not found",
                        "not_found_error"
                    );
                }
            }

            try
            {
                // If the scene is open, close it without saving changes
                var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneByPath(scenePath);
                if (scene.IsValid() && scene.isLoaded)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
                }

                // Remove from Build Settings
                RemoveSceneFromBuildSettings(scenePath);

                // Delete asset
                bool deleted = AssetDatabase.DeleteAsset(scenePath);
                AssetDatabase.Refresh();

                if (!deleted)
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        $"Failed to delete scene at '{scenePath}'",
                        "delete_error"
                    );
                }

                McpLogger.LogInfo($"Deleted scene at path '{scenePath}' and removed from Build Settings");

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Successfully deleted scene at path '{scenePath}' and removed from Build Settings",
                    ["scenePath"] = scenePath
                };
            }
            catch (Exception ex)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Error deleting scene: {ex.Message}",
                    "scene_delete_error"
                );
            }
        }

        private void RemoveSceneFromBuildSettings(string scenePath)
        {
            var scenes = UnityEditor.EditorBuildSettings.scenes;
            var filtered = scenes.Where(s => s.path != scenePath).ToArray();
            if (filtered.Length != scenes.Length)
            {
                UnityEditor.EditorBuildSettings.scenes = filtered;
            }
        }
    }
}


