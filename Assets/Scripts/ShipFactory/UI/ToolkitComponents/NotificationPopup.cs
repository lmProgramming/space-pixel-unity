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
        private const string ActionPopupWarningClassName = "action-popup--warning";
        private const string ActionPopupErrorClassName = "action-popup--error";

        private readonly VisualElement _actionPopup;
        private readonly Label _actionPopupLabel;

        public NotificationPopup(VisualElement root)
        {
            _actionPopup = root.Q<VisualElement>("action-popup");
            _actionPopupLabel = root.Q<Label>("action-popup-label");

            if (_actionPopup == null || _actionPopupLabel == null)
                throw new InvalidOperationException(
                    "[ShipFactoryNotificationPopup] Required action popup elements are missing in UXML!");
        }

        public void Show(string message, PopupLevel level = PopupLevel.Info)
        {
            _actionPopup.RemoveFromClassList(ActionPopupWarningClassName);
            _actionPopup.RemoveFromClassList(ActionPopupErrorClassName);

            switch (level)
            {
                case PopupLevel.Warning:
                    _actionPopup.AddToClassList(ActionPopupWarningClassName);
                    break;
                case PopupLevel.Error:
                    _actionPopup.AddToClassList(ActionPopupErrorClassName);
                    break;
                case PopupLevel.Info:
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