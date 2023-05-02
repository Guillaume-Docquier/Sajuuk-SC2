using System.Numerics;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.MapAnalysis.ExpandAnalysis;
using Bot.MapAnalysis.RegionAnalysis;
using SC2APIProtocol;

namespace Bot.Tests.GameSense.RegionTracking;

// TODO GD Test that it updates automatically and once per frame
public class RegionsEvaluatorTests : BaseTestClass {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GivenMultipleRegions_WhenInit_ThenAllEvaluationsAreInitializedWithZero(bool normalized) {
        // Arrange
        var regionsEvaluator = new TestRegionsEvaluator();
        var regions = new IRegion[]
        {
            new TestRegion(),
            new TestRegion(),
            new TestRegion(),
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
            { new TestRegion(), 1 },
            { new TestRegion(), 2 },
            { new TestRegion(), 3 },
        };
        var regionsEvaluator = new TestRegionsEvaluator(evaluations);

        regionsEvaluator.Init(evaluations.Keys);
        regionsEvaluator.UpdateEvaluations();

        // Act
        var unknownRegion = new TestRegion();
        var evaluation = regionsEvaluator.GetEvaluation(unknownRegion, normalized);

        //Assert
        Assert.Equal(0, evaluation);
    }

    [Fact]
    public void GivenInit_WhenUpdateEvaluations_ThenEvaluationsAreUpdated() {
        // Arrange
        var evaluations = new Dictionary<IRegion, float>
        {
            { new TestRegion(), 1 },
            { new TestRegion(), 2 },
            { new TestRegion(), 3 },
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
            { new TestRegion(), 1 },
            { new TestRegion(), 2 },
            { new TestRegion(), 3 },
            { new TestRegion(), 4 },
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
            { new TestRegion(), 0 },
            { new TestRegion(), 0 },
            { new TestRegion(), 0 },
            { new TestRegion(), 0 },
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
            : base("test", () => 1) {
            _evaluations = null;
        }

        public TestRegionsEvaluator(Dictionary<IRegion, float> evaluations)
            : base("test", () => 1) {
            _evaluations = evaluations;
        }

        protected override IEnumerable<(IRegion region, float evaluation)> DoUpdateEvaluations(IReadOnlyCollection<IRegion> regions) {
            return regions.Select(region => (region, _evaluations == null ? 0 : _evaluations[region]));
        }
    }

    private class TestRegion : IRegion {
        public int Id { get; }
        public Color Color { get; }
        public Vector2 Center { get; }
        public HashSet<Vector2> Cells { get; }
        public float ApproximatedRadius { get; }
        public RegionType Type { get; }
        public IExpandLocation ExpandLocation { get; }
        public IEnumerable<INeighboringRegion> Neighbors { get; }
        public bool IsObstructed { get; }

        public TestRegion() {

        }

        public IEnumerable<IRegion> GetReachableNeighbors() {
            throw new NotImplementedException();
        }
    }
}
