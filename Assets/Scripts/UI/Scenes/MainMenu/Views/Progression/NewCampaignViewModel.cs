using System.Collections.Generic;
using Core.Ships;

namespace UI.Scenes.MainMenu.Views.Progression
{
    public class NewCampaignViewModel
    {
        public NewCampaignViewModel(
            IReadOnlyList<SavedShipSnapshotDescriptor> ships,
            string campaignName,
            bool canStart)
        {
            Ships = ships;
            CampaignName = campaignName;
            CanStart = canStart;
        }

        public IReadOnlyList<SavedShipSnapshotDescriptor> Ships { get; }

        public string CampaignName { get; }

        public bool CanStart { get; }
    }
}