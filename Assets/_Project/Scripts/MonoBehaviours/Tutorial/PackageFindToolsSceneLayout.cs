using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Tutorial
{
    public static class PackageFindToolsSceneLayout
    {
        private static readonly Vector3[] YardPositions =
        {
            new(-4.5f, 0.6f, -0.5f),
            new(2.75f, 0.6f, 2.25f),
            new(-1.5f, 0.6f, 5.25f),
        };

        private static readonly Vector3[] ShedEdgePositions =
        {
            new(6.2f, 0.6f, -2.5f),
            new(7.8f, 0.6f, 0.75f),
            new(5.5f, 0.6f, 3.6f),
        };

        private static readonly Vector3[] FieldPathPositions =
        {
            new(-7.2f, 0.6f, -1.4f),
            new(-6.3f, 0.6f, 2.1f),
            new(-4.7f, 0.6f, 5.8f),
        };

        public static Vector3[] GetPickupPositions(string searchZone)
        {
            return searchZone switch
            {
                "shed_edge" => ShedEdgePositions,
                "field_path" => FieldPathPositions,
                _ => YardPositions,
            };
        }
    }
}
