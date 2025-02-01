namespace SC2Client.GameData;

// TODO GD I think we don't need this. The game data gives us all the necessary info?
public static class Buildings {
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
        { UnitTypeId.Hatchery,         new Dimension(width: 5, height: 5) },
        { UnitTypeId.Extractor,        new Dimension(width: 3, height: 3) },
        { UnitTypeId.SpawningPool,     new Dimension(width: 3, height: 3) },
        { UnitTypeId.EvolutionChamber, new Dimension(width: 3, height: 3) },
        { UnitTypeId.Lair,             new Dimension(width: 5, height: 5) },
        { UnitTypeId.RoachWarren,      new Dimension(width: 3, height: 3) },
        { UnitTypeId.BanelingNest,     new Dimension(width: 3, height: 3) },
        { UnitTypeId.SpineCrawler,     new Dimension(width: 2, height: 2) },
        { UnitTypeId.SporeCrawler,     new Dimension(width: 2, height: 2) },
        { UnitTypeId.HydraliskDen,     new Dimension(width: 3, height: 3) },
        { UnitTypeId.InfestationPit,   new Dimension(width: 3, height: 3) },
        { UnitTypeId.Spire,            new Dimension(width: 2, height: 2) },
        { UnitTypeId.NydusNetwork,     new Dimension(width: 3, height: 3) },
        { UnitTypeId.LurkerDen,        new Dimension(width: 3, height: 3) },
        { UnitTypeId.Hive,             new Dimension(width: 5, height: 5) },
        { UnitTypeId.UltraliskCavern,  new Dimension(width: 3, height: 3) },
        { UnitTypeId.GreaterSpire,     new Dimension(width: 2, height: 2) },
        { UnitTypeId.CreepTumor,       new Dimension(width: 1, height: 1) },

        // Protoss

        // Terran
    };
}
