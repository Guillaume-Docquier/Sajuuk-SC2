using SC2APIProtocol;

namespace Bot.Tests;

public static class TestUtils {
    private static ulong _currentTag = 0;

    public static Bot.Unit CreateUnit(uint unitType, uint frame = 0, Alliance alliance = Alliance.Self, Point? position = null) {
        var rawUnit = new SC2APIProtocol.Unit
        {
            Tag = _currentTag,
            UnitType = unitType,
            Alliance = alliance,
            Pos = position ?? new Point { X = 0, Y = 0, Z = 0 },
        };

        // Just make sure to never collide
        _currentTag++;

        return new Bot.Unit(rawUnit, frame);
    }
}
