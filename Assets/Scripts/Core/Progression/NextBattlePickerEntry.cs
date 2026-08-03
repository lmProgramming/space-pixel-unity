using System;
using Core.Ships;
using UnityEngine;

namespace Core.Progression
{
    public readonly struct NextBattlePickerEntry
    {
        public NextBattlePickerEntry(string displayName, Sprite previewSprite,
            ShipSnapshot[] enemySnapshots, int asteroidsCount, int creditsReward)
        {
            DisplayName = displayName;
            PreviewSprite = previewSprite;
            EnemySnapshots = enemySnapshots;
            AsteroidsCount = asteroidsCount;
            CreditsReward = creditsReward;

            Id = Guid.NewGuid();
        }

        public string DisplayName { get; }
        public Guid Id { get; }

        public Sprite PreviewSprite { get; }
        public int EnemiesCount => EnemySnapshots.Length;
        public ShipSnapshot[] EnemySnapshots { get; }
        public int AsteroidsCount { get; }
        public int CreditsReward { get; }
    }
}