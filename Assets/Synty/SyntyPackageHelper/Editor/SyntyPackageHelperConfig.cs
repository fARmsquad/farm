#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SyntyPackageHelperConfig))]
public class ExamplePackConfigLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SyntyPackageHelperConfig config = (SyntyPackageHelperConfig)target;
        if (GUILayout.Button("Install Packages"))
        {
            SyntyPackageHelper.ProcessConfigs(new SyntyPackageHelperConfig[] { config }, true);
        }
    }
}

[CreateAssetMenu(fileName = "SyntyPackageHelperConfig", menuName = "Scriptable Objects/SyntyPackageHelperConfig")]
public class SyntyPackageHelperConfig : ScriptableObject
{
    public string assetPackDisplayName;
    public string[] packageIds;
    public bool hasPromptedUser;
}
#endif