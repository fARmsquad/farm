using System;
using System.Threading.Tasks;
using McpUnity.Unity;
using McpUnity.Utils;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for executing Unity Editor menu items
    /// </summary>
    public class MenuItemTool : McpToolBase
    {
        public MenuItemTool()
        {
            Name = "execute_menu_item";
            Description = "Executes functions tagged with the MenuItem attribute";
        }
        
        /// <summary>
        /// Execute the MenuItem tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject</param>
        public override JObject Execute(JObject parameters)
        {
            // Extract parameters with defaults
            string menuPath = parameters["menuPath"]?.ToObject<string>();
            if (string.IsNullOrEmpty(menuPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'menuPath' not provided", 
                    "validation_error"
                );
            }
                
            // Log the execution
            McpLogger.LogInfo($"[MCP Unity] Executing menu item: {menuPath}");
                
            // Execute the menu item
            bool success = EditorApplication.ExecuteMenuItem(menuPath);
                
            // Create the response
            return new JObject
            {
                ["success"] = success,
                ["type"] = "text",
                ["message"] = success 
                    ? $"Successfully executed menu item: {menuPath}" 
                    : $"Failed to execute menu item: {menuPath}"
            };
        }
    }
}
