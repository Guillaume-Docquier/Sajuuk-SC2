using System.Numerics;
using Sajuuk.Actions;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.Tests.Mocks;
using Moq;
using SC2APIProtocol;

namespace Sajuuk.Tests.UnitModules.MiningModule;

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
    public void GivenNullResource_WhenNewMiningModule_DoesNotAssignResource() {
        // Arrange
        var worker = CreateUnit(Units.Drone);

        // Act
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, null);

        // Assert
        Assert.NotNull(miningModule);
        Assert.Null(miningModule.AssignedResource);
        Assert.Equal(Resources.ResourceType.None, miningModule.ResourceType);
    }

    [Fact]
    public void GivenNullResource_WhenNewMiningModule_DisablesModule() {
        // Arrange
        var worker = CreateUnit(Units.Drone);

        // Act
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, null);
        var executed = miningModule.Execute();

        // Assert
        Assert.False(executed);
    }

    public static IEnumerable<object[]> MineralsTestData() {
        return Units.MineralFields.Select(mineralField => new object[] { mineralField });
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    public void GivenMineralFieldWithCapacityModule_WhenNewMiningModule_AssignsMineralFieldAndUpdatesCapacityModule(uint mineralFieldType) {
        // Arrange
        var worker = CreateUnit(Units.Drone);

        var resource = CreateUnit(mineralFieldType);
        var capacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, resource, 1);
        resource.Modules.Add(capacityModule.Tag, capacityModule);

        // Act
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, resource);

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
    public void GivenGasGeyserWithCapacityModule_WhenNewMiningModule_AssignsGasGeyserAndUpdatesCapacityModule(uint gasGeyserType) {
        // Arrange
        var worker = CreateUnit(Units.Drone);

        var resource = CreateUnit(gasGeyserType);
        var capacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, resource, 1);
        resource.Modules.Add(capacityModule.Tag, capacityModule);

        // Act
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, resource);

        // Assert
        Assert.NotNull(miningModule);
        Assert.Equal(resource, miningModule.AssignedResource);
        Assert.Equal(Resources.ResourceType.Gas, miningModule.ResourceType);
        Assert.Single(capacityModule.AssignedUnits);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithCapacityModule_WhenNewMiningModule_EnablesModule(uint resourceType) {
        // Arrange
        var resource = CreateUnit(resourceType);
        var capacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, resource, 1);
        resource.Modules.Add(capacityModule.Tag, capacityModule);

        var worker = CreateUnit(Units.Drone);
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, resource);

        // Act
        var executed = miningModule.Execute();

        // Assert
        Assert.True(executed);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithoutCapacityModule_WhenNewMiningModule_Throws(uint resourceType) {
        // Arrange
        var worker = CreateUnit(Units.Drone);
        var resource = CreateUnit(resourceType);

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, resource));
    }

    [Fact]
    public void GivenNotAResourceResource_WhenNewMiningModule_Throws() {
        // Arrange
        var notAResource = CreateUnit(Units.Zergling);
        var capacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, notAResource, 1);
        notAResource.Modules.Add(capacityModule.Tag, capacityModule);

        var worker = CreateUnit(Units.Drone);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, notAResource));
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithCapacityModule_WhenAssigningResourceWithoutAllowingReleasingPreviousOne_DoesNothing(uint resourceType) {
        // Arrange
        var initialResource = CreateUnit(resourceType);
        var initialCapacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, initialResource, 1);
        initialResource.Modules.Add(initialCapacityModule.Tag, initialCapacityModule);

        var newResource = CreateUnit(resourceType);
        var newCapacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, newResource, 1);
        newResource.Modules.Add(newCapacityModule.Tag, newCapacityModule);

        var worker = CreateUnit(Units.Drone);
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, initialResource);

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
        var initialResource = CreateUnit(resourceType);
        var initialCapacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, initialResource, 1);
        initialResource.Modules.Add(initialCapacityModule.Tag, initialCapacityModule);

        var newResource = CreateUnit(resourceType);
        var newCapacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, newResource, 1);
        newResource.Modules.Add(newCapacityModule.Tag, newCapacityModule);

        var worker = CreateUnit(Units.Drone);
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, initialResource);

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
        var resource = CreateUnit(resourceType);
        var capacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, resource, 1);
        resource.Modules.Add(capacityModule.Tag, capacityModule);

        var worker = CreateUnit(Units.Drone);
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, resource);

        // Act
        miningModule.AssignResource(null, releasePreviouslyAssignedResource: true);

        // Assert
        Assert.Null(miningModule.AssignedResource);
        Assert.Equal(Resources.ResourceType.None, miningModule.ResourceType);
        Assert.Empty(capacityModule.AssignedUnits);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithCapacityModule_WhenAssigningNullResourceWithoutAllowingReleasingPreviousOne_DoesNothing(uint resourceType) {
        // Arrange
        var resource = CreateUnit(resourceType);
        var capacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, resource, 1);
        resource.Modules.Add(capacityModule.Tag, capacityModule);

        var worker = CreateUnit(Units.Drone);
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, resource);

        // Act
        miningModule.AssignResource(null, releasePreviouslyAssignedResource: false);

        // Assert
        Assert.Equal(resource, miningModule.AssignedResource);
        Assert.Equal(Resources.GetResourceType(resource), miningModule.ResourceType);
        Assert.Single(capacityModule.AssignedUnits);
    }

    [Fact]
    public void GivenNullResource_WhenAssigningNotAResourceWithCapacityModule_Throws() {
        // Arrange
        var initialResource = CreateUnit(Units.MineralField);
        var initialCapacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, initialResource, 1);
        initialResource.Modules.Add(initialCapacityModule.Tag, initialCapacityModule);

        var worker = CreateUnit(Units.Drone);
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, initialResource);

        var notAResource = CreateUnit(Units.Zergling);
        var capacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, notAResource, 1);
        notAResource.Modules.Add(capacityModule.Tag, capacityModule);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => miningModule.AssignResource(notAResource, releasePreviouslyAssignedResource: true));
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithCapacityModule_WhenReleasingResourceWithoutAllowingToUpdateCapacityModule_UnsetsAssignedResourceButDoesntUpdateCapacityModule(uint resourceType) {
        // Arrange
        var resource = CreateUnit(resourceType);
        var capacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, resource, 1);
        resource.Modules.Add(capacityModule.Tag, capacityModule);

        var worker = CreateUnit(Units.Drone);
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, resource);

        // Act
        miningModule.ReleaseResource(updateCapacityModule: false);

        // Assert
        Assert.Null(miningModule.AssignedResource);
        Assert.Equal(Resources.ResourceType.None, miningModule.ResourceType);
        Assert.Single(capacityModule.AssignedUnits);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithCapacityModule_WhenReleasingResourceAndAllowingToUpdateCapacityModule_UnsetsAssignedResourceAndUpdatesCapacityModule(uint resourceType) {
        // Arrange
        var resource = CreateUnit(resourceType);
        var capacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, resource, 1);
        resource.Modules.Add(capacityModule.Tag, capacityModule);

        var worker = CreateUnit(Units.Drone);
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, resource);

        // Act
        miningModule.ReleaseResource(updateCapacityModule: true);

        // Assert
        Assert.Null(miningModule.AssignedResource);
        Assert.Equal(Resources.ResourceType.None, miningModule.ResourceType);
        Assert.Empty(capacityModule.AssignedUnits);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithCapacityModule_WhenReleasingResource_DisablesModule(uint resourceType) {
        // Arrange
        var resource = CreateUnit(resourceType);
        var capacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, resource, 1);
        resource.Modules.Add(capacityModule.Tag, capacityModule);

        var worker = CreateUnit(Units.Drone);
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, resource);

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
        var resource = CreateUnit(resourceType);
        var capacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, resource, 1);
        resource.Modules.Add(capacityModule.Tag, capacityModule);

        var worker = CreateUnit(Units.Drone);
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, resource);
        worker.Modules.Add(miningModule.Tag, miningModule);

        // Act
        Sajuuk.UnitModules.UnitModule.Uninstall<Sajuuk.UnitModules.MiningModule>(worker);

        // Assert
        Assert.Empty(capacityModule.AssignedUnits);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithoutCapacityModule_WhenUninstalling_DoesNothing(uint resourceType) {
        // Arrange
        var resource = CreateUnit(resourceType);
        var capacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, resource, 1);
        resource.Modules.Add(capacityModule.Tag, capacityModule);

        var worker = CreateUnit(Units.Drone);
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, resource);
        worker.Modules.Add(miningModule.Tag, miningModule);

        Sajuuk.UnitModules.UnitModule.Uninstall<Sajuuk.UnitModules.CapacityModule>(resource);

        // Act
        var exception = Record.Exception(() => Sajuuk.UnitModules.UnitModule.Uninstall<Sajuuk.UnitModules.MiningModule>(worker));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void GivenNullResource_WhenUninstalling_DoesNothing() {
        // Arrange
        var worker = CreateUnit(Units.Drone);
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, null);
        worker.Modules.Add(miningModule.Tag, miningModule);

        // Act
        var exception = Record.Exception(() => Sajuuk.UnitModules.UnitModule.Uninstall<Sajuuk.UnitModules.MiningModule>(worker));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void GivenResourceWithCapacityModule_WhenToString_ThenReturnsStringWithWorkerInfo() {
        // Arrange
        var resource = CreateUnit(Units.MineralField);
        var capacityModule = new Sajuuk.UnitModules.CapacityModule(_graphicalDebuggerMock.Object, resource, 1);
        resource.Modules.Add(capacityModule.Tag, capacityModule);

        var worker = CreateUnit(Units.Drone);
        var miningModule = new Sajuuk.UnitModules.MiningModule(_graphicalDebuggerMock.Object, worker, resource);

        // Act
        var stringRepresentation = miningModule.ToString();

        // Assert
        Assert.Equal($"{worker}_{miningModule.Tag}", stringRepresentation);
    }

    private Unit CreateUnit(
        uint unitType,
        uint frame = 0,
        Alliance alliance = Alliance.Self,
        Vector3 position = default,
        int vespeneContents = 0,
        float buildProgress = 1f
    ) {
        return TestUtils.CreateUnit(
            unitType,
            KnowledgeBase, _frameClockMock.Object, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker,
            frame, alliance, position, vespeneContents, buildProgress
        );
    }
}
