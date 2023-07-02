using System.Linq;
using Sajuuk.GameData;
using Sajuuk.UnitModules;

namespace Sajuuk.Managers.EconomyManagement.TownHallSupervision;

public partial class TownHallSupervisor {
    private class TownHallSupervisorAssigner : IAssigner {
        private const int MaxExtractorsPerGas = 1;

        private readonly TownHallSupervisor _supervisor;

        public TownHallSupervisorAssigner(TownHallSupervisor supervisor) {
            _supervisor = supervisor;
        }

        public void Assign(Unit unit) {
            switch (unit.UnitType) {
                case Units.Hatchery:
                case Units.Lair:
                case Units.Hive:
                    AssignTownHall(unit);
                    return;
                case Units.Queen:
                case Units.QueenBurrowed:
                    AssignQueen(unit);
                    return;
                case Units.Drone:
                case Units.DroneBurrowed:
                    AssignWorker(unit);
                    return;
                case Units.Extractor:
                    AssignExtractor(unit);
                    return;
            }

            if (Units.MineralFields.Contains(unit.UnitType)) {
                AssignMineral(unit);
                return;
            }

            if (Units.GasGeysers.Contains(unit.UnitType)) {
                AssignGas(unit);
                return;
            }

            Logger.Error("({0}) Tried to assign {1}, but we don't supervise this unit type", _supervisor, unit);
        }

        private void LogAssignment(Unit unit) {
            Logger.Debug("({0}) Assigned {1}", _supervisor, unit);
        }

        private void AssignTownHall(Unit townHall) {
            if (_supervisor.TownHall != null) {
                Logger.Error("({0}) Trying to assign town hall, but we already have one", this);
                return;
            }

            _supervisor.TownHall = townHall;
            _supervisor._unitModuleInstaller.InstallDebugLocationModule(_supervisor.TownHall, _supervisor._color);

            LogAssignment(townHall);
        }

        private void AssignQueen(Unit queen) {
            if (_supervisor.Queen != null) {
                Logger.Error("({0}) Trying to assign queen, but we already have one", _supervisor);
                return;
            }

            _supervisor.Queen = queen;
            _supervisor._unitModuleInstaller.InstallDebugLocationModule(queen, _supervisor._color);

            var queenMicroModule = UnitModule.Get<QueenMicroModule>(queen);
            if (queenMicroModule != null) {
                queenMicroModule.AssignTownHall(_supervisor.TownHall);
            }
            else {
                _supervisor._unitModuleInstaller.InstallQueenMicroModule(queen, _supervisor.TownHall);
            }

            LogAssignment(queen);
        }

        private void AssignWorker(Unit worker) {
            _supervisor._workers.Add(worker);

            _supervisor._unitModuleInstaller.InstallDebugLocationModule(worker, _supervisor._color);
            _supervisor._unitModuleInstaller.InstallMiningModule(worker, null);

            LogAssignment(worker);
        }

        private void AssignExtractor(Unit extractor) {
            extractor.AddDeathWatcher(_supervisor);

            _supervisor._extractors.Add(extractor);

            _supervisor._unitModuleInstaller.InstallDebugLocationModule(extractor, _supervisor._color);
            _supervisor._unitModuleInstaller.InstallCapacityModule(extractor, Resources.MaxDronesPerExtractor);

            UnitModule.Get<CapacityModule>(_supervisor._gasses.First(gas => gas.DistanceTo(extractor) < 1)).Assign(extractor); // TODO GD Make this cleaner

            LogAssignment(extractor);
        }

        private void AssignMineral(Unit mineral) {
            mineral.AddDeathWatcher(_supervisor);

            _supervisor._minerals.Add(mineral);

            _supervisor._unitModuleInstaller.InstallDebugLocationModule(mineral, _supervisor._color);
            _supervisor._unitModuleInstaller.InstallCapacityModule(mineral, Resources.MaxDronesPerMinerals);

            LogAssignment(mineral);
        }

        // TODO GD Should we add special logic to kill gasses when they are depleted?
        private void AssignGas(Unit gas) {
            _supervisor._gasses.Add(gas);

            _supervisor._unitModuleInstaller.InstallDebugLocationModule(gas, _supervisor._color);
            _supervisor._unitModuleInstaller.InstallCapacityModule(gas, MaxExtractorsPerGas, showDebugInfo: false);

            LogAssignment(gas);
        }
    }
}
