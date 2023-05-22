using Bot.Debugging.GraphicalDebugging;
using Bot.GameData;
using Bot.GameSense;
using Bot.Tests.Mocks;
using Bot.Wrapper;
using Moq;

namespace Bot.Tests.UnitModules.MiningModule;

public class MiningModuleTests : BaseTestClass {
    private readonly Mock<IFrameClock> _frameClockMock;
    private readonly IActionBuilder _actionBuilder;
    private readonly Mock<IActionService> _actionServiceMock;
    private readonly Mock<ITerrainTracker> _terrainTrackerMock;
    private readonly Mock<IRegionsTracker> _regionsTrackerMock;
    private readonly IUnitsTracker _unitsTracker;
    private readonly Mock<IGraphicalDebugger> _graphicalDebuggerMock;

    public MiningModuleTests() {
        _frameClockMock = new Mock<IFrameClock>();
        _actionBuilder = new ActionBuilder(KnowledgeBase);
        _actionServiceMock = new Mock<IActionService>();
        _terrainTrackerMock = new Mock<ITerrainTracker>();
        _regionsTrackerMock = new Mock<IRegionsTracker>();
        _unitsTracker = new TestUnitsTracker();
        _graphicalDebuggerMock = new Mock<IGraphicalDebugger>();
    }

    [Fact]
    public void GivenNullWorker_WhenInstalling_DoesNotInstall() {
        // Act
        var miningModule = Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, null, null);

