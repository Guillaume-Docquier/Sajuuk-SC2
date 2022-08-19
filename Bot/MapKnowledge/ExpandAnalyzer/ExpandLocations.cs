using System.Collections.Generic;
using System.Numerics;

namespace Bot.MapKnowledge;

public partial class ExpandAnalyzer {
    private readonly Dictionary<string, List<Vector3>> _precomputedExpandLocations = new Dictionary<string, List<Vector3>>
    {
        {
            Maps.Names.Berlingrad,
            new List<Vector3>
            {
                new Vector3(25.5f, 47.5f, 9.987f),
                new Vector3(118.5f, 134.5f, 7.9870005f),
                new Vector3(60.5f, 112.5f, 7.9870005f),
                new Vector3(120.5f, 24.5f, 11.987f),
                new Vector3(68.5f, 23.5f, 9.987f),
                new Vector3(33.5f, 21.5f, 7.9870005f),
                new Vector3(126.5f, 78.5f, 7.9870005f),
                new Vector3(83.5f, 132.5f, 9.987f),
                new Vector3(126.5f, 108.5f, 9.987f),
                new Vector3(123.5f, 48.5f, 9.987f),
                new Vector3(28.5f, 107.5f, 9.987f),
                new Vector3(91.5f, 43.5f, 7.9870005f),
                new Vector3(25.5f, 77.5f, 7.9870005f),
                new Vector3(31.5f, 131.5f, 11.999389f),
            }
        },
        {
            Maps.Names.Blackburn,
            new List<Vector3>
            {
                new Vector3(144.5f, 80.5f, 3.9870005f),
                new Vector3(36.5f, 31.5f, 7.9870005f),
                new Vector3(36.5f, 54.5f, 5.9870005f),
                new Vector3(67.5f, 54.5f, 3.9870005f),
                new Vector3(39.5f, 80.5f, 3.9870005f),
                new Vector3(116.5f, 54.5f, 3.9870005f),
                new Vector3(91.5f, 121.5f, 5.9870005f),
                new Vector3(92.5f, 32.5f, 5.9870005f),
                new Vector3(147.5f, 54.5f, 5.9870005f),
                new Vector3(36.5f, 115.5f, 5.9870005f),
                new Vector3(147.5f, 115.5f, 5.9870005f),
                new Vector3(126.5f, 99.5f, 3.9870005f),
                new Vector3(57.5f, 99.5f, 3.9870005f),
                new Vector3(147.5f, 31.5f, 7.9895344f),
            }
        },
        {
            Maps.Names.Hardwire,
            new List<Vector3>
            {
                new Vector3(140.5f, 134.5f, 7.9870005f),
                new Vector3(61.5f, 137.5f, 7.9870005f),
                new Vector3(75.5f, 81.5f, 7.9870005f),
                new Vector3(80.5f, 48.5f, 9.987f),
                new Vector3(109.5f, 47.5f, 9.987f),
                new Vector3(67.5f, 164.5f, 7.9870005f),
                new Vector3(91.5f, 148.5f, 9.987f),
                new Vector3(124.5f, 67.5f, 9.987f),
                new Vector3(135.5f, 167.5f, 9.987f),
                new Vector3(148.5f, 51.5f, 7.9870005f),
                new Vector3(106.5f, 168.5f, 9.987f),
                new Vector3(157.5f, 157.5f, 11.987f),
                new Vector3(156.5f, 108.5f, 9.987f),
                new Vector3(154.5f, 78.5f, 7.9870005f),
                new Vector3(59.5f, 107.5f, 9.987f),
                new Vector3(58.5f, 58.5f, 11.992681f),
            }
        },
        {
            Maps.Names.CuriousMinds,
            new List<Vector3>
            {
                new Vector3(50.5f, 91.5f, 5.9870005f),
                new Vector3(23.5f, 88.5f, 7.9870005f),
                new Vector3(66.5f, 115.5f, 7.9870005f),
                new Vector3(101.5f, 48.5f, 5.9870005f),
                new Vector3(128.5f, 51.5f, 7.9870005f),
                new Vector3(24.5f, 20.5f, 5.9870005f),
                new Vector3(85.5f, 24.5f, 7.9870005f),
                new Vector3(128.5f, 84.5f, 7.9870005f),
                new Vector3(125.5f, 24.5f, 9.987f),
                new Vector3(127.5f, 119.5f, 5.9870005f),
                new Vector3(23.5f, 55.5f, 7.9870005f),
                new Vector3(26.5f, 115.5f, 9.995708f),
            }
        },
        {
            Maps.Names.GlitteringAshes,
            new List<Vector3>
            {
                new Vector3(92.5f, 81.5f, 5.9870005f),
                new Vector3(63.5f, 123.5f, 7.9870005f),
                new Vector3(42.5f, 129.5f, 5.9870005f),
                new Vector3(173.5f, 74.5f, 5.9870005f),
                new Vector3(123.5f, 122.5f, 5.9870005f),
                new Vector3(108.5f, 47.5f, 7.9870005f),
                new Vector3(152.5f, 80.5f, 7.9870005f),
                new Vector3(75.5f, 47.5f, 7.9870005f),
                new Vector3(171.5f, 102.5f, 7.9870005f),
                new Vector3(107.5f, 156.5f, 7.9870005f),
                new Vector3(151.5f, 44.5f, 5.9870005f),
                new Vector3(48.5f, 54.5f, 9.987f),
                new Vector3(64.5f, 159.5f, 5.9870005f),
                new Vector3(140.5f, 156.5f, 7.9870005f),
                new Vector3(150.5f, 125.5f, 7.9870005f),
                new Vector3(65.5f, 78.5f, 7.9870005f),
                new Vector3(44.5f, 101.5f, 7.9870005f),
                new Vector3(167.5f, 149.5f, 9.986153f),
            }
        },
        {
            Maps.Names.TwoThousandAtmospheres,
            new List<Vector3>
            {
                new Vector3(157.5f, 72.5f, 7.9870005f),
                new Vector3(57.5f, 60.5f, 9.987f),
                new Vector3(169.5f, 98.5f, 7.9870005f),
                new Vector3(53.5f, 154.5f, 7.9870005f),
                new Vector3(144.5f, 152.5f, 7.9870005f),
                new Vector3(84.5f, 156.5f, 5.9870005f),
                new Vector3(139.5f, 47.5f, 5.9870005f),
                new Vector3(170.5f, 49.5f, 7.9870005f),
                new Vector3(54.5f, 105.5f, 7.9870005f),
                new Vector3(111.5f, 155.5f, 7.9870005f),
                new Vector3(77.5f, 80.5f, 5.9870005f),
                new Vector3(66.5f, 131.5f, 7.9870005f),
                new Vector3(146.5f, 123.5f, 5.9870005f),
                new Vector3(79.5f, 51.5f, 7.9870005f),
                new Vector3(112.5f, 48.5f, 7.9870005f),
                new Vector3(166.5f, 143.5f, 9.989865f),
            }
        },
    };
}
