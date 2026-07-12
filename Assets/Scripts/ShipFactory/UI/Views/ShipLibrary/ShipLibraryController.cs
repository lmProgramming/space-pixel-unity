using System;
using System.Collections.Generic;
using Core.Services;
using UI.MVCVM;
using Zenject;

namespace ShipFactory.UI.Views.ShipLibrary
{
    public class ShipLibraryController
        : Controller<
            ShipLibraryModel,
            ShipLibraryView,
            ShipLibraryViewModel>
    {
        [Inject]
        private IShipSnapshotService _snapshotService;

        protected override void OnEnable()
        {
            base.OnEnable();

            View.CloseClicked += OnViewCloseClicked;
            View.LoadClicked += OnViewLoadClicked;
            View.DeleteClicked += OnViewDeleteClicked;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            View.CloseClicked -= OnViewCloseClicked;
            View.LoadClicked -= OnViewLoadClicked;
            View.DeleteClicked -= OnViewDeleteClicked;
        }

        public event Action CloseClicked;

        public event Action<string> SnapshotSelected;

        public event Action<string> SnapshotDeleted;

        public void Show(
            string snapshotFolderPath)
        {
            if (string.IsNullOrWhiteSpace(snapshotFolderPath))
                throw new ArgumentException("Snapshot folder path is required.", nameof(snapshotFolderPath));

            gameObject.SetActive(true);
            Model.SetShipEntries(CreateEntries(snapshotFolderPath));
        }

        protected override ShipLibraryModel CreateModel()
        {
            return new ShipLibraryModel();
        }

        protected override ShipLibraryView CreateView()
        {
            return new ShipLibraryView();
        }

        protected override ShipLibraryViewModel CreateViewModel(
            ShipLibraryModel model)
        {
            return new ShipLibraryViewModel(model.Entries);
        }

        private IReadOnlyList<ShipLibraryEntry> CreateEntries(
            string snapshotFolderPath)
        {
            var snapshots = _snapshotService.GetSavedSnapshots(snapshotFolderPath);
            var entries = new ShipLibraryEntry[snapshots.Count];

            for (var index = 0; index < snapshots.Count; index++)
            {
                var snapshot = snapshots[index];
                entries[index] = new ShipLibraryEntry(
                    snapshot.DisplayName,
                    snapshot.FilePath,
                    null);
            }

            return entries;
        }

        private void OnViewCloseClicked()
        {
            CloseClicked?.Invoke();
        }

        private void OnViewLoadClicked(string snapshotPath)
        {
            SnapshotSelected?.Invoke(snapshotPath);
        }

        private void OnViewDeleteClicked(string snapshotPath)
        {
            SnapshotDeleted?.Invoke(snapshotPath);
        }
    }
}