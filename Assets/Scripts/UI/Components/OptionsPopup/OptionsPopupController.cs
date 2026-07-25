using System;
using System.Collections.Generic;
using Core.UI;
using UI.MVCVM;
using UnityEngine;

namespace UI.Components.OptionsPopup
{
    [RequireComponent(typeof(OptionsPopupView))]
    public class OptionsPopupController : Controller<OptionsPopupModel, OptionsPopupView, OptionsPopupViewModel>
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            View.CloseClicked += OnCloseClicked;
            View.OptionClicked += OnOptionClicked;
        }

        protected override void OnDisable()
        {
            View.CloseClicked -= OnCloseClicked;
            View.OptionClicked -= OnOptionClicked;
            base.OnDisable();
        }

        public event Action Closed;
        public event Action<string> OptionSelected;

        public void Show(string title, string description, params OptionsPopupOption[] options)
        {
            Show(title, description, (IReadOnlyList<OptionsPopupOption>)options);
        }

        public void Show(string title, string description, IReadOnlyList<OptionsPopupOption> options)
        {
            Model.Configure(title, description, options);
            View.Show();
        }

        public void Close()
        {
            if (GameUi == null)
                throw new InvalidOperationException("[OptionsPopupController] IGameUi is not injected.");

            GameUi.Pop();

            Closed?.Invoke();
        }

        protected override OptionsPopupModel CreateModel()
        {
            return new OptionsPopupModel();
        }

        protected override OptionsPopupViewModel CreateViewModel(OptionsPopupModel model)
        {
            return new OptionsPopupViewModel(model.Title, model.Description, model.Options);
        }

        private void OnCloseClicked()
        {
            Close();
        }

        private void OnOptionClicked(string optionId)
        {
            OptionSelected?.Invoke(optionId);
            Close();
        }
    }
}