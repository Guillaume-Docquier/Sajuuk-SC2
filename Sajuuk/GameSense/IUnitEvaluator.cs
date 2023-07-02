using System.Collections.Generic;

namespace Sajuuk.GameSense;

public interface IUnitEvaluator {
    /// <summary>
    /// Evaluates the force of a unit
    /// TODO GD Make this more sophisticated (based on unit cost, range, counters, etc)
    /// </summary>
    /// <param name="unit">The unit to evaluate</param>
    /// <param name="areWorkersOffensive">Whether we should consider the workers as being offensive</param>
    /// <returns>The force of the unit</returns>
    float EvaluateForce(Unit unit, bool areWorkersOffensive = false);
    float EvaluateForce(IEnumerable<Unit> army, bool areWorkersOffensive = false);

    /// <summary>
    /// Evaluates the value of a unit
    /// </summary>
    /// <param name="unit">The valuable unit</param>
    /// <returns>The value of the unit</returns>
    float EvaluateValue(Unit unit);
}
