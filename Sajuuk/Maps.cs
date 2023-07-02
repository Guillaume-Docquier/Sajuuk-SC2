using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Sajuuk;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class Maps {
    public static class Season_2022_3 {
        public static class FileNames {
            public const string TwoThousandAtmospheres = "2000AtmospheresAIE.SC2Map";
            public const string Berlingrad = "BerlingradAIE.SC2Map";
            public const string Blackburn = "BlackburnAIE.SC2Map";
            public const string CuriousMinds = "CuriousMindsAIE.SC2Map";
            public const string GlitteringAshes = "GlitteringAshesAIE.SC2Map";
            public const string Hardwire = "HardwireAIE.SC2Map";
        }
    }

    public static class Season_2022_4 {
        public static class FileNames {
            public const string Berlingrad = "BerlingradAIE.SC2Map";
            public const string Hardwire = "HardwireAIE.SC2Map";
            public const string InsideAndOut = "InsideAndOutAIE.SC2Map";
            public const string Moondance = "MoondanceAIE.SC2Map";
            public const string Stargazers = "StargazersAIE.SC2Map";
            public const string Waterfall = "WaterfallAIE.SC2Map";
            public const string CosmicSapphire = "CosmicSapphireAIE.SC2Map";

            public static IEnumerable<string> GetAll() {
                return typeof(FileNames).GetFields().Select(x => x.GetValue(null)).Cast<string>();
            }
        }
    }

    public static class Season_2023_2 {
        public static class FileNames {
            public const string AncientCistern = "AncientCisternAIE.SC2Map";
            public const string DragonScales = "DragonScalesAIE.SC2Map";
            public const string Goldenaura = "GoldenauraAIE.SC2Map";
            public const string Gresvan = "GresvanAIE.SC2Map";
            public const string InfestationStation = "InfestationStationAIE.SC2Map";
            public const string RoyalBlood = "RoyalBloodAIE.SC2Map";

            public static IEnumerable<string> GetAll() {
                return typeof(FileNames).GetFields().Select(x => x.GetValue(null)).Cast<string>();
            }
        }
    }
}
