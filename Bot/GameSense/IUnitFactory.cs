namespace Bot.GameSense;

public interface IUnitFactory {
    public Unit CreateUnit(SC2APIProtocol.Unit rawUnit, ulong frame);
}
