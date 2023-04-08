using System.Numerics;
using Bot.GameSense.RegionTracking;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Tests.GameSense.RegionTracking;

public class RegionsEvaluatorTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GivenMultipleRegions_WhenInit_ThenAllEvaluationsAreInitializedWithZero(bool normalized) {
        // Arrange
        var regionsEvaluator = new TestRegionsEvaluator(Alliance.Self, "test");
        var regions = new Region[]
        {
            new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false),
            new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false),
            new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false),
        };

        // Act
        regionsEvaluator.Init(regions);

        //Assert
        foreach (var region in regions) {
            Assert.Equal(0, regionsEvaluator.GetEvaluation(region, normalized));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GivenAnUnknownRegion_WhenGetEvaluation_ThenReturnsZero(bool normalized) {
        // Arrange
        var evaluations = new Dictionary<Region, float>
        {
            { new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false), 1 },
            { new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false), 2 },
            { new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false), 3 },
        };
        var regionsEvaluator = new TestRegionsEvaluator(evaluations, Alliance.Self, "test");

        regionsEvaluator.Init(evaluations.Keys);
        regionsEvaluator.Evaluate();

        // Act
        var unknownRegion = new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false);
        var evaluation = regionsEvaluator.GetEvaluation(unknownRegion, normalized);

        //Assert
        Assert.Equal(0, evaluation);
    }

    [Fact]
    public void GivenInit_WhenEvaluate_ThenEvaluationsAreUpdated() {
        // Arrange
        var evaluations = new Dictionary<Region, float>
        {
            { new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false), 1 },
            { new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false), 2 },
            { new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false), 3 },
        };
        var regionsEvaluator = new TestRegionsEvaluator(evaluations, Alliance.Self, "test");

        regionsEvaluator.Init(evaluations.Keys);

        // Act
        regionsEvaluator.Evaluate();

        //Assert
        foreach (var region in evaluations.Keys) {
            Assert.Equal(evaluations[region], regionsEvaluator.GetEvaluation(region));
        }
    }

    [Fact]
    public void GivenNonZeroEvaluations_WhenEvaluate_ThenNormalizedEvaluationsAreUpdated() {
        // Arrange
        var evaluations = new Dictionary<Region, float>
        {
            { new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false), 1 },
            { new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false), 2 },
            { new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false), 3 },
            { new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false), 4 },
        };
        var regionsEvaluator = new TestRegionsEvaluator(evaluations, Alliance.Self, "test");

        regionsEvaluator.Init(evaluations.Keys);

        // Act
        regionsEvaluator.Evaluate();

        //Assert
        var totalEvaluations = evaluations.Values.Sum();
        foreach (var region in evaluations.Keys) {
            Assert.Equal(evaluations[region] / totalEvaluations, regionsEvaluator.GetEvaluation(region, normalized: true));
        }
    }

    [Fact]
    public void GivenAllZeroEvaluations_WhenEvaluate_ThenNormalizedEvaluationsAreAllZero() {
        // Arrange
        var evaluations = new Dictionary<Region, float>
        {
            { new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false), 0 },
            { new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false), 0 },
            { new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false), 0 },
            { new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false), 0 },
        };
        var regionsEvaluator = new TestRegionsEvaluator(evaluations, Alliance.Self, "test");

        regionsEvaluator.Init(evaluations.Keys);

        // Act
        regionsEvaluator.Evaluate();

        //Assert
        foreach (var region in evaluations.Keys) {
            Assert.Equal(0, regionsEvaluator.GetEvaluation(region, normalized: true));
        }
    }

    private class TestRegionsEvaluator : RegionsEvaluator {
        private readonly Dictionary<Region, float>? _evaluations;

        public TestRegionsEvaluator(Alliance alliance, string evaluatedPropertyName)
            : base(alliance, evaluatedPropertyName) {
            _evaluations = null;
        }

        public TestRegionsEvaluator(Dictionary<Region, float> evaluations, Alliance alliance, string evaluatedPropertyName)
            : base(alliance, evaluatedPropertyName) {
            _evaluations = evaluations;
        }

        protected override IEnumerable<(Region region, float value)> DoEvaluate(IReadOnlyCollection<Region> regions) {
            return regions.Select(region => (region, _evaluations == null ? 0 : _evaluations[region]));
        }
    }
}
