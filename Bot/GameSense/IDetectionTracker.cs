using System.Collections.Generic;

namespace Bot.GameSense;

public interface IDetectionTracker {
    bool IsStealthEffective();
    bool IsDetected(Unit unit);
    bool IsDetected(IReadOnlyCollection<Unit> army);
}