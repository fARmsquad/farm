using UnityEditor;
using UnityEngine;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// Partial class: world-scattered vegetation (trees, grass, flowers, bushes, rocks, ferns).
    /// </summary>
    public static partial class WorldSceneBuilder
    {
        private static void BuildVegetation()
        {
            var vegRoot = CreateEmpty("Vegetation", Vector3.zero);
            var treesParent = CreateEmpty("Trees", Vector3.zero, vegRoot.transform);
            var groundCover = CreateEmpty("GroundCover", Vector3.zero, vegRoot.transform);
            var flowersParent = CreateEmpty("Flowers", Vector3.zero, vegRoot.transform);
            var bushesParent = CreateEmpty("Bushes", Vector3.zero, vegRoot.transform);
            var rocksParent = CreateEmpty("Rocks", Vector3.zero, vegRoot.transform);
            var fernsParent = CreateEmpty("Ferns", Vector3.zero, vegRoot.transform);

            // ── Trees (~20 placed) ──────────────────────────────
            string[] treePrefabs = {
                "Assets/PolygonNature/Prefabs/Trees/SM_Tree_Birch_01.prefab",
                "Assets/PolygonNature/Prefabs/Trees/SM_Tree_Birch_02.prefab",
                "Assets/PolygonNature/Prefabs/Trees/SM_Tree_Birch_03.prefab",
                "Assets/PolygonNature/Prefabs/Trees/SM_Tree_Pine_01.prefab",
                "Assets/PolygonNature/Prefabs/Trees/SM_Tree_Pine_02.prefab",
                "Assets/PolygonNature/Prefabs/Trees/SM_Tree_Pine_Large_01.prefab",
                "Assets/PolygonNature/Prefabs/Trees/SM_Tree_Willow_Medium_01.prefab",
                "Assets/PolygonNature/Prefabs/Trees/SM_Tree_Willow_Large_01.prefab",
            };

            Vector3[] treePositions = {
                // North Field
                new(-108f, 0f, 108f), new(-60f, 0f, 96f), new(-36f, 0f, 78f),
                // Meadow edges
                new(-114f, 0f, -54f), new(-42f, 0f, -60f), new(-84f, 0f, -90f),
                // River banks (willows)
                new(-30f, 0f, -60f), new(0f, 0f, -63f), new(30f, 0f, -57f), new(54f, 0f, -60f),
                // Trail sides
                new(-18f, 0f, 42f), new(18f, 0f, 39f), new(-15f, 0f, 24f),
                // Sandy edges
                new(30f, 0f, 84f), new(114f, 0f, 78f),
                // Hills
                new(-60f, 2f, -105f), new(30f, 2f, -111f), new(90f, 1f, -102f),
                // Fair edges
                new(75f, 0f, -51f), new(117f, 0f, -54f),
            };

            var treeRng = new System.Random(42);
            for (int i = 0; i < treePositions.Length; i++)
            {
                string prefab = treePrefabs[i % treePrefabs.Length];
                float yRot = (float)(treeRng.NextDouble() * 360.0);
                var tree = InstantiatePrefab(prefab, treePositions[i],
                    Quaternion.Euler(0f, yRot, 0f), treesParent.transform);
                tree.name = $"Tree_{i}";
            }

            // Dead trees
            InstantiatePrefab("Assets/PolygonNature/Prefabs/Trees/SM_Tree_Dead_01.prefab",
                new Vector3(-18f, 0f, -78f), Quaternion.identity, treesParent.transform).name = "DeadTree_0";
            InstantiatePrefab("Assets/PolygonNature/Prefabs/Trees/SM_Tree_Dead_01.prefab",
                new Vector3(42f, 0f, -81f), Quaternion.identity, treesParent.transform).name = "DeadTree_1";

            // Stumps
            InstantiatePrefab("Assets/PolygonNature/Prefabs/Trees/SM_Tree_Stump_01.prefab",
                new Vector3(-90f, 0f, 66f), Quaternion.identity, treesParent.transform).name = "Stump_0";
            InstantiatePrefab("Assets/PolygonNature/Prefabs/Trees/SM_Tree_Stump_01.prefab",
                new Vector3(18f, 0f, -102f), Quaternion.identity, treesParent.transform).name = "Stump_1";

            // ── Ground Cover (~150 grass patches) ───────────────
            string[] grassPrefabs = {
                "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Grass_Patch_01.prefab",
                "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Grass_Patch_02.prefab",
                "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Grass_Patch_03.prefab",
            };

            var grassRng = new System.Random(42);
            for (int i = 0; i < 150; i++)
            {
                float x = -118f + (float)(grassRng.NextDouble() * 236.0);
                float z = -118f + (float)(grassRng.NextDouble() * 236.0);

                // Skip Farm zone
                if (x > 27f && x < 117f && z > -9f && z < 57f) continue;
                // Skip Town zone
                if (x < -27f && x > -117f && z > -45f && z < 57f) continue;

                float yRot = (float)(grassRng.NextDouble() * 360.0);
                var grass = InstantiatePrefab(grassPrefabs[i % grassPrefabs.Length],
                    new Vector3(x, 0f, z), Quaternion.Euler(0f, yRot, 0f), groundCover.transform);
                grass.name = $"GrassPatch_{i}";
            }

            // ── Flowers (~40 in Meadow/NorthField/Hills) ────────
            string[] flowerPrefabs = {
                "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Flowers_01.prefab",
                "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Flowers_02.prefab",
                "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Flowers_03.prefab",
            };

            var flowerRng = new System.Random(42);
            for (int i = 0; i < 40; i++)
            {
                float x, z;
                int zone = i % 3;
                if (zone == 0) // Meadow
                {
                    x = -108f + (float)(flowerRng.NextDouble() * 72.0);
                    z = -93f + (float)(flowerRng.NextDouble() * 42.0);
                }
                else if (zone == 1) // North Field
                {
                    x = -114f + (float)(flowerRng.NextDouble() * 84.0);
                    z = 63f + (float)(flowerRng.NextDouble() * 54.0);
                }
                else // Hills
                {
                    x = -114f + (float)(flowerRng.NextDouble() * 228.0);
                    z = -117f + (float)(flowerRng.NextDouble() * 21.0);
                }

                var flower = InstantiatePrefab(flowerPrefabs[i % flowerPrefabs.Length],
                    new Vector3(x, 0f, z), Quaternion.identity, flowersParent.transform);
                flower.name = $"Flower_{i}";
            }

            // ── Bushes (~20) ────────────────────────────────────
            string[] bushPrefabs = {
                "Assets/PolygonNature/Prefabs/Plants/SM_Plant_Bush_01.prefab",
                "Assets/PolygonNature/Prefabs/Plants/SM_Plant_Bush_02.prefab",
                "Assets/PolygonNature/Prefabs/Plants/SM_Plant_Bush_03.prefab",
            };

            var bushRng = new System.Random(42);
            for (int i = 0; i < 20; i++)
            {
                float x = -118f + (float)(bushRng.NextDouble() * 236.0);
                float z = -118f + (float)(bushRng.NextDouble() * 236.0);

                // Skip Farm zone
                if (x > 27f && x < 117f && z > -9f && z < 57f) continue;
                // Skip Town zone
                if (x < -27f && x > -117f && z > -45f && z < 57f) continue;

                var bush = InstantiatePrefab(bushPrefabs[i % bushPrefabs.Length],
                    new Vector3(x, 0f, z), Quaternion.identity, bushesParent.transform);
                bush.name = $"Bush_{i}";
            }

            // ── Rocks (~25) ─────────────────────────────────────
            string[] rockPrefabs = {
                "Assets/PolygonNature/Prefabs/Rocks/SM_Rock_Small_01.prefab",
                "Assets/PolygonNature/Prefabs/Rocks/SM_Rock_Small_02.prefab",
                "Assets/PolygonNature/Prefabs/Rocks/SM_Rock_Cluster_Large_01.prefab",
                "Assets/PolygonNature/Prefabs/Rocks/SM_Rock_Boulder_01.prefab",
            };

            var rockRng = new System.Random(42);
            // 15 along river
            for (int i = 0; i < 15; i++)
            {
                float x = -30f + (float)(rockRng.NextDouble() * 96.0);
                float z = -84f + (float)(rockRng.NextDouble() * 24.0);
                var rock = InstantiatePrefab(rockPrefabs[i % rockPrefabs.Length],
                    new Vector3(x, 0f, z), Quaternion.identity, rocksParent.transform);
                rock.name = $"RiverRock_{i}";
            }
            // 10 along hills
            for (int i = 0; i < 10; i++)
            {
                float x = -108f + (float)(rockRng.NextDouble() * 216.0);
                float z = -117f + (float)(rockRng.NextDouble() * 18.0);
                var rock = InstantiatePrefab(rockPrefabs[i % rockPrefabs.Length],
                    new Vector3(x, 0f, z), Quaternion.identity, rocksParent.transform);
                rock.name = $"HillRock_{i}";
            }

            // ── Ferns (~15) ─────────────────────────────────────
            string[] fernPrefabs = {
                "Assets/PolygonNature/Prefabs/Plants/SM_Plant_Fern_01.prefab",
                "Assets/PolygonNature/Prefabs/Plants/SM_Plant_Fern_02.prefab",
                "Assets/PolygonNature/Prefabs/Plants/SM_Plant_Fern_03.prefab",
            };

            var fernRng = new System.Random(42);
            for (int i = 0; i < 15; i++)
            {
                float x = -102f + (float)(fernRng.NextDouble() * 120.0);
                float z = -93f + (float)(fernRng.NextDouble() * 48.0);
                var fern = InstantiatePrefab(fernPrefabs[i % fernPrefabs.Length],
                    new Vector3(x, 0f, z), Quaternion.identity, fernsParent.transform);
                fern.name = $"Fern_{i}";
            }

            Debug.Log("[WorldSceneBuilder] World vegetation scattered (trees, grass, flowers, bushes, rocks, ferns).");
        }
    }
}
