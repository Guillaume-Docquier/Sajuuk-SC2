using System.Numerics;
using Bot.GameSense.RegionTracking;
using Bot.MapKnowledge;

namespace Bot.Tests.GameSense.RegionTracking;

// TODO GD Test that it updates automatically and once per frame
public class RegionsEvaluatorTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GivenMultipleRegions_WhenInit_ThenAllEvaluationsAreInitializedWithZero(bool normalized) {
        // Arrange
        var regionsEvaluator = new TestRegionsEvaluator();
        var regions = new Region[]
        {
            new Region(new HashSet<Vector2>(), new Vector2(1, 0), RegionType.Expand, isObstructed: false),
            new Region(new HashSet<Vector2>(), new Vector2(2, 0), RegionType.Expand, isObstructed: false),
            new Region(new HashSet<Vector2>(), new Vector2(3, 0), RegionType.Expand, isObstructed: false),
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
        var evaluations = new Dictionary<IRegion, float>
        {
            { new Region(new HashSet<Vector2>(), new Vector2(1, 0), RegionType.Expand, isObstructed: false), 1 },
            { new Region(new HashSet<Vector2>(), new Vector2(2, 0), RegionType.Expand, isObstructed: false), 2 },
            { new Region(new HashSet<Vector2>(), new Vector2(3, 0), RegionType.Expand, isObstructed: false), 3 },
        };
        var regionsEvaluator = new TestRegionsEvaluator(evaluations);

        regionsEvaluator.Init(evaluations.Keys);
        regionsEvaluator.UpdateEvaluations();

        // Act
        var unknownRegion = new Region(new HashSet<Vector2>(), new Vector2(), RegionType.Expand, isObstructed: false);
        var evaluation = regionsEvaluator.GetEvaluation(unknownRegion, normalized);

        //Assert
        Assert.Equal(0, evaluation);
    }

    [Fact]
    public void GivenInit_WhenUpdateEvaluations_ThenEvaluationsAreUpdated() {
        // Arrange
        var evaluations = new Dictionary<IRegion, float>
        {
            { new Region(new HashSet<Vector2>(), new Vector2(1, 0), RegionType.Expand, isObstructed: false), 1 },
            { new Region(new HashSet<Vector2>(), new Vector2(2, 0), RegionType.Expand, isObstructed: false), 2 },
            { new Region(new HashSet<Vector2>(), new Vector2(3, 0), RegionType.Expand, isObstructed: false), 3 },
        };
        var regionsEvaluator = new TestRegionsEvaluator(evaluations);

        regionsEvaluator.Init(evaluations.Keys);

        // Act
        regionsEvaluator.UpdateEvaluations();

        //Assert
        foreach (var region in evaluations.Keys) {
            Assert.Equal(evaluations[region], regionsEvaluator.GetEvaluation(region));
        }
    }

    [Fact]
    public void GivenNonZeroEvaluations_WhenUpdateEvaluations_ThenNormalizedEvaluationsAreUpdated() {
        // Arrange
        var evaluations = new Dictionary<IRegion, float>
        {
            { new Region(new HashSet<Vector2>(), new Vector2(1, 0), RegionType.Expand, isObstructed: false), 1 },
            { new Region(new HashSet<Vector2>(), new Vector2(2, 0), RegionType.Expand, isObstructed: false), 2 },
            { new Region(new HashSet<Vector2>(), new Vector2(3, 0), RegionType.Expand, isObstructed: false), 3 },
            { new Region(new HashSet<Vector2>(), new Vector2(4, 0), RegionType.Expand, isObstructed: false), 4 },
        };
        var regionsEvaluator = new TestRegionsEvaluator(evaluations);

        regionsEvaluator.Init(evaluations.Keys);

        // Act
        regionsEvaluator.UpdateEvaluations();

        //Assert
        var totalEvaluations = evaluations.Values.Sum();
        foreach (var region in evaluations.Keys) {
            Assert.Equal(evaluations[region] / totalEvaluations, regionsEvaluator.GetEvaluation(region, normalized: true));
        }
    }

    [Fact]
    public void GivenAllZeroEvaluations_WhenUpdateEvaluations_ThenNormalizedEvaluationsAreAllZero() {
        // Arrange
        var evaluations = new Dictionary<IRegion, float>
        {
            { new Region(new HashSet<Vector2>(), new Vector2(1, 0), RegionType.Expand, isObstructed: false), 0 },
            { new Region(new HashSet<Vector2>(), new Vector2(2, 0), RegionType.Expand, isObstructed: false), 0 },
            { new Region(new HashSet<Vector2>(), new Vector2(3, 0), RegionType.Expand, isObstructed: false), 0 },
            { new Region(new HashSet<Vector2>(), new Vector2(4, 0), RegionType.Expand, isObstructed: false), 0 },
        };
        var regionsEvaluator = new TestRegionsEvaluator(evaluations);

        regionsEvaluator.Init(evaluations.Keys);

        // Act
        regionsEvaluator.UpdateEvaluations();

        //Assert
        foreach (var region in evaluations.Keys) {
            Assert.Equal(0, regionsEvaluator.GetEvaluation(region, normalized: true));
        }
    }

    private class TestRegionsEvaluator : RegionsEvaluator {
        private readonly Dictionary<IRegion, float>? _evaluations;

        public TestRegionsEvaluator()
            : base("test") {
            _evaluations = null;
        }

        public TestRegionsEvaluator(Dictionary<IRegion, float> evaluations)
            : base("test") {
            _evaluations = evaluations;
        }

        protected override IEnumerable<(IRegion region, float evaluation)> DoUpdateEvaluations(IReadOnlyCollection<IRegion> regions) {
            return regions.Select(region => (region, _evaluations == null ? 0 : _evaluations[region]));
        }
    }
}
