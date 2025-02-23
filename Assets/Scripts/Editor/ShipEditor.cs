using Pixelation;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(PixelatedRigidbody), true)]
    public class ShipEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var pixelatedRigidbody = (PixelatedRigidbody)target;
            DrawDefaultInspector();
            if (!GUILayout.Button("Generate Pixels")) return;
            pixelatedRigidbody.Setup();
        }
    }
}