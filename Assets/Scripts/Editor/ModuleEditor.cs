using Ships.Modules;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(Module), true)]
    public class ModuleEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying) return;

            var module = (Module)target;
            var efficiency = module.InternalEfficiency;

            var efficiencyText = $"Efficiency: {efficiency:P1}";

            EditorGUILayout.LabelField(efficiencyText);
        }
    }
}