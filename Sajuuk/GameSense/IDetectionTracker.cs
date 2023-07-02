using System.Collections.Generic;

namespace Sajuuk.GameSense;

public interface IDetectionTracker {
    bool IsStealthEffective();
    bool IsDetected(Unit unit);
    bool IsDetected(IReadOnlyCollection<Unit> army);
}