        // Assert
        Assert.Null(miningModule);
    }

    [Fact]
    public void GivenNullResource_WhenInstalling_DoesNotAssignResource() {
        // Arrange
        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);

        // Act
        var miningModule = Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, null);

        // Assert
        Assert.NotNull(miningModule);
        Assert.Null(miningModule.AssignedResource);
        Assert.Equal(Resources.ResourceType.None, miningModule.ResourceType);
    }

    [Fact]
    public void GivenNullResource_WhenInstalling_DisablesModule() {
        // Arrange
        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);
        var miningModule = Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, null);

        // Act
        var executed = miningModule.Execute();

        // Assert
        Assert.False(executed);
    }

    public static IEnumerable<object[]> MineralsTestData() {
        return Units.MineralFields.Select(mineralField => new object[] { mineralField });
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    public void GivenMineralFieldWithCapacityModule_WhenInstalling_AssignsMineralFieldAndUpdatesCapacityModule(uint mineralFieldType) {
        // Arrange
        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);

        var resource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, mineralFieldType);
        var capacityModule = Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, resource, 1);

        // Act
        var miningModule = Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, resource);

        // Assert
        Assert.NotNull(miningModule);
        Assert.Equal(resource, miningModule.AssignedResource);
        Assert.Equal(Resources.ResourceType.Mineral, miningModule.ResourceType);
        Assert.Single(capacityModule.AssignedUnits);
    }

    public static IEnumerable<object[]> GasGeysersTestData() {
        return Units.GasGeysers.Select(gasGeyser => new object[] { gasGeyser });
    }

    [Theory]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenGasGeyserWithCapacityModule_WhenInstalling_AssignsGasGeyserAndUpdatesCapacityModule(uint gasGeyserType) {
        // Arrange
        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);

        var resource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, gasGeyserType);
        var capacityModule = Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, resource, 1);

        // Act
        var miningModule = Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, resource);

        // Assert
        Assert.NotNull(miningModule);
        Assert.Equal(resource, miningModule.AssignedResource);
        Assert.Equal(Resources.ResourceType.Gas, miningModule.ResourceType);
        Assert.Single(capacityModule.AssignedUnits);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithCapacityModule_WhenInstalling_EnablesModule(uint resourceType) {
        // Arrange
        var resource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, resourceType);
        Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, resource, 1);

        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);
        Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, resource);

        var miningModule = Bot.UnitModules.UnitModule.Get<Bot.UnitModules.MiningModule>(worker);

        // Act
        var executed = miningModule.Execute();

        // Assert
        Assert.True(executed);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithoutCapacityModule_WhenInstalling_Throws(uint resourceType) {
        // Arrange
        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);
        var resource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, resourceType);

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, resource));
    }

    [Fact]
    public void GivenNotAResourceResource_WhenInstalling_Throws() {
        // Arrange
        var notAResource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Zergling);
        Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, notAResource, 1);

        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, notAResource));
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithCapacityModule_WhenAssigningResourceWithoutAllowingReleasingPreviousOne_DoesNothing(uint resourceType) {
        // Arrange
        var initialResource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, resourceType);
        var initialCapacityModule = Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, initialResource, 1);

        var newResource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, resourceType);
        var newCapacityModule = Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, newResource, 1);

        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);
        var miningModule = Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, initialResource);

        // Act
        miningModule.AssignResource(newResource, releasePreviouslyAssignedResource: false);

        // Assert
        Assert.Equal(initialResource, miningModule.AssignedResource);
        Assert.Single(initialCapacityModule.AssignedUnits);
        Assert.Empty(newCapacityModule.AssignedUnits);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithCapacityModule_WhenAssigningResourceAndAllowingReleasingPreviousOne_ReleasesThePreviousResourceAndUpdatesCapacityModule(uint resourceType) {
        // Arrange
        var initialResource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, resourceType);
        var initialCapacityModule = Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, initialResource, 1);

        var newResource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, resourceType);
        var newCapacityModule = Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, newResource, 1);

        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);
        var miningModule = Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, initialResource);

        // Act
        miningModule.AssignResource(newResource, releasePreviouslyAssignedResource: true);

        // Assert
        Assert.Equal(newResource, miningModule.AssignedResource);
        Assert.Empty(initialCapacityModule.AssignedUnits);
        Assert.Single(newCapacityModule.AssignedUnits);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithCapacityModule_WhenAssigningNullResourceAndAllowingReleasingPreviousOne_ReleasesThePreviousResourceAndUpdatesCapacityModule(uint resourceType) {
        // Arrange
        var initialResource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, resourceType);
        var initialCapacityModule = Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, initialResource, 1);

        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);
        var miningModule = Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, initialResource);

        // Act
        miningModule.AssignResource(null, releasePreviouslyAssignedResource: true);

        // Assert
        Assert.Null(miningModule.AssignedResource);
        Assert.Equal(Resources.ResourceType.None, miningModule.ResourceType);
        Assert.Empty(initialCapacityModule.AssignedUnits);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithCapacityModule_WhenAssigningNullResourceWithoutAllowingReleasingPreviousOne_DoesNothing(uint resourceType) {
        // Arrange
        var initialResource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, resourceType);
        var initialCapacityModule = Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, initialResource, 1);

        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);
        var miningModule = Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, initialResource);

        // Act
        miningModule.AssignResource(null, releasePreviouslyAssignedResource: false);

        // Assert
        Assert.Equal(initialResource, miningModule.AssignedResource);
        Assert.Equal(Resources.GetResourceType(initialResource), miningModule.ResourceType);
        Assert.Single(initialCapacityModule.AssignedUnits);
    }

    [Fact]
    public void GivenNullResource_WhenAssigningNotAResourceWithCapacityModule_Throws() {
        // Arrange
        var initialResource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.MineralField);
        Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, initialResource, 1);

        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);
        var miningModule = Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, initialResource);

        var notAResource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Zergling);
        Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, notAResource, 1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => miningModule.AssignResource(notAResource, releasePreviouslyAssignedResource: true));
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithCapacityModule_WhenReleasingResourceWithoutAllowingToUpdateCapacityModule_UnsetsAssignedResourceButDoesntUpdateCapacityModule(uint resourceType) {
        // Arrange
        var initialResource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, resourceType);
        var initialCapacityModule = Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, initialResource, 1);

        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);
        var miningModule = Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, initialResource);

        // Act
        miningModule.ReleaseResource(updateCapacityModule: false);

        // Assert
        Assert.Null(miningModule.AssignedResource);
        Assert.Equal(Resources.ResourceType.None, miningModule.ResourceType);
        Assert.Single(initialCapacityModule.AssignedUnits);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithCapacityModule_WhenReleasingResourceAndAllowingToUpdateCapacityModule_UnsetsAssignedResourceAndUpdatesCapacityModule(uint resourceType) {
        // Arrange
        var initialResource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, resourceType);
        var initialCapacityModule = Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, initialResource, 1);

        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);
        var miningModule = Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, initialResource);

        // Act
        miningModule.ReleaseResource(updateCapacityModule: true);

        // Assert
        Assert.Null(miningModule.AssignedResource);
        Assert.Equal(Resources.ResourceType.None, miningModule.ResourceType);
        Assert.Empty(initialCapacityModule.AssignedUnits);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithCapacityModule_WhenReleasingResource_DisablesModule(uint resourceType) {
        // Arrange
        var resource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, resourceType);
        Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, resource, 1);

        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);
        var miningModule = Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, resource);

        // Act
        miningModule.ReleaseResource(updateCapacityModule: false);
        var executed = miningModule.Execute();

        // Assert
        Assert.False(executed);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithCapacityModule_WhenUninstalling_ReleasesWorkerFromResourceCapacityModule(uint resourceType) {
        // Arrange
        var resource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, resourceType);
        var capacityModule =  Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, resource, 1);

        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);
        Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, resource);

        // Act
        Bot.UnitModules.UnitModule.Uninstall<Bot.UnitModules.MiningModule>(worker);

        // Assert
        Assert.Empty(capacityModule.AssignedUnits);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithoutCapacityModule_WhenUninstalling_DoesNothing(uint resourceType) {
        // Arrange
        var resource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, resourceType);
        Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, resource, 1);

        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);
        Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, resource);

        Bot.UnitModules.UnitModule.Uninstall<Bot.UnitModules.CapacityModule>(resource);

        // Act
        var exception = Record.Exception(() => Bot.UnitModules.UnitModule.Uninstall<Bot.UnitModules.MiningModule>(worker));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void GivenNullResource_WhenUninstalling_DoesNothing() {
        // Arrange
        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);
        Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, null);

        // Act
        var exception = Record.Exception(() => Bot.UnitModules.UnitModule.Uninstall<Bot.UnitModules.MiningModule>(worker));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void GivenResourceWithCapacityModule_WhenToString_ThenReturnsStringWithWorkerInfo() {
        // Arrange
        var resource = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.MineralField);
        Bot.UnitModules.CapacityModule.Install(_graphicalDebuggerMock.Object, resource, 1);

        var worker = TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, Units.Drone);
        var miningModule = Bot.UnitModules.MiningModule.Install(_graphicalDebuggerMock.Object, worker, resource);

        // Act
        var stringRepresentation = miningModule.ToString();

        // Assert
        Assert.Equal($"{worker}_{Bot.UnitModules.MiningModule.Tag}", stringRepresentation);
    }
}
