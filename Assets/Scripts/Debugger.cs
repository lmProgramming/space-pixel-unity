using Other;
using UnityEngine;

public class Debugger : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            var gameObjectUnderPointer = GameInput.ObjectUnderPointer;

            var pixelated = gameObjectUnderPointer?.GetComponent<IPixelated>();

            if (pixelated is null) return;

            var pixelPoint = pixelated.WorldToLocalPoint(GameInput.WorldPointerPosition);

            pixelated.RemovePixelAt(pixelPoint);
        }

        if (Input.GetKeyDown(KeyCode.Home)) Debug.Log(GameInput.WorldPointerPosition);
    }
}