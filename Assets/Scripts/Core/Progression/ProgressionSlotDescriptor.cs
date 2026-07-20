namespace Core.Progression
{
    public readonly struct ProgressionSlotDescriptor
    {
        public ProgressionSlotDescriptor(int slotIndex, bool hasSave, string campaignName)
        {
            SlotIndex = slotIndex;
            HasSave = hasSave;
            CampaignName = campaignName;
        }

        public int SlotIndex { get; }

        public bool HasSave { get; }

        public string CampaignName { get; }
    }
}