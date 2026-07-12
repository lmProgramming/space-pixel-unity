using UI;
using UI.Elements;
using UnityEngine.UIElements;

namespace ShipFactory.UI.Views.Notification
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