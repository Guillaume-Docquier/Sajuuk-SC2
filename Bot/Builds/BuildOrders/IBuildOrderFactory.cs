namespace Bot.Builds.BuildOrders;

public interface IBuildOrderFactory {
    public FourBasesRoach CreateFourBasesRoach();
    public TestExpands CreateTestExpands();
    public TestGasMining CreateTestGasMining();
    public TestMineralSaturatedMining CreateTestMineralSaturatedMining();
    public TestMineralSpeedMining CreateTestMineralSpeedMining();
    public TwoBasesRoach CreateTwoBasesRoach();
}
