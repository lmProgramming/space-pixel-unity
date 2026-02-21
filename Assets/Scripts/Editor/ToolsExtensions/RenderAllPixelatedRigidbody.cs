using Pixelation;
using UnityEditor;
using UnityEngine;

namespace Editor.ToolsExtensions
{
    public class RenderAllPixelatedRigidbody : EditorWindow
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        [MenuItem("Tools/Render all pixelated rigidbodies")]
        private static void RenderAllPixelated()
        {
            var allObjects =
                FindObjectsByType<PixelatedRigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var obj in allObjects)
            {
                if (PrefabUtility.IsPartOfAnyPrefab(obj)) continue;
                obj.Setup(forceSetup: true);
            }

            Debug.Log("Rendered all pixelated rigidbodies.");
        }
    }
}