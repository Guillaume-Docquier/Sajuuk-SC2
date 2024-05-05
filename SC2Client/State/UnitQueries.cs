using SC2Client.GameData;

namespace SC2Client.State;

/// <summary>
/// A collection of methods to query for units by unit types.
/// </summary>
public static class UnitQueries {
    /// <summary>
    /// Returns all units of a certain type from the provided unitPool, including units of equivalent types.
    /// Buildings that are in production are included.
    /// </summary>
    /// <param name="unitPool">The unit pool to query from.</param>
    /// <param name="unitTypeToGet">The unit type to get.</param>
    /// <returns></returns>
    public static IEnumerable<IUnit> GetUnits(IEnumerable<IUnit> unitPool, uint unitTypeToGet) {
        return GetUnits(unitPool, new HashSet<uint>{ unitTypeToGet });
    }

    /// <summary>
    /// Returns all units that match a certain set of types from the provided unitPool, including units of equivalent types.
    /// Buildings that are in production are included.
    /// </summary>
    /// <param name="unitPool">The unit pool to query from.</param>
    /// <param name="unitTypesToGet">The unit types to get.</param>
    /// <param name="includeCloaked">Whether to include units that are cloaked.</param>
    /// <returns></returns>
    public static IEnumerable<IUnit> GetUnits(IEnumerable<IUnit> unitPool, HashSet<uint> unitTypesToGet, bool includeCloaked = false) {
        var equivalentUnitTypes = unitTypesToGet
            .Where(unitTypeToGet => UnitTypeId.EquivalentTo.ContainsKey(unitTypeToGet))
            .SelectMany(unitTypeToGet => UnitTypeId.EquivalentTo[unitTypeToGet])
            .ToList();

        unitTypesToGet.UnionWith(equivalentUnitTypes);

        foreach (var unit in unitPool) {
            if (!unitTypesToGet.Contains(unit.UnitType)) {
                continue;
            }

            if (unit.IsCloaked && !includeCloaked) {
                continue;
            }

            yield return unit;
        }
    }
}
