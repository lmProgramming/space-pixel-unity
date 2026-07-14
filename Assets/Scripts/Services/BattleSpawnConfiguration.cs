using System;
using System.Collections.Generic;
using Core.Services;
using Core.Ships;

namespace Services
{
    public sealed class BattleSpawnConfiguration : IBattleSpawnConfiguration
    {
        public BattleSpawnConfiguration(
            ShipSnapshot playerShipSnapshot,
            IReadOnlyList<ShipSnapshot> allySnapshots,
            int enemyCount,
            int asteroidCount,
            int randomFriendlyCount)
        {
            PlayerShipSnapshot = playerShipSnapshot;
            AllySnapshots = allySnapshots ?? Array.Empty<ShipSnapshot>();
            EnemyCount = enemyCount;
            AsteroidCount = asteroidCount;
            RandomFriendlyCount = randomFriendlyCount;
        }

        public ShipSnapshot PlayerShipSnapshot { get; }

        public IReadOnlyList<ShipSnapshot> AllySnapshots { get; }

        public int EnemyCount { get; }

        public int AsteroidCount { get; }

        public int RandomFriendlyCount { get; }
    }
}