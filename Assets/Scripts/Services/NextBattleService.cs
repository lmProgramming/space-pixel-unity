using Core.Progression;
using Core.Services;
using UnityEngine;
using Zenject;

namespace Services
{
    public class NextBattleService : INextBattleService
    {
        [Inject] private ISkirmishSnapshotCatalog _skirmishSnapshotCatalog;

        public NextBattlePickerEntry[] GetNextBattlePickerEntries()
        {
            return new NextBattlePickerEntry[]
            {
                new("easy", null, _skirmishSnapshotCatalog.GetRandomEnemySnapshots(1), Random.Range(0, 15)),
                new("medium", null, _skirmishSnapshotCatalog.GetRandomEnemySnapshots(2), Random.Range(0, 15)),
                new("boss", null, _skirmishSnapshotCatalog.GetRandomEnemySnapshots(3), Random.Range(0, 15))
            };
        }
    }
}