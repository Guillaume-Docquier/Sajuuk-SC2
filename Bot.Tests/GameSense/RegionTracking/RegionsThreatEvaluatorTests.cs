﻿using System.Numerics;
using Bot.GameSense.RegionTracking;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Tests.GameSense.RegionTracking;

public class RegionsThreatEvaluatorTests {
    [Fact]
    public void GivenNoForcesAndNoValues_WhenUpdateEvaluations_ThenAllEvaluationsAreZero() {
        // Arrange
        // 6←---   1←-→2
        // ↑   |   ↑   ↑
        // |   |   |   |
        // ↓   |   ↓   ↓
        // 5←-----→3←-→4
        var region6 = new TestRegion(6, new Vector2(0, 1));
        var region1 = new TestRegion(1, new Vector2(1, 1));
        var region2 = new TestRegion(2, new Vector2(2, 1));
        var region5 = new TestRegion(5, new Vector2(0, 0));
        var region3 = new TestRegion(3, new Vector2(1, 0));
        var region4 = new TestRegion(4, new Vector2(2, 0));

        region1.AddNeighbors(new [] { region2, region3 });
        region2.AddNeighbors(new [] { region1, region4 });
        region3.AddNeighbors(new [] { region1, region4, region5, region6 });
        region4.AddNeighbors(new [] { region2, region3 });
        region5.AddNeighbors(new [] { region3, region6 });
        region6.AddNeighbors(new [] { region3, region5 });

        var regions = new[] { region1, region2, region3, region4, region5, region6 };

        var enemyForceEvaluator = new TestRegionsForceEvaluator();
        var selfValueEvaluator = new TestRegionsValueEvaluator();
        var threatEvaluator = new RegionsThreatEvaluator(enemyForceEvaluator, selfValueEvaluator);

        enemyForceEvaluator.Init(regions);
        selfValueEvaluator.Init(regions);
        threatEvaluator.Init(regions);

        // Act
        threatEvaluator.UpdateEvaluations();

        // Assert
        foreach (var region in regions) {
            Assert.Equal(0, threatEvaluator.GetEvaluation(region));
        }
    }

    [Fact]
    public void GivenValuesAndForces_WhenUpdateEvaluations_ThenNonZeroForcesAreThreats() {
        // Arrange
        // 6←---   1←-→2
        // ↑   |   ↑   ↑
        // |   |   |   |
        // ↓   |   ↓   ↓
        // 5←-----→3←-→4
        var region6 = new TestRegion(6, new Vector2(0, 1));
        var region1 = new TestRegion(1, new Vector2(1, 1));
        var region2 = new TestRegion(2, new Vector2(2, 1));
        var region5 = new TestRegion(5, new Vector2(0, 0));
        var region3 = new TestRegion(3, new Vector2(1, 0));
        var region4 = new TestRegion(4, new Vector2(2, 0));

        region1.AddNeighbors(new [] { region2, region3 });
        region2.AddNeighbors(new [] { region1, region4 });
        region3.AddNeighbors(new [] { region1, region4, region5, region6 });
        region4.AddNeighbors(new [] { region2, region3 });
        region5.AddNeighbors(new [] { region3, region6 });
        region6.AddNeighbors(new [] { region3, region5 });

        var regions = new[] { region1, region2, region3, region4, region5, region6 };

        var forceEvaluations = new Dictionary<IRegion, float>
        {
            { region1, 0 },
            { region2, 1 },
            { region3, 1 },
            { region4, 0 },
            { region5, 0 },
            { region6, 0 },
        };
        var enemyForceEvaluator = new TestRegionsForceEvaluator(forceEvaluations);

        var valueEvaluations = new Dictionary<IRegion, float>
        {
            { region1, 0 },
            { region2, 0 },
            { region3, 0 },
            { region4, 0 },
            { region5, 1 },
            { region6, 0 },
        };
        var selfValueEvaluator = new TestRegionsValueEvaluator(valueEvaluations);
        var threatEvaluator = new RegionsThreatEvaluator(enemyForceEvaluator, selfValueEvaluator);

        enemyForceEvaluator.Init(regions);
        selfValueEvaluator.Init(regions);
        threatEvaluator.Init(regions);

        // Act
        threatEvaluator.UpdateEvaluations();

        // Assert
        foreach (var (region, force) in forceEvaluations) {
            if (force == 0) {
                Assert.Equal(0, threatEvaluator.GetEvaluation(region));
            }
            else {
                Assert.True(threatEvaluator.GetEvaluation(region) > 0);
            }
        }
    }

