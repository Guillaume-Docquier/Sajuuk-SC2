using Sajuuk.Managers.WarManagement.ArmySupervision.UnitsControl.StutterStepUnitsControl;

namespace Sajuuk.Tests.Managers.WarManagement.ArmySupervision.UnitsControl.StutterStepUnitsControl;

public class PressureGraphTests {
    [Fact]
    public void Given_ABA_Cycle_WhenBreakCycles_ThenCycleIsBroken() {
        // Arrange
        var pressureGraph = new Dictionary<string, PressureGraph.Pressure<string>>
        {
            ["A"] = new()
            {
                From = { "B" },
                To = { "B" }
            },
            ["B"] = new()
            {
                From = { "A" },
                To = { "A" }
            },
        };

        // Act
        var removedPressures = PressureGraph.BreakCycles(pressureGraph);

        // Assert
        Assert.Single(removedPressures);

        var removedPressure = removedPressures[0];
        Assert.Equal("A", removedPressure.From.First());
        Assert.Equal("B", removedPressure.To.First());
    }
    [Fact]
    public void GivenTwo_ABA_Cycles_WhenBreakCycles_ThenCyclesAreBroken() {
        // Arrange
        var pressureGraph = new Dictionary<string, PressureGraph.Pressure<string>>
        {
            ["A"] = new()
            {
                From = { "B" },
                To = { "B" }
            },
            ["B"] = new()
            {
                From = { "A" },
                To = { "A" }
            },
            ["C"] = new()
            {
                From = { "D" },
                To = { "D" }
            },
            ["D"] = new()
            {
                From = { "C" },
                To = { "C" }
            },
        };

        // Act
        var removedPressures = PressureGraph.BreakCycles(pressureGraph);

        // Assert
        Assert.Equal(2, removedPressures.Count);

        Assert.Equal("A", removedPressures[0].From.First());
        Assert.Equal("B", removedPressures[0].To.First());

        Assert.Equal("C", removedPressures[1].From.First());
        Assert.Equal("D", removedPressures[1].To.First());
    }

    [Fact]
    public void Given_ABCA_Cycle_WhenBreakCycles_ThenCycleIsBroken() {
        // Arrange
        var pressureGraph = new Dictionary<string, PressureGraph.Pressure<string>>
        {
            ["A"] = new()
            {
                From = { "C" },
                To = { "B" }
            },
            ["B"] = new()
            {
                From = { "A" },
                To = { "C" }
            },
            ["C"] = new()
            {
                From = { "B" },
                To = { "A" }
            },
        };

        // Act
        var removedPressures = PressureGraph.BreakCycles(pressureGraph);

        // Assert
        Assert.Single(removedPressures);

        var removedPressure = removedPressures[0];
        Assert.Equal("A", removedPressure.From.First());
        Assert.Equal("B", removedPressure.To.First());
    }

    [Fact]
    public void GivenCycleWithStartButNoEnd_WhenBreakCycles_ThenCycleIsBroken() {
        // Arrange
        var pressureGraph = new Dictionary<string, PressureGraph.Pressure<string>>
        {
            ["A"] = new()
            {
                To = { "B" }
            },
            ["B"] = new()
            {
                From = { "A", "D" },
                To = { "C" }
            },
            ["C"] = new()
            {
                From = { "B" },
                To = { "D" }
            },
            ["D"] = new()
            {
                From = { "C" },
                To = { "B" }
            },
        };

        // Act
        var removedPressures = PressureGraph.BreakCycles(pressureGraph);

        // Assert
        Assert.Single(removedPressures);

        var removedPressure = removedPressures[0];
        Assert.Equal("D", removedPressure.From.First());
        Assert.Equal("B", removedPressure.To.First());
    }

    [Fact]
    public void GivenCycleWithEndButNoStart_WhenBreakCycles_ThenCycleIsBroken() {
        // Arrange
        var pressureGraph = new Dictionary<string, PressureGraph.Pressure<string>>
        {
            ["A"] = new()
            {
                From = { "C" },
                To = { "B" }
            },
            ["B"] = new()
            {
                From = { "A" },
                To = { "C" }
            },
            ["C"] = new()
            {
                From = { "B" },
                To = { "D", "A" }
            },
            ["D"] = new()
            {
                From = { "C" },
            },
        };

        // Act
        var removedPressures = PressureGraph.BreakCycles(pressureGraph);

        // Assert
        Assert.Single(removedPressures);

        var removedPressure = removedPressures[0];
        Assert.Equal("C", removedPressure.From.First());
        Assert.Equal("A", removedPressure.To.First());
    }

    [Fact]
    public void GivenCycleWithStartAndEndNodes_WhenBreakCycles_ThenCycleIsBroken() {
        // Arrange
        var pressureGraph = new Dictionary<string, PressureGraph.Pressure<string>>
        {
            ["A"] = new()
            {
                To = { "B" }
            },
            ["B"] = new()
            {
                From = { "A", "D" },
                To = { "C" }
            },
            ["C"] = new()
            {
                From = { "B" },
                To = { "D" }
            },
            ["D"] = new()
            {
                From = { "C" },
                To = { "B", "E" }
            },
            ["E"] = new()
            {
                From = { "D" },
            },
        };

        // Act
        var removedPressures = PressureGraph.BreakCycles(pressureGraph);

        // Assert
        Assert.Single(removedPressures);

        var removedPressure = removedPressures[0];
        Assert.Equal("D", removedPressure.From.First());
        Assert.Equal("B", removedPressure.To.First());
    }

    [Fact]
    public void GivenGraphWithoutCycles_WhenBreakCycles_ThenNoCyclesAreBroken() {
        // Arrange
        var pressureGraph = new Dictionary<string, PressureGraph.Pressure<string>>
        {
            ["A1"] = new()
            {
                To = { "B1", "C1" }
            },
            ["B1"] = new()
            {
                From = { "A1" },
                To = { "D1", "E1" }
            },
            ["C1"] = new()
            {
                From = { "A1" },
                To = { "E1" }
            },
            ["D1"] = new()
            {
                From = { "B1" },
            },
            ["E1"] = new()
            {
                From = { "B1", "C1" },
            },
            ["A2"] = new()
            {
                From = { "B2", "C2" }
            },
            ["B2"] = new()
            {
                To = { "A2" },
                From = { "D2", "E2" }
            },
            ["C2"] = new()
            {
                To = { "A2" },
                From = { "E2" }
            },
            ["D2"] = new()
            {
                To = { "B2" },
            },
            ["E2"] = new()
            {
                To = { "B2", "C2" },
            },
        };

        // Act
        var removedPressures = PressureGraph.BreakCycles(pressureGraph);

        // Assert
        Assert.Empty(removedPressures);
    }
}
