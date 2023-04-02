using Bot.GameData;

namespace Bot.Tests.UnitModules.MiningModule;

public class MiningModuleTests: BaseTestClass {
    [Fact]
    public void GivenNullResource_WhenInstalling_DoesNotAssignResource() {
        // Arrange
        var worker = TestUtils.CreateUnit(Units.Drone);

        // Act
        Bot.UnitModules.MiningModule.Install(worker, null);
        var miningModule = Bot.UnitModules.UnitModule.Get<Bot.UnitModules.MiningModule>(worker);

        // Assert
        Assert.NotNull(miningModule);
        Assert.Null(miningModule.AssignedResource);
        Assert.Equal(Resources.ResourceType.None, miningModule.ResourceType);
    }

    public static IEnumerable<object[]> MineralsTestData() {
        return Units.MineralFields.Select(mineralField => new object[] { mineralField });
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    public void GivenMineralFieldWithCapacityModule_WhenInstalling_AssignsMineralFieldAndUpdatesCapacityModule(uint mineralFieldType) {
        // Arrange
        var worker = TestUtils.CreateUnit(Units.Drone);

        var resource = TestUtils.CreateUnit(mineralFieldType);
        Bot.UnitModules.CapacityModule.Install(resource, 3);

        // Act
        Bot.UnitModules.MiningModule.Install(worker, resource);
        var module = Bot.UnitModules.UnitModule.Get<Bot.UnitModules.MiningModule>(worker);

        // Assert
        Assert.NotNull(module);
        Assert.Equal(resource, module.AssignedResource);
        Assert.Equal(Resources.ResourceType.Mineral, module.ResourceType);
        Assert.Single(Bot.UnitModules.UnitModule.Get<Bot.UnitModules.CapacityModule>(resource).AssignedUnits);
    }

    public static IEnumerable<object[]> GasGeysersTestData() {
        return Units.GasGeysers.Select(gasGeyser => new object[] { gasGeyser });
    }

    [Theory]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenGasGeyser_WhenInstalling_AssignsGasGeyser(uint gasGeyserType) {
        // Arrange
        var worker = TestUtils.CreateUnit(Units.Drone);

        var resource = TestUtils.CreateUnit(gasGeyserType);
        Bot.UnitModules.CapacityModule.Install(resource, 3);

        // Act
        Bot.UnitModules.MiningModule.Install(worker, resource);
        var module = Bot.UnitModules.UnitModule.Get<Bot.UnitModules.MiningModule>(worker);

        // Assert
        Assert.NotNull(module);
        Assert.Equal(resource, module.AssignedResource);
        Assert.Equal(Resources.ResourceType.Gas, module.ResourceType);
        Assert.Single(Bot.UnitModules.UnitModule.Get<Bot.UnitModules.CapacityModule>(resource).AssignedUnits);
    }

    [Fact]
    public void GivenNullResource_WhenInstalling_DisablesModule() {
        // Arrange
        var worker = TestUtils.CreateUnit(Units.Drone);
        Bot.UnitModules.MiningModule.Install(worker, null);

        var miningModule = Bot.UnitModules.UnitModule.Get<Bot.UnitModules.MiningModule>(worker);

        // Act
        var executed = miningModule.Execute();

        // Assert
        Assert.False(executed);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResource_WhenInstalling_EnablesModule(uint resourceType) {
        // Arrange
        var resource = TestUtils.CreateUnit(resourceType);
        Bot.UnitModules.CapacityModule.Install(resource, 1);

        var worker = TestUtils.CreateUnit(Units.Drone);
        Bot.UnitModules.MiningModule.Install(worker, resource);

        var miningModule = Bot.UnitModules.UnitModule.Get<Bot.UnitModules.MiningModule>(worker);

        // Act
        var executed = miningModule.Execute();

        // Assert
        Assert.True(executed);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResource_WhenAssigningResourceWithoutAllowingReleasingPreviousOne_DoesNothing(uint resourceType) {
        // Arrange
        var initialResource = TestUtils.CreateUnit(resourceType);
        var initialCapacityModule = Bot.UnitModules.CapacityModule.Install(initialResource, 1);

        var newResource = TestUtils.CreateUnit(resourceType);
        var newCapacityModule = Bot.UnitModules.CapacityModule.Install(newResource, 1);

        var worker = TestUtils.CreateUnit(Units.Drone);
        var miningModule = Bot.UnitModules.MiningModule.Install(worker, initialResource);

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
    public void GivenResource_WhenReleasingResourceWithoutAllowingToUpdateTheCapacityModule_UnsetsAssignedResourceButDoesntUpdateCapacityModule(uint resourceType) {
        // Arrange
        var initialResource = TestUtils.CreateUnit(resourceType);
        var initialCapacityModule = Bot.UnitModules.CapacityModule.Install(initialResource, 1);

        var worker = TestUtils.CreateUnit(Units.Drone);
        var miningModule = Bot.UnitModules.MiningModule.Install(worker, initialResource);

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
    public void GivenResource_WhenReleasingResource_DisablesModule(uint resourceType) {
        // Arrange
        var resource = TestUtils.CreateUnit(resourceType);
        var capacityModule = Bot.UnitModules.CapacityModule.Install(resource, 1);

        var worker = TestUtils.CreateUnit(Units.Drone);
        var miningModule = Bot.UnitModules.MiningModule.Install(worker, resource);

        // Act
        miningModule.ReleaseResource(updateCapacityModule: false);
        var executed = miningModule.Execute();

        // Assert
        Assert.False(executed);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResource_WhenUninstalling_ReleasesWorkerFromResourceCapacityModule(uint resourceType) {
        // Arrange
        var resource = TestUtils.CreateUnit(resourceType);
        var capacityModule =  Bot.UnitModules.CapacityModule.Install(resource, 1);

        var worker = TestUtils.CreateUnit(Units.Drone);
        Bot.UnitModules.MiningModule.Install(worker, resource);

        // Act
        Bot.UnitModules.UnitModule.Uninstall<Bot.UnitModules.MiningModule>(worker);

        // Assert
        Assert.Empty(capacityModule.AssignedUnits);
    }

    [Fact]
    public void GivenNullResource_WhenUninstalling_DoesNothing() {
        // Arrange
        var worker = TestUtils.CreateUnit(Units.Drone);
        Bot.UnitModules.MiningModule.Install(worker, null);

        // Act
        var exception = Record.Exception(() => Bot.UnitModules.UnitModule.Uninstall<Bot.UnitModules.MiningModule>(worker));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResourceWithoutCapacityModule_WhenInstalling_Throws(uint resourceType) {
        // Arrange
        var worker = TestUtils.CreateUnit(Units.Drone);
        var resource = TestUtils.CreateUnit(resourceType);

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => Bot.UnitModules.MiningModule.Install(worker, resource));
    }

    [Fact]
    public void GivenResourceWithCapacityModule_WhenToString_ThenReturnsStringWithWorkerInfo() {
        // Arrange
        var resource = TestUtils.CreateUnit(Units.MineralField);
        Bot.UnitModules.CapacityModule.Install(resource, 1);

        var worker = TestUtils.CreateUnit(Units.Drone);
        var miningModule = Bot.UnitModules.MiningModule.Install(worker, resource);

        // Act
        var stringRepresentation = miningModule.ToString();

        // Assert
        Assert.Equal($"{worker}_{Bot.UnitModules.MiningModule.Tag}", stringRepresentation);
    }
}
