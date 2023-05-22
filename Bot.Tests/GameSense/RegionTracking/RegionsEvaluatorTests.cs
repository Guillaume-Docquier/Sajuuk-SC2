using Bot.GameSense.RegionsEvaluationsTracking.RegionsEvaluations;
using Bot.MapAnalysis.RegionAnalysis;
using Moq;

namespace Bot.Tests.GameSense.RegionTracking;

// TODO GD Test that it updates automatically and once per frame
public class RegionsEvaluatorTests : BaseTestClass {
    private readonly Mock<IFrameClock> _frameClockMock;

    public RegionsEvaluatorTests() {
        _frameClockMock = new Mock<IFrameClock>();
        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GivenMultipleRegions_WhenInit_ThenAllEvaluationsAreInitializedWithZero(bool normalized) {
        // Arrange
        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(1);
        var regionsEvaluator = new TestRegionsEvaluator(_frameClockMock.Object);
        var regions = new IRegion[]
        {
            new Mock<IRegion>().Object,
            new Mock<IRegion>().Object,
            new Mock<IRegion>().Object,
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
            { new Mock<IRegion>().Object, 1 },
            { new Mock<IRegion>().Object, 2 },
            { new Mock<IRegion>().Object, 3 },
        };
        var regionsEvaluator = new TestRegionsEvaluator(_frameClockMock.Object, evaluations);

        regionsEvaluator.Init(evaluations.Keys);
        regionsEvaluator.UpdateEvaluations();

        // Act
        var unknownRegion = new Mock<IRegion>().Object;
        var evaluation = regionsEvaluator.GetEvaluation(unknownRegion, normalized);

        //Assert
        Assert.Equal(0, evaluation);
    }

    [Fact]
    public void GivenInit_WhenUpdateEvaluations_ThenEvaluationsAreUpdated() {
        // Arrange
        var evaluations = new Dictionary<IRegion, float>
        {
            { new Mock<IRegion>().Object, 1 },
            { new Mock<IRegion>().Object, 2 },
            { new Mock<IRegion>().Object, 3 },
        };
        var regionsEvaluator = new TestRegionsEvaluator(_frameClockMock.Object, evaluations);

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
            { new Mock<IRegion>().Object, 1 },
            { new Mock<IRegion>().Object, 2 },
            { new Mock<IRegion>().Object, 3 },
            { new Mock<IRegion>().Object, 4 },
        };
        var regionsEvaluator = new TestRegionsEvaluator(_frameClockMock.Object, evaluations);

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
            { new Mock<IRegion>().Object, 0 },
            { new Mock<IRegion>().Object, 0 },
            { new Mock<IRegion>().Object, 0 },
            { new Mock<IRegion>().Object, 0 },
        };
        var regionsEvaluator = new TestRegionsEvaluator(_frameClockMock.Object, evaluations);

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

        public TestRegionsEvaluator(IFrameClock frameClock)
            : base(frameClock, "test") {
            _evaluations = null;
        }

        public TestRegionsEvaluator(IFrameClock frameClock, Dictionary<IRegion, float> evaluations)
            : base(frameClock, "test") {
            _evaluations = evaluations;
        }

        protected override IEnumerable<(IRegion region, float evaluation)> DoUpdateEvaluations(IReadOnlyCollection<IRegion> regions) {
            return regions.Select(region => (region, _evaluations == null ? 0 : _evaluations[region]));
        }
    }
}
