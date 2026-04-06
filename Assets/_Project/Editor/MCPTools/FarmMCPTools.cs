using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
/// <summary>
/// Custom MCP tools for FarmSim VR development.
/// These can be called via unity-mcp's execute_custom_tool.
/// </summary>
public static class FarmMCPTools
{
    /// <summary>
    /// Create a complete crop plot with all components pre-wired.
    /// Usage: create_crop_plot Tomato 2 3
    /// </summary>
    [MenuItem("FarmSim/MCP/Create Crop Plot (Test)")]
    public static void CreateCropPlotMenu()
    {
        CreateCropPlot("Test", 0, 0);
    }

    public static string CreateCropPlot(string cropName, float x, float z)
    {
        var go = new GameObject($"CropPlot_{cropName}");
        go.transform.position = new Vector3(x, 0, z);
        go.tag = "Untagged";
        go.layer = LayerMask.NameToLayer("Default");

        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(1f, 0.1f, 1f);
        col.isTrigger = true;

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "SoilMesh";
        visual.transform.SetParent(go.transform);
        visual.transform.localPosition = new Vector3(0, -0.05f, 0);
        visual.transform.localScale = new Vector3(1f, 0.1f, 1f);

        Undo.RegisterCreatedObjectUndo(go, $"Create CropPlot_{cropName}");

        return $"Created crop plot '{cropName}' at ({x}, 0, {z})";
    }

    /// <summary>
    /// Bulk create a grid of crop plots.
    /// Usage: create_crop_grid 3 4 1.5
    /// </summary>
    public static string CreateCropGrid(int rows, int cols, float spacing)
    {
        var parent = new GameObject("CropGrid");
        int count = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var go = new GameObject($"CropPlot_{r}_{c}");
                go.transform.SetParent(parent.transform);
                go.transform.localPosition = new Vector3(
                    c * spacing, 0, r * spacing);
                count++;
            }
        }

        Undo.RegisterCreatedObjectUndo(parent, "Create CropGrid");

        return $"Created {count} crop plots in {rows}x{cols} grid";
    }

    /// <summary>
    /// Quick Quest performance check against budgets.
    /// Usage: quest_perf_check
    /// </summary>
    public static string QuestPerfCheck()
    {
        var drawCalls = UnityStats.drawCalls;
        var tris = UnityStats.triangles;
        var texMem = UnityEngine.Profiling.Profiler
            .GetTotalAllocatedMemoryLong() / (1024 * 1024);

        var result = $"Draw calls: {drawCalls} (budget: 100)\n";
        result += $"Triangles: {tris} (budget: 750K)\n";
        result += $"Memory: {texMem}MB\n";

        if (drawCalls > 100)
            result += "WARNING: OVER draw call budget\n";
        if (tris > 750000)
            result += "WARNING: OVER triangle budget\n";

        return result;
    }
    /// <summary>
    /// Check a model asset against Quest performance budgets.
    /// Usage: check_model_budget "Assets/_Project/Art/Models/Tools/Tool_WateringCan.glb"
    /// </summary>
    public static string CheckModelBudget(string assetPath)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (go == null) return $"Asset not found: {assetPath}";

        var filters = go.GetComponentsInChildren<MeshFilter>();
        int totalTris = 0;
        int totalVerts = 0;
        int meshCount = 0;

        foreach (var mf in filters)
        {
            if (mf.sharedMesh != null)
            {
                totalTris += mf.sharedMesh.triangles.Length / 3;
                totalVerts += mf.sharedMesh.vertexCount;
                meshCount++;
            }
        }

        var renderers = go.GetComponentsInChildren<Renderer>();
        int matCount = 0;
        foreach (var r in renderers)
            matCount += r.sharedMaterials.Length;

        var skinned = go.GetComponentsInChildren<SkinnedMeshRenderer>();
        int boneCount = 0;
        foreach (var smr in skinned)
            if (smr.bones != null) boneCount += smr.bones.Length;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Model: {assetPath}");
        sb.AppendLine($"Meshes: {meshCount}");
        sb.AppendLine($"Triangles: {totalTris:N0}");
        sb.AppendLine($"Vertices: {totalVerts:N0}");
        sb.AppendLine($"Materials: {matCount}");
        sb.AppendLine($"Bones: {boneCount}");
        sb.AppendLine();

        if (totalTris > 50000)
            sb.AppendLine("FAIL: OVER 50K tri hard limit");
        else if (totalTris > 10000)
            sb.AppendLine("WARN: Over hero budget (10K), OK for structures");
        else if (totalTris > 2000)
            sb.AppendLine("OK: Within hero object budget");
        else
            sb.AppendLine("OK: Within prop budget");

        if (matCount > 5)
            sb.AppendLine("WARN: Many materials — consider atlasing");
        if (boneCount > 75)
            sb.AppendLine("WARN: Over Quest bone budget (75)");

        return sb.ToString();
    }

    /// <summary>
    /// Find .glb/.gltf files that haven't been organized into Art/Models/.
    /// Usage: list_unprocessed_glbs
    /// </summary>
    public static string ListUnprocessedGlbs()
    {
        var guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets" });
        var unprocessed = new System.Collections.Generic.List<string>();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".glb") || path.EndsWith(".gltf"))
            {
                if (!path.Contains("Assets/_Project/Art/Models/"))
                {
                    unprocessed.Add(path);
                }
            }
        }

        if (unprocessed.Count == 0)
            return "No unprocessed .glb files found.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Found {unprocessed.Count} unprocessed .glb file(s):");
        foreach (var p in unprocessed)
            sb.AppendLine($"  - {p}");
        return sb.ToString();
    }
}
#endif
