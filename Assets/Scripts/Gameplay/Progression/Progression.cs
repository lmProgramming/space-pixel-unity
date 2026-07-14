using System;
using Core.Gameplay.Progression;
using Core.Services;
using Core.Ships;
using Core.Snapshot;

namespace Gameplay.Progression
{
    public class Progression : ISnapshottable<ProgressionSave>
    {
        public Progression(ProgressionSave save)
        {
            RestoreFromSnapshot(save, null);
        }

        public string CampaignName { get; private set; }

        public ShipSnapshot[] Allies { get; private set; }

        public int EnemiesKilled { get; set; }

        public string Credits { get; set; }

        public ProgressionSave CaptureSnapshot(IGameContentCatalog contentCatalog)
        {
            return new ProgressionSave
            {
                campaignName = CampaignName,
                allies = Allies,
                enemiesKilled = EnemiesKilled,
                credits = Credits
            };
        }

        public void RestoreFromSnapshot(ProgressionSave snapshot, IGameContentCatalog contentCatalog)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            CampaignName = snapshot.campaignName;
            Allies = snapshot.allies ?? Array.Empty<ShipSnapshot>();
            EnemiesKilled = snapshot.enemiesKilled;
            Credits = snapshot.credits ?? "0";
        }
    }
}