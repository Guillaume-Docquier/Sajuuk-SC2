using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis;
using Sajuuk.Tests.Fixtures;
using Sajuuk.UnitModules;
using Moq;

namespace Sajuuk.Tests.UnitModules;

public class UnitModuleInstallerTests : IClassFixture<NoLoggerFixture> {
    private readonly UnitModuleInstaller _sut;

    public UnitModuleInstallerTests() {
        _sut = new UnitModuleInstaller(
            new Mock<IUnitsTracker>().Object,
            new Mock<IGraphicalDebugger>().Object,
            new Mock<IBuildingTracker>().Object,
            new Mock<IRegionsTracker>().Object,
            new Mock<ICreepTracker>().Object,
            new Mock<IPathfinder>().Object,
            new Mock<IVisibilityTracker>().Object,
            new Mock<ITerrainTracker>().Object,
            new Mock<IFrameClock>().Object
        );
    }

    [Fact]
    public void GivenNullWorker_WhenInstallingMiningModule_DoesNotInstall() {
        // Act
        var miningModule = _sut.InstallMiningModule(null, null);

        // Assert
        Assert.Null(miningModule);
    }
}
