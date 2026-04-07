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
                new(-40f, -0.8f, -120f),
                new(-10f, -0.8f, -115f),
                new(20f, -0.8f, -125f),
                new(50f, -0.8f, -118f),
                new(80f, -0.8f, -122f),
                new(110f, -0.8f, -115f),
            };
            for (int i = 0; i < riverPoints.Length; i++)
            {
                float yRot = (i % 2 == 0) ? 0f : 15f;
                var seg = InstantiatePrefab(
                    "Assets/PolygonNature/Prefabs/Terrain/River_Plane_01.prefab",
                    riverPoints[i], Quaternion.Euler(0f, yRot, 0f), riverParent.transform);
                seg.name = $"RiverSegment_{i}";
            }

            // ── River Banks ──
            for (int i = 0; i < riverPoints.Length; i++)
            {
                var northPos = riverPoints[i] + new Vector3(0f, 0f, 12f);
                var northBank = InstantiatePrefab(
                    "Assets/PolygonNature/Prefabs/Terrain/SM_Terrain_RiverSide_01.prefab",
                    northPos, Quaternion.identity, riverParent.transform);
                northBank.name = $"RiverBank_North_{i}";

                var southPos = riverPoints[i] + new Vector3(0f, 0f, -12f);
                var southBank = InstantiatePrefab(
                    "Assets/PolygonNature/Prefabs/Terrain/SM_Terrain_RiverSide_01.prefab",
                    southPos, Quaternion.Euler(0f, 180f, 0f), riverParent.transform);
                southBank.name = $"RiverBank_South_{i}";
            }

            // ── Reeds along river banks ──
            for (int i = 0; i < 10; i++)
            {
                int ptIdx = i % riverPoints.Length;
                float zOffset = (i % 2 == 0) ? 14f : -14f;
                var reedPos = riverPoints[ptIdx] + new Vector3(i * 3f - 15f, 0.2f, zOffset);
                var reed = InstantiatePrefab(
                    "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Reeds_01.prefab",
                    reedPos, Quaternion.identity, riverParent.transform);
                reed.name = $"Reed_{i}";
            }

            // ── Creek ──
            var creekParent = CreateEmpty("Creek", Vector3.zero, waterRoot.transform);
            var creek0 = InstantiatePrefab(
                "Assets/PolygonNature/Prefabs/Terrain/River_Plane_01.prefab",
                new Vector3(-10f, -0.3f, 55f), Quaternion.Euler(0f, 30f, 0f), creekParent.transform);
            creek0.transform.localScale = Vector3.one * 0.4f;
            creek0.name = "CreekSegment_0";

            var creek1 = InstantiatePrefab(
                "Assets/PolygonNature/Prefabs/Terrain/River_Plane_01.prefab",
                new Vector3(10f, -0.3f, 50f), Quaternion.Euler(0f, 30f, 0f), creekParent.transform);
            creek1.transform.localScale = Vector3.one * 0.4f;
            creek1.name = "CreekSegment_1";

            // ── Bridge over creek ──
            var bridge = InstantiatePrefab(
                "Assets/PolygonNature/Prefabs/Props/SM_Prop_Bridge_Curved_01.prefab",
                new Vector3(0f, 0f, 52f), Quaternion.Euler(0f, 90f, 0f), creekParent.transform);
            bridge.name = "CreekBridge";

            // ── Farm Pond ──
            var pond = InstantiatePrefab(
                "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Pond_01.prefab",
                new Vector3(165f, 0f, 20f), Quaternion.identity, waterRoot.transform);
            pond.name = "FarmPond";

            // ── Lily pads ──
            var lilypads = InstantiatePrefab(
                "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Lillypads_01.prefab",
                new Vector3(165f, 0.05f, 20f), Quaternion.identity, waterRoot.transform);
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

            for (float x = -185f; x <= -55f; x += 10f)
            {
                var seg = InstantiatePrefab(gravelStraight,
                    new Vector3(x, 0.02f, 30f), Quaternion.Euler(0f, 90f, 0f), mainStreet.transform);
                seg.name = $"MainStreet_Straight_{(int)x}";
            }

            var endCapW = InstantiatePrefab(gravelEnd,
                new Vector3(-190f, 0.02f, 30f), Quaternion.Euler(0f, -90f, 0f), mainStreet.transform);
            endCapW.name = "MainStreet_EndCap_West";
            var endCapE = InstantiatePrefab(gravelEnd,
                new Vector3(-50f, 0.02f, 30f), Quaternion.Euler(0f, 90f, 0f), mainStreet.transform);
            endCapE.name = "MainStreet_EndCap_East";

            // ── TrailToFarm ──
            var trailToFarm = CreateEmpty("TrailToFarm", Vector3.zero, pathsRoot.transform);
            const string dirtStraight = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Road_Dirt_Straight_01.prefab";
            const string dirtSwerve = "Assets/Synty/PolygonFarm/Prefabs/Environments/SM_Env_Road_Dirt_Swerve_01.prefab";

            Vector3[] trailPoints = {
                new(-35f, 0.02f, 30f), new(-25f, 0.02f, 35f),
                new(-15f, 0.02f, 42f), new(-5f, 0.02f, 48f),
                new(5f, 0.02f, 50f),   new(15f, 0.02f, 50f),
                new(25f, 0.02f, 48f),  new(35f, 0.02f, 45f),
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
            for (float z = 45f; z <= 75f; z += 10f)
            {
                var seg = InstantiatePrefab(dirtStraight,
                    new Vector3(100f, 0.52f, z), Quaternion.identity, farmPaths.transform);
                seg.name = $"FarmPath_NS_{(int)z}";
            }
            for (float x = 70f; x <= 100f; x += 10f)
            {
                var seg = InstantiatePrefab(dirtStraight,
                    new Vector3(x, 0.52f, 55f), Quaternion.Euler(0f, 90f, 0f), farmPaths.transform);
                seg.name = $"FarmPath_EW_{(int)x}";
            }

            Debug.Log("[WorldSceneBuilder] Path network built (MainStreet, TrailToFarm, FarmPaths).");
        }

        // ── Farm Zone ────────────────────────────────────────────

        private static void BuildFarmZone()
        {
            var farm = GameObject.Find("Farm");
            if (farm == null) farm = CreateEmpty("Farm", new Vector3(120f, 0f, 40f));

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
                new Vector3(100f, 0.5f, 60f), Quaternion.Euler(0f, 180f, 0f), buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Barn_01.prefab"),
                new Vector3(140f, 0.5f, 70f), Quaternion.Euler(0f, 90f, 0f), buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Silo_01.prefab"),
                new Vector3(155f, 0.5f, 75f), Quaternion.identity, buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Silo_Small_01.prefab"),
                new Vector3(150f, 0.5f, 65f), Quaternion.identity, buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Greenhouse_01.prefab"),
                new Vector3(60f, 0.5f, 30f), Quaternion.identity, buildings);

            // ── Circular Pen (El Pollo Loco) ──
            Vector3 penCenter = new(120f, 0.5f, 40f);
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
                new(70f, 0.5f, 50f), new(85f, 0.5f, 50f),
                new(70f, 0.5f, 35f), new(85f, 0.5f, 35f),
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
            Vector3 pastureMin = new(155f, 0.5f, 15f);
            Vector3 pastureMax = new(185f, 0.5f, 45f);

            // E-W runs (north & south sides)
            for (float x = pastureMin.x; x <= pastureMax.x; x += 5f)
            {
                InstantiatePrefab(P("Props/SM_Prop_Fence_Wire_01.prefab"),
                    new Vector3(x, 0.5f, pastureMax.z), Quaternion.identity, pasture);
                InstantiatePrefab(P("Props/SM_Prop_Fence_Wire_01.prefab"),
                    new Vector3(x, 0.5f, pastureMin.z), Quaternion.identity, pasture);
            }
            // N-S runs (east & west sides)
            for (float z = pastureMin.z; z <= pastureMax.z; z += 5f)
            {
                InstantiatePrefab(P("Props/SM_Prop_Fence_Wire_01.prefab"),
                    new Vector3(pastureMin.x, 0.5f, z), Quaternion.Euler(0f, 90f, 0f), pasture);
                InstantiatePrefab(P("Props/SM_Prop_Fence_Wire_01.prefab"),
                    new Vector3(pastureMax.x, 0.5f, z), Quaternion.Euler(0f, 90f, 0f), pasture);
            }
            // Corner poles
            Vector3[] corners = {
                new(pastureMin.x, 0.5f, pastureMin.z), new(pastureMax.x, 0.5f, pastureMin.z),
                new(pastureMin.x, 0.5f, pastureMax.z), new(pastureMax.x, 0.5f, pastureMax.z),
            };
            foreach (var corner in corners)
                InstantiatePrefab(P("Props/SM_Prop_Fence_Wire_Pole_01.prefab"), corner, Quaternion.identity, pasture);
            // Gate on south side
            InstantiatePrefab(P("Props/SM_Prop_Fence_Wire_Gate_01.prefab"),
                new Vector3(170f, 0.5f, pastureMin.z), Quaternion.identity, pasture);

            // ── Props ──
            InstantiatePrefab(P("Props/SM_Prop_Well_01.prefab"), new Vector3(95f, 0.5f, 55f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Scarecrow_01.prefab"), new Vector3(78f, 0.5f, 45f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_HayBale_02.prefab"), new Vector3(145f, 0.5f, 62f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_HayBale_02.prefab"), new Vector3(148f, 0.5f, 60f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_HayBale_01.prefab"), new Vector3(143f, 0.5f, 65f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Crate_01.prefab"), new Vector3(137f, 0.5f, 68f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Crate_02.prefab"), new Vector3(139f, 0.5f, 68f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Wheelbarrow_01.prefab"), new Vector3(75f, 0.5f, 40f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_WateringCan_01.prefab"), new Vector3(93f, 0.5f, 56f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Barrel_01.prefab"), new Vector3(105f, 0.5f, 58f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Barrel_02.prefab"), new Vector3(107f, 0.5f, 59f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Trough_01.prefab"), new Vector3(170f, 0.5f, 30f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Letterbox_01.prefab"), new Vector3(50f, 0.5f, 45f), Quaternion.identity, props);

            // ── Trees ──
            InstantiatePrefab(P("Environments/SM_Env_Tree_Apple_Grown_01.prefab"), new Vector3(90f, 0.5f, 70f), Quaternion.identity, trees);
            InstantiatePrefab(P("Environments/SM_Env_Tree_Apple_Grown_01.prefab"), new Vector3(85f, 0.5f, 75f), Quaternion.identity, trees);
            InstantiatePrefab(P("Environments/SM_Env_Tree_Apple_Grown_01.prefab"), new Vector3(95f, 0.5f, 76f), Quaternion.identity, trees);
            InstantiatePrefab(P("Environments/SM_Env_Tree_Cherry_Grown_01.prefab"), new Vector3(55f, 0.5f, 48f), Quaternion.identity, trees);
            InstantiatePrefab(P("Environments/SM_Env_Tree_Cherry_Grown_01.prefab"), new Vector3(50f, 0.5f, 52f), Quaternion.identity, trees);
            InstantiatePrefab(P("Environments/SM_Env_Tree_Large_01.prefab"), new Vector3(110f, 0.5f, 80f), Quaternion.identity, trees);
            InstantiatePrefab(P("Environments/SM_Env_Tree_Large_01.prefab"), new Vector3(160f, 0.5f, 85f), Quaternion.identity, trees);

            // ── FX ──
            InstantiatePrefab(P("FX/FX_Pollen_Wind_01.prefab"), new Vector3(80f, 2f, 45f), Quaternion.identity, fx);
            InstantiatePrefab(P("FX/FX_Sprinkler_01.prefab"), new Vector3(75f, 0.5f, 50f), Quaternion.identity, fx);
            InstantiatePrefab(P("FX/FX_Sprinkler_01.prefab"), new Vector3(85f, 0.5f, 35f), Quaternion.identity, fx);

            Debug.Log("[WorldSceneBuilder] Farm zone populated (buildings, pen, plots, pasture, props, trees, fx).");
        }

        // ── Town Zone ────────────────────────────────────────────

        private static void BuildTownZone()
        {
            var town = GameObject.Find("Town");
            if (town == null) town = CreateEmpty("Town", new Vector3(-120f, 0f, 10f));

            var buildings = town.transform.Find("Buildings") ?? CreateEmpty("Buildings", Vector3.zero, town.transform).transform;
            var mainSt = town.transform.Find("MainStreet") ?? CreateEmpty("MainStreet", Vector3.zero, town.transform).transform;
            var props = town.transform.Find("Props") ?? CreateEmpty("Props", Vector3.zero, town.transform).transform;
            var trees = town.transform.Find("Trees") ?? CreateEmpty("Trees", Vector3.zero, town.transform).transform;

            string P(string name) => $"Assets/Synty/PolygonFarm/Prefabs/{name}";

            // ── Buildings ──
            InstantiatePrefab(P("Buildings/SM_Bld_Farmhouse_02.prefab"),
                new Vector3(-160f, 0f, 20f), Quaternion.Euler(0f, 90f, 0f), buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_ProduceStand_01.prefab"),
                new Vector3(-120f, 0f, 38f), Quaternion.Euler(0f, 180f, 0f), buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Garage_01.prefab"),
                new Vector3(-75f, 0f, 38f), Quaternion.Euler(0f, 180f, 0f), buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Shelter_01.prefab"),
                new Vector3(-160f, 0f, -10f), Quaternion.identity, buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Greenhouse_Large_01.prefab"),
                new Vector3(-120f, 0f, -10f), Quaternion.identity, buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Farmhouse_01.prefab"),
                new Vector3(-80f, 0f, -20f), Quaternion.Euler(0f, 45f, 0f), buildings);
            InstantiatePrefab(P("Buildings/SM_Bld_Farmhouse_01.prefab"),
                new Vector3(-100f, 0f, -40f), Quaternion.Euler(0f, -30f, 0f), buildings);

            // ── Main Street Furniture ──
            // Fences along z=25 and z=35
            for (float x = -185f; x <= -55f; x += 8f)
            {
                InstantiatePrefab(P("Props/SM_Prop_Fence_Painted_01.prefab"),
                    new Vector3(x, 0f, 25f), Quaternion.Euler(0f, 90f, 0f), mainSt);
                InstantiatePrefab(P("Props/SM_Prop_Fence_Painted_01.prefab"),
                    new Vector3(x, 0f, 35f), Quaternion.Euler(0f, 90f, 0f), mainSt);
            }
            // Lamps
            for (float x = -180f; x <= -60f; x += 25f)
            {
                InstantiatePrefab(P("Props/SM_Prop_Lamp_01.prefab"),
                    new Vector3(x, 0f, 24f), Quaternion.identity, mainSt);
            }
            // Signs
            InstantiatePrefab(P("Props/SM_Prop_Sign_01.prefab"),
                new Vector3(-145f, 0f, 36f), Quaternion.Euler(0f, 180f, 0f), mainSt);
            InstantiatePrefab(P("Props/SM_Prop_Sign_02.prefab"),
                new Vector3(-95f, 0f, 36f), Quaternion.Euler(0f, 180f, 0f), mainSt);
            // Letterboxes
            InstantiatePrefab(P("Props/SM_Prop_Letterbox_01.prefab"),
                new Vector3(-155f, 0f, 18f), Quaternion.identity, mainSt);
            InstantiatePrefab(P("Props/SM_Prop_Letterbox_01.prefab"),
                new Vector3(-75f, 0f, -18f), Quaternion.identity, mainSt);
            InstantiatePrefab(P("Props/SM_Prop_Letterbox_01.prefab"),
                new Vector3(-95f, 0f, -38f), Quaternion.identity, mainSt);

            // ── Props ──
            InstantiatePrefab(P("Props/SM_Prop_Barrel_01.prefab"), new Vector3(-118f, 0f, 36f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Barrel_02.prefab"), new Vector3(-116f, 0f, 37f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Crate_01.prefab"), new Vector3(-122f, 0f, 36f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Crate_02.prefab"), new Vector3(-124f, 0f, 37f), Quaternion.identity, props);
            InstantiatePrefab(P("Props/SM_Prop_Bench_01.prefab"),
                new Vector3(-73f, 0f, 28f), Quaternion.Euler(0f, 90f, 0f), props);
            InstantiatePrefab(P("Props/SM_Prop_Bench_01.prefab"),
                new Vector3(-73f, 0f, 22f), Quaternion.Euler(0f, 90f, 0f), props);
            InstantiatePrefab(P("Props/SM_Prop_Beehive_01.prefab"),
                new Vector3(-85f, 0f, -25f), Quaternion.identity, props);
            // Bushes
            float[] bushXPositions = { -165f, -155f, -125f, -115f, -80f, -70f, -105f, -95f };
            for (int i = 0; i < bushXPositions.Length; i++)
            {
                string bush = (i % 2 == 0)
                    ? P("Environments/SM_Env_Plant_Bush_01.prefab")
                    : P("Environments/SM_Env_Plant_Bush_02.prefab");
                float bz = 18f + (i % 3) * 2f;
                InstantiatePrefab(bush, new Vector3(bushXPositions[i], 0f, bz), Quaternion.identity, props);
            }

            // ── Trees ──
            float[] treeLargeX = { -170f, -140f, -100f, -60f };
            foreach (float tx in treeLargeX)
                InstantiatePrefab(P("Environments/SM_Env_Tree_Large_01.prefab"),
                    new Vector3(tx, 0f, 30f), Quaternion.identity, trees);
            InstantiatePrefab(P("Environments/SM_Env_Tree_Patch_01.prefab"),
                new Vector3(-190f, 0f, -50f), Quaternion.identity, trees);
            InstantiatePrefab(P("Environments/SM_Env_Tree_Patch_01.prefab"),
                new Vector3(-50f, 0f, -60f), Quaternion.Euler(0f, 45f, 0f), trees);

            Debug.Log("[WorldSceneBuilder] Town zone populated (buildings, main street, props, trees).");
        }

        // ── Remaining Stubs (future tasks) ───────────────────────

        private static void BuildUnpopulatedZones() { }
        private static void BuildVegetation() { }
        private static void BuildFX() { }
        private static void BuildMarkers() { }
    }
}
