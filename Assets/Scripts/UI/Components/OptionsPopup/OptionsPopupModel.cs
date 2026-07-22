using System;
using System.Collections.Generic;
using Core.MVCVM;
using Core.UI;

namespace UI.Components.OptionsPopup
{
    public class OptionsPopupModel : ObservableModel
    {
        public string Title { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public IReadOnlyList<OptionsPopupOption> Options { get; private set; } = Array.Empty<OptionsPopupOption>();

        public void Configure(string title, string description, IReadOnlyList<OptionsPopupOption> options)
        {
            if (options == null || options.Count == 0)
                throw new ArgumentException("At least one option is required.", nameof(options));

            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Options = options;
            NotifyChanged();
        }
    }
}