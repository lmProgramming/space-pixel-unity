using System.Collections.Generic;
using Core.Ships;

namespace Core.Services
{
    public interface IBattleSpawnConfiguration
    {
        ShipSnapshot PlayerShipSnapshot { get; }

        IReadOnlyList<ShipSnapshot> AllySnapshots { get; }

        int EnemyCount { get; }

        int AsteroidCount { get; }

        int RandomFriendlyCount { get; }
    }
}