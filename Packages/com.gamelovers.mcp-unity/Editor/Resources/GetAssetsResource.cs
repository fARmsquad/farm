using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using McpUnity.Unity;
using Newtonsoft.Json.Linq;

namespace McpUnity.Resources
{
    /// <summary>
    /// Resource for getting asset information from the Unity Asset Database
    /// </summary>
    public class GetAssetsResource : McpResourceBase
    {
        public GetAssetsResource()
        {
            Name = "get_assets";
            Description = "Retrieves assets from the Unity Asset Database";
            Uri = "unity://assets";
        }
        
        /// <summary>
        /// Execute the resource to get asset information
        /// </summary>
        /// <param name="parameters">Optional parameters for filtering</param>
        /// <returns>JObject containing asset information</returns>
        public override JObject Fetch(JObject parameters)
        {
            // Extract optional filter parameters
            string assetType = parameters?["assetType"]?.ToObject<string>();
            string searchPattern = parameters?["searchPattern"]?.ToObject<string>();
                
            // Get all assets from the project
            JArray assets = GetAllAssets(assetType, searchPattern);
                
            // Return result
            return new JObject
            {
                ["success"] = true,
                ["message"] = $"Retrieved {assets.Count} assets",
                ["assets"] = assets
            };
        }
        
        /// <summary>
        /// Get all assets from the project, optionally filtered by type and search pattern
        /// </summary>
        /// <param name="assetType">Optional filter by asset type</param>
        /// <param name="searchPattern">Optional search pattern for asset names</param>
        /// <returns>JArray containing asset information</returns>
        private JArray GetAllAssets(string assetType, string searchPattern)
        {
            JArray result = new JArray();
            
            // Find all assets
            string[] assetGuids = AssetDatabase.FindAssets(string.IsNullOrEmpty(searchPattern) ? "" : searchPattern);
            
            foreach (string guid in assetGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                
                // Skip folders
                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }
                
                // Get asset type
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (asset == null)
                {
                    continue;
                }
                
                string fileType = asset.GetType().Name;
                
                // Filter by asset type if specified
                if (!string.IsNullOrEmpty(assetType) && !fileType.Equals(assetType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                // Create asset information
                JObject assetInfo = new JObject
                {
                    ["name"] = Path.GetFileNameWithoutExtension(assetPath),
                    ["filename"] = Path.GetFileName(assetPath),
                    ["path"] = assetPath,
                    ["type"] = fileType,
                    ["extension"] = Path.GetExtension(assetPath).TrimStart('.'),
                    ["guid"] = guid,
                    ["size"] = GetAssetSize(assetPath)
                };
                
                result.Add(assetInfo);
            }
            
            return result;
        }
        
        /// <summary>
        /// Get the size of an asset file
        /// </summary>
        /// <param name="assetPath">Path to the asset</param>
        /// <returns>Size in bytes, or -1 if the file cannot be found</returns>
        private long GetAssetSize(string assetPath)
        {
            string fullPath = Path.Combine(Application.dataPath, "..", assetPath);
            FileInfo fileInfo = new FileInfo(fullPath);
            return fileInfo.Exists ? fileInfo.Length : -1;
        }
    }
}
