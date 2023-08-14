namespace Sajuuk.Builds.BuildRequests;

public interface IBuildRequestFactory {
    public TargetBuildRequest CreateTargetBuildRequest(
        BuildType buildType,
        uint unitOrUpgradeType,
        int targetQuantity,
        uint atSupply = 0,
        bool queue = false,
        BuildBlockCondition blockCondition = BuildBlockCondition.None,
        BuildRequestPriority priority = BuildRequestPriority.Normal
    );

    public QuantityBuildRequest CreateQuantityBuildRequest(
        BuildType buildType,
        uint unitOrUpgradeType,
        int quantity = 1,
        uint atSupply = 0,
        bool queue = false,
        BuildBlockCondition blockCondition = BuildBlockCondition.None,
        BuildRequestPriority priority = BuildRequestPriority.Normal
    );
}
