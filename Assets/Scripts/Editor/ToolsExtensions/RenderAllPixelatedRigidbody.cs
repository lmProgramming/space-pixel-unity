using Pixelation;
using UnityEditor;
using UnityEngine;

namespace Editor.ToolsExtensions
{
    public class RenderAllPixelatedRigidbody : EditorWindow
    {
        [MenuItem("Tools/Render all pixelated rigidbodies")]
        private static void RenderAllPixelated()
        {
            var allObjects =
                FindObjectsByType<PixelatedRigidbody>(FindObjectsInactive.Exclude);

            foreach (var obj in allObjects)
            {
                if (PrefabUtility.IsPartOfAnyPrefab(obj)) continue;
                obj.Setup(forceSetup: true);
            }

            Debug.Log("Rendered all pixelated rigidbodies.");
        }
    }
}