    [Fact]
    public void GivenForcesButNoValues_WhenUpdateEvaluations_ThenAllEvaluationsAreZero() {
        // Arrange
        // 6←---   1←-→2
        // ↑   |   ↑   ↑
        // |   |   |   |
        // ↓   |   ↓   ↓
        // 5←-----→3←-→4
        var region6 = new TestRegion(6, new Vector2(0, 1));
        var region1 = new TestRegion(1, new Vector2(1, 1));
        var region2 = new TestRegion(2, new Vector2(2, 1));
        var region5 = new TestRegion(5, new Vector2(0, 0));
        var region3 = new TestRegion(3, new Vector2(1, 0));
        var region4 = new TestRegion(4, new Vector2(2, 0));

        region1.AddNeighbors(new [] { region2, region3 });
        region2.AddNeighbors(new [] { region1, region4 });
        region3.AddNeighbors(new [] { region1, region4, region5, region6 });
        region4.AddNeighbors(new [] { region2, region3 });
        region5.AddNeighbors(new [] { region3, region6 });
        region6.AddNeighbors(new [] { region3, region5 });

        var regions = new[] { region1, region2, region3, region4, region5, region6 };

        var forceEvaluations = new Dictionary<IRegion, float>
        {
            { region1, 1 },
            { region2, 2 },
            { region3, 3 },
            { region4, 4 },
            { region5, 5 },
            { region6, 6 },
        };
        var enemyForceEvaluator = new TestRegionsForceEvaluator(forceEvaluations);
        var selfValueEvaluator = new TestRegionsValueEvaluator();
        var threatEvaluator = new RegionsThreatEvaluator(enemyForceEvaluator, selfValueEvaluator);

        enemyForceEvaluator.Init(regions);
        selfValueEvaluator.Init(regions);
        threatEvaluator.Init(regions);

        // Act
        threatEvaluator.UpdateEvaluations();

        // Assert
        foreach (var region in regions) {
            Assert.Equal(0, threatEvaluator.GetEvaluation(region));
        }
    }

    [Fact]
    public void GivenForcesAndValues_WhenUpdateEvaluations_ThenAForceNextToMoreValuesIsMoreThreatening() {
        // Arrange
        // 6←---   1←-→2
        // ↑   |   ↑   ↑
        // |   |   |   |
        // ↓   |   ↓   ↓
        // 5←-----→3←-→4
        var region6 = new TestRegion(6, new Vector2(0, 1));
        var region1 = new TestRegion(1, new Vector2(1, 1));
        var region2 = new TestRegion(2, new Vector2(2, 1));
        var region5 = new TestRegion(5, new Vector2(0, 0));
        var region3 = new TestRegion(3, new Vector2(1, 0));
        var region4 = new TestRegion(4, new Vector2(2, 0));

        region1.AddNeighbors(new [] { region2, region3 });
        region2.AddNeighbors(new [] { region1, region4 });
        region3.AddNeighbors(new [] { region1, region4, region5, region6 });
        region4.AddNeighbors(new [] { region2, region3 });
        region5.AddNeighbors(new [] { region3, region6 });
        region6.AddNeighbors(new [] { region3, region5 });

        var regions = new[] { region1, region2, region3, region4, region5, region6 };

        var forceEvaluations = new Dictionary<IRegion, float>
        {
            { region1, 0 },
            { region2, 1 },
            { region3, 1 },
            { region4, 0 },
            { region5, 0 },
            { region6, 0 },
        };
        var enemyForceEvaluator = new TestRegionsForceEvaluator(forceEvaluations);

        var valueEvaluations = new Dictionary<IRegion, float>
        {
            { region1, 0 },
            { region2, 0 },
            { region3, 0 },
            { region4, 1 },
            { region5, 1 },
            { region6, 0 },
        };
        var selfValueEvaluator = new TestRegionsValueEvaluator(valueEvaluations);
        var threatEvaluator = new RegionsThreatEvaluator(enemyForceEvaluator, selfValueEvaluator);

        enemyForceEvaluator.Init(regions);
        selfValueEvaluator.Init(regions);
        threatEvaluator.Init(regions);

        // Act
        threatEvaluator.UpdateEvaluations();

        // Assert
        var mostThreateningRegion = regions.MaxBy(region => threatEvaluator.GetEvaluation(region));
        Assert.Equal(region3, mostThreateningRegion);
    }

