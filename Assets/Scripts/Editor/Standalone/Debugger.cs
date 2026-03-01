using LM;
using Pixelation;
using UnityEngine;

namespace Editor.Standalone
{
    public class Debugger : MonoBehaviour
    {
#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKey(KeyCode.Delete)) HandleDelete();

            if (Input.GetKeyDown(KeyCode.Home)) Debug.Log(GameInput.WorldPointerPosition.ToString());

            if (Input.GetKeyDown(KeyCode.P)) Debug.Break();
        }

        private static void HandleDelete()
        {
            var gameObjectUnderPointer = GameInput.ObjectUnderPointer;

            var pixelated = gameObjectUnderPointer?.GetComponent<PixelatedRigidbody>();

            if (pixelated is null) return;

            var pixelPoint = pixelated.WorldToLocalPixel(GameInput.WorldPointerPosition);

            if (pixelated.PixelGrid.InBounds(pixelPoint)) pixelated.RemovePixelAt(pixelPoint);

            if (!Input.GetKey(KeyCode.RightShift)) return;

            if (pixelated.PixelGrid.InBounds(pixelPoint + Vector2Int.left))
                pixelated.RemovePixelAt(pixelPoint + Vector2Int.left);
            if (pixelated.PixelGrid.InBounds(pixelPoint + Vector2Int.right))
                pixelated.RemovePixelAt(pixelPoint + Vector2Int.right);
            if (pixelated.PixelGrid.InBounds(pixelPoint + Vector2Int.down))
                pixelated.RemovePixelAt(pixelPoint + Vector2Int.down);
            if (pixelated.PixelGrid.InBounds(pixelPoint + Vector2Int.up))
                pixelated.RemovePixelAt(pixelPoint + Vector2Int.up);
        }
#endif
    }
}