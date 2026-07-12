using System;
using UI;
using UnityEngine.UIElements;

namespace ShipFactory.UI.Views.Notification
{
    public enum PopupLevel
    {
        Info,
        Warning,
        Error
    }

    public class NotificationView : PanelRendererBase
    {
        private const string ToastInfoClassName = "ds-toast--info";
        private const string ToastWarningClassName = "ds-toast--warning";
        private const string ToastDangerClassName = "ds-toast--danger";

        private VisualElement _actionPopup;
        private VisualElement _actionPopupIcon;
        private Label _actionPopupLabel;
        private IVisualElementScheduledItem _hideJob;

        protected override void BindUiCore(VisualElement root)
        {
            _actionPopup = root.Q<VisualElement>("action-popup");
            _actionPopupIcon = root.Q<VisualElement>("action-popup-icon");
            _actionPopupLabel = root.Q<Label>("action-popup-label");

            if (_actionPopup == null || _actionPopupIcon == null || _actionPopupLabel == null)
                throw new InvalidOperationException(
                    "[ShipFactoryNotificationPopup] Required action popup elements are missing in UXML!");
        }

        protected override void UnbindUiCore()
        {
            _actionPopup = null;
            _actionPopupIcon = null;
            _actionPopupLabel = null;
        }

        public void Show(string message, PopupLevel level = PopupLevel.Info)
        {
            _hideJob?.Pause();

            _actionPopup.RemoveFromClassList(ToastInfoClassName);
            _actionPopup.RemoveFromClassList(ToastWarningClassName);
            _actionPopup.RemoveFromClassList(ToastDangerClassName);
            _actionPopupIcon.RemoveFromClassList("ds-icon--info");
            _actionPopupIcon.RemoveFromClassList("ds-icon--warning");
            _actionPopupIcon.RemoveFromClassList("ds-icon--error");

            switch (level)
            {
                case PopupLevel.Warning:
                    _actionPopup.AddToClassList(ToastWarningClassName);
                    _actionPopupIcon.AddToClassList("ds-icon--warning");
                    break;
                case PopupLevel.Error:
                    _actionPopup.AddToClassList(ToastDangerClassName);
                    _actionPopupIcon.AddToClassList("ds-icon--error");
                    break;
                case PopupLevel.Info:
                    _actionPopup.AddToClassList(ToastInfoClassName);
                    _actionPopupIcon.AddToClassList("ds-icon--info");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }

            _actionPopupLabel.text = message;
            _actionPopup.style.display = DisplayStyle.Flex;

            _hideJob = _actionPopup.schedule.Execute(() =>
            {
                _actionPopup.style.display = DisplayStyle.None;
                _hideJob = null;
            }).StartingIn(1600);
        }
    }
}