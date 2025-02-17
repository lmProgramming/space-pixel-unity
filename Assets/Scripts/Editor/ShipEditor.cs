using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(Ship), true)]
    public class ShipEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var ship = (Ship)target;
            DrawDefaultInspector();
            if (GUILayout.Button("Generate Pixels")) ship.RecalculateColliders();
        }
    }
}