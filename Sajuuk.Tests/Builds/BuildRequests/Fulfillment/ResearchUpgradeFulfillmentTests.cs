using Moq;
using Sajuuk.Actions;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.Builds.BuildRequests.Fulfillment;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis;
using Sajuuk.Tests.Fixtures;

namespace Sajuuk.Tests.Builds.BuildRequests.Fulfillment;

public class ResearchUpgradeFulfillmentTests : IClassFixture<NoLoggerFixture> {
    private readonly Mock<IUnitsTracker> _unitsTrackerMock = new Mock<IUnitsTracker>();
    private readonly Mock<IFrameClock> _frameClockMock = new Mock<IFrameClock>();
    private readonly KnowledgeBase _knowledgeBase = new TestKnowledgeBase();
    private readonly Mock<IPathfinder> _pathfinderMock = new Mock<IPathfinder>();
    private readonly Mock<ITerrainTracker> _terrainTrackerMock = new Mock<ITerrainTracker>();
    private readonly Mock<IController> _controllerMock = new Mock<IController>();
    private readonly IActionBuilder _actionBuilder;

    private readonly BuildRequestFulfillmentFactory _buildRequestFulfillmentFactory;

    public ResearchUpgradeFulfillmentTests() {
        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(0);
        _controllerMock.Setup(controller => controller.ResearchedUpgrades).Returns(new HashSet<uint>());
        _actionBuilder = new ActionBuilder(_knowledgeBase);

        _buildRequestFulfillmentFactory = new BuildRequestFulfillmentFactory(
            _unitsTrackerMock.Object,
            _frameClockMock.Object,
            _knowledgeBase,
            _pathfinderMock.Object,
            new FootprintCalculator(_terrainTrackerMock.Object),
            _terrainTrackerMock.Object,
            _controllerMock.Object
        );
    }

    private static IEnumerable<object[]> BuildRequestsToSatisfy() {
        yield return new object[] { new DummyBuildRequest(BuildType.Research, Upgrades.Burrow)        , true };
        yield return new object[] { new DummyBuildRequest(BuildType.Train, Upgrades.Burrow)           , false };
        yield return new object[] { new DummyBuildRequest(BuildType.Build, Upgrades.Burrow)           , false };
        yield return new object[] { new DummyBuildRequest(BuildType.Expand, Upgrades.Burrow)          , false };
        yield return new object[] { new DummyBuildRequest(BuildType.Research, Upgrades.TunnelingClaws), false };
        yield return new object[] { new DummyBuildRequest(BuildType.Train, Upgrades.TunnelingClaws)   , false };
        yield return new object[] { new DummyBuildRequest(BuildType.Build, Upgrades.TunnelingClaws)   , false };
        yield return new object[] { new DummyBuildRequest(BuildType.Expand, Upgrades.TunnelingClaws)  , false };
    }

    [Theory]
    [MemberData(nameof(BuildRequestsToSatisfy))]
    public void GivenBuildRequest_WhenCanSatisfy_ThenReturnsTrueIfBuildTypeIsResearchAndUpgradeTypeMatches(IBuildRequest buildRequest, bool expectedCanSatisfy) {
        // Arrange
        var producer = TestUtils.CreateUnit(Units.EvolutionChamber, _knowledgeBase, actionBuilder: _actionBuilder);
        const uint upgradeTypeToResearch = Upgrades.Burrow;
        var producerOrder = producer.ResearchUpgrade(upgradeTypeToResearch);
        var researchUpgradeFulfillment = _buildRequestFulfillmentFactory.CreateResearchUpgradeFulfillment(producer, producerOrder, upgradeTypeToResearch);

        // Act
        var canSatisfy = researchUpgradeFulfillment.CanSatisfy(buildRequest);

        // Assert
        Assert.Equal(expectedCanSatisfy, canSatisfy);
    }

    [Theory]
    [InlineData(new uint[] { Upgrades.GlialReconstitution })]
    [InlineData(new uint[] { Upgrades.GlialReconstitution, Upgrades.TunnelingClaws })]
    public void GivenOtherResearchQueuedBefore_WhenCreatingFulfillment_ThenStatusIsPreparing(uint[] upgradeTypesToQueueBefore) {
        // Arrange
        var producer = TestUtils.CreateUnit(Units.EvolutionChamber, _knowledgeBase, actionBuilder: _actionBuilder);
        foreach (var upgradeTypeToQueueBefore in upgradeTypesToQueueBefore) {
            producer.ResearchUpgrade(upgradeTypeToQueueBefore);
        }

        const uint upgradeTypeToResearch = Upgrades.Burrow;
        var producerOrder = producer.ResearchUpgrade(upgradeTypeToResearch);

        // Act
        var researchUpgradeFulfillment = _buildRequestFulfillmentFactory.CreateResearchUpgradeFulfillment(producer, producerOrder, upgradeTypeToResearch);

        // Assert
        Assert.Equal(BuildRequestFulfillmentStatus.Preparing, researchUpgradeFulfillment.Status);
    }

