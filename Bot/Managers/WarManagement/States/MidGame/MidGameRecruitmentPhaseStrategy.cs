using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.Managers.WarManagement.States.MidGame;

public class MidGameRecruitmentPhaseStrategy : WarManagerStrategy {
    private static readonly HashSet<uint> ManageableUnitTypes = Units.ZergMilitary.Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed }).ToHashSet();

    public MidGameRecruitmentPhaseStrategy(WarManager context) : base(context) {}

    public override void Execute() {
        WarManager.Assign(Controller.GetUnits(UnitsTracker.NewOwnedUnits, ManageableUnitTypes));
    }
}
