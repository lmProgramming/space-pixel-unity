using System;
using Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Components.Notification
{
    [UxmlElement]
    public partial class NotificationPopup : VisualElement
    {
        private const string TemplateResourcePath = "UI/NotificationPopup";
        private const string ToastInfoClassName = "ds-toast--info";
        private const string ToastWarningClassName = "ds-toast--warning";
        private const string ToastDangerClassName = "ds-toast--danger";

        private const int CharacterTimeMs = 1;
        private const int DefaultTimeMs = 3000;

        private VisualElement PopupIcon => this.Q("action-popup-icon");
        private Label ActionPopupLabel => this.Q<Label>("action-popup-label");
        private VisualElement ActionPopup => this.Q<VisualElement>("action-popup");

        public static NotificationPopup Create(string message, PopupLevel level = PopupLevel.Info)
        {
            var asset = Resources.Load<VisualTreeAsset>(TemplateResourcePath);
            if (!asset)
                throw new InvalidOperationException(
                    $"[NotificationPopup] VisualTreeAsset '{TemplateResourcePath}' was not found in Resources.");

            var popup = asset.Instantiate().Q<NotificationPopup>()
                        ?? throw new InvalidOperationException(
                            "[NotificationPopup] NotificationPopup root is missing in NotificationPopup.uxml.");

            popup.Configure(message, level);
            return popup;
        }

        private void Configure(string message, PopupLevel level = PopupLevel.Info)
        {
            switch (level)
            {
                case PopupLevel.Warning:
                    ActionPopup.AddToClassList(ToastWarningClassName);
                    PopupIcon.AddToClassList("ds-icon--warning");
                    break;
                case PopupLevel.Error:
                    ActionPopup.AddToClassList(ToastDangerClassName);
                    PopupIcon.AddToClassList("ds-icon--error");
                    break;
                case PopupLevel.Info:
                    ActionPopup.AddToClassList(ToastInfoClassName);
                    PopupIcon.AddToClassList("ds-icon--info");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }

            ActionPopupLabel.text = message;

            ActionPopup.schedule.Execute(() => { ActionPopup.RemoveFromHierarchy(); })
                .StartingIn(DefaultTimeMs + CharacterTimeMs * message.Length);
        }
    }
}