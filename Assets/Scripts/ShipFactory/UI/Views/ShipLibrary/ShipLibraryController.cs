using System;
using System.Collections.Generic;
using Core.Services;
using Core.Ships;
using Core.ShipSnapshots;
using UI.MVCVM;
using UnityEngine;
using Zenject;

namespace ShipFactory.UI.Views.ShipLibrary
{
    [RequireComponent(typeof(ShipLibraryView))]
    public class ShipLibraryController
        : Controller<
            ShipSnapshotCatalogModel,
            ShipLibraryView,
            ShipLibraryViewModel>
    {
        [Inject]
        private IShipSnapshotRepository _snapshotRepository;

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

        protected override ShipSnapshotCatalogModel CreateModel()
        {
            return _snapshotRepository.Model;
        }

        protected override ShipLibraryViewModel CreateViewModel(
            ShipSnapshotCatalogModel model)
        {
            return new ShipLibraryViewModel(CreateEntries(model.Snapshots));
        }

        private static IReadOnlyList<ShipLibraryEntry> CreateEntries(
            IReadOnlyList<SavedShipSnapshotDescriptor> snapshots)
        {
            var entries = new ShipLibraryEntry[snapshots.Count];

            for (var index = 0; index < snapshots.Count; index++)
            {
                var snapshot = snapshots[index];
                entries[index] = new ShipLibraryEntry(
                    snapshot.DisplayName,
                    snapshot.FilePath,
                    snapshot.PreviewSprite);
            }

            return entries;
        }

        private void OnViewCloseClicked()
        {
            CloseClicked?.Invoke();
            GameUi.Pop();
        }

        private void OnViewLoadClicked(string snapshotPath)
        {
            SnapshotSelected?.Invoke(snapshotPath);
            GameUi.Pop();
        }

        private void OnViewDeleteClicked(string snapshotPath)
        {
            _snapshotRepository.DeleteSnapshot(snapshotPath);
            SnapshotDeleted?.Invoke(snapshotPath);
        }
    }
}