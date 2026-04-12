using FarmSimVR.MonoBehaviours;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Farming
{
    public static class WorldFarmZoneInstaller
    {
        public static void Apply(Transform farmRoot, Transform plotsRoot)
        {
            if (farmRoot == null || plotsRoot == null)
                return;

            var zonesRoot = EnsureChild(farmRoot, "Zones");
            var houseRoot = ResolveHouseRoot(farmRoot);
            var coopRoot = ResolveCoopRoot(farmRoot);

            EnsureZone(
                zonesRoot,
                "FarmPlotsZone",
                "Farm Plots",
                CalculateBounds(plotsRoot.gameObject, new Vector3(8f, 4f, 12f)));

            EnsureZone(
                zonesRoot,
                "FarmHouseZone",
                "Farm House",
                CalculateBounds(houseRoot.gameObject, new Vector3(14f, 6f, 14f)));

            EnsureZone(
                zonesRoot,
                "ChickenCoopZone",
                "Chicken Coop",
                CalculateBounds(coopRoot.gameObject, new Vector3(16f, 6f, 16f)));

            EnsureZoneTracker();
        }

        private static Transform ResolveHouseRoot(Transform farmRoot)
        {
            var buildings = farmRoot.Find("Buildings");
            if (buildings == null)
                return farmRoot;

            foreach (Transform child in buildings)
            {
                if (child.name.ToLowerInvariant().Contains("house"))
                    return child;
            }

            return buildings;
        }

        private static Transform ResolveCoopRoot(Transform farmRoot)
        {
            var pen = farmRoot.Find("Pen");
            return pen != null ? pen : farmRoot;
        }

        private static void EnsureZone(Transform parent, string objectName, string zoneName, Bounds bounds)
        {
            var zone = parent.Find(objectName);
            if (zone == null)
            {
                zone = new GameObject(objectName).transform;
                zone.SetParent(parent, false);
            }

            zone.position = bounds.center;
            zone.localRotation = Quaternion.identity;

            var collider = zone.GetComponent<BoxCollider>();
            if (collider == null)
                collider = zone.gameObject.AddComponent<BoxCollider>();

            collider.isTrigger = true;
            collider.size = bounds.size;
            collider.center = Vector3.zero;

            var marker = zone.GetComponent<ZoneMarker>();
            if (marker == null)
                marker = zone.gameObject.AddComponent<ZoneMarker>();

            marker.SetZoneName(zoneName);
        }

        private static Bounds CalculateBounds(GameObject root, Vector3 fallbackSize)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var found = false;
            var bounds = new Bounds(root.transform.position, fallbackSize);

            for (var i = 0; i < renderers.Length; i++)
            {
                if (!found)
                {
                    bounds = renderers[i].bounds;
                    found = true;
                    continue;
                }

                bounds.Encapsulate(renderers[i].bounds);
            }

            if (!found)
                bounds = new Bounds(root.transform.position, fallbackSize);

            bounds.size = new Vector3(
                Mathf.Max(bounds.size.x, fallbackSize.x),
                Mathf.Max(bounds.size.y, fallbackSize.y),
                Mathf.Max(bounds.size.z, fallbackSize.z));

            return bounds;
        }

        private static void EnsureZoneTracker()
        {
            var player = ResolvePlayer();
            if (player == null)
                return;

            if (player.GetComponent<ZoneTracker>() == null)
                player.AddComponent<ZoneTracker>();
        }

        private static GameObject ResolvePlayer()
        {
            var third = Object.FindAnyObjectByType<ThirdPersonFarmExplorer>();
            if (third != null)
                return third.gameObject;

            var explorer = Object.FindAnyObjectByType<FirstPersonExplorer>();
            if (explorer != null)
                return explorer.gameObject;

            var named = GameObject.Find("ExplorationPlayer");
            if (named != null)
                return named;

            try
            {
                var tagged = GameObject.FindWithTag("Player");
                if (tagged != null)
                    return tagged;
            }
            catch (UnityException)
            {
            }

            var rig = FarmFirstPersonRigUtility.EnsureRig();
            return rig != null ? rig.gameObject : null;
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
                return child;

            child = new GameObject(childName).transform;
            child.SetParent(parent, false);
            return child;
        }
    }
}
