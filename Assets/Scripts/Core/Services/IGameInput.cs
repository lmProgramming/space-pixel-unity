using UnityEngine;

namespace Core.Services
{
    public interface IGameInput
    {
        Vector2 WorldPointerPosition { get; }
        bool IsPointerOverUI { get; }
        bool IsTextInputFocused { get; }
        bool IsPaused { get; }
        GameObject ObjectUnderPointer { get; }

        // Gameplay intent gates: the input system owns the rules for when the player
        // is allowed to act, so gameplay code does not need to know about pause/UI state.
        bool CanControlShip { get; }
        bool CanFireWeapons { get; }

        bool LeftDoubleClick { get; }
        bool PressingAfterLeftDoubleClick { get; }
        float PressingTime { get; }
        float SimDeltaTime { get; }

        bool JustClickedOutsideUI { get; }
        bool JustStoppedClickingOutsideUI { get; }

        Vector2 CenteredScreenPointerPosition { get; }
        float TouchesAndPointersCount { get; }

        int HeldUiElementCount { get; }
        void StartHoldingUIElement();
        void StopHoldingUIElement();
    }
}
