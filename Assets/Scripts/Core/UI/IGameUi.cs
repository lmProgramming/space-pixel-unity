using System;
using UnityEngine;

namespace Core.UI
{
    public interface IGameUi
    {
        int Depth { get; }

        event Action DepthChanged;

        void SetRoot(Component root);

        T PushById<T>(string panelId);

        T Push<T>(GameObject prefab);

        void Pop();

        bool TryPop();

        void ShowOptions(
            string title,
            string description,
            Action<string> optionSelected,
            params OptionsPopupOption[] options);

        void Notify(string message, PopupLevel level = PopupLevel.Info);
    }
}