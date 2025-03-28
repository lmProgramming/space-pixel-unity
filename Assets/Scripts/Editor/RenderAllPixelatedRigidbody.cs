using Pixelation;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class RenderAllPixelatedRigidbody : EditorWindow
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        [MenuItem("Tools/Render all pixelated rigidbodies")]
        private static void RenderAllPixelated()
        {
            var allObjects = FindObjectsOfType<PixelatedRigidbody>();

            foreach (var obj in allObjects)
            {
                if (PrefabUtility.IsPartOfAnyPrefab(obj)) continue;
                obj.Setup(forceSetup: true);
            }

            Debug.Log("Rendered all pixelated rigidbodies.");
        }
    }
}