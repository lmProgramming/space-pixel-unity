using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Components
{
    [UxmlElement]
    public partial class ShipPickerRow : VisualElement
    {
        private const string TemplateResourcePath = "UI/ShipPickerRow";

        private EventCallback<ClickEvent> _clickHandler;

        public static ShipPickerRow Create()
        {
            var asset = Resources.Load<VisualTreeAsset>(TemplateResourcePath);
            if (!asset)
                throw new InvalidOperationException(
                    $"[ShipPickerRow] VisualTreeAsset '{TemplateResourcePath}' was not found in Resources.");

            return asset.Instantiate().Q<ShipPickerRow>()
                   ?? throw new InvalidOperationException(
                       "[ShipPickerRow] ShipPickerRow root is missing in ShipPickerRow.uxml.");
        }

        public void Bind(Sprite previewSprite, string displayName, Action onClicked)
        {
            Unbind();

            var icon = this.Q<Image>("ship-row-icon")
                       ?? throw new InvalidOperationException(
                           "[ShipPickerRow] 'ship-row-icon' is missing in ShipPickerRow.uxml.");
            var label = this.Q<Label>("ship-row-label")
                        ?? throw new InvalidOperationException(
                            "[ShipPickerRow] 'ship-row-label' is missing in ShipPickerRow.uxml.");

            if (previewSprite)
            {
                icon.sprite = previewSprite;
                icon.scaleMode = ScaleMode.ScaleToFit;
            }
            else
            {
                var thumb = this.Q("ship-row-thumb");
                if (thumb != null)
                    thumb.visible = false;
            }

            label.text = displayName;

            _clickHandler = _ => onClicked?.Invoke();
            RegisterCallback(_clickHandler);
        }

        public void Unbind()
        {
            if (_clickHandler == null)
                return;

            UnregisterCallback(_clickHandler);
            _clickHandler = null;
        }

        public void SetSelected(bool selected)
        {
            EnableInClassList("is-selected", selected);
        }
    }
}