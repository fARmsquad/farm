using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using McpUnity.Unity;
using Newtonsoft.Json.Linq;

namespace McpUnity.Resources
{
    /// <summary>
    /// Resource for getting package information from the Unity Package Manager
    /// </summary>
    public class GetPackagesResource : McpResourceBase
    {
        private ListRequest _listRequest;
        
        public GetPackagesResource()
        {
            Name = "get_packages";
            Description = "Retrieve all packages from the Unity Package Manager";
            Uri = "unity://packages";
        }
        
        /// <summary>
        /// Execute the resource to get packages information
        /// </summary>
        /// <param name="parameters">Optional parameters for filtering</param>
        /// <returns>JObject containing packages information</returns>
        public override JObject Fetch(JObject parameters)
        {
            // Get project packages (installed)
            var projectPackages = GetProjectPackages();
                
            // Get registry packages
            var registryPackages = GetRegistryPackages();
                
            // Return combined result
            return new JObject
            {
                ["success"] = true,
                ["message"] = $"Retrieved {projectPackages.Count} project packages and {registryPackages.Count} registry packages",
                ["projectPackages"] = projectPackages,
                ["registryPackages"] = registryPackages
            };
        }
        
        /// <summary>
        /// Get packages installed in the current project
        /// </summary>
        private JArray GetProjectPackages()
        {
            JArray result = new JArray();
            
            // List installed packages
            _listRequest = Client.List(true);
            
            // Wait for the request to complete
            while (!_listRequest.IsCompleted)
            {
                System.Threading.Thread.Sleep(100);
            }
            
            if (_listRequest.Status == StatusCode.Success)
            {
                foreach (var package in _listRequest.Result)
                {
                    result.Add(PackageToJObject(package, "installed"));
                }
            }
            else if (_listRequest.Status == StatusCode.Failure)
            {
                Debug.LogError($"[MCP Unity] Failed to list project packages: {_listRequest.Error.message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Get packages available from the Unity Registry
        /// </summary>
        private JArray GetRegistryPackages()
        {
            JArray result = new JArray();
            
            // Search Unity registry packages
            SearchRequest searchRequest = Client.SearchAll();
            
            // Wait for the request to complete
            while (!searchRequest.IsCompleted)
            {
                System.Threading.Thread.Sleep(100);
            }
            
            if (searchRequest.Status == StatusCode.Success)
            {
                foreach (var package in searchRequest.Result)
                {
                    // Check if package is already installed
                    string state = "not_installed";
                    if (_listRequest.Status == StatusCode.Success)
                    {
                        foreach (var installedPackage in _listRequest.Result)
                        {
                            if (installedPackage.name == package.name)
                            {
                                state = "installed";
                                break;
                            }
                        }
                    }
                    
                    result.Add(PackageToJObject(package, state));
                }
            }
            else if (searchRequest.Status == StatusCode.Failure)
            {
                Debug.LogError($"[MCP Unity] Failed to search registry packages: {searchRequest.Error.message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Convert a package info object to JObject
        /// </summary>
        /// <param name="package">Package info</param>
        /// <param name="state">Installation state</param>
        /// <returns>JObject with package info</returns>
        private JObject PackageToJObject(UnityEditor.PackageManager.PackageInfo package, string state)
        {
            return new JObject
            {
                ["name"] = package.name,
                ["displayName"] = package.displayName,
                ["version"] = package.version,
                ["description"] = package.description,
                ["category"] = package.category,
                ["source"] = package.source.ToString(),
                ["state"] = state,
                ["author"] = new JObject
                {
                    ["name"] = package.author?.name,
                    ["email"] = package.author?.email,
                    ["url"] = package.author?.url
                }
            };
        }
    }
}
