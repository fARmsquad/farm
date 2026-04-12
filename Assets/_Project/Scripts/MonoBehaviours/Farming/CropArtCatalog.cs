using System;
using System.Collections.Generic;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.MonoBehaviours
{
    internal static class CropArtCatalog
    {
        public const string StreamingAssetRelativePath = "Farming/crops_low_poly.glb";
        private const float PlotPaddingMeters = 0.24f;

        internal readonly struct CropDisplayProfile
        {
            public CropDisplayProfile(float matureHeightMeters, float maxFootprintMeters)
            {
                MatureHeightMeters = matureHeightMeters;
                MaxFootprintMeters = maxFootprintMeters;
            }

            public float MatureHeightMeters { get; }
            public float MaxFootprintMeters { get; }
        }

        private static readonly Dictionary<string, string[]> StageNamesByCrop = new(StringComparer.Ordinal)
        {
            ["carrot"] = new[] { "Carrot_F1_Carrot_0", "Carrot_F2_Carrot_0", "Carrot_F3_Carrot_0" },
            ["potato"] = new[] { "Potatoe_F1_Potatoe_0", "Potatoe_F2_Potatoe_0", "Potatoe_F3_Potatoe_0" },
            ["tomato"] = new[] { "Tomatoe_F1_Tomatoe_0", "Tomatoe_F2_Tomatoe_0", "Tomatoe_F3_Tomatoe_0" },
            ["wheat"] = new[] { "Wheat_F1_Wheat_0", "Wheat_F2_Wheat_0", "Wheat_F3_Wheat_0" },
        };

        private static readonly Dictionary<string, CropDisplayProfile> DisplayProfilesByCrop = new(StringComparer.Ordinal)
        {
            ["carrot"] = new CropDisplayProfile(0.84f, 1.77f),
            ["potato"] = new CropDisplayProfile(0.84f, 1.77f),
            ["tomato"] = new CropDisplayProfile(0.89f, 1.99f),
            ["wheat"] = new CropDisplayProfile(0.76f, 1.80f),
        };

        public static IReadOnlyList<string> SoilSourceNames { get; } = new[]
        {
            "Soil_Dirt_0",
            "Soil.001_Dirt_0",
            "Soil.002_Dirt_0",
            "Soil.003_Dirt_0",
        };

        public static IEnumerable<KeyValuePair<string, string[]>> StageDefinitions => StageNamesByCrop;
        public static float RecommendedPlotSurfaceSizeMeters { get; } = ResolveRecommendedPlotSurfaceSize();

        public static bool TryGetCropKey(string cropId, out string cropKey)
        {
            cropKey = cropId switch
            {
                "seed_carrot" => "carrot",
                "crop_carrot" => "carrot",
                "seed_potato" => "potato",
                "crop_potato" => "potato",
                "seed_tomato" => "tomato",
                "crop_tomato" => "tomato",
                "seed_wheat" => "wheat",
                "crop_wheat" => "wheat",
                _ => null,
            };

            return cropKey != null;
        }

        public static bool TryGetSourceStageNames(string cropKey, out string[] stageNames)
        {
            return StageNamesByCrop.TryGetValue(cropKey, out stageNames);
        }

        public static bool TryGetDisplayProfile(string cropKey, out CropDisplayProfile profile)
        {
            return DisplayProfilesByCrop.TryGetValue(cropKey, out profile);
        }

        public static int ResolveStageIndex(PlotPhase phase)
        {
            return phase switch
            {
                PlotPhase.Planted    => 1,
                PlotPhase.Sprout     => 1,
                PlotPhase.YoungPlant => 2,
                PlotPhase.Budding    => 2,
                PlotPhase.Fruiting   => 3,
                PlotPhase.Ready      => 3,
                _                    => 0,
            };
        }

        private static float ResolveRecommendedPlotSurfaceSize()
        {
            float maxFootprint = 1f;
            foreach (var profile in DisplayProfilesByCrop.Values)
            {
                if (profile.MaxFootprintMeters > maxFootprint)
                    maxFootprint = profile.MaxFootprintMeters;
            }

            return maxFootprint + PlotPaddingMeters;
        }
    }
}
