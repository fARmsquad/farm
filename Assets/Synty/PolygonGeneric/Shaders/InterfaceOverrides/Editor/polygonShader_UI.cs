using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class polygonShader_UI : ShaderGUI
{
    #region TabProperties
    private int selectedTab = 0;
    private string[] tabNames = { "Base", "Overlay", "Triplanar", "Snow" };

    private bool showSurfaceType = false;
    private bool checkedRenderPipe = false;
    private int renderPipeType = 0;

    void CheckRenderPipeline()
    {
        var pipelineAsset = GraphicsSettings.defaultRenderPipeline;

        if (pipelineAsset == null)
        {
            Debug.Log("Using Built-In Render Pipeline (BIRP)");
            renderPipeType = 0;
        }
        else if (pipelineAsset.GetType().Name.Contains("UniversalRenderPipelineAsset"))
        {
            Debug.Log("Using Universal Render Pipeline (URP)");
            renderPipeType = 1;
        }
        else if (pipelineAsset.GetType().Name.Contains("HDRenderPipelineAsset"))
        {
            Debug.Log("Using High Definition Render Pipeline (HDRP)");
            renderPipeType = 1;
        }
        else
        {
            Debug.Log("Using custom render pipeline, falling back to BIRP settings");
            renderPipeType = 0;
        }
        checkedRenderPipe = true;
    }
    //Build Surface Type options BIRP
    public void SurfaceOptionsBIRP(MaterialEditor materialEditor)
    {
        GUIStyle backdropStyle = makeBackdrop();

        Material material = materialEditor.target as Material;
        if (material != null)
        {
            EditorGUILayout.LabelField("Surface Options", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(backdropStyle);

            // Surface Type Dropdown
            int surfaceType = (int)material.GetFloat("_BUILTIN_Surface");

            surfaceType = EditorGUILayout.Popup("Surface Type", surfaceType, new string[] { "Opaque", "Transparent" });
            material.SetFloat("_BUILTIN_Surface", surfaceType);

            // Transparent
            if (surfaceType == 1)
            {
                material.SetFloat("_BUILTIN_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_BUILTIN_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_BUILTIN_ZWrite", 0);
                material.EnableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_ZWRITE_ON");
                material.EnableKeyword("_BUILTIN_SURFACE_TYPE_TRANSPARENT");
            }
            else // Opaque
            {
                material.SetFloat("_BUILTIN_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_BUILTIN_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                material.SetFloat("_BUILTIN_ZWrite", 1);
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ZWRITE_ON");
                material.DisableKeyword("_BUILTIN_SURFACE_TYPE_TRANSPARENT");
            }

            // Render Face Type
            int renderFace = (int)material.GetFloat("_BUILTIN_CullMode");
            renderFace = EditorGUILayout.Popup("Render Face", renderFace, new string[] { "Both", "Back", "Front" });
            material.SetFloat("_BUILTIN_CullMode", renderFace);

            // Alpha Clipping
            bool alphaClip = material.GetFloat("_BUILTIN_AlphaClip") == 1;
            alphaClip = EditorGUILayout.Toggle("Alpha Clipping", alphaClip);
            material.SetFloat("_BUILTIN_AlphaClip", alphaClip ? 1 : 0);

            if (alphaClip)
            {
                material.EnableKeyword("_BUILTIN_ALPHATEST_ON");
                material.EnableKeyword("_BUILTIN_AlphaClip");
            }
            else
            {
                material.DisableKeyword("_BUILTIN_ALPHATEST_ON");
                material.DisableKeyword("_BUILTIN_AlphaClip");
            }


            EditorGUILayout.EndVertical();
            EditorGUILayout.Separator();

            material.shader = Shader.Find(material.shader.name);

        }
    }

    //Build Surface Type options URP
    public void SurfaceOptionsURP(MaterialEditor materialEditor)
    {
        GUIStyle backdropStyle = makeBackdrop();

        Material material = materialEditor.target as Material;
        if (material != null)
        {
            EditorGUILayout.LabelField("Surface Options", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(backdropStyle);

            // Surface Type Dropdown
            int surfaceType = (int)material.GetFloat("_Surface");

            surfaceType = EditorGUILayout.Popup("Surface Type", surfaceType, new string[] { "Opaque", "Transparent" });
            material.SetFloat("_Surface", surfaceType);

            // Transparent
            if (surfaceType == 1)
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.EnableKeyword("_ZWRITE_ON");
            }
            else // Opaque
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                material.SetFloat("_ZWrite", 1);
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ZWRITE_ON");
            }

            // Render Face Type
            int renderFace = (int)material.GetFloat("_Cull");
            renderFace = EditorGUILayout.Popup("Render Face", renderFace, new string[] { "Both", "Back", "Front" });
            material.SetFloat("_Cull", renderFace);

            // Alpha Clipping
            bool alphaClip = material.GetFloat("_AlphaClip") == 1;
            alphaClip = EditorGUILayout.Toggle("Alpha Clipping", alphaClip);
            material.SetFloat("_AlphaClip", alphaClip ? 1 : 0);
            material.SetFloat("_AlphaToMask", alphaClip ? 1 : 0);
            if (alphaClip)
            {
                material.EnableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
            }
            else
            {
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
            }


            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();

            material.shader = Shader.Find(material.shader.name);

        }
    }

    //used to setup the tabs and what settings to call
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        #region Surface Type Properties
        //Set Default RenderType's
        if (!checkedRenderPipe)
        {
            Material material = materialEditor.target as Material;
            CheckRenderPipeline();

            //Set BIRP
            if (renderPipeType == 0)
            {
                //Make opaque
                material.SetFloat("_BUILTIN_Surface", 0);
                material.SetFloat("_BUILTIN_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_BUILTIN_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                material.SetFloat("_BUILTIN_ZWrite", 1);
                material.DisableKeyword("_ZWRITE_ON");
                material.DisableKeyword("_BUILTIN_SURFACE_TYPE_TRANSPARENT");
                //alpha clip on
                material.SetFloat("_BUILTIN_AlphaClip", 0);
                material.EnableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_BUILTIN_AlphaClip");
                //Render both faces
                material.SetFloat("_BUILTIN_CullMode", 2);
            }
            //Set URP
            else if (renderPipeType == 1)
            {
                //Make opaque
                material.SetFloat("_Surface", 0);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                material.SetFloat("_ZWrite", 1);
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ZWRITE_ON");
                //alpha clip on
                material.SetFloat("_AlphaClip", 1);
                material.SetFloat("_AlphaToMask", 1);
                material.EnableKeyword("_ALPHATEST_ON");
                //Render both faces
                material.SetFloat("_Cull", 2);
            }

        }

        //Manual Options
        showSurfaceType = EditorGUILayout.Foldout(showSurfaceType, "Material Override properties");
        if (showSurfaceType)
        {
            //Set BIRP
            if (renderPipeType == 0)
            {
                SurfaceOptionsBIRP(materialEditor);
            }
            //Set URP
            else if (renderPipeType == 1)
            {
                SurfaceOptionsURP(materialEditor);
            }

        }
        #endregion

        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);


        GUILayout.Space(10);

        switch (selectedTab)
        {
            case 0:
                DrawBaseSettings(materialEditor, properties);
                break;
            case 1:
                DrawOverlaySettings(materialEditor, properties);
                break;
            case 2:
                DrawTriplanarSettings(materialEditor, properties);
                break;
            case 3:
                DrawSnowSettings(materialEditor, properties);
                break;

        }
    }
    #endregion

    #region Vector Gui Custom Properties
    Dictionary<string, string> vecGuiProperties = new Dictionary<string, string>
        {
            { "_UVPan", "Vector2" },
            { "_Base_Tiling", "Vector2" },
            { "_Base_Offset", "Vector2" },
            { "_Overlay_Tiling", "Vector2"},
            { "_Overlay_Offset", "Vector2" },
            { "_Normal_Tiling", "Vector2" },
            { "_Normal_Offset", "Vector2" },
            { "_Emission_Tiling", "Vector2" },
            { "_Emission_Offset", "Vector2" },
            { "_AO_Tiling", "Vector2" },
            { "_AO_Offset", "Vector2" },
            { "_Triplanar_Tiling_Top", "Vector2" },
            { "_Triplanar_Offset_Top", "Vector2" },
            { "_Triplanar_Tiling_Bottom", "Vector2" },
            { "_Triplanar_Offset_Bottom", "Vector2" },
            { "_Triplanar_Tiling_Side", "Vector2" },
            { "_Triplanar_Offset_Side", "Vector2" },
            { "_Triplanar_Normal_Tiling_Top", "Vector2" },
            { "_Triplanar_Normal_Offset_Top", "Vector2" },
            { "_Triplanar_Normal_Tiling_Bottom", "Vector2" },
            { "_Triplanar_Normal_Offset_Bottom", "Vector2" },
            { "_Triplanar_Normal_Tiling_Side", "Vector2" },
            { "_Triplanar_Normal_Offset_Side", "Vector2" }

        };

    #endregion

    #region Gui Backdrops
    private static GUIStyle makeBackdrop()
    {
        GUIStyle backdropStyle = new GUIStyle(GUI.skin.box);
        backdropStyle.padding = new RectOffset(10, 10, 10, 10);
        backdropStyle.margin = new RectOffset(5, 5, 5, 5);
        return backdropStyle;
    }
    #endregion

    //Tab 1
    #region BaseProperties
    bool showBaseTextures = true;
    bool showBaseNormals = true;
    bool showBaseEmissions = true;
    bool showBaseAO = true;
    bool isTriplanarBaseEnabled = false;
    bool isTriplanarEmissionEnabled = false;
    private void DrawBaseSettings(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        GUIStyle backdropStyle = makeBackdrop();

        if (isTriplanarBaseEnabled)
        {
            EditorGUILayout.LabelField(">>> Triplanar enabled, Some Base Parameters Ignored. <<<", EditorStyles.boldLabel);
            EditorGUILayout.Separator();
        }

        GUILayout.Label("Base Properties", EditorStyles.boldLabel);
        // Add properties here
        EditorGUILayout.BeginVertical(backdropStyle);

        string[] shaderProperties =
        {
            "_Color_Tint",
            "_Metallic",
            "_Smoothness",
            "_Metallic_Smoothness_Texture",
            //"_Specular_Color",
            "_UVPan"
        };

        EditorGUILayout.Separator();
        setProperties(materialEditor, properties, shaderProperties, vecGuiProperties);
        EditorGUILayout.EndVertical();


        #region BaseTextures
        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(backdropStyle);
        MaterialProperty materialPropBaseTex = FindProperty("_Enable_Base_Texture", properties);
        materialEditor.ShaderProperty(materialPropBaseTex, "Enable Base Textures");
        if (materialPropBaseTex.floatValue == 1)
        {
            showBaseTextures = EditorGUILayout.Foldout(showBaseTextures, "Base Textures");
            if (showBaseTextures)
            {
                // Base Texture Parameters
                shaderProperties = new[]
                {
                    "_Base_Texture",
                    "_Base_Tiling",
                    "_Base_Offset"
                };

                setProperties(materialEditor, properties, shaderProperties, vecGuiProperties);
            }
        }
        EditorGUILayout.EndVertical();
        #endregion

        EditorGUILayout.Separator();

        #region NormalTextures

        EditorGUILayout.BeginVertical(backdropStyle);
        MaterialProperty materialPropNormalTex = FindProperty("_Enable_Normal_Texture", properties);
        materialEditor.ShaderProperty(materialPropNormalTex, "Enable Normal Texture");

        if (materialPropNormalTex.floatValue == 1)
        {
            showBaseNormals = EditorGUILayout.Foldout(showBaseNormals, "Base Normal Textures");
            if (showBaseNormals)
            {



                // Normal Texture Parameters
                shaderProperties = new[]
                {
                    "_Normal_Texture",
                    "_Normal_Intensity",
                    "_Normal_Tiling",
                    "_Normal_Offset"

                };

                setProperties(materialEditor, properties, shaderProperties, vecGuiProperties);
            }
        }
        EditorGUILayout.EndVertical();
        #endregion

        EditorGUILayout.Separator();

        #region EmissionTextures

        EditorGUILayout.BeginVertical(backdropStyle);
        MaterialProperty materialPropEmissionTex = FindProperty("_Enable_Emission_Texture", properties);
        materialEditor.ShaderProperty(materialPropEmissionTex, "Enable Emission Texture");
        if (materialPropEmissionTex.floatValue == 1)
        {
            showBaseEmissions = EditorGUILayout.Foldout(showBaseEmissions, "Base Emission Textures");
            if (isTriplanarEmissionEnabled)
            {
                EditorGUILayout.LabelField(">>> Emission Triplanar Enabled, Section Overriden <<<", EditorStyles.boldLabel);
                EditorGUILayout.Separator();
            }
            if (showBaseEmissions)
            {
                // Emission Texture Parameters
                shaderProperties = new[]
                {
                    "_Emission_Texture",
                    "_Emission_Tiling",
                    "_Emission_Offset",
                    "_Emission_Color_Tint"
                };

                setProperties(materialEditor, properties, shaderProperties, vecGuiProperties);
            }
        }
        EditorGUILayout.EndVertical();
        #endregion

        EditorGUILayout.Separator();

        #region Ambient Occlusion
        EditorGUILayout.BeginVertical(backdropStyle);

        MaterialProperty materialPropBaseAO = FindProperty("_Enable_Ambient_Occlusion", properties);
        materialEditor.ShaderProperty(materialPropBaseAO, "Enable Ambient Occlusion");
        if (materialPropBaseAO.floatValue == 1)
        {
            showBaseAO = EditorGUILayout.Foldout(showBaseAO, "Ambient Occlusion");
            if (showBaseAO)
            {
                // Base Texture Parameters
                shaderProperties = new[]
                {
                    "_AO_Intensity",
                    "_AO_Texture",
                    "_AO_Tiling",
                    "_AO_Offset",
                    "_Generate_From_Base_Normals"
                };

                setProperties(materialEditor, properties, shaderProperties, vecGuiProperties);
            }
        }
        EditorGUILayout.EndVertical();
        #endregion

    }

    #endregion

    //Tab 2
    #region OverlayProperties
    private void DrawOverlaySettings(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        GUIStyle backdropStyleOverlay = makeBackdrop();
        GUILayout.Label("Overlay Texture Properties", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(backdropStyleOverlay);

        MaterialProperty materialPropOverlayTex = FindProperty("_Enable_Overlay_Texture", properties);
        materialEditor.ShaderProperty(materialPropOverlayTex, "Enable Overlay Texture");
        if (materialPropOverlayTex.floatValue == 1)
        {
            string[] shaderProperties =
            {
                "_Overlay_Texture",
                "_Overlay_Tiling",
                "_Overlay_Offset",
                "_OVERLAY_UV_CHANNEL",
                "_Overlay_Intensity"
            };

            setProperties(materialEditor, properties, shaderProperties, vecGuiProperties);
        }
        EditorGUILayout.EndVertical();
    }
    #endregion

    //Tab 3
    #region TriplanarProperties

    bool enableTriplanarBase = true;
    bool enableTriplanarNormals = true;
    bool enableTriplanarEmission = true;

    private void DrawTriplanarSettings(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        GUIStyle backdropStyleTriplanar = makeBackdrop();
        #region Base Triplanar Textures

        EditorGUILayout.BeginVertical(backdropStyleTriplanar);
        MaterialProperty materialPropTriplanarBase = FindProperty("_Enable_Triplanar_Texture", properties);
        materialEditor.ShaderProperty(materialPropTriplanarBase, "Enable Base Triplanar Textures");

        if (materialPropTriplanarBase.floatValue == 1)
        {
            isTriplanarBaseEnabled = true;
            enableTriplanarBase = EditorGUILayout.Foldout(enableTriplanarBase, "Triplanar Base Textures");
            if (enableTriplanarBase)
            {
                // Triplanar Base 
                string[] shaderProperties =
                {
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
                    "_Triplanar_Fade",
                    "_Triplanar_Intensity",
                    "_Top_Metallic",
                    "_Side_Metallic",
                    "_Bottom_Metallic",
                    "_Top_Smoothness",
                    "_Side_Smoothness",
                    "_Bottom_Smoothness"

                };

                setProperties(materialEditor, properties, shaderProperties, vecGuiProperties);
            }

        }
        else
        {
            isTriplanarBaseEnabled = false;
        }
        EditorGUILayout.EndVertical();
        #endregion
        EditorGUILayout.Separator();
        #region Normal Triplanar Textures
        EditorGUILayout.BeginVertical(backdropStyleTriplanar);
        MaterialProperty materialPropTriplanarNormals = FindProperty("_Enable_Triplanar_Normals", properties);
        materialEditor.ShaderProperty(materialPropTriplanarNormals, "Enable Triplanar Normals");

        if (materialPropTriplanarNormals.floatValue == 1)
        {
            enableTriplanarNormals = EditorGUILayout.Foldout(enableTriplanarNormals, "Triplanar Normal Textures");
            if (enableTriplanarNormals)
            {
                // Triplanar Base 
                string[] shaderProperties =
                {
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

                setProperties(materialEditor, properties, shaderProperties, vecGuiProperties);
            }
        }
        EditorGUILayout.EndVertical();
        #endregion
        EditorGUILayout.Separator();
        #region Emission Triplanar Textures
        EditorGUILayout.BeginVertical(backdropStyleTriplanar);
        MaterialProperty materialPropTriplanarEmission = FindProperty("_Enable_Triplanar_Emission", properties);
        materialEditor.ShaderProperty(materialPropTriplanarEmission, "Enable Triplanar Emission");

        if (materialPropTriplanarEmission.floatValue == 1)
        {
            isTriplanarEmissionEnabled = true;
            enableTriplanarEmission = EditorGUILayout.Foldout(enableTriplanarEmission, "Triplanar Emission Textures");
            if (enableTriplanarEmission)
            {
                // Triplanar Base 
                string[] shaderProperties =
                {
                    "_Triplanar_Emission_Texture",
                    "_Triplanar_Emission_Tiling",
                    "_Triplanar_Emission_Blend",
                    "_Triplanar_Emission_Intensity",
                    "_Triplanar_Emission_Color_Tint"
                };

                setProperties(materialEditor, properties, shaderProperties, vecGuiProperties);
            }

        }
        else
        {
            isTriplanarEmissionEnabled = false;
        }
        EditorGUILayout.EndVertical();
        #endregion

    }
    #endregion

    //Tab 4
    #region SnowProperties
    private void DrawSnowSettings(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        GUIStyle backdropStyleSnow = makeBackdrop();
        GUILayout.Label("Snow Properties", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(backdropStyleSnow);
        MaterialProperty materialPropSnow = FindProperty("_Enable_Snow", properties);
        materialEditor.ShaderProperty(materialPropSnow, "Enable Snow");

        if (materialPropSnow.floatValue == 1)
        {
            string[] shaderProperties =
            {
                "_Snow_Use_World_Up",
                "_Snow_Color",
                "_Snow_Metallic",
                "_Snow_Smoothness",
                "_Snow_Level",
                "_Snow_Transition"

            };

            setProperties(materialEditor, properties, shaderProperties, vecGuiProperties);
        }
        EditorGUILayout.EndVertical();
    }
    #endregion

    #region Create Properties
    //Gathers list of properties to sort and display in inspector
    private static void setProperties(MaterialEditor materialEditor, MaterialProperty[] properties, string[] shaderProperties, Dictionary<string, string> vecGuiProperties)
    {
        foreach (string property in shaderProperties)
        {
            MaterialProperty propertyReference = FindProperty(property, properties);

            if (vecGuiProperties.ContainsKey(property))
            {
                string type = vecGuiProperties[property];

                if (type == "Vector2")
                {
                    Vector2 vec2Value = new Vector2(propertyReference.vectorValue.x, propertyReference.vectorValue.y);
                    vec2Value = EditorGUILayout.Vector2Field(propertyReference.displayName, vec2Value);
                    propertyReference.vectorValue = new Vector4(vec2Value.x, vec2Value.y, 0, 0);
                }
                else if (type == "Vector3")
                {
                    Vector3 vec3Value = new Vector3(propertyReference.vectorValue.x, propertyReference.vectorValue.y, propertyReference.vectorValue.z);
                    vec3Value = EditorGUILayout.Vector3Field(propertyReference.displayName, vec3Value);
                    propertyReference.vectorValue = new Vector4(vec3Value.x, vec3Value.y, vec3Value.z, 0);
                }

            }
            else
            {
                materialEditor.ShaderProperty(propertyReference, propertyReference.displayName);
            }


        }
    }
    #endregion

}
