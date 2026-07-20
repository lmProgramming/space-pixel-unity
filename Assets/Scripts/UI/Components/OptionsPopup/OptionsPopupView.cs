using System;
using System.Collections.Generic;
using UI.MVCVM;
using UnityEngine.UIElements;

namespace UI.Components.OptionsPopup
{
    public class OptionsPopupView : View<OptionsPopupViewModel>
    {
        private readonly List<(Button button, EventCallback<ClickEvent> callback)> _optionCallbacks = new();

        private VisualElement _actions;
        private VisualElement _backdrop;
        private Button _closeButton;
        private Label _descriptionLabel;
        private Label _titleLabel;
        private OptionsPopupViewModel _viewModel;

        public event Action CloseClicked;
        public event Action<string> OptionClicked;

        public override void BindUI(VisualElement root)
        {
            _backdrop = root.Q<VisualElement>("options-popup-backdrop");
            _titleLabel = root.Q<Label>("options-popup-title");
            _descriptionLabel = root.Q<Label>("options-popup-description");
            _actions = root.Q<VisualElement>("options-popup-actions");
            _closeButton = root.Q<Button>("options-popup-close-button");

            if (_backdrop == null || _titleLabel == null || _descriptionLabel == null || _actions == null ||
                _closeButton == null)
                throw new InvalidOperationException("[OptionsPopupView] Required controls are missing in UXML.");

            _closeButton.clicked += OnCloseClicked;
            _backdrop.RegisterCallback<ClickEvent>(OnBackdropClicked);
            Render();
        }

        public override void UnbindUI()
        {
            if (_closeButton != null)
                _closeButton.clicked -= OnCloseClicked;
            _backdrop?.UnregisterCallback<ClickEvent>(OnBackdropClicked);
            ClearOptionButtons();
        }

        public override void SetData(OptionsPopupViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            Render();
        }

        private void Render()
        {
            if (_viewModel == null || _titleLabel == null || _descriptionLabel == null || _actions == null)
                return;

            _titleLabel.text = _viewModel.Title;
            _descriptionLabel.text = _viewModel.Description;
            RebuildOptionButtons(_viewModel.Options);
        }

        private void RebuildOptionButtons(IReadOnlyList<OptionsPopupOption> options)
        {
            ClearOptionButtons();

            foreach (var option in options)
            {
                var button = new Button { text = option.Label };
                button.AddToClassList("ds-btn");
                button.AddToClassList(GetStyleClass(option.Style));

                var optionId = option.Id;
                EventCallback<ClickEvent> callback = _ => OptionClicked?.Invoke(optionId);
                button.RegisterCallback(callback);
                _optionCallbacks.Add((button, callback));
                _actions.Add(button);
            }
        }

        private void ClearOptionButtons()
        {
            foreach (var (button, callback) in _optionCallbacks)
                button.UnregisterCallback(callback);

            _optionCallbacks.Clear();
            _actions?.Clear();
        }

        private static string GetStyleClass(OptionsPopupOptionStyle style)
        {
            return style switch
            {
                OptionsPopupOptionStyle.Primary => "ds-btn--primary",
                OptionsPopupOptionStyle.Secondary => "ds-btn--secondary",
                OptionsPopupOptionStyle.Ghost => "ds-btn--ghost",
                OptionsPopupOptionStyle.Danger => "ds-btn--danger",
                _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
            };
        }

        private void OnCloseClicked()
        {
            CloseClicked?.Invoke();
        }

        private void OnBackdropClicked(ClickEvent evt)
        {
            if (evt.target != _backdrop)
                return;

            CloseClicked?.Invoke();
        }
    }
}