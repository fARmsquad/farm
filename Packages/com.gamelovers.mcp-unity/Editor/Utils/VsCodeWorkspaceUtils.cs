using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace McpUnity.Utils
{
    /// <summary>
    /// Manages VSCode-like IDE workspace integration for Unity projects
    /// </summary>
    public class VsCodeWorkspaceUtils
    {
        /// <summary>
        /// The default folder structure for code-workspace files
        /// </summary>
        private static readonly JArray DefaultFolders = JArray.Parse(@"[
            {
                ""path"": ""Assets""
            },
            {
                ""path"": ""Packages""
            },
            {
                ""path"": ""Library/PackageCache""
            }
        ]");
        
        /// <summary>
        /// Add the Library/PackageCache folder to the .code-workspace file if not already present
        /// This ensures that the Unity cache is available to code intelligence tools
        /// </summary>
        public static bool AddPackageCacheToWorkspace()
        {
            try
            {
                // Get the project root directory
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                
                // Determine the workspace filename based on the project directory name
                string projectDirName = new DirectoryInfo(projectRoot).Name;
                string workspaceFilename = $"{projectDirName}.code-workspace";
                string workspacePath = Path.Combine(projectRoot, workspaceFilename);
                JObject workspaceConfig = new JObject
                {
                    ["folders"] = DefaultFolders.DeepClone(),
                    ["settings"] = new JObject()
                };
                
                // If file exists, update it rather than overwriting
                if (File.Exists(workspacePath))
                {
                    string existingContent = File.ReadAllText(workspacePath);
                    JObject existingWorkspace = JObject.Parse(existingContent);
                    
                    // Merge the new config with the existing one
                    MergeWorkspaceConfigs(existingWorkspace, workspaceConfig);
                    workspaceConfig = existingWorkspace;
                }
                
                // Write the updated workspace file
                File.WriteAllText(workspacePath, workspaceConfig.ToString(Formatting.Indented));
                Debug.Log($"[MCP Unity] Updated workspace configuration in {workspacePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Unity] Error updating workspace file: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Merges a source workspace config into a target workspace config
        /// Ensures folders are uniquely added based on path properties
        /// </summary>
        private static void MergeWorkspaceConfigs(JObject target, JObject source)
        {
            // Merge folders array if both exist
            if (source["folders"] != null && source["folders"].Type == JTokenType.Array)
            {
                if (target["folders"] == null || target["folders"].Type != JTokenType.Array)
                {
                    target["folders"] = new JArray();
                }
                
                // Get existing folder paths
                var existingPaths = new HashSet<string>();
                foreach (var folder in target["folders"])
                {
                    if (folder.Type == JTokenType.Object && folder["path"] != null)
                    {
                        existingPaths.Add(folder["path"].ToString());
                    }
                }
                
                // Add new folders if they don't exist
                foreach (var folder in source["folders"])
                {
                    if (folder.Type == JTokenType.Object && folder["path"] != null)
                    {
                        string path = folder["path"].ToString();
                        if (!existingPaths.Contains(path))
                        {
                            ((JArray)target["folders"]).Add(folder.DeepClone());
                            existingPaths.Add(path);
                        }
                    }
                }
            }
            
            // Merge settings if both exist
            if (source["settings"] != null && source["settings"].Type == JTokenType.Object)
            {
                if (target["settings"] == null || target["settings"].Type != JTokenType.Object)
                {
                    target["settings"] = new JObject();
                }
                
                // Deep merge settings
                foreach (var property in (JObject)source["settings"])
                {
                    target["settings"][property.Key] = property.Value.DeepClone();
                }
            }
            
            // Merge any other top-level properties
            foreach (var property in source)
            {
                if (property.Key != "folders" && property.Key != "settings")
                {
                    target[property.Key] = property.Value.DeepClone();
                }
            }
        }
    }
}
