using FarmSimVR.MonoBehaviours;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Hunting
{
    public static class WorldPenBootstrap
    {
        public static bool EnsureInstalled(GameObject host)
        {
            if (host == null)
                throw new System.ArgumentNullException(nameof(host));

            var penRoot = ResolvePenRoot();
            if (penRoot == null)
                return false;

            var player = ResolvePlayer();
            if (player == null)
                return false;

            var input = EnsureComponent<KeyboardPlayerInput>(player);
            if (player.GetComponent<ZoneTracker>() == null)
                player.AddComponent<ZoneTracker>();

            var runtimeRoot = EnsureChild(penRoot, "PenGameRuntime");
            var spawner = EnsureComponent<WildAnimalSpawner>(runtimeRoot.gameObject);
            spawner.enabled = false;

            var animalPen = EnsureComponent<AnimalPen>(runtimeRoot.gameObject);
            animalPen.ConfigureRuntime(System.Array.Empty<PenAnimalEntry>(), penRoot.position, ResolvePenRadius(penRoot), false);

            var dropOff = EnsureDropOff(runtimeRoot, penRoot);
            var progression = EnsureComponent<WorldPenProgressionController>(host);
            var controller = EnsureComponent<WorldPenGameController>(host);
            controller.Configure(penRoot, spawner, dropOff, animalPen, input, progression);

            var shortcuts = EnsureComponent<WorldPenDevShortcuts>(host);
            shortcuts.Configure(controller, progression);
            EnsureComponent<WorldPenOverlay>(host).Configure(controller, progression, shortcuts);
            return true;
        }

        private static Transform ResolvePenRoot()
        {
            var farm = GameObject.Find("Farm");
            if (farm != null)
            {
                var pen = farm.transform.Find("Pen");
                if (pen != null)
                    return pen;
            }

            var fallback = GameObject.Find("Pen");
            return fallback != null ? fallback.transform : null;
        }

        private static GameObject ResolvePlayer()
        {
            var explorer = Object.FindAnyObjectByType<FirstPersonExplorer>();
            if (explorer != null)
                return explorer.gameObject;

            var named = GameObject.Find("ExplorationPlayer");
            if (named != null)
                return named;

            try
            {
                return GameObject.FindWithTag("Player");
            }
            catch (UnityException)
            {
                return null;
            }
        }

        private static BarnDropOff EnsureDropOff(Transform runtimeRoot, Transform penRoot)
        {
            var dropOff = runtimeRoot.Find("PenDropOff");
            if (dropOff == null)
            {
                var gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gate.name = "PenDropOff";
                gate.transform.SetParent(runtimeRoot, false);
                dropOff = gate.transform;
            }

            var bounds = CalculateBounds(penRoot.gameObject, new Vector3(12f, 2f, 12f));
            dropOff.position = bounds.center + new Vector3(0f, 1f, bounds.extents.z + 1.25f);
            dropOff.localRotation = Quaternion.identity;
            dropOff.localScale = new Vector3(2.5f, 2f, 1.25f);

            var collider = dropOff.GetComponent<BoxCollider>();
            if (collider == null)
                collider = dropOff.gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;

            return EnsureComponent<BarnDropOff>(dropOff.gameObject);
        }

        private static float ResolvePenRadius(Transform penRoot)
        {
            var bounds = CalculateBounds(penRoot.gameObject, new Vector3(12f, 2f, 12f));
            return Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.75f;
        }

        private static Bounds CalculateBounds(GameObject root, Vector3 fallbackSize)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var found = false;
            var bounds = new Bounds(root.transform.position, fallbackSize);

            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].transform.name == "PenDropOff")
                    continue;

                if (!found)
                {
                    bounds = renderers[i].bounds;
                    found = true;
                    continue;
                }

                bounds.Encapsulate(renderers[i].bounds);
            }

            return found ? bounds : new Bounds(root.transform.position, fallbackSize);
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

        private static T EnsureComponent<T>(GameObject host) where T : Component
        {
            var existing = host.GetComponent<T>();
            return existing != null ? existing : host.AddComponent<T>();
        }
    }
}
