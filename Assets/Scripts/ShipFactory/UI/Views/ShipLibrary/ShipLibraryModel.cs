using System.Collections.Generic;
using UI.MVCVM;
using UnityEngine;

namespace ShipFactory.UI.Views.ShipLibrary
{
    public class ShipLibraryModel : ObservableModel
    {
        private readonly List<ShipLibraryEntry> _entries = new();

        public IReadOnlyList<ShipLibraryEntry> Entries => _entries;

        public void SetShipEntries(
            IReadOnlyList<ShipLibraryEntry> entries)
        {
            _entries.Clear();

            if (entries != null)
                _entries.AddRange(entries);

            NotifyChanged();
        }
    }

    public sealed class ShipLibraryEntry
    {
        public ShipLibraryEntry(
            string displayName,
            string filePath,
            Sprite previewSprite)
        {
            DisplayName = displayName;
            FilePath = filePath;
            PreviewSprite = previewSprite;
        }

        public string DisplayName { get; }

        public string FilePath { get; }

        public Sprite PreviewSprite { get; }
    }
}