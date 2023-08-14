using Sajuuk.Builds.BuildRequests;
using Sajuuk.GameSense;

namespace Sajuuk.Builds.BuildOrders;

public class BuildOrderFactory : IBuildOrderFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IBuildRequestFactory _buildRequestFactory;

    public BuildOrderFactory(
        IUnitsTracker unitsTracker,
        IBuildRequestFactory buildRequestFactory
    ) {
        _unitsTracker = unitsTracker;
        _buildRequestFactory = buildRequestFactory;
    }

    public FourBasesRoach CreateFourBasesRoach() {
        return new FourBasesRoach(_unitsTracker, _buildRequestFactory);
    }

    public TestExpands CreateTestExpands() {
        return new TestExpands(_buildRequestFactory);
    }

    public TestGasMining CreateTestGasMining() {
        return new TestGasMining(_buildRequestFactory);
    }

    public TestMineralSaturatedMining CreateTestMineralSaturatedMining() {
        return new TestMineralSaturatedMining(_buildRequestFactory);
    }

    public TestMineralSpeedMining CreateTestMineralSpeedMining() {
        return new TestMineralSpeedMining(_buildRequestFactory);
    }

    public TwoBasesRoach CreateTwoBasesRoach() {
        return new TwoBasesRoach(_unitsTracker, _buildRequestFactory);
    }
}
