using UnityEditor;

public class PolygonTransparencyShaderGUI : ShaderGUI
{
    private bool _showBaseTexture = false;
    private bool _showOverlayTexture = false;
    private bool _showAlphaTexture = false;
    private bool _showNormalTexture = false;
    private bool _showEmissionTexture = false;
    private bool _showSnow = false;

    private bool CreatePropertyGroup(string title, string[] groupProperties, bool foldout, MaterialEditor materialEditor, MaterialProperty[] allProperties)
    {
        foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, title);
        if (foldout)
        {
            foreach (string property in groupProperties)
            {
                MaterialProperty propertyReference = FindProperty(property, allProperties);
                materialEditor.ShaderProperty(propertyReference, propertyReference.displayName, 1);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        return foldout;
    }
    
    override public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        EditorGUILayout.LabelField("Basic Parameters", EditorStyles.boldLabel);
        
        // Ungrouped Basic Parameters
        string[] shaderProperties =
        {
            "_Color_Tint",
            "_Metallic",
            "_Smoothness",
            "_Metallic_Smoothness_Texture",
            "_Ambient_Occlusion",
            "_Alpha_Transparency",
            "_Alpha_Clip_Threshold"            
        };
        
        foreach (string property in shaderProperties)
        {
            MaterialProperty propertyReference = FindProperty(property, properties);
            materialEditor.ShaderProperty(propertyReference, propertyReference.displayName, 0);
        }
        
        EditorGUILayout.Separator();
        
        // Base Texture Parameters
        shaderProperties = new[]
        {
            "_Enable_Base_Texture",
            "_Base_Texture",
            "_Base_Tiling",
            "_Base_Offset"
        };
        
        _showBaseTexture = CreatePropertyGroup("Base Texture", shaderProperties, _showBaseTexture, materialEditor, properties);
        
        // Alpha Texture Parameters
        shaderProperties = new[]
        {
            "_Enable_Alpha_Texture",
            "_Alpha_Texture",
            "_Alpha_Tiling",
            "_Alpha_Offset"
        };
        
        _showAlphaTexture = CreatePropertyGroup("Alpha Texture", shaderProperties, _showAlphaTexture, materialEditor, properties);
        
        // Normal Texture Parameters
        shaderProperties = new[]
        {
            "_Enable_Normal_Texture",
            "_Normal_Texture",
            "_Normal_Tiling",
            "_Normal_Offset"
        };
        
        _showNormalTexture = CreatePropertyGroup("Normal Texture", shaderProperties, _showNormalTexture, materialEditor, properties);
        
        // Emission Texture Parameters
        shaderProperties = new[]
        {
            "_Enable_Emission_Texture",
            "_Emission_Texture",
            "_Emission_Tiling",
            "_Emission_Offset"
        };
        
        _showEmissionTexture = CreatePropertyGroup("Emission Texture", shaderProperties, _showEmissionTexture, materialEditor, properties);
        
        EditorGUILayout.Separator();
        
        EditorGUILayout.LabelField("Advanced Parameters", EditorStyles.boldLabel);
        
        // Overlay Texture Parameters
        shaderProperties = new[]
        {
            "_Enable_Overlay_Texture",
            "_Overlay_Texture",
            "_Overlay_Tiling",
            "_Overlay_Offset",
            "_OVERLAY_UV_CHANNEL",
            "_Overlay_Intensity"
        };
        
        _showOverlayTexture = CreatePropertyGroup("Overlay Texture", shaderProperties, _showOverlayTexture, materialEditor, properties);
        
        // Snow Parameters
        shaderProperties = new[]
        {
            "_Enable_Snow",
            "_Snow_Metallic",
            "_Snow_Smoothness",
            "_Snow_Level",
            "_Snow_Transition",
            "_Snow_Transition_Threshold",
            "_Snow_Transition_Threshold_Steps",
            "_Snow_Transition_Threshold_Step_Count",
            "_Snow_Angle_Threshold",
            "_Snow_Color",
            "_Sea_Level",
            "_Snow_Edge_Noise",
            "_Snow_Noise_Tiling",
            "_Snow_Noise_Offset",
            "_Snow_Noise_Fade",
            "_Snow_Noise_Top_To_Side_Difference",
            "_Snow_Normal_Texture",
            "_Snow_Overrides_Normals",
            "_Snow_Normal_Intensity",
            "_Snow_Normal_Tiling",
            "_Snow_Normal_Offset",
            "_Snow_Normal_Fade",
            "_Snow_Metallic_Smoothness_Texture",
            "_Snow_Metallic_Smoothness_Tiling",
            "_Snow_Metallic_Smoothness_Offset"
        };
        
        _showSnow = CreatePropertyGroup("Snow", shaderProperties, _showSnow, materialEditor, properties);
        
    }
}
