using System.Collections.Generic;
using Core.Ships;
using JetBrains.Annotations;

namespace Core.Services
{
    public interface IBattleSpawnConfiguration
    {
        [CanBeNull]
        ShipSnapshot PlayerShipSnapshot { get; }

        IReadOnlyList<ShipSnapshot> AllySnapshots { get; }

        IReadOnlyList<ShipSnapshot> EnemySnapshots { get; }

        int AsteroidCount { get; }
    }
}