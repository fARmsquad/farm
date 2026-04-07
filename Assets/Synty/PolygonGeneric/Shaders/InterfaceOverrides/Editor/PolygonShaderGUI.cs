using UnityEditor;

public class PolygonShaderGUI : ShaderGUI
{
    private bool _showBaseTexture = false;
    private bool _showOverlayTexture = false;
    private bool _showAlphaTexture = false;
    private bool _showNormalTexture = false;
    private bool _showTriplanar = false;
    private bool _showNormalTriplanar = false;
    private bool _showEmissionTexture = false;
    private bool _showSnow = false;
    private bool _showWave = false;

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
            "_Specular_Color",
            "_Alpha_Transparency"
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
        
        // Triplanar Texture Parameters
        shaderProperties = new[]
        {
            "_Enable_Triplanar_Texture", 
            "_Triplanar_Texture_Top",
            "_Triplanar_Tiling_Top",
            "_Triplanar_Offset_Top",
            "_Triplanar_Texture_Bottom",
            "_Triplanar_Tiling_Bottom",
            "_Triplanar_Offset_Bottom",
            "_Triplanar_Texture_Side",
            "_Triplanar_Tiling_Side",
            "_Triplanar_Offset_Side",
            "_Triplanar_Top_To_Side_Difference",
            "_Triplanar_Fade", "_Triplanar_Intensity"
        };
        
        _showTriplanar = CreatePropertyGroup("Triplanar Texture", shaderProperties, _showTriplanar, materialEditor, properties);
        
        // Triplanar Normal Texture Parameters
        shaderProperties = new[]
        {
            "_Enable_Triplanar_Normals",
            "_Triplanar_Normal_Texture_Top",
            "_Triplanar_Normal_Intensity_Top",
            "_Triplanar_Normal_Tiling_Top",
            "_Triplanar_Normal_Offset_Top",
            "_Triplanar_Normal_Texture_Bottom",
            "_Triplanar_Normal_Intensity_Bottom",
            "_Triplanar_Normal_Tiling_Bottom",
            "_Triplanar_Normal_Offset_Bottom",
            "_Triplanar_Normal_Texture_Side",
            "_Triplanar_Normal_Intensity_Side",
            "_Triplanar_Normal_Tiling_Side",
            "_Triplanar_Normal_Offset_Side",
            "_Triplanar_Normal_Top_To_Side_Difference",
            "_Triplanar_Normal_Fade"
        };
        
        _showNormalTriplanar = CreatePropertyGroup("Triplanar Normal Texture", shaderProperties, _showNormalTriplanar, materialEditor, properties);
        
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
        
        // Wave Parameters
        shaderProperties = new[]
        {
            "_Enable_Wave",
            "_Wave_Direction_Vector",
            "_Wave_Scale",
            "_Wave_Noise_Tiling",
            "_Wave_Speed",
            "_Fade_Wave_To_Object_Origin",
            "_Fade_Wave_Scale",
            "_Fade_Wave_Vertical_Offset",
            "_Wave_Object_Offset"
        };
        
        _showWave = CreatePropertyGroup("Wave", shaderProperties, _showWave, materialEditor, properties);
    }
}
