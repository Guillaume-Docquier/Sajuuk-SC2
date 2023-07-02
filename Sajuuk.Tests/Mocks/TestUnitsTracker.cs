using Sajuuk.GameSense;
using SC2APIProtocol;

namespace Sajuuk.Tests.Mocks;

public class TestUnitsTracker : IUnitsTracker {
    public Dictionary<ulong, Unit> UnitsByTag { get; private set; } = new Dictionary<ulong, Unit>();
    public List<Unit> NewOwnedUnits { get; private set; } = new List<Unit>();
    public List<Unit> NeutralUnits { get; private set; } = new List<Unit>();
    public List<Unit> OwnedUnits { get; private set; } = new List<Unit>();
    public List<Unit> EnemyUnits { get; private set; } = new List<Unit>();
    public Dictionary<ulong, Unit> EnemyGhostUnits { get; private set; } = new Dictionary<ulong, Unit>();
    public Dictionary<ulong, Unit> EnemyMemorizedUnits { get; private set; } = new Dictionary<ulong, Unit>();

    public IEnumerable<Unit> GetUnits(IEnumerable<Unit> unitPool, uint unitTypeToGet) {
        return GetUnits(unitPool, new HashSet<uint>{ unitTypeToGet });
    }

    public IEnumerable<Unit> GetUnits(IEnumerable<Unit> unitPool, HashSet<uint> unitTypesToGet, bool includeCloaked = false) {
        return unitPool.Where(unit => unitTypesToGet.Contains(unit.UnitType));
    }

    public List<Unit> GetUnits(Alliance alliance) {
        return alliance switch
        {
            Alliance.Self => OwnedUnits,
            Alliance.Enemy => EnemyUnits,
            Alliance.Neutral => NeutralUnits,
            _ => new List<Unit>()
        };
    }

    public List<Unit> GetGhostUnits(Alliance alliance) {
        return alliance switch
        {
            Alliance.Enemy => EnemyGhostUnits.Values.ToList(),
            _ => new List<Unit>()
        };
    }

    public void SetUnits(List <Unit> units) {
        UnitsByTag = units.ToDictionary(unit => unit.Tag, unit => unit);
        NewOwnedUnits.Clear();

        NeutralUnits = units.Where(unit => unit.Alliance == Alliance.Neutral).ToList();
        OwnedUnits = units.Where(unit => unit.Alliance == Alliance.Self).ToList();
        EnemyUnits = units.Where(unit => unit.Alliance == Alliance.Enemy).ToList();

        EnemyGhostUnits.Clear();
        EnemyMemorizedUnits.Clear();
    }

    public void AddNewUnits(List <Unit> newUnits) {
        foreach (var newUnit in newUnits) {
            UnitsByTag.Add(newUnit.Tag, newUnit);

            switch (newUnit.Alliance) {
                case Alliance.Neutral:
                    NeutralUnits.Add(newUnit);
                    break;
                case Alliance.Self:
                    NewOwnedUnits.Add(newUnit);
                    OwnedUnits.Add(newUnit);
                    break;
                case Alliance.Enemy:
                    EnemyUnits.Add(newUnit);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
