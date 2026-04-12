using System.Collections.Generic;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public sealed class HorseTrainingSceneObjects
    {
        public Transform Horse { get; set; }
        public GameObject[] TreatMarkers { get; set; }
        public GameObject[] JumpRails { get; set; }
        public GameObject[] SlalomGates { get; set; }
    }

    public static class HorseTrainingSceneLayout
    {
        public static readonly Vector3[] TreatMarkerPositions =
        {
            new(-3.5f, 0.55f, -2.5f),
            new(0f, 0.55f, 1.5f),
            new(3.5f, 0.55f, 5.5f),
        };

        public static readonly Vector3[] JumpRailPositions =
        {
            new(0f, 0.6f, 9f),
            new(0f, 0.6f, 12.5f),
        };

        public static readonly Vector3[] SlalomGatePositions =
        {
            new(-2.2f, 0.5f, 16f),
            new(2.2f, 0.5f, 19.5f),
            new(-2.2f, 0.5f, 23f),
            new(2.2f, 0.5f, 26.5f),
        };

        public static HorseTrainingSceneObjects Ensure()
        {
            var existingRoot = GameObject.Find("HorseTrainingGrounds_Root");
            if (existingRoot == null)
                existingRoot = BuildRoot();

            return new HorseTrainingSceneObjects
            {
                Horse = GameObject.Find("HorseProxy")?.transform,
                TreatMarkers = FindObjectsByPrefix(existingRoot.transform, "TreatMarker"),
                JumpRails = FindObjectsByPrefix(existingRoot.transform, "JumpRail"),
                SlalomGates = FindObjectsByPrefix(existingRoot.transform, "SlalomGate"),
            };
        }

        private static GameObject BuildRoot()
        {
            var root = new GameObject("HorseTrainingGrounds_Root");
            var course = new GameObject("HorseTrainingCourse");
            course.transform.SetParent(root.transform, false);

            CreateGround(course.transform);
            CreateSpawnPoint(root.transform);
            CreatePaddock(course.transform);
            CreateHorse(course.transform);
            CreateTreatMarkers(course.transform);
            CreateJumpRails(course.transform);
            CreateSlalomGates(course.transform);
            return root;
        }

        private static GameObject[] FindObjectsByPrefix(Transform root, string prefix)
        {
            var matches = new List<GameObject>();
            var allTransforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var child in allTransforms)
            {
                if (child == root)
                    continue;

                if (child.name.StartsWith(prefix))
                    matches.Add(child.gameObject);
            }

            matches.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return matches.ToArray();
        }

        private static void CreateGround(Transform parent)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.localScale = new Vector3(3.4f, 1f, 3.8f);
            ground.GetComponent<Renderer>().material.color = new Color(0.76f, 0.74f, 0.64f);

            var lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lane.name = "TrainingLane";
            lane.transform.SetParent(parent, false);
            lane.transform.position = new Vector3(0f, 0.04f, 12f);
            lane.transform.localScale = new Vector3(7f, 0.06f, 26f);
            lane.GetComponent<Renderer>().material.color = new Color(0.91f, 0.88f, 0.78f);
        }

        private static void CreateSpawnPoint(Transform parent)
        {
            var spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(parent, false);
            spawnPoint.transform.position = new Vector3(0f, 0.1f, -9f);
        }

        private static void CreatePaddock(Transform parent)
        {
            CreateFence(parent, new Vector3(0f, 0.7f, 30.5f), new Vector3(18f, 1.4f, 0.5f));
            CreateFence(parent, new Vector3(0f, 0.7f, -11.5f), new Vector3(18f, 1.4f, 0.5f));
            CreateFence(parent, new Vector3(-9f, 0.7f, 9.5f), new Vector3(0.5f, 1.4f, 42f));
            CreateFence(parent, new Vector3(9f, 0.7f, 9.5f), new Vector3(0.5f, 1.4f, 42f));

            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "TrainingGroundsSign";
            sign.transform.SetParent(parent, false);
            sign.transform.position = new Vector3(-5.6f, 1.8f, -7.5f);
            sign.transform.localScale = new Vector3(3.8f, 1.8f, 0.25f);
            sign.GetComponent<Renderer>().material.color = new Color(0.49f, 0.34f, 0.2f);
        }

        private static void CreateFence(Transform parent, Vector3 position, Vector3 scale)
        {
            var fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fence.name = "BoundaryFence";
            fence.transform.SetParent(parent, false);
            fence.transform.position = position;
            fence.transform.localScale = scale;
            fence.GetComponent<Renderer>().material.color = new Color(0.47f, 0.33f, 0.18f);
        }

        private static void CreateHorse(Transform parent)
        {
            var horse = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            horse.name = "HorseProxy";
            horse.transform.SetParent(parent, false);
            horse.transform.position = new Vector3(-2f, 1f, -7f);
            horse.transform.localScale = new Vector3(1.2f, 1f, 2.1f);
            horse.GetComponent<Renderer>().material.color = new Color(0.56f, 0.36f, 0.22f);

            var neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neck.name = "HorseNeck";
            neck.transform.SetParent(horse.transform, false);
            neck.transform.localPosition = new Vector3(0.2f, 0.55f, 0.55f);
            neck.transform.localRotation = Quaternion.Euler(72f, 0f, 0f);
            neck.transform.localScale = new Vector3(0.22f, 0.45f, 0.22f);
            neck.GetComponent<Renderer>().material.color = new Color(0.5f, 0.3f, 0.16f);
        }

        private static void CreateTreatMarkers(Transform parent)
        {
            for (int i = 0; i < TreatMarkerPositions.Length; i++)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"TreatMarker_{i + 1}";
                marker.transform.SetParent(parent, false);
                marker.transform.position = TreatMarkerPositions[i];
                marker.transform.localScale = Vector3.one * 0.65f;
                marker.GetComponent<Renderer>().material.color = new Color(0.95f, 0.7f, 0.28f);
            }
        }

        private static void CreateJumpRails(Transform parent)
        {
            for (int i = 0; i < JumpRailPositions.Length; i++)
            {
                var rail = new GameObject($"JumpRail_{i + 1}");
                rail.transform.SetParent(parent, false);
                rail.transform.position = JumpRailPositions[i];

                CreateRailPart(rail.transform, "RailBar", Vector3.zero, new Vector3(2.8f, 0.18f, 0.18f), new Color(0.25f, 0.52f, 0.85f));
                CreateRailPart(rail.transform, "RailPostLeft", new Vector3(-1.3f, 0f, 0f), new Vector3(0.18f, 1.4f, 0.18f), Color.white);
                CreateRailPart(rail.transform, "RailPostRight", new Vector3(1.3f, 0f, 0f), new Vector3(0.18f, 1.4f, 0.18f), Color.white);
            }
        }

        private static void CreateRailPart(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().material.color = color;
        }

        private static void CreateSlalomGates(Transform parent)
        {
            for (int i = 0; i < SlalomGatePositions.Length; i++)
            {
                var gate = new GameObject($"SlalomGate_{i + 1}");
                gate.transform.SetParent(parent, false);
                gate.transform.position = SlalomGatePositions[i];

                CreateFlag(gate.transform, "LeftFlag", new Vector3(-0.7f, 0f, 0f), new Color(0.93f, 0.27f, 0.2f));
                CreateFlag(gate.transform, "RightFlag", new Vector3(0.7f, 0f, 0f), new Color(0.98f, 0.93f, 0.37f));
            }
        }

        private static void CreateFlag(Transform parent, string name, Vector3 localPosition, Color color)
        {
            var flag = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flag.name = name;
            flag.transform.SetParent(parent, false);
            flag.transform.localPosition = localPosition;
            flag.transform.localScale = new Vector3(0.12f, 1f, 0.12f);
            flag.GetComponent<Renderer>().material.color = color;
        }
    }
}
