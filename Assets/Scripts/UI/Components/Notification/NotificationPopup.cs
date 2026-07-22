using System;
using Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

// Define the custom control type.
namespace UI.Components.Notification
{
    [UxmlElement]
    public partial class NotificationPopup : VisualElement
    {
        private const string ToastInfoClassName = "ds-toast--info";
        private const string ToastWarningClassName = "ds-toast--warning";
        private const string ToastDangerClassName = "ds-toast--danger";

        private const int CharacterTimeMs = 1;
        private const int DefaultTimeMs = 3000;

        // Custom controls need a default constructor. This default constructor 
        // calls the other constructor in this class.
        // ReSharper disable once MemberCanBePrivate.Global
        public NotificationPopup()
        {
        }

        // Define a constructor that loads the UXML document that defines 
        // the hierarchy of CardElement and assigns an image and badge values.
        public NotificationPopup(string message, PopupLevel level = PopupLevel.Info)
        {
            // It assumes the UXML file is called "CardElement.uxml" and 
            // is placed at the "Resources" folder.
            var asset = Resources.Load<VisualTreeAsset>("UI/NotificationPopup");
            asset.CloneTree(this);

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

        private VisualElement PopupIcon => this.Q("action-popup-icon");
        private Label ActionPopupLabel => this.Q<Label>("action-popup-label");
        private VisualElement ActionPopup => this.Q<VisualElement>("action-popup");
    }
}