using System;

namespace Bot.Managers.WarManagement.States.MidGame;

[Flags]
public enum Stance {
    None = 0,
    Attack = 1,
    Defend = 2,
    Finisher = 4,
    TerranFinisher = 8,
}
