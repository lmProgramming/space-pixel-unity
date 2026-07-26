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
            IReadOnlyList<ShipSnapshot> enemySnapshots,
            int asteroidCount)
        {
            PlayerShipSnapshot = playerShipSnapshot;
            AllySnapshots = allySnapshots ?? Array.Empty<ShipSnapshot>();
            EnemySnapshots = enemySnapshots ?? Array.Empty<ShipSnapshot>();
            AsteroidCount = asteroidCount;
        }

        public ShipSnapshot PlayerShipSnapshot { get; }

        public IReadOnlyList<ShipSnapshot> AllySnapshots { get; }

        public IReadOnlyList<ShipSnapshot> EnemySnapshots { get; }

        public int AsteroidCount { get; }
    }
}