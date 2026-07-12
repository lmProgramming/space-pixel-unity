using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UI.MVCVM;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShipFactory.UI.Views.ShipLibrary
{
    public class ShipLibraryView
        : View<ShipLibraryViewModel>
    {
        private readonly List<VisualElement> _entryCards = new();
        private readonly List<(VisualElement element, EventCallback<ClickEvent> callback)> _entryClickCallbacks = new();

        private Button _closeButton;
        private Button _deleteButton;
        private VisualElement _grid;
        private Button _loadButton;
        private int? _selectedSnapshotIndex;
        private Label _statusLabel;
        private ShipLibraryViewModel _viewModel;

        [CanBeNull]
        private ShipLibraryEntry SelectedEntry =>
            _selectedSnapshotIndex.HasValue && _selectedSnapshotIndex < _viewModel.Entries.Count &&
            _selectedSnapshotIndex >= 0
                ? _viewModel.Entries[_selectedSnapshotIndex.Value]
                : null;

        public event Action CloseClicked;

        public event Action<string> LoadClicked;

        public event Action<string> DeleteClicked;

        public override void BindUI(
            VisualElement root)
        {
            _closeButton = root.Q<Button>("ship-library-close-button");
            _loadButton = root.Q<Button>("ship-library-load-button");
            _deleteButton = root.Q<Button>("ship-library-delete-button");
            _statusLabel = root.Q<Label>("ship-library-status-label");
            _grid = root.Q<VisualElement>("ship-library-grid");

            if (_closeButton == null || _loadButton == null || _deleteButton == null || _statusLabel == null ||
                _grid == null)
                throw new InvalidOperationException("[ShipLibraryView] Required controls are missing in UXML.");

            _closeButton.clicked += OnCloseClicked;
            _loadButton.clicked += OnLoadClicked;
            _deleteButton.clicked += OnDeleteClicked;
            Render();
        }

        public override void UnbindUI()
        {
            if (_closeButton != null)
                _closeButton.clicked -= OnCloseClicked;
            if (_loadButton != null)
                _loadButton.clicked -= OnLoadClicked;
            if (_deleteButton != null)
                _deleteButton.clicked -= OnDeleteClicked;

            ClearEntryCards();

            _closeButton = null;
            _loadButton = null;
            _deleteButton = null;
            _statusLabel = null;
            _grid = null;
        }

        public override void SetData(
            ShipLibraryViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _selectedSnapshotIndex = null;
            Render();
        }

        private void Render()
        {
            if (_viewModel == null || _statusLabel == null || _grid == null)
                return;

            ClearEntryCards();

            var entries = _viewModel.Entries;
            for (var index = 0; index < entries.Count; index++)
                AddEntryCard(entries[index], index);

            _statusLabel.text = entries.Count switch
            {
                0 => "No saved ships yet.",
                1 => "1 saved ship.",
                _ => $"{entries.Count} saved ships."
            };

            UpdateSelectionVisuals();
        }

        private void AddEntryCard(
            ShipLibraryEntry entry,
            int index)
        {
            var card = new VisualElement();
            card.AddToClassList("ds-grid__item");
            card.AddToClassList("ds-item-card");

            var image = new VisualElement();
            image.AddToClassList("ds-item-card__image");
            if (entry.PreviewSprite)
            {
                var spriteImage = new Image { sprite = entry.PreviewSprite, scaleMode = ScaleMode.ScaleToFit };
                image.Add(spriteImage);
            }

            var titleRow = new VisualElement();
            titleRow.AddToClassList("ds-item-card__title-row");

            var title = new Label(entry.DisplayName);
            title.AddToClassList("ds-item-card__title");

            titleRow.Add(title);
            card.Add(image);
            card.Add(titleRow);

            EventCallback<ClickEvent> clickHandler = _ => SelectSnapshot(index);
            card.RegisterCallback(clickHandler);
            _entryClickCallbacks.Add((card, clickHandler));
            _entryCards.Add(card);
            _grid.Add(card);
        }

        private void ClearEntryCards()
        {
            foreach (var (element, callback) in _entryClickCallbacks)
                element.UnregisterCallback(callback);

            _entryClickCallbacks.Clear();
            _entryCards.Clear();
            _grid?.Clear();
        }

        private void SelectSnapshot(
            int snapshotIndex)
        {
            _selectedSnapshotIndex = snapshotIndex;
            UpdateSelectionVisuals();
        }

        private void UpdateSelectionVisuals()
        {
            for (var index = 0; index < _entryCards.Count; index++)
                _entryCards[index].EnableInClassList("is-selected", index == _selectedSnapshotIndex);

            _loadButton.SetEnabled(_selectedSnapshotIndex.HasValue);
            _deleteButton.SetEnabled(_selectedSnapshotIndex.HasValue);
        }

        private void OnCloseClicked()
        {
            CloseClicked?.Invoke();
        }

        private void OnLoadClicked()
        {
            var selectedEntry = SelectedEntry;
            if (selectedEntry == null)
                return;

            LoadClicked?.Invoke(selectedEntry.FilePath);
        }

        private void OnDeleteClicked()
        {
            var selectedEntry = SelectedEntry;
            if (selectedEntry == null)
                return;

            DeleteClicked?.Invoke(selectedEntry.FilePath);
        }
    }
}