    [Theory]
    [InlineData(new uint[] {})]
    [InlineData(new uint[] { Upgrades.GlialReconstitution })]
    [InlineData(new uint[] { Upgrades.GlialReconstitution, Upgrades.TunnelingClaws })]
    public void GivenNoOtherResearchQueuedBefore_WhenCreatingFulfillment_ThenStatusIsExecuting(uint[] upgradeTypesToQueueAfter) {
        // Arrange
        var producer = TestUtils.CreateUnit(Units.EvolutionChamber, _knowledgeBase, actionBuilder: _actionBuilder);
        const uint upgradeTypeToResearch = Upgrades.Burrow;
        var producerOrder = producer.ResearchUpgrade(upgradeTypeToResearch);

        foreach (var upgradeTypeToQueueAfter in upgradeTypesToQueueAfter) {
            producer.ResearchUpgrade(upgradeTypeToQueueAfter);
        }

        // Act
        var researchUpgradeFulfillment = _buildRequestFulfillmentFactory.CreateResearchUpgradeFulfillment(producer, producerOrder, upgradeTypeToResearch);

        // Assert
        Assert.Equal(BuildRequestFulfillmentStatus.Executing, researchUpgradeFulfillment.Status);
    }

    [Fact]
    public void GivenOtherResearchQueuedBeforeAndAfter_WhenCreatingFulfillment_ThenExpectedCompletionFrameSumsResearchTimeOfEverythingBefore() {
        // Arrange
        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(100);
        var producer = TestUtils.CreateUnit(Units.EvolutionChamber, _knowledgeBase, actionBuilder: _actionBuilder);

        var upgradeTypesToQueueBefore = new List<uint>
        {
            Upgrades.GlialReconstitution,
            Upgrades.TunnelingClaws,
        };

        foreach (var upgradeTypeToQueueBefore in upgradeTypesToQueueBefore) {
            producer.ResearchUpgrade(upgradeTypeToQueueBefore);
        }

        const uint upgradeTypeToResearch = Upgrades.Burrow;
        var producerOrder = producer.ResearchUpgrade(upgradeTypeToResearch);

        producer.ResearchUpgrade(Upgrades.ZergMeleeWeaponsLevel1);
        producer.ResearchUpgrade(Upgrades.ZergGroundArmorsLevel1);

        // Act
        var researchUpgradeFulfillment = _buildRequestFulfillmentFactory.CreateResearchUpgradeFulfillment(producer, producerOrder, upgradeTypeToResearch);

        // Assert
        var expectedCompletionFrame = _frameClockMock.Object.CurrentFrame
                                      + upgradeTypesToQueueBefore.Sum(upgradeType => _knowledgeBase.GetUpgradeData(upgradeType).ResearchTime)
                                      + _knowledgeBase.GetUpgradeData(upgradeTypeToResearch).ResearchTime;

        Assert.Equal(expectedCompletionFrame, researchUpgradeFulfillment.ExpectedCompletionFrame);
    }

    [Fact]
    public void GivenOtherResearchQueuedBefore_WhenCreatingFulfillment_ThenExpectedCompletionFrameConsidersFirstResearchProgress() {
        // Arrange
        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(100);
        var producer = TestUtils.CreateUnit(Units.EvolutionChamber, _knowledgeBase, actionBuilder: _actionBuilder);

        var firstOrder = producer.ResearchUpgrade(Upgrades.GlialReconstitution);
        firstOrder.Progress = 0.5f;

        const uint upgradeTypeToResearch = Upgrades.Burrow;
        var producerOrder = producer.ResearchUpgrade(upgradeTypeToResearch);

        // Act
        var researchUpgradeFulfillment = _buildRequestFulfillmentFactory.CreateResearchUpgradeFulfillment(producer, producerOrder, upgradeTypeToResearch);

        // Assert
        var expectedCompletionFrame = _frameClockMock.Object.CurrentFrame
                                      + firstOrder.Progress * _knowledgeBase.GetUpgradeDataFromAbilityId(firstOrder.AbilityId).ResearchTime
                                      + _knowledgeBase.GetUpgradeDataFromAbilityId(producerOrder.AbilityId).ResearchTime;

        Assert.Equal(expectedCompletionFrame, researchUpgradeFulfillment.ExpectedCompletionFrame);
    }

