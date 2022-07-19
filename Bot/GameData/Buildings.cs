using System;
using System.Collections.Generic;

namespace Bot.GameData;

public static class Buildings {
    public static float GetRadius(uint buildingType) {
        var buildingAbilityId = KnowledgeBase.GetUnitTypeData(buildingType).AbilityId;

        return KnowledgeBase.GetAbilityData((int)buildingAbilityId).FootprintRadius;
    }

    public class Dimension {
        public int Width { get; }
        public int Height { get; }
        public double Radius { get; }

        public Dimension(int width, int height) {
            Width = width;
            Height = height;

            var halfWidth = width / 2;
            var halfHeight = height / 2;
            Radius = Math.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight);
        }
    }

    public static readonly Dictionary<uint, Dimension> Dimensions = new Dictionary<uint, Dimension>
    {
        // Zerg
        { Units.Hatchery,         new Dimension(width: 5, height: 5) },
        { Units.Extractor,        new Dimension(width: 3, height: 3) },
        { Units.SpawningPool,     new Dimension(width: 3, height: 3) },
        { Units.EvolutionChamber, new Dimension(width: 3, height: 3) },
        { Units.Lair,             new Dimension(width: 5, height: 5) },
        { Units.RoachWarren,      new Dimension(width: 3, height: 3) },
        { Units.BanelingNest,     new Dimension(width: 3, height: 3) },
        { Units.SpineCrawler,     new Dimension(width: 2, height: 2) },
        { Units.SporeCrawler,     new Dimension(width: 2, height: 2) },
        { Units.HydraliskDen,     new Dimension(width: 3, height: 3) },
        { Units.InfestationPit,   new Dimension(width: 3, height: 3) },
        { Units.Spire,            new Dimension(width: 2, height: 2) },
        { Units.NydusNetwork,     new Dimension(width: 3, height: 3) },
        { Units.LurkerDen,        new Dimension(width: 3, height: 3) },
        { Units.Hive,             new Dimension(width: 5, height: 5) },
        { Units.UltraliskCavern,  new Dimension(width: 3, height: 3) },
        { Units.GreaterSpire,     new Dimension(width: 2, height: 2) },

        // Protoss

        // Terran
    };
}
