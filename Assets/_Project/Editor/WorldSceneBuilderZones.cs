using UnityEditor;
using UnityEngine;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// Partial class: zone population methods (water, paths, farm, town, etc.).
    /// </summary>
    public static partial class WorldSceneBuilder
    {
        // ── Water ────────────────────────────────────────────────

        private static void BuildWater()
        {
            var waterRoot = CreateEmpty("Water", Vector3.zero);

            // ── River ──
            var riverParent = CreateEmpty("River", Vector3.zero, waterRoot.transform);
            Vector3[] riverPoints = {
                new(-24f, -0.8f, -72f),
                new(-6f, -0.8f, -69f),
                new(12f, -0.8f, -75f),
                new(30f, -0.8f, -71f),
                new(48f, -0.8f, -73f),
                new(66f, -0.8f, -69f),
            };
            for (int i = 0; i < riverPoints.Length; i++)
            {
                float yRot = (i % 2 == 0) ? 0f : 15f;
                var seg = InstantiatePrefab(
                    "Assets/PolygonNature/Prefabs/Terrain/River_Plane_01.prefab",
                    riverPoints[i], Quaternion.Euler(0f, yRot, 0f), riverParent.transform, false);
                seg.name = $"RiverSegment_{i}";
            }

            // ── River Banks ──
            for (int i = 0; i < riverPoints.Length; i++)
            {
                var northPos = riverPoints[i] + new Vector3(0f, 0f, 10f);
                var northBank = InstantiatePrefab(
                    "Assets/PolygonNature/Prefabs/Terrain/SM_Terrain_RiverSide_01.prefab",
                    northPos, Quaternion.identity, riverParent.transform, false);
                northBank.name = $"RiverBank_North_{i}";

                var southPos = riverPoints[i] + new Vector3(0f, 0f, -10f);
                var southBank = InstantiatePrefab(
                    "Assets/PolygonNature/Prefabs/Terrain/SM_Terrain_RiverSide_01.prefab",
                    southPos, Quaternion.Euler(0f, 180f, 0f), riverParent.transform, false);
                southBank.name = $"RiverBank_South_{i}";
            }

            // ── Reeds along river banks ──
            for (int i = 0; i < 10; i++)
            {
                int ptIdx = i % riverPoints.Length;
                float zOffset = (i % 2 == 0) ? 12f : -12f;
                var reedPos = riverPoints[ptIdx] + new Vector3(i * 2f - 9f, 0.2f, zOffset);
                var reed = InstantiatePrefab(
                    "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Reeds_01.prefab",
                    reedPos, Quaternion.identity, riverParent.transform);
                reed.name = $"Reed_{i}";
            }

            // ── Creek ──
            var creekParent = CreateEmpty("Creek", Vector3.zero, waterRoot.transform);
            var creek0 = InstantiatePrefab(
                "Assets/PolygonNature/Prefabs/Terrain/River_Plane_01.prefab",
                new Vector3(-6f, -0.3f, 33f), Quaternion.Euler(0f, 30f, 0f), creekParent.transform, false);
            creek0.transform.localScale = Vector3.one * 0.4f;
            creek0.name = "CreekSegment_0";

            var creek1 = InstantiatePrefab(
                "Assets/PolygonNature/Prefabs/Terrain/River_Plane_01.prefab",
                new Vector3(6f, -0.3f, 30f), Quaternion.Euler(0f, 30f, 0f), creekParent.transform, false);
            creek1.transform.localScale = Vector3.one * 0.4f;
            creek1.name = "CreekSegment_1";

            // ── Bridge over creek ──
            var bridge = InstantiatePrefab(
                "Assets/PolygonNature/Prefabs/Props/SM_Prop_Bridge_Curved_01.prefab",
                new Vector3(0f, 0f, 31f), Quaternion.Euler(0f, 90f, 0f), creekParent.transform);
            bridge.name = "CreekBridge";

            // ── Farm Pond (snaps to terrain) ──
            var pond = InstantiatePrefab(
                "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Pond_01.prefab",
                new Vector3(99f, 0f, 12f), Quaternion.identity, waterRoot.transform);
            pond.name = "FarmPond";

            // ── Lily pads ──
            var lilyY = SampleTerrainHeight(99f, 12f) + 0.05f;
            var lilypads = InstantiatePrefab(
                "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Lillypads_01.prefab",
                new Vector3(99f, lilyY, 12f), Quaternion.identity, waterRoot.transform, false);
            lilypads.name = "FarmPondLilypads";

            Debug.Log("[WorldSceneBuilder] Water system built (river, creek, bridge, pond).");
        }

        // ── Paths ────────────────────────────────────────────────
        private static void BuildPaths()
        {
            var pathsRoot = CreateEmpty("Paths", Vector3.zero);

            // ── MainStreet ──
            var mainStreet = CreateEmpty("MainStreet", Vector3.zero, pathsRoot.transform);
            const string gravelStraight = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Road_Gravel_Straight_01.prefab";
            const string gravelEnd = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Road_Gravel_End_01.prefab";

            for (float x = -111f; x <= -33f; x += 10f)
            {
                var seg = InstantiatePrefab(gravelStraight,
                    new Vector3(x, 0.02f, 18f), Quaternion.Euler(0f, 90f, 0f), mainStreet.transform);
                seg.name = $"MainStreet_Straight_{(int)x}";
            }

            var endCapW = InstantiatePrefab(gravelEnd,
                new Vector3(-114f, 0.02f, 18f), Quaternion.Euler(0f, -90f, 0f), mainStreet.transform);
            endCapW.name = "MainStreet_EndCap_West";
            var endCapE = InstantiatePrefab(gravelEnd,
                new Vector3(-30f, 0.02f, 18f), Quaternion.Euler(0f, 90f, 0f), mainStreet.transform);
            endCapE.name = "MainStreet_EndCap_East";

            // ── TrailToFarm ──
            var trailToFarm = CreateEmpty("TrailToFarm", Vector3.zero, pathsRoot.transform);
            const string dirtStraight = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Road_Dirt_Straight_01.prefab";
            const string dirtSwerve = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Road_Dirt_Swerve_01.prefab";

            Vector3[] trailPoints = {
                new(-21f, 0.02f, 18f), new(-15f, 0.02f, 21f),
                new(-9f, 0.02f, 25f),  new(-3f, 0.02f, 29f),
                new(3f, 0.02f, 30f),   new(9f, 0.02f, 30f),
                new(15f, 0.02f, 29f),  new(21f, 0.02f, 27f),
            };
            int[] swerveIndices = { 2, 5 };

            for (int i = 0; i < trailPoints.Length; i++)
            {
                bool isSwerve = System.Array.IndexOf(swerveIndices, i) >= 0;
                string prefab = isSwerve ? dirtSwerve : dirtStraight;
                float yRot;
                if (i < trailPoints.Length - 1)
                {
                    Vector3 dir = trailPoints[i + 1] - trailPoints[i];
                    yRot = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                }
                else
                {
                    Vector3 dir = trailPoints[i] - trailPoints[i - 1];
                    yRot = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                }
                var seg = InstantiatePrefab(prefab,
                    trailPoints[i], Quaternion.Euler(0f, yRot, 0f), trailToFarm.transform);
                seg.name = $"Trail_{(isSwerve ? "Swerve" : "Straight")}_{i}";
            }

            // ── FarmPaths ──
            var farmPaths = CreateEmpty("FarmPaths", Vector3.zero, pathsRoot.transform);
            for (float z = 27f; z <= 45f; z += 10f)
            {
                var seg = InstantiatePrefab(dirtStraight,
                    new Vector3(60f, 0.52f, z), Quaternion.identity, farmPaths.transform);
                seg.name = $"FarmPath_NS_{(int)z}";
            }
            for (float x = 42f; x <= 60f; x += 10f)
            {
                var seg = InstantiatePrefab(dirtStraight,
                    new Vector3(x, 0.52f, 33f), Quaternion.Euler(0f, 90f, 0f), farmPaths.transform);
                seg.name = $"FarmPath_EW_{(int)x}";
            }

            Debug.Log("[WorldSceneBuilder] Path network built (MainStreet, TrailToFarm, FarmPaths).");
        }

        // ── Farm Zone ────────────────────────────────────────────
        private static void BuildFarmZone()
        {
            var farm = GameObject.Find("Farm");
            if (farm == null) farm = CreateEmpty("Farm", new Vector3(72f, 0f, 24f));

            var buildings = farm.transform.Find("Buildings") ?? CreateEmpty("Buildings", Vector3.zero, farm.transform).transform;
            var props = farm.transform.Find("Props") ?? CreateEmpty("Props", Vector3.zero, farm.transform).transform;
            var plots = farm.transform.Find("Plots") ?? CreateEmpty("Plots", Vector3.zero, farm.transform).transform;
            var pen = farm.transform.Find("Pen") ?? CreateEmpty("Pen", Vector3.zero, farm.transform).transform;
            var pasture = farm.transform.Find("Pasture") ?? CreateEmpty("Pasture", Vector3.zero, farm.transform).transform;
            var trees = farm.transform.Find("Trees") ?? CreateEmpty("Trees", Vector3.zero, farm.transform).transform;
            var fx = farm.transform.Find("FX") ?? CreateEmpty("FX", Vector3.zero, farm.transform).transform;

            string P(string name) => $"Assets/Synty/PolygonFarm/Prefabs/{name}";

            // ── Buildings ──
            InstantiatePrefab(P("Buildings/SM_Bld_Farmhouse_01.prefab"),
                new Vector3(60f, 0.5f, 36f), Quaternion.Euler(0f, 180f, 0f), buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Barn_01.prefab"),
                new Vector3(84f, 0.5f, 42f), Quaternion.Euler(0f, 90f, 0f), buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Silo_01.prefab"),
                new Vector3(93f, 0.5f, 45f), Quaternion.identity, buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Silo_Small_01.prefab"),
                new Vector3(90f, 0.5f, 39f), Quaternion.identity, buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Greenhouse_01.prefab"),
                new Vector3(36f, 0.5f, 18f), Quaternion.identity, buildings);

            // ── Circular Pen (El Pollo Loco) ──
            Vector3 penCenter = new(72f, 0.5f, 24f);
            const float penRadius = 8f;
            for (int i = 0; i < 12; i++)
            {
                float angle = i * (360f / 12f);
                float rad = angle * Mathf.Deg2Rad;
                var pos = penCenter + new Vector3(Mathf.Sin(rad) * penRadius, 0f, Mathf.Cos(rad) * penRadius);
                string fencePrefab = i == 0
                    ? P("Props/SM_Prop_Fence_Wood_Gate_01.prefab")
                    : P("Props/SM_Prop_Fence_Wood_Round_01.prefab");
                InstantiatePrefab(fencePrefab, pos, Quaternion.Euler(0f, angle, 0f), pen);
            }

            // ── Crop Plots ──
            Vector3[] plotCenters = {
                new(42f, 0.5f, 30f), new(51f, 0.5f, 30f),
                new(42f, 0.5f, 21f), new(51f, 0.5f, 21f),
            };
            for (int c = 0; c < plotCenters.Length; c++)
            {
                var center = plotCenters[c];
                for (int r = 0; r < 6; r++)
                {
                    var rowPos = center + new Vector3(0f, 0f, r * 1.5f - 3.75f);
                    InstantiatePrefab(P("Environments/SM_Env_Dirt_Rows_Center_01.prefab"),
                        rowPos, Quaternion.identity, plots);
                    string vege = (r % 2 == 0)
                        ? P("Environments/SM_Env_Vege_Rows_01.prefab")
                        : P("Environments/SM_Env_Vege_Rows_02.prefab");
                    InstantiatePrefab(vege, rowPos + Vector3.up * 0.05f, Quaternion.identity, plots);
                }
                InstantiatePrefab(P("Environments/SM_Env_Dirt_Skirt_01.prefab"),
                    center + new Vector3(0f, 0f, 5f), Quaternion.identity, plots);
                InstantiatePrefab(P("Environments/SM_Env_Dirt_Skirt_01.prefab"),
                    center + new Vector3(0f, 0f, -5f), Quaternion.identity, plots);
            }

            // ── Pasture (wire fence enclosure) ──
            Vector3 pastureMin = new(93f, 0.5f, 9f);
            Vector3 pastureMax = new(111f, 0.5f, 27f);

            for (float x = pastureMin.x; x <= pastureMax.x; x += 5f)
            {
                InstantiatePrefab(P("Props/SM_Prop_Fence_Wire_01.prefab"),
                    new Vector3(x, 0.5f, pastureMax.z), Quaternion.identity, pasture);
                InstantiatePrefab(P("Props/SM_Prop_Fence_Wire_01.prefab"),
                    new Vector3(x, 0.5f, pastureMin.z), Quaternion.identity, pasture);
            }
            for (float z = pastureMin.z; z <= pastureMax.z; z += 5f)
            {
                InstantiatePrefab(P("Props/SM_Prop_Fence_Wire_01.prefab"),
                    new Vector3(pastureMin.x, 0.5f, z), Quaternion.Euler(0f, 90f, 0f), pasture);
                InstantiatePrefab(P("Props/SM_Prop_Fence_Wire_01.prefab"),
                    new Vector3(pastureMax.x, 0.5f, z), Quaternion.Euler(0f, 90f, 0f), pasture);
            }
            Vector3[] corners = {
                new(pastureMin.x, 0.5f, pastureMin.z), new(pastureMax.x, 0.5f, pastureMin.z),
                new(pastureMin.x, 0.5f, pastureMax.z), new(pastureMax.x, 0.5f, pastureMax.z),
            };
            foreach (var corner in corners)
                InstantiatePrefab(P("Props/SM_Prop_Fence_Wire_Pole_01.prefab"), corner, Quaternion.identity, pasture);
            InstantiatePrefab(P("Props/SM_Prop_Fence_Wire_Gate_01.prefab"),
                new Vector3(102f, 0.5f, pastureMin.z), Quaternion.identity, pasture);

            // ── Props ──
            InstantiatePrefab(P("Props/SM_Prop_Well_01.prefab"), new Vector3(57f, 0.5f, 33f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Scarecrow_01.prefab"), new Vector3(47f, 0.5f, 27f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Hay_Bale_Square_01.prefab"), new Vector3(87f, 0.5f, 37f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Hay_Bale_Square_01.prefab"), new Vector3(89f, 0.5f, 36f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Hay_Bale_Round_01.prefab"), new Vector3(86f, 0.5f, 39f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Crate_01.prefab"), new Vector3(82f, 0.5f, 41f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_PalletCrate_01.prefab"), new Vector3(83f, 0.5f, 41f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Wheelbarrow_01.prefab"), new Vector3(45f, 0.5f, 24f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Watering_Can_01.prefab"), new Vector3(56f, 0.5f, 34f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Barrel_01.prefab"), new Vector3(63f, 0.5f, 35f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Barrel_02.prefab"), new Vector3(64f, 0.5f, 35f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Trough_01.prefab"), new Vector3(102f, 0.5f, 18f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_LetterBox_01.prefab"), new Vector3(30f, 0.5f, 27f), Quaternion.identity, props);

            // ── Trees ──
            InstantiatePrefab(P("Environments/SM_Env_Tree_Apple_Grown_01.prefab"), new Vector3(54f, 0.5f, 42f), Quaternion.identity, trees);
            InstantiatePrefab(P("Environments/SM_Env_Tree_Apple_Grown_01.prefab"), new Vector3(51f, 0.5f, 45f), Quaternion.identity, trees);
            InstantiatePrefab(P("Environments/SM_Env_Tree_Apple_Grown_01.prefab"), new Vector3(57f, 0.5f, 46f), Quaternion.identity, trees);
            InstantiatePrefab(P("Environments/SM_Env_Tree_Cherry_Grown_01.prefab"), new Vector3(33f, 0.5f, 29f), Quaternion.identity, trees);
            InstantiatePrefab(P("Environments/SM_Env_Tree_Cherry_Grown_01.prefab"), new Vector3(30f, 0.5f, 31f), Quaternion.identity, trees);
            InstantiatePrefab(P("Environments/SM_Env_Tree_Large_01.prefab"), new Vector3(66f, 0.5f, 48f), Quaternion.identity, trees);
            InstantiatePrefab(P("Environments/SM_Env_Tree_Large_01.prefab"), new Vector3(96f, 0.5f, 51f), Quaternion.identity, trees);

            // ── FX ──
            InstantiatePrefab(P("FX/FX_Pollen_Wind_01.prefab"), new Vector3(48f, 2f, 27f), Quaternion.identity, fx);
            InstantiatePrefab(P("FX/FX_Sprinkler_01.prefab"), new Vector3(45f, 0.5f, 30f), Quaternion.identity, fx);
            InstantiatePrefab(P("FX/FX_Sprinkler_01.prefab"), new Vector3(51f, 0.5f, 21f), Quaternion.identity, fx);

            Debug.Log("[WorldSceneBuilder] Farm zone populated (buildings, pen, plots, pasture, props, trees, fx).");
        }

        // ── Town Zone ────────────────────────────────────────────
        private static void BuildTownZone()
        {
            var town = GameObject.Find("Town");
            if (town == null) town = CreateEmpty("Town", new Vector3(-72f, 0f, 6f));

            var buildings = town.transform.Find("Buildings") ?? CreateEmpty("Buildings", Vector3.zero, town.transform).transform;
            var mainSt = town.transform.Find("MainStreet") ?? CreateEmpty("MainStreet", Vector3.zero, town.transform).transform;
            var props = town.transform.Find("Props") ?? CreateEmpty("Props", Vector3.zero, town.transform).transform;
            var trees = town.transform.Find("Trees") ?? CreateEmpty("Trees", Vector3.zero, town.transform).transform;

            string P(string name) => $"Assets/Synty/PolygonFarm/Prefabs/{name}";

            // ── Buildings ──
            InstantiatePrefab(P("Buildings/SM_Bld_Farmhouse_02.prefab"),
                new Vector3(-96f, 0f, 12f), Quaternion.Euler(0f, 90f, 0f), buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_ProduceStand_01.prefab"),
                new Vector3(-72f, 0f, 23f), Quaternion.Euler(0f, 180f, 0f), buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Garage_01.prefab"),
                new Vector3(-45f, 0f, 23f), Quaternion.Euler(0f, 180f, 0f), buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Shelter_01.prefab"),
                new Vector3(-96f, 0f, -6f), Quaternion.identity, buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Greenhouse_Large_01.prefab"),
                new Vector3(-72f, 0f, -6f), Quaternion.identity, buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Farmhouse_01.prefab"),
                new Vector3(-48f, 0f, -12f), Quaternion.Euler(0f, 45f, 0f), buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Farmhouse_01.prefab"),
                new Vector3(-60f, 0f, -24f), Quaternion.Euler(0f, -30f, 0f), buildings);

            // ── Main Street Furniture ──
            for (float x = -111f; x <= -33f; x += 8f)
            {
                InstantiatePrefab(P("Props/SM_Prop_Fence_Painted_01.prefab"),
                    new Vector3(x, 0f, 15f), Quaternion.Euler(0f, 90f, 0f), mainSt);
                InstantiatePrefab(P("Props/SM_Prop_Fence_Painted_01.prefab"),
                    new Vector3(x, 0f, 21f), Quaternion.Euler(0f, 90f, 0f), mainSt);
            }
            for (float x = -108f; x <= -36f; x += 25f)
            {
                InstantiatePrefab("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Light_Roof_01.prefab",
                    new Vector3(x, 0f, 14f), Quaternion.identity, mainSt);
            }
            InstantiatePrefab(P("Props/SM_Prop_SignPost_01.prefab"),
                new Vector3(-87f, 0f, 22f), Quaternion.Euler(0f, 180f, 0f), mainSt);
            InstantiatePrefab(P("Props/SM_Prop_SignPost_02.prefab"),
                new Vector3(-57f, 0f, 22f), Quaternion.Euler(0f, 180f, 0f), mainSt);
            InstantiatePrefab(P("Props/SM_Prop_LetterBox_01.prefab"),
                new Vector3(-93f, 0f, 11f), Quaternion.identity, mainSt);
            InstantiatePrefab(P("Props/SM_Prop_LetterBox_01.prefab"),
                new Vector3(-45f, 0f, -11f), Quaternion.identity, mainSt);
            InstantiatePrefab(P("Props/SM_Prop_LetterBox_01.prefab"),
                new Vector3(-57f, 0f, -23f), Quaternion.identity, mainSt);

            // ── Props ──
            InstantiatePrefab(P("Props/SM_Prop_Barrel_01.prefab"), new Vector3(-71f, 0f, 22f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Barrel_02.prefab"), new Vector3(-70f, 0f, 22f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Crate_01.prefab"), new Vector3(-73f, 0f, 22f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Crate_01.prefab"), new Vector3(-74f, 0f, 22f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Bench_01.prefab"),
                new Vector3(-44f, 0f, 17f), Quaternion.Euler(0f, 90f, 0f), props);
            InstantiatePrefab(P("Props/SM_Prop_Bench_01.prefab"),
                new Vector3(-44f, 0f, 13f), Quaternion.Euler(0f, 90f, 0f), props);
            InstantiatePrefab(P("Props/SM_Prop_Beehive_01.prefab"),
                new Vector3(-51f, 0f, -15f), Quaternion.identity, props);
            float[] bushXPositions = { -99f, -93f, -75f, -69f, -48f, -42f, -63f, -57f };
            for (int i = 0; i < bushXPositions.Length; i++)
            {
                string bush = (i % 2 == 0)
                    ? "Assets/PolygonNature/Prefabs/Plants/SM_Plant_Bush_01.prefab"
                    : "Assets/PolygonNature/Prefabs/Plants/SM_Plant_Bush_02.prefab";
                float bz = 11f + (i % 3) * 2f;
                InstantiatePrefab(bush, new Vector3(bushXPositions[i], 0f, bz), Quaternion.identity, props);
            }

            // ── Trees ──
            float[] treeLargeX = { -102f, -84f, -60f, -36f };
            foreach (float tx in treeLargeX)
                InstantiatePrefab(P("Environments/SM_Env_Tree_Large_01.prefab"),
                    new Vector3(tx, 0f, 18f), Quaternion.identity, trees);
            InstantiatePrefab("Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Tree_Patch_01.prefab",
                new Vector3(-114f, 0f, -30f), Quaternion.identity, trees);
            InstantiatePrefab("Assets/Synty/PolygonFarm/Prefabs/Generic/SM_Generic_Tree_Patch_01.prefab",
                new Vector3(-30f, 0f, -36f), Quaternion.Euler(0f, 45f, 0f), trees);

            Debug.Log("[WorldSceneBuilder] Town zone populated (buildings, main street, props, trees).");
        }

        // ── Unpopulated Zones ────────────────────────────────────

        private static Transform FindZoneMarkers(string zoneName)
        {
            var zone = GameObject.Find(zoneName);
            return zone != null ? zone.transform.Find("Markers") : null;
        }

        private static void BuildUnpopulatedZones()
        {
            // ── North Field ──
            var m = FindZoneMarkers("NorthField");
            if (m != null)
            {
                var festival = CreateEmpty("FestivalCenter", new Vector3(-72f, 0f, 90f), m);
                festival.AddComponent<BoxCollider>().isTrigger = true;
            }

            // ── Sandy Shores ──
            m = FindZoneMarkers("SandyShores");
            if (m != null)
            {
                CreateEmpty("TrevorTrailerPosition", new Vector3(72f, 0f, 96f), m);
                for (int i = 0; i < 5; i++)
                    InstantiatePrefab("Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Pebbles_01.prefab",
                        new Vector3(30f + i * 18f, 0f, 71f), Quaternion.identity, m);
            }

            // ── Meadow ──
            m = FindZoneMarkers("Meadow");
            if (m != null)
            {
                Vector3[] truffleSpots = { new(-90f,0f,-66f), new(-60f,0f,-78f), new(-102f,0f,-84f) };
                for (int i = 0; i < truffleSpots.Length; i++)
                    CreateEmpty($"TruffleSpot_{i}", truffleSpots[i], m);
                for (int i = 0; i < 5; i++)
                {
                    var flower = CreateEmpty($"WildflowerSpecies_{i}",
                        new Vector3(-108f + i * 15f, 0f, -60f - i * 6f), m);
                    flower.AddComponent<BoxCollider>().isTrigger = true;
                }
            }

            // ── County Fair ──
            m = FindZoneMarkers("CountyFair");
            if (m != null)
            {
                CreateEmpty("TrainLoopCenter", new Vector3(96f, 0f, -72f), m);
                CreateEmpty("PettingZooArea", new Vector3(90f, 0f, -60f), m);
                Vector3 fc = new(96f, 0f, -72f);
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f;
                    float rad = angle * Mathf.Deg2Rad;
                    var pos = fc + new Vector3(Mathf.Sin(rad) * 21f, 0f, Mathf.Cos(rad) * 21f);
                    InstantiatePrefab("Assets/Synty/PolygonFarm/Prefabs/Props/SM_Prop_Fence_Fancy_01.prefab",
                        pos, Quaternion.Euler(0f, angle, 0f), m);
                }
            }

            // ── River ──
            m = FindZoneMarkers("River");
            if (m != null)
                CreateEmpty("TheTruthVanPosition", new Vector3(6f, 0f, -84f), m);

            // ── Wildflower Hills ──
            m = FindZoneMarkers("WildflowerHills");
            if (m != null)
            {
                CreateEmpty("MichaelEaselPosition", new Vector3(0f, 2f, -105f), m);
                for (int i = 0; i < 5; i++)
                    CreateEmpty($"FlowerSpawnPoint_{i}", new Vector3(-90f + i * 45f, 0f, -108f), m);
            }

            // ── Trail ──
            m = FindZoneMarkers("Trail");
            if (m != null)
                CreateEmpty("TenpennyLampPost", new Vector3(0f, 0f, 36f), m);

            Debug.Log("[WorldSceneBuilder] Unpopulated zone markers placed.");
        }

        // ── FX ───────────────────────────────────────────────────

        private static void BuildFX()
        {
            var fxRoot = CreateEmpty("FX", Vector3.zero);
            InstantiatePrefab("Assets/Synty/PolygonFarm/Prefabs/FX/FX_Pollen_Wind_01.prefab",
                new Vector3(-78f, 2f, -72f), Quaternion.identity, fxRoot.transform);
            InstantiatePrefab("Assets/Synty/PolygonFarm/Prefabs/FX/FX_Dust_Wind_01.prefab",
                new Vector3(72f, 1f, 96f), Quaternion.identity, fxRoot.transform);
            Debug.Log("[WorldSceneBuilder] World FX placed (pollen, dust).");
        }

        // ── Markers ──────────────────────────────────────────────

        private static void BuildMarkers()
        {
            var markersRoot = CreateEmpty("Markers", Vector3.zero);
            var spawn = CreateEmpty("SpawnPoint", GroundPos(57f, 33f, 0.5f), markersRoot.transform);
            // Register the tag if it doesn't exist
            var tagManager = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
            var tagsProp = tagManager.FindProperty("tags");
            bool tagExists = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == "SpawnPoint")
                { tagExists = true; break; }
            }
            if (!tagExists)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "SpawnPoint";
                tagManager.ApplyModifiedProperties();
            }
            spawn.tag = "SpawnPoint";
            Debug.Log("[WorldSceneBuilder] Global markers placed (SpawnPoint).");
        }
    }
}
