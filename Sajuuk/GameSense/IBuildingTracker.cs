using System.Numerics;
using SC2APIProtocol;

namespace Sajuuk.GameSense;

public interface IBuildingTracker {
    public Vector2 FindConstructionSpot(uint buildingType);
    public void ConfirmPlacement(uint buildingType, Vector2 position, Unit builder);
    public bool CanPlace(uint buildingType, Vector2 position);
    public ActionResult QueryPlacement(uint buildingType, Vector2 position);
}
