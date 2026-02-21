using System;
using Pixelation;
using Ships.Modules;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ZLinq;

namespace Editor.ToolsExtensions
{
    public class ComponentSorterEditor : EditorWindow
    {
        private static readonly Type[] PriorityOrder =
        {
            typeof(Transform),
            typeof(Module),
            typeof(PixelatedRigidbody),
            typeof(Rigidbody),
            typeof(Rigidbody2D),
            typeof(Collider),
            typeof(Collider2D),
            typeof(Renderer),
            typeof(Camera),
            typeof(Light),
            typeof(AudioSource),
            typeof(Animator),
            typeof(MonoBehaviour)
        };

        [MenuItem("Tools/Sort Components in All GameObjects")]
        private static void SortAllComponents()
        {
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            var sortedCount = 0;

            foreach (var obj in allObjects)
            {
                if (PrefabUtility.IsPartOfAnyPrefab(obj)) continue;
                if (SortComponents(obj)) sortedCount++;
            }

            Debug.Log($"Components sorted for {sortedCount} GameObjects.");
        }

        private static bool SortComponents(GameObject obj)
        {
            var components = obj.GetComponents<Component>().AsValueEnumerable()
                .Where(c => c != null)
                .ToList();

            if (components.Count <= 1) return false;

            var desiredOrder = components.AsValueEnumerable()
                .OrderBy(GetComponentPriority)
                .ThenBy(c => c.GetType().Name)
                .ToList();

            var changed = false;

            for (var targetIndex = 0; targetIndex < desiredOrder.Count; targetIndex++)
            {
                var component = desiredOrder[targetIndex];

                var currentComponents =
                    obj.GetComponents<Component>().AsValueEnumerable().Where(c => c != null).ToList();
                var currentIndex = currentComponents.IndexOf(component);

                while (currentIndex > targetIndex)
                {
                    ComponentUtility.MoveComponentUp(component);
                    currentIndex--;
                    changed = true;
                }
            }

            if (changed) Debug.Log($"Sorted components on {obj.name}");

            return changed;
        }

        private static int GetComponentPriority(Component component)
        {
            var componentType = component.GetType();

            for (var i = 0; i < PriorityOrder.Length; i++)
                if (PriorityOrder[i].IsAssignableFrom(componentType))
                    return i;

            return PriorityOrder.Length;
        }
    }
}