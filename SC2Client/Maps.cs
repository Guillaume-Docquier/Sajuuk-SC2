namespace SC2Client;

public static class Maps {
    public const string AncientCistern = "AncientCisternAIE.SC2Map";
    public const string Berlingrad = "BerlingradAIE.SC2Map";
    public const string Blackburn = "BlackburnAIE.SC2Map";
    public const string CosmicSapphire = "CosmicSapphireAIE.SC2Map";
    public const string CuriousMinds = "CuriousMindsAIE.SC2Map";
    public const string DragonScales = "DragonScalesAIE.SC2Map";
    public const string GlitteringAshes = "GlitteringAshesAIE.SC2Map";
    public const string GoldenAura = "GoldenauraAIE.SC2Map";
    public const string Gresvan = "GresvanAIE.SC2Map";
    public const string Hardwire = "HardwireAIE.SC2Map";
    public const string InfestationStation = "InfestationStationAIE.SC2Map";
    public const string InsideAndOut = "InsideAndOutAIE.SC2Map";
    public const string Moondance = "MoondanceAIE.SC2Map";
    public const string RoyalBlood = "RoyalBloodAIE.SC2Map";
    public const string Stargazers = "StargazersAIE.SC2Map";
    public const string TwoThousandAtmospheres = "2000AtmospheresAIE.SC2Map";
    public const string Waterfall = "WaterfallAIE.SC2Map";

    /// <summary>
    /// Gets all the map file names.
    /// </summary>
    /// <returns>All the map file names.</returns>
    public static IEnumerable<string> GetAll() {
        return typeof(Maps)
            .GetFields()
            .Where(field => field.IsLiteral)
            .Select(x => x.GetValue(null))
            .Cast<string>();
    }

    /// <summary>
    /// Represents the map pool of season 3 of 2022.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static readonly List<string> Season_2022_3 = new List<string>
    {
        TwoThousandAtmospheres,
        Berlingrad,
        Blackburn,
        CuriousMinds,
        GlitteringAshes,
        Hardwire,
    };

    /// <summary>
    /// Represents the map pool of season 4 of 2022.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static readonly List<string> Season_2022_4 = new List<string>
    {
        Berlingrad,
        Hardwire,
        InsideAndOut,
        Moondance,
        Stargazers,
        Waterfall,
        CosmicSapphire,
    };

    /// <summary>
    /// Represents the map pool of season 2 of 2023.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static readonly List<string> Season_2023_2 = new List<string>
    {
        AncientCistern,
        DragonScales,
        GoldenAura,
        Gresvan,
        InfestationStation,
        RoyalBlood,
    };
}
