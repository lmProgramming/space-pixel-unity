using System.Collections.Generic;
using Core.Services;
using Core.Ships;
using UnityEngine;
using ZLinq;

namespace Services
{
    [CreateAssetMenu(fileName = "SkirmishSnapshotCatalog", menuName = "Game/Skirmish Snapshot Catalog")]
    public class SkirmishSnapshotCatalog : ScriptableObject, ISkirmishSnapshotCatalog
    {
        [SerializeField] private List<TextAsset> enemySnapshots = new();
        [SerializeField] private List<TextAsset> friendlySnapshots = new();

        public bool HasEnemySnapshots()
        {
            return HasSnapshots(enemySnapshots);
        }

        public bool HasFriendlySnapshots()
        {
            return HasSnapshots(friendlySnapshots);
        }

        public ShipSnapshot GetRandomEnemySnapshot()
        {
            return GetRandomSnapshot(enemySnapshots, "enemy");
        }

        public ShipSnapshot GetRandomFriendlySnapshot()
        {
            return GetRandomSnapshot(friendlySnapshots, "friendly");
        }

        private static bool HasSnapshots(List<TextAsset> snapshots)
        {
            return snapshots.AsValueEnumerable()
                .Any(snapshot => snapshot != null && !string.IsNullOrWhiteSpace(snapshot.text));
        }

        private static ShipSnapshot GetRandomSnapshot(List<TextAsset> snapshots, string teamName)
        {
            var availableSnapshots = snapshots.AsValueEnumerable()
                .Where(textAsset => textAsset != null && !string.IsNullOrWhiteSpace(textAsset.text)).ToList();

            if (availableSnapshots.Count == 0)
                throw new UnityException(
                    $"[SkirmishSnapshotCatalog] No {teamName} snapshots are configured. Add at least one JSON TextAsset.");

            var index = Random.Range(0, availableSnapshots.Count);
            var snapshotJson = availableSnapshots[index].text;
            var snapshot = ShipSnapshotService.FromJson(snapshotJson);

            if (snapshot == null)
                throw new UnityException($"[SkirmishSnapshotCatalog] Failed to parse {teamName} snapshot JSON.");

            return snapshot;
        }
    }
}