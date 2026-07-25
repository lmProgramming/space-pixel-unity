using System;
using System.Collections.Generic;
using Core.UI;
using UI.Common;
using UnityEngine.UIElements;

namespace UI.Components.Notification
{
    public class NotificationView : PanelRendererBase
    {
        private readonly List<(string message, PopupLevel level)> _pendingNotifications = new();
        private VisualElement _notificationContainer;

        protected override void BindUiCore(VisualElement root)
        {
            _notificationContainer = root.Q<VisualElement>("notification-container");
            if (_notificationContainer == null)
                throw new InvalidOperationException("[NotificationView] notification-container is missing in UXML.");

            foreach (var (message, level) in _pendingNotifications)
                AddNotification(message, level);

            _pendingNotifications.Clear();
        }

        protected override void UnbindUiCore()
        {
            _notificationContainer = null;
        }

        public void Show(string message, PopupLevel level = PopupLevel.Info)
        {
            if (_notificationContainer == null)
            {
                _pendingNotifications.Add((message, level));
                return;
            }

            AddNotification(message, level);
        }

        private void AddNotification(string message, PopupLevel level)
        {
            var popup = new NotificationPopup(message, level);
            _notificationContainer.Add(popup);
        }
    }
}