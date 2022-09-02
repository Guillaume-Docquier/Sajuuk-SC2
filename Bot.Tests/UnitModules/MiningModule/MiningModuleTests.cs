using Bot.GameData;

namespace Bot.Tests.UnitModules.MiningModule;

public class MiningModuleTests: IClassFixture<KnowledgeBaseFixture>, IClassFixture<GraphicalDebuggerFixture> {
    [Fact]
    public void GivenNullResource_WhenInstalling_DoesNotAssignResource() {
        // Arrange
        var unit = TestUtils.CreateUnit(Units.Drone);

        // Act
        Bot.UnitModules.MiningModule.Install(unit, null);
        var module = Bot.UnitModules.UnitModule.Get<Bot.UnitModules.MiningModule>(unit);

        // Assert
        Assert.NotNull(module);
        Assert.Null(module.AssignedResource);
        Assert.Equal(Resources.ResourceType.None, module.ResourceType);
    }

    public static IEnumerable<object[]> MineralsTestData() {
        return Units.MineralFields.Select(mineralField => new object[] { mineralField });
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    public void GivenMineralField_WhenInstalling_AssignsMineralField(uint mineralFieldType) {
        // Arrange
        var unit = TestUtils.CreateUnit(Units.Drone);
        var resource = TestUtils.CreateUnit(mineralFieldType);

        // Act
        Bot.UnitModules.MiningModule.Install(unit, resource);
        var module = Bot.UnitModules.UnitModule.Get<Bot.UnitModules.MiningModule>(unit);

        // Assert
        Assert.NotNull(module);
        Assert.Equal(resource, module.AssignedResource);
        Assert.Equal(Resources.ResourceType.Mineral, module.ResourceType);
    }

    public static IEnumerable<object[]> GasGeysersTestData() {
        return Units.GasGeysers.Select(gasGeyser => new object[] { gasGeyser });
    }

    [Theory]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenGasGeyser_WhenInstalling_AssignsGasGeyser(uint gasGeyserType) {
        // Arrange
        var unit = TestUtils.CreateUnit(Units.Drone);
        var resource = TestUtils.CreateUnit(gasGeyserType);

        // Act
        Bot.UnitModules.MiningModule.Install(unit, resource);
        var module = Bot.UnitModules.UnitModule.Get<Bot.UnitModules.MiningModule>(unit);

        // Assert
        Assert.NotNull(module);
        Assert.Equal(resource, module.AssignedResource);
        Assert.Equal(Resources.ResourceType.Gas, module.ResourceType);
    }

    [Fact]
    public void GivenNullResource_WhenExecuting_DoesNotExecute() {
        // Arrange
        var unit = TestUtils.CreateUnit(Units.Drone);
        Bot.UnitModules.MiningModule.Install(unit, null);
        var module = Bot.UnitModules.UnitModule.Get<Bot.UnitModules.MiningModule>(unit);

        // Act
        var executed = module.Execute();

        // Assert
        Assert.False(executed);
    }

    [Theory]
    [MemberData(nameof(MineralsTestData))]
    [MemberData(nameof(GasGeysersTestData))]
    public void GivenResource_WhenExecuting_DoesExecutes(uint resourceType) {
        // Arrange
        var resource = TestUtils.CreateUnit(resourceType);
        Bot.UnitModules.CapacityModule.Install(resource, 1);

        var unit = TestUtils.CreateUnit(Units.Drone);
        Bot.UnitModules.MiningModule.Install(unit, resource);

        var module = Bot.UnitModules.UnitModule.Get<Bot.UnitModules.MiningModule>(unit);

        // Act
        var executed = module.Execute();

        // Assert
        Assert.True(executed);
    }
}
