using Pixelation;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class RenderAllPixelatedRigidbody : EditorWindow
    {
        [MenuItem("Tools/Render all pixelated rigidbodies")]
        private static void RenderAllPixelated()
        {
            var allObjects = FindObjectsByType<PixelatedRigidbody>(FindObjectsSortMode.None);

            foreach (var obj in allObjects)
            {
                if (PrefabUtility.IsPartOfAnyPrefab(obj)) continue;
                obj.Setup(forceSetup: true);
            }

            Debug.Log("Components sorted for all GameObjects.");
        }
    }
}