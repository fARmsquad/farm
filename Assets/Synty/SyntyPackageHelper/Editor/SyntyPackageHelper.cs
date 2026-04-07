#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

[InitializeOnLoad]
public class SyntyPackageHelper
{
    static AddAndRemoveRequest Request;

    static SyntyPackageHelper()
    {
        EditorApplication.projectChanged += OnProjectChanged;
    }

    //Non async way of quickly checking a package is added to the package manager
    public static bool IsPackageInstalled(string packageId)
    {
        if ( !File.Exists("Packages/manifest.json") )
            return false;

        string jsonText = File.ReadAllText("Packages/manifest.json");
        return jsonText.Contains( packageId );
    }

    private static SyntyPackageHelperConfig[] LoadSyntyPackageHelperConfigs()
    {
        List<SyntyPackageHelperConfig> configs = new List<SyntyPackageHelperConfig>();
        string[] assetGuids = AssetDatabase.FindAssets("t:SyntyPackageHelperConfig");
        
        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SyntyPackageHelperConfig config = AssetDatabase.LoadAssetAtPath<SyntyPackageHelperConfig>(path);
            if (config != null)
            {
                configs.Add(config);
            }
        }

        Debug.Log($"Loaded {configs.Count} ExamplePackConfig assets");

        return configs.ToArray();
    }

    static void OnProjectChanged()
    {
        EditorApplication.projectChanged -= OnProjectChanged;

        ProcessConfigs(LoadSyntyPackageHelperConfigs());
    }

    [MenuItem("Synty/Package Helper/Install Packages")]
    public static void InstallShaderGraphPackage()
    {
        ProcessConfigs(LoadSyntyPackageHelperConfigs(), true);
    }

    static List<string> packagesToInstall = new List<string>();
    static int requiredPackageCount = 0;
    public static void ProcessConfigs(SyntyPackageHelperConfig[] configs, bool forceInstall = false)
    {
        if(Request != null)
        {
            EditorUtility.DisplayDialog(
                "Synty Package Helper",
                "A package installation process is already underway. Please wait until it finished before trying again.",
                "OK");
            return;
        }

        packagesToInstall.Clear();
        requiredPackageCount = 0;

        foreach(var config in configs)
        {
            BuildInstallList(config, forceInstall);
            config.hasPromptedUser = true;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if(packagesToInstall.Count == 0)
        {
            if(requiredPackageCount == 0)
            {
                if(forceInstall)
                {
                    EditorUtility.DisplayDialog(
                        "Synty Package Helper",
                        "All required packages installed.",
                        "OK");
                }
                return;
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Synty Package Helper",
                    "No packages to install. You skipped the installation of " + requiredPackageCount + " packages.",
                    "OK");
                return;
            }
        }

        InstallPackages(packagesToInstall.ToArray());
    }

    public static void BuildInstallList( SyntyPackageHelperConfig config, bool forceInstall = false )
    {
        if(!forceInstall && config.hasPromptedUser)
        {
            return;
        }

        foreach(var packageId in config.packageIds)
        {
            if(IsPackageInstalled(packageId))
            {
                continue;
            }

            if(packagesToInstall.Contains(packageId))
            {
                continue;
            }

            requiredPackageCount++;
            bool addPackage = PromptPackageInstall(config.assetPackDisplayName, packageId);

            if(addPackage)
            {
                packagesToInstall.Add(packageId);
            }
        }
    }

    static bool PromptPackageInstall(string packName, string packageName)
    {
        return EditorUtility.DisplayDialog(
                "Synty Package Helper",
                packName + " requires the package " + packageName + " to function correctly. Do you want to install this package now?",
                "Install", "Skip");
    } 
    
    static string[] requestPackages = null;
    static void InstallPackages(string[] packagesToInstall)
    {
        if(Request != null)
        {
            Debug.LogError( "Trying to install a packages when when packages are already being installed." );
            return;
        }

        // Add a package to the project
        requestPackages = packagesToInstall;
        Request = Client.AddAndRemove(packagesToInstall);
        EditorApplication.update += Progress;
        Progress();
    }

    static void Progress()
    {
        if(Request == null)
        {
            EditorApplication.update -= Progress;
        }

        if(requestPackages != null)
        {
            int installed = 0;
            foreach(var package in requestPackages)
            {
                if(IsPackageInstalled(package))
                {
                    installed++;
                }
            }

            EditorUtility.DisplayProgressBar("Synty Package Helper", "Installing packages... " + installed + "/" + requestPackages.Length + ".", (float)installed / (float)requestPackages.Length);
        }

        if (Request.IsCompleted)
        {
            EditorUtility.ClearProgressBar();
            if (Request.Status == StatusCode.Success)
            {
                string message = requestPackages.Length + " packages installed successfully!";
                int skipped = requiredPackageCount - requestPackages.Length;
                if(skipped > 0)
                {
                    message = message + "\n" + (requiredPackageCount - requestPackages.Length) + " were skipped.";
                }
                EditorUtility.DisplayDialog("Synty Package Helper", message, "OK");
            }
            else if (Request.Status >= StatusCode.Failure)
            {
                EditorUtility.DisplayDialog("Synty Package Helper", "There was an error installing the required packages.\n\nError:\n" + Request.Error.message, "OK");
            }

            Request = null;
            EditorApplication.update -= Progress;
        }
    }
}
#endif