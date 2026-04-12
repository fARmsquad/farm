using UnityEngine;

namespace FarmSimVR.MonoBehaviours.HorseTaming
{
    /// <summary>
    /// Spawns copied Synty / PolygonNature prefabs from <c>Resources/HorseTaming/Synty/</c> around the paddock.
    /// Source assets (for re-copy if you change selection): PolygonFarm, PolygonCity, PolygonGeneric, PolygonNature.
    /// </summary>
    public static class HorseTamingSyntyEnvironment
    {
        private const string ResourceRoot = "HorseTaming/Synty";

        /// <param name="sceneryRoot">Parent for all props (world space).</param>
        /// <param name="groundTransform">Optional ground; dirt overlay is parented here when present.</param>
        public static void Build(Transform sceneryRoot, Transform groundTransform = null)
        {
            if (sceneryRoot == null)
                return;

            SpawnDirtOverlay(groundTransform);

            var fencePrefab = Resources.Load<GameObject>($"{ResourceRoot}/SM_Prop_Fence_Fancy_01");
            if (fencePrefab != null)
                PlaceFencePerimeter(sceneryRoot, fencePrefab, halfExtent: 9.8f, step: 2.35f);

            var gatePrefab = Resources.Load<GameObject>($"{ResourceRoot}/SM_Prop_Fence_Fancy_Gate_01");
            if (gatePrefab != null)
            {
                var gate = Object.Instantiate(gatePrefab, sceneryRoot);
                gate.name = "SM_Prop_Fence_Fancy_Gate_01";
                gate.transform.position = new Vector3(0f, 0f, 10.15f);
                gate.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                StripColliders(gate);
            }

            TrySpawn($"{ResourceRoot}/SM_Tree_02", new Vector3(-12f, 0f, -11f), Quaternion.Euler(0f, 35f, 0f), Vector3.one * 1.15f, sceneryRoot);
            TrySpawn($"{ResourceRoot}/SM_Tree_02", new Vector3(12.5f, 0f, -10f), Quaternion.Euler(0f, -25f, 0f), Vector3.one * 1.05f, sceneryRoot);
            TrySpawn($"{ResourceRoot}/SM_Tree_02", new Vector3(-11f, 0f, 12f), Quaternion.Euler(0f, 140f, 0f), Vector3.one * 1.1f, sceneryRoot);

            TrySpawn($"{ResourceRoot}/SM_Rock_Boulder_01", new Vector3(7f, 0f, -6.5f), Quaternion.Euler(0f, 20f, 0f), Vector3.one * 0.9f, sceneryRoot);
            TrySpawn($"{ResourceRoot}/SM_Rock_Boulder_01", new Vector3(-6f, 0f, 5f), Quaternion.Euler(0f, -50f, 0f), Vector3.one * 0.75f, sceneryRoot);

            var jumpPrefab = Resources.Load<GameObject>($"{ResourceRoot}/SM_Prop_Horse_Jump_01");
            if (jumpPrefab != null)
            {
                var jump = Object.Instantiate(jumpPrefab, sceneryRoot);
                jump.name = "SM_Prop_Horse_Jump_01";
                jump.transform.position = new Vector3(8.2f, 0f, 6f);
                jump.transform.rotation = Quaternion.Euler(0f, -35f, 0f);
                StripColliders(jump);
            }

            var skyline = Resources.Load<GameObject>($"{ResourceRoot}/SM_Gen_Bld_Background_01");
            if (skyline != null)
            {
                var go = Object.Instantiate(skyline, sceneryRoot);
                go.name = "SM_Gen_Bld_Background_01";
                go.transform.position = new Vector3(0f, 0f, 48f);
                go.transform.rotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * 1.8f;
                StripColliders(go);
            }

            var apartment = Resources.Load<GameObject>($"{ResourceRoot}/SM_Bld_Apartment_01");
            if (apartment != null)
            {
                var go = Object.Instantiate(apartment, sceneryRoot);
                go.name = "SM_Bld_Apartment_01";
                go.transform.position = new Vector3(-28f, 0f, 36f);
                go.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
                go.transform.localScale = Vector3.one * 0.85f;
                StripColliders(go);

                var go2 = Object.Instantiate(apartment, sceneryRoot);
                go2.name = "SM_Bld_Apartment_01_East";
                go2.transform.position = new Vector3(26f, 0f, 34f);
                go2.transform.rotation = Quaternion.Euler(0f, -15f, 0f);
                go2.transform.localScale = Vector3.one * 0.75f;
                StripColliders(go2);
            }
        }

        private static void SpawnDirtOverlay(Transform groundTransform)
        {
            var dirtPrefab = Resources.Load<GameObject>($"{ResourceRoot}/SM_Env_Dirt_01");
            if (dirtPrefab == null || groundTransform == null)
                return;

            var go = Object.Instantiate(dirtPrefab, groundTransform);
            go.name = "SM_Env_Dirt_01";
            go.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(2.15f, 1f, 2.15f);
            StripColliders(go);
        }

        private static void PlaceFencePerimeter(Transform parent, GameObject fencePrefab, float halfExtent, float step)
        {
            float inset = step * 0.3f;
            float z = halfExtent + 0.12f;
            for (float x = -halfExtent + inset; x <= halfExtent - inset + 0.001f; x += step)
            {
                SpawnFence(fencePrefab, new Vector3(x, 0f, z), Quaternion.Euler(0f, 0f, 0f), parent);
                SpawnFence(fencePrefab, new Vector3(x, 0f, -z), Quaternion.Euler(0f, 180f, 0f), parent);
            }

            float xEdge = halfExtent + 0.12f;
            for (float zz = -halfExtent + inset; zz <= halfExtent - inset + 0.001f; zz += step)
            {
                SpawnFence(fencePrefab, new Vector3(xEdge, 0f, zz), Quaternion.Euler(0f, 90f, 0f), parent);
                SpawnFence(fencePrefab, new Vector3(-xEdge, 0f, zz), Quaternion.Euler(0f, -90f, 0f), parent);
            }
        }

        private static void SpawnFence(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
        {
            var go = Object.Instantiate(prefab, parent);
            go.name = prefab.name;
            go.transform.position = pos;
            go.transform.rotation = rot;
            StripColliders(go);
        }

        private static void TrySpawn(string resourcePath, Vector3 worldPos, Quaternion rot, Vector3 scale, Transform parent)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
                return;

            var go = Object.Instantiate(prefab, parent);
            go.name = prefab.name;
            go.transform.position = worldPos;
            go.transform.rotation = rot;
            go.transform.localScale = scale;
            StripColliders(go);
        }

        private static void StripColliders(GameObject go)
        {
            if (go == null)
                return;
            foreach (var c in go.GetComponentsInChildren<Collider>())
            {
                if (Application.isPlaying)
                    Object.Destroy(c);
                else
                    Object.DestroyImmediate(c);
            }
        }
    }
}
