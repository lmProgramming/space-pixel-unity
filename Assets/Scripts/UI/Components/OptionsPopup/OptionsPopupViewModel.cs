using System;
using System.Collections.Generic;
using Core.UI;

namespace UI.Components.OptionsPopup
{
    public class OptionsPopupViewModel
    {
        public OptionsPopupViewModel(string title, string description, IReadOnlyList<OptionsPopupOption> options)
        {
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public string Title { get; }
        public string Description { get; }
        public IReadOnlyList<OptionsPopupOption> Options { get; }
    }
}