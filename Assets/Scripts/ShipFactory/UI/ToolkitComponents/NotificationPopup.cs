using System;
using UnityEngine.UIElements;

namespace ShipFactory.UI.ToolkitComponents
{
    public enum PopupLevel
    {
        Info,
        Warning,
        Error
    }

    public class NotificationPopup
    {
        private const string ToastInfoClassName = "ds-toast--info";
        private const string ToastWarningClassName = "ds-toast--warning";
        private const string ToastDangerClassName = "ds-toast--danger";

        private readonly VisualElement _actionPopup;
        private readonly VisualElement _actionPopupIcon;
        private readonly Label _actionPopupLabel;

        public NotificationPopup(VisualElement root)
        {
            _actionPopup = root.Q<VisualElement>("action-popup");
            _actionPopupIcon = root.Q<VisualElement>("action-popup-icon");
            _actionPopupLabel = root.Q<Label>("action-popup-label");

            if (_actionPopup == null || _actionPopupIcon == null || _actionPopupLabel == null)
                throw new InvalidOperationException(
                    "[ShipFactoryNotificationPopup] Required action popup elements are missing in UXML!");
        }

        public void Show(string message, PopupLevel level = PopupLevel.Info)
        {
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

            _actionPopup.schedule.Execute(() => { _actionPopup.style.display = DisplayStyle.None; }).StartingIn(1600);
        }
    }
}