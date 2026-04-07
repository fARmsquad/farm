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
                "Assets/PolygonNature/Prefabs/Trees/SM_Tree_Pine_03.prefab",
                "Assets/PolygonNature/Prefabs/Trees/SM_Tree_Willow_Medium_01.prefab",
                "Assets/PolygonNature/Prefabs/Trees/SM_Tree_Willow_Large_01.prefab",
            };

            Vector3[] treePositions = {
                // North Field
                new(-180f, 0f, 180f), new(-100f, 0f, 160f), new(-60f, 0f, 130f),
                // Meadow edges
                new(-190f, 0f, -90f), new(-70f, 0f, -100f), new(-140f, 0f, -150f),
                // River banks (willows)
                new(-50f, 0f, -100f), new(0f, 0f, -105f), new(50f, 0f, -95f), new(90f, 0f, -100f),
                // Trail sides
                new(-30f, 0f, 70f), new(30f, 0f, 65f), new(-25f, 0f, 40f),
                // Sandy edges
                new(50f, 0f, 140f), new(190f, 0f, 130f),
                // Hills
                new(-100f, 2f, -175f), new(50f, 2f, -185f), new(150f, 1f, -170f),
                // Fair edges
                new(125f, 0f, -85f), new(195f, 0f, -90f),
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
                new Vector3(-30f, 0f, -130f), Quaternion.identity, treesParent.transform).name = "DeadTree_0";
            InstantiatePrefab("Assets/PolygonNature/Prefabs/Trees/SM_Tree_Dead_01.prefab",
                new Vector3(70f, 0f, -135f), Quaternion.identity, treesParent.transform).name = "DeadTree_1";

            // Stumps
            InstantiatePrefab("Assets/PolygonNature/Prefabs/Trees/SM_Tree_Stump_01.prefab",
                new Vector3(-150f, 0f, 110f), Quaternion.identity, treesParent.transform).name = "Stump_0";
            InstantiatePrefab("Assets/PolygonNature/Prefabs/Trees/SM_Tree_Stump_01.prefab",
                new Vector3(30f, 0f, -170f), Quaternion.identity, treesParent.transform).name = "Stump_1";

            // ── Ground Cover (~150 grass patches) ───────────────
            string[] grassPrefabs = {
                "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Grass_Patch_01.prefab",
                "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Grass_Patch_02.prefab",
                "Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Grass_Patch_03.prefab",
            };

            var grassRng = new System.Random(42);
            for (int i = 0; i < 150; i++)
            {
                float x = -190f + (float)(grassRng.NextDouble() * 380.0);
                float z = -190f + (float)(grassRng.NextDouble() * 380.0);

                // Skip Farm zone
                if (x > 45f && x < 195f && z > -15f && z < 95f) continue;
                // Skip Town zone
                if (x < -45f && x > -195f && z > -75f && z < 95f) continue;

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
                    x = -180f + (float)(flowerRng.NextDouble() * 120.0);
                    z = -155f + (float)(flowerRng.NextDouble() * 70.0);
                }
                else if (zone == 1) // North Field
                {
                    x = -190f + (float)(flowerRng.NextDouble() * 140.0);
                    z = 105f + (float)(flowerRng.NextDouble() * 90.0);
                }
                else // Hills
                {
                    x = -190f + (float)(flowerRng.NextDouble() * 380.0);
                    z = -195f + (float)(flowerRng.NextDouble() * 35.0);
                }

                var flower = InstantiatePrefab(flowerPrefabs[i % flowerPrefabs.Length],
                    new Vector3(x, 0f, z), Quaternion.identity, flowersParent.transform);
                flower.name = $"Flower_{i}";
            }

            // ── Bushes (~20) ────────────────────────────────────
            string[] bushPrefabs = {
                "Assets/PolygonNature/Prefabs/Plants/SM_Bush_01.prefab",
                "Assets/PolygonNature/Prefabs/Plants/SM_Bush_02.prefab",
                "Assets/PolygonNature/Prefabs/Plants/SM_Bush_03.prefab",
            };

            var bushRng = new System.Random(42);
            for (int i = 0; i < 20; i++)
            {
                float x = -190f + (float)(bushRng.NextDouble() * 380.0);
                float z = -190f + (float)(bushRng.NextDouble() * 380.0);

                // Skip Farm zone
                if (x > 45f && x < 195f && z > -15f && z < 95f) continue;
                // Skip Town zone
                if (x < -45f && x > -195f && z > -75f && z < 95f) continue;

                var bush = InstantiatePrefab(bushPrefabs[i % bushPrefabs.Length],
                    new Vector3(x, 0f, z), Quaternion.identity, bushesParent.transform);
                bush.name = $"Bush_{i}";
            }

            // ── Rocks (~25) ─────────────────────────────────────
            string[] rockPrefabs = {
                "Assets/PolygonNature/Prefabs/Rocks/SM_Rock_Small_01.prefab",
                "Assets/PolygonNature/Prefabs/Rocks/SM_Rock_Small_02.prefab",
                "Assets/PolygonNature/Prefabs/Rocks/SM_Rock_Large_01.prefab",
                "Assets/PolygonNature/Prefabs/Rocks/SM_Rock_Boulder_01.prefab",
            };

            var rockRng = new System.Random(42);
            // 15 along river
            for (int i = 0; i < 15; i++)
            {
                float x = -50f + (float)(rockRng.NextDouble() * 160.0);
                float z = -140f + (float)(rockRng.NextDouble() * 40.0);
                var rock = InstantiatePrefab(rockPrefabs[i % rockPrefabs.Length],
                    new Vector3(x, 0f, z), Quaternion.identity, rocksParent.transform);
                rock.name = $"RiverRock_{i}";
            }
            // 10 along hills
            for (int i = 0; i < 10; i++)
            {
                float x = -180f + (float)(rockRng.NextDouble() * 360.0);
                float z = -195f + (float)(rockRng.NextDouble() * 30.0);
                var rock = InstantiatePrefab(rockPrefabs[i % rockPrefabs.Length],
                    new Vector3(x, 0f, z), Quaternion.identity, rocksParent.transform);
                rock.name = $"HillRock_{i}";
            }

            // ── Ferns (~15) ─────────────────────────────────────
            string[] fernPrefabs = {
                "Assets/PolygonNature/Prefabs/Plants/SM_Fern_01.prefab",
                "Assets/PolygonNature/Prefabs/Plants/SM_Fern_02.prefab",
                "Assets/PolygonNature/Prefabs/Plants/SM_Fern_03.prefab",
            };

            var fernRng = new System.Random(42);
            for (int i = 0; i < 15; i++)
            {
                float x = -170f + (float)(fernRng.NextDouble() * 200.0);
                float z = -155f + (float)(fernRng.NextDouble() * 80.0);
                var fern = InstantiatePrefab(fernPrefabs[i % fernPrefabs.Length],
                    new Vector3(x, 0f, z), Quaternion.identity, fernsParent.transform);
                fern.name = $"Fern_{i}";
            }

            Debug.Log("[WorldSceneBuilder] World vegetation scattered (trees, grass, flowers, bushes, rocks, ferns).");
        }
    }
}
