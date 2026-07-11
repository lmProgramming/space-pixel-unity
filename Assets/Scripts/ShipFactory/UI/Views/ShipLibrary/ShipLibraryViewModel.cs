using System.Collections.Generic;

namespace ShipFactory.UI.Views.ShipLibrary
{
    public class ShipLibraryViewModel
    {
        public ShipLibraryViewModel(
            IReadOnlyList<ShipLibraryEntry> entries)
        {
            Entries = entries;
        }

        public IReadOnlyList<ShipLibraryEntry> Entries { get; }
    }
}