    [Fact]
    public void GivenForcesAndValues_WhenUpdateEvaluations_ThenAForceCloserToValueIsMoreThreatening() {
        // Arrange
        // 6←---   1←-→2
        // ↑   |   ↑   ↑
        // |   |   |   |
        // ↓   |   ↓   ↓
        // 5←-----→3←-→4
        var region6 = new TestRegion(6, new Vector2(0, 1));
        var region1 = new TestRegion(1, new Vector2(1, 1));
        var region2 = new TestRegion(2, new Vector2(2, 1));
        var region5 = new TestRegion(5, new Vector2(0, 0));
        var region3 = new TestRegion(3, new Vector2(1, 0));
        var region4 = new TestRegion(4, new Vector2(2, 0));

        region1.AddNeighbors(new [] { region2, region3 });
        region2.AddNeighbors(new [] { region1, region4 });
        region3.AddNeighbors(new [] { region1, region4, region5, region6 });
        region4.AddNeighbors(new [] { region2, region3 });
        region5.AddNeighbors(new [] { region3, region6 });
        region6.AddNeighbors(new [] { region3, region5 });

        var regions = new[] { region1, region2, region3, region4, region5, region6 };

        var forceEvaluations = new Dictionary<IRegion, float>
        {
            { region1, 0 },
            { region2, 0 },
            { region3, 1 },
            { region4, 1 },
            { region5, 0 },
            { region6, 0 },
        };
        var enemyForceEvaluator = new TestRegionsForceEvaluator(forceEvaluations);

        var valueEvaluations = new Dictionary<IRegion, float>
        {
            { region1, 0 },
            { region2, 0 },
            { region3, 0 },
            { region4, 0 },
            { region5, 1 },
            { region6, 0 },
        };
        var selfValueEvaluator = new TestRegionsValueEvaluator(valueEvaluations);
        var threatEvaluator = new RegionsThreatEvaluator(enemyForceEvaluator, selfValueEvaluator);

        enemyForceEvaluator.Init(regions);
        selfValueEvaluator.Init(regions);
        threatEvaluator.Init(regions);

        // Act
        threatEvaluator.UpdateEvaluations();

        // Assert
        var mostThreateningRegion = regions.MaxBy(region => threatEvaluator.GetEvaluation(region));
        Assert.Equal(region3, mostThreateningRegion);
    }

    [Fact]
    public void GivenForcesAndValues_WhenUpdateEvaluations_ThenABiggerForceIsMoreThreatening() {
        // Arrange
        // 6←---   1←-→2
        // ↑   |   ↑   ↑
        // |   |   |   |
        // ↓   |   ↓   ↓
        // 5←-----→3←-→4
        var region6 = new TestRegion(6, new Vector2(0, 1));
        var region1 = new TestRegion(1, new Vector2(1, 1));
        var region2 = new TestRegion(2, new Vector2(2, 1));
        var region5 = new TestRegion(5, new Vector2(0, 0));
        var region3 = new TestRegion(3, new Vector2(1, 0));
        var region4 = new TestRegion(4, new Vector2(2, 0));

        region1.AddNeighbors(new [] { region2, region3 });
        region2.AddNeighbors(new [] { region1, region4 });
        region3.AddNeighbors(new [] { region1, region4, region5, region6 });
        region4.AddNeighbors(new [] { region2, region3 });
        region5.AddNeighbors(new [] { region3, region6 });
        region6.AddNeighbors(new [] { region3, region5 });

        var regions = new[] { region1, region2, region3, region4, region5, region6 };

        var forceEvaluations = new Dictionary<IRegion, float>
        {
            { region1, 0 },
            { region2, 1 },
            { region3, 2 },
            { region4, 0 },
            { region5, 0 },
            { region6, 0 },
        };
        var enemyForceEvaluator = new TestRegionsForceEvaluator(forceEvaluations);

        var valueEvaluations = new Dictionary<IRegion, float>
        {
            { region1, 0 },
            { region2, 0 },
            { region3, 0 },
            { region4, 1 },
            { region5, 0 },
            { region6, 0 },
        };
        var selfValueEvaluator = new TestRegionsValueEvaluator(valueEvaluations);
        var threatEvaluator = new RegionsThreatEvaluator(enemyForceEvaluator, selfValueEvaluator);

        enemyForceEvaluator.Init(regions);
        selfValueEvaluator.Init(regions);
        threatEvaluator.Init(regions);

        // Act
        threatEvaluator.UpdateEvaluations();

        // Assert
        var mostThreateningRegion = regions.MaxBy(region => threatEvaluator.GetEvaluation(region));
        Assert.Equal(region3, mostThreateningRegion);
    }

