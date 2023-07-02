using System.Collections.Generic;
using SC2APIProtocol;

namespace Sajuuk.GameSense;

public interface IUnitsTracker {
    public Dictionary<ulong, Unit> UnitsByTag { get; }
    public List<Unit> NewOwnedUnits { get; }

    public List<Unit> NeutralUnits { get; }
    public List<Unit> OwnedUnits { get; }
    public List<Unit> EnemyUnits { get; }

    public Dictionary<ulong, Unit> EnemyGhostUnits { get; }
    public Dictionary<ulong, Unit> EnemyMemorizedUnits { get; }

    public IEnumerable<Unit> GetUnits(IEnumerable<Unit> unitPool, uint unitTypeToGet);
    public IEnumerable<Unit> GetUnits(IEnumerable<Unit> unitPool, HashSet<uint> unitTypesToGet, bool includeCloaked = false);
    public List<Unit> GetUnits(Alliance alliance);
    public List<Unit> GetGhostUnits(Alliance alliance);
}
