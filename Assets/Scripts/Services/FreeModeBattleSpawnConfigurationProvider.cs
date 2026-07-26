using System;
using Core.Gameplay;
using Core.Services;
using Core.Ships;
using Core.State;

namespace Services
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class FreeModeBattleSpawnConfigurationProvider : IBattleSpawnConfigurationProvider
    {
        private readonly IShipSnapshotService _snapshotService;

        public FreeModeBattleSpawnConfigurationProvider(IShipSnapshotService snapshotService)
        {
            _snapshotService = snapshotService;
        }

        public IBattleSpawnConfiguration GetConfiguration()
        {
            if (SaveState.Mode != GameSessionMode.FreeMode)
                throw new InvalidOperationException(
                    "[FreeModeBattleSpawnConfigurationProvider] Save state is not in Free Mode.");

            ShipSnapshot playerSnapshot = null;
            var snapshotFile = SaveState.PlayerShipSnapshotFilePath;
            if (!string.IsNullOrWhiteSpace(snapshotFile))
                playerSnapshot = _snapshotService.LoadSnapshotFromFile(snapshotFile);

            return new BattleSpawnConfiguration(
                playerSnapshot,
                SaveState.AllySnapshots ?? Array.Empty<ShipSnapshot>(),
                SaveState.EnemySnapshots ?? Array.Empty<ShipSnapshot>(),
                SaveState.AsteroidCount);
        }
    }
}