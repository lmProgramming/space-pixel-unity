using UI.Common;
using UnityEngine.UIElements;

namespace UI.Components.Notification
{
    public class NotificationView : PanelRendererBase
    {
        private VisualElement _notificationContainer;

        protected override void BindUiCore(VisualElement root)
        {
            _notificationContainer = root.Q<VisualElement>("notification-container");
        }

        protected override void UnbindUiCore()
        {
            _notificationContainer = null;
        }

        public void Show(string message, PopupLevel level = PopupLevel.Info)
        {
            var popup = new NotificationPopup(message, level);
            _notificationContainer.Add(popup);
        }
    }
}