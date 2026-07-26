using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Components.OptionsPopup
{
    [UxmlElement]
    public partial class OptionsPopupOptionButton : Button
    {
        private const string TemplateResourcePath = "UI/OptionsPopupOptionButton";

        private EventCallback<ClickEvent> _clickHandler;

        public static OptionsPopupOptionButton Create(string label, string styleClass, Action onClick)
        {
            var asset = Resources.Load<VisualTreeAsset>(TemplateResourcePath);
            if (!asset)
                throw new InvalidOperationException(
                    $"[OptionsPopupOptionButton] VisualTreeAsset '{TemplateResourcePath}' was not found in Resources.");

            var button = asset.Instantiate().Q<OptionsPopupOptionButton>()
                         ?? throw new InvalidOperationException(
                             "[OptionsPopupOptionButton] OptionsPopupOptionButton root is missing in UXML.");

            button.Configure(label, styleClass, onClick);
            return button;
        }

        private void Configure(string label, string styleClass, Action onClick)
        {
            Unbind();

            text = label;
            AddToClassList(styleClass);

            _clickHandler = _ => onClick?.Invoke();
            RegisterCallback(_clickHandler);
        }

        public void Unbind()
        {
            if (_clickHandler == null)
                return;

            UnregisterCallback(_clickHandler);
            _clickHandler = null;
        }
    }
}