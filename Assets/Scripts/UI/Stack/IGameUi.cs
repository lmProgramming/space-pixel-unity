using System;
using UI.Components.Notification;
using UI.Components.OptionsPopup;
using UnityEngine;

namespace UI.Stack
{
    public interface IGameUi
    {
        int Depth { get; }

        event Action DepthChanged;

        void SetRoot(Component root);

        T PushById<T>(string panelId) where T : Component;

        T Push<T>(GameObject prefab) where T : Component;

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