using Ships.Modules;
using UnityEditor;
using UnityEngine;

namespace Editor.InspectorExtensions
{
    [CustomEditor(typeof(Module), true)] [CanEditMultipleObjects]
    public class ModuleEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying) return;
            if (EditorUtility.IsPersistent(target)) return;

            foreach (var obj in targets)
            {
                if (obj is not Module module) continue;
                var efficiency = module.InternalEfficiency;
                EditorGUILayout.LabelField($"Efficiency: {efficiency:P1}");
            }
        }
    }
}