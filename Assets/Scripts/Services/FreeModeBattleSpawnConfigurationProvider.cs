using System;
using Core.Services;
using Core.Ships;
using Core.State;

namespace Services
{
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
                Array.Empty<ShipSnapshot>(),
                SaveState.EnemyShipCount,
                SaveState.AsteroidCount,
                SaveState.FriendlyShipCount);
        }
    }
}