using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class ComponentSorterEditor : EditorWindow
    {
        [MenuItem("Tools/Sort Components in All GameObjects")]
        private static void SortAllComponents()
        {
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (var obj in allObjects)
            {
                if (PrefabUtility.IsPartOfAnyPrefab(obj)) continue;
                SortComponents(obj);
            }

            Debug.Log("Components sorted for all GameObjects.");
        }

        private static void SortComponents(GameObject obj)
        {
            var components = obj.GetComponents<Component>();

            if (components.Length <= 1) return;

            Type[] priorityOrder =
            {
                typeof(MonoBehaviour),
                typeof(Transform),
                typeof(Rigidbody),
                typeof(Collider),
                typeof(Renderer),
                typeof(Camera),
                typeof(Light),
                typeof(AudioSource),
                typeof(Animator)
            };

            var sortedComponents = components
                .Where(c => c is not Transform)
                .OrderBy(c => GetComponentPriority(c, priorityOrder))
                .ThenBy(c => c.GetType().Name)
                .ToArray();

            foreach (var c in sortedComponents) DestroyImmediate(c);

            foreach (var c in sortedComponents) obj.AddComponent(c.GetType());

            Debug.Log($"Sorted components on {obj.name}");
        }

        private static int GetComponentPriority(Component component, Type[] priorityOrder)
        {
            for (var i = 0; i < priorityOrder.Length; i++)
                if (priorityOrder[i].IsAssignableFrom(component.GetType()))
                    return i;
            return priorityOrder.Length;
        }
    }
}