    [Fact]
    public void GivenOtherResearchQueuedBefore_WhenUpdateStatus_ThenExpectedCompletionFrameUpdates() {
        // Arrange
        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(100);
        var producer = TestUtils.CreateUnit(Units.EvolutionChamber, _knowledgeBase, actionBuilder: _actionBuilder);

        producer.ResearchUpgrade(Upgrades.GlialReconstitution);
        producer.ResearchUpgrade(Upgrades.TunnelingClaws);

        const uint upgradeTypeToResearch = Upgrades.Burrow;
        var producerOrder = producer.ResearchUpgrade(upgradeTypeToResearch);

        var researchUpgradeFulfillment = _buildRequestFulfillmentFactory.CreateResearchUpgradeFulfillment(producer, producerOrder, upgradeTypeToResearch);

        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(200);
        producer.LastSeen = _frameClockMock.Object.CurrentFrame;
        producer.Orders.Clear();

        var newFirstOrder = producer.ResearchUpgrade(Upgrades.TunnelingClaws);
        newFirstOrder.Progress = 0.25f;

        producer.ResearchUpgrade(upgradeTypeToResearch);

        // Act
        researchUpgradeFulfillment.UpdateStatus();

        // Assert
        var expectedCompletionFrame = _frameClockMock.Object.CurrentFrame
                                      + (1 - newFirstOrder.Progress) * _knowledgeBase.GetUpgradeDataFromAbilityId(newFirstOrder.AbilityId).ResearchTime
                                      + _knowledgeBase.GetUpgradeDataFromAbilityId(producerOrder.AbilityId).ResearchTime;

        Assert.Equal(expectedCompletionFrame, researchUpgradeFulfillment.ExpectedCompletionFrame);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void GivenUpgradeIsResearched_WhenUpdateStatus_ThenStatusIsCompletedRegardlessOfOtherFactors(bool producerIsDead, bool orderHasDisappeared) {
        // Arrange
        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(100);
        var producer = TestUtils.CreateUnit(Units.EvolutionChamber, _knowledgeBase, actionBuilder: _actionBuilder);

        const uint upgradeTypeToResearch = Upgrades.Burrow;
        var producerOrder = producer.ResearchUpgrade(upgradeTypeToResearch);

        var researchUpgradeFulfillment = _buildRequestFulfillmentFactory.CreateResearchUpgradeFulfillment(producer, producerOrder, upgradeTypeToResearch);

        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(200);
        _controllerMock.Setup(controller => controller.ResearchedUpgrades).Returns(new HashSet<uint> { upgradeTypeToResearch });

        if (!producerIsDead) {
            producer.LastSeen = _frameClockMock.Object.CurrentFrame;
        }

        if (orderHasDisappeared) {
            producer.Orders.Clear();
        }

        // Act
        researchUpgradeFulfillment.UpdateStatus();

        // Assert
        Assert.Equal(BuildRequestFulfillmentStatus.Completed, researchUpgradeFulfillment.Status);
    }

    [Fact]
    public void GivenProducerDied_WhenUpdateStatus_ThenStatusIsPrevented() {
        // Arrange
        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(100);
        var producer = TestUtils.CreateUnit(Units.EvolutionChamber, _knowledgeBase, actionBuilder: _actionBuilder);

        const uint upgradeTypeToResearch = Upgrades.Burrow;
        var producerOrder = producer.ResearchUpgrade(upgradeTypeToResearch);

        var researchUpgradeFulfillment = _buildRequestFulfillmentFactory.CreateResearchUpgradeFulfillment(producer, producerOrder, upgradeTypeToResearch);

        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(200);

        // Act
        researchUpgradeFulfillment.UpdateStatus();

        // Assert
        Assert.Equal(BuildRequestFulfillmentStatus.Prevented, researchUpgradeFulfillment.Status);
    }

    [Fact]
    public void GivenOrderDisappeared_WhenUpdateStatus_ThenStatusIsAborted() {
        // Arrange
        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(100);
        var producer = TestUtils.CreateUnit(Units.EvolutionChamber, _knowledgeBase, actionBuilder: _actionBuilder);

        const uint upgradeTypeToResearch = Upgrades.Burrow;
        var producerOrder = producer.ResearchUpgrade(upgradeTypeToResearch);

        var researchUpgradeFulfillment = _buildRequestFulfillmentFactory.CreateResearchUpgradeFulfillment(producer, producerOrder, upgradeTypeToResearch);

        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(200);
        producer.LastSeen = _frameClockMock.Object.CurrentFrame;
        producer.Orders.Clear();

        producer.ResearchUpgrade(Upgrades.TunnelingClaws);

        // Act
        researchUpgradeFulfillment.UpdateStatus();

        // Assert
        Assert.Equal(BuildRequestFulfillmentStatus.Aborted, researchUpgradeFulfillment.Status);
    }

    [Fact]
    public void GivenStatusIsTerminated_WhenUpdateStatus_ThenStatusDoesNotChange() {
        // Arrange
        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(100);
        var producer = TestUtils.CreateUnit(Units.EvolutionChamber, _knowledgeBase, actionBuilder: _actionBuilder);

        const uint upgradeTypeToResearch = Upgrades.Burrow;
        var producerOrder = producer.ResearchUpgrade(upgradeTypeToResearch);

        var researchUpgradeFulfillment = _buildRequestFulfillmentFactory.CreateResearchUpgradeFulfillment(producer, producerOrder, upgradeTypeToResearch);

        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(200);
        producer.LastSeen = _frameClockMock.Object.CurrentFrame;
        producer.Orders.Clear();

        researchUpgradeFulfillment.UpdateStatus(); // Aborted

        // Act
        _controllerMock.Setup(controller => controller.ResearchedUpgrades).Returns(new HashSet<uint> { upgradeTypeToResearch });
        researchUpgradeFulfillment.UpdateStatus();

        // Assert
        Assert.Equal(BuildRequestFulfillmentStatus.Aborted, researchUpgradeFulfillment.Status);
    }
}
