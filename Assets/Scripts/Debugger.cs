using Other;
using Pixelation;
using UnityEngine;

public class Debugger : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKey(KeyCode.Delete))
        {
            var gameObjectUnderPointer = GameInput.ObjectUnderPointer;

            var pixelated = gameObjectUnderPointer?.GetComponent<PixelatedRigidbody>();

            if (pixelated is null) return;

            var pixelPoint = pixelated.WorldToLocalPoint(GameInput.WorldPointerPosition);

            pixelated.RemovePixelAt(pixelPoint);
        }

        if (Input.GetKeyDown(KeyCode.Home)) Debug.Log(GameInput.WorldPointerPosition);
    }
}