    [Fact]
    public void GivenForcesAndValues_WhenUpdateEvaluations_ThenConsidersAllValues() {
        // Arrange
        // 6←---   1←-→2
        // ↑   |   ↑   ↑
        // |   |   |   |
        // ↓   |   ↓   ↓
        // 5←-----→3←-→4
        var region6 = new TestRegion(6, new Vector2(0, 1));
        var region1 = new TestRegion(1, new Vector2(1, 1));
        var region2 = new TestRegion(2, new Vector2(2, 1));
        var region5 = new TestRegion(5, new Vector2(0, 0));
        var region3 = new TestRegion(3, new Vector2(1, 0));
        var region4 = new TestRegion(4, new Vector2(2, 0));

        region1.AddNeighbors(new [] { region2, region3 });
        region2.AddNeighbors(new [] { region1, region4 });
        region3.AddNeighbors(new [] { region1, region4, region5, region6 });
        region4.AddNeighbors(new [] { region2, region3 });
        region5.AddNeighbors(new [] { region3, region6 });
        region6.AddNeighbors(new [] { region3, region5 });

        var regions = new[] { region1, region2, region3, region4, region5, region6 };

        var forceEvaluations = new Dictionary<IRegion, float>
        {
            { region1, 0 },
            { region2, 1 },
            { region3, 0 },
            { region4, 0 },
            { region5, 0 },
            { region6, 0 },
        };
        var enemyForceEvaluator = new TestRegionsForceEvaluator(forceEvaluations);

        var valueEvaluations = new Dictionary<IRegion, float>
        {
            { region1, 1000 },
            { region2, 1 },
            { region3, 0 },
            { region4, 0 },
            { region5, 0 },
            { region6, 0 },
        };
        var selfValueEvaluator = new TestRegionsValueEvaluator(valueEvaluations);
        var threatEvaluator = new RegionsThreatEvaluator(enemyForceEvaluator, selfValueEvaluator);

        enemyForceEvaluator.Init(regions);
        selfValueEvaluator.Init(regions);
        threatEvaluator.Init(regions);

        // Act
        threatEvaluator.UpdateEvaluations();
        var threat = threatEvaluator.GetEvaluation(region2);

        // Assert
        Assert.True(threat > 0.45);
        Assert.True(threat < 1);
    }

    private class TestRegionsForceEvaluator : RegionsForceEvaluator {
        private readonly Dictionary<IRegion, float>? _evaluations;

        public TestRegionsForceEvaluator()
            : base(Alliance.Enemy) {
            _evaluations = null;
        }

        public TestRegionsForceEvaluator(Dictionary<IRegion, float> evaluations)
            : base(Alliance.Enemy) {
            _evaluations = evaluations;
        }

        protected override IEnumerable<(IRegion region, float evaluation)> DoUpdateEvaluations(IReadOnlyCollection<IRegion> regions) {
            return regions.Select(region => (region, _evaluations == null ? 0 : _evaluations[region]));
        }
    }

    private class TestRegionsValueEvaluator : RegionsValueEvaluator {
        private readonly Dictionary<IRegion, float>? _evaluations;

        public TestRegionsValueEvaluator()
            : base(Alliance.Self) {
            _evaluations = null;
        }

        public TestRegionsValueEvaluator(Dictionary<IRegion, float> evaluations)
            : base(Alliance.Self) {
            _evaluations = evaluations;
        }

        protected override IEnumerable<(IRegion region, float evaluation)> DoUpdateEvaluations(IReadOnlyCollection<IRegion> regions) {
            return regions.Select(region => (region, _evaluations == null ? 0 : _evaluations[region]));
        }
    }

    private class TestRegion : IRegion {
        public int Id { get; }
        public Color Color { get; } = new Color();
        public Vector2 Center { get; }
        public HashSet<Vector2> Cells { get; } = new HashSet<Vector2>();
        public float ApproximatedRadius { get; } = 0;
        public RegionType Type { get; } = RegionType.Expand;
        public HashSet<NeighboringRegion> Neighbors { get; } = new HashSet<NeighboringRegion>();
        public bool IsObstructed { get; }
        public ExpandLocation ExpandLocation { get; } = default;

        public TestRegion(int id, Vector2 center, bool isObstructed = false) {
            Id = id;
            Center = center;
            IsObstructed = isObstructed;
        }

        public IEnumerable<IRegion> GetReachableNeighbors() {
            return Neighbors.Select(neighbor => neighbor.Region);
        }

        public void AddNeighbors(IEnumerable<TestRegion> neighbors) {
            foreach (var neighbor in neighbors) {
                Neighbors.Add(new NeighboringRegion(neighbor, new HashSet<Vector2>()));
            }
        }

        public void UpdateObstruction() {
            throw new NotImplementedException();
        }

        public override string ToString() {
            return $"TestRegion {Id}";
        }
    }
}