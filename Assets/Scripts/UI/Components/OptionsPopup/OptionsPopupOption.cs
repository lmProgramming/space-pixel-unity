using System;

namespace UI.Components.OptionsPopup
{
    public readonly struct OptionsPopupOption
    {
        public OptionsPopupOption(string id, string label,
            OptionsPopupOptionStyle style = OptionsPopupOptionStyle.Secondary)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Option id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Option label is required.", nameof(label));

            Id = id;
            Label = label;
            Style = style;
        }

        public string Id { get; }
        public string Label { get; }
        public OptionsPopupOptionStyle Style { get; }
    }
}