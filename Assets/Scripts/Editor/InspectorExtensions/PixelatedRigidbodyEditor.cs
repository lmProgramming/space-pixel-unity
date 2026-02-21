using Pixelation;
using UnityEditor;
using UnityEngine;

namespace Editor.InspectorExtensions
{
    [CustomEditor(typeof(PixelatedRigidbody), true)]
    public class PixelatedRigidbodyEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var pixelatedRigidbody = (PixelatedRigidbody)target;
            DrawDefaultInspector();
            if (!GUILayout.Button("Generate Pixels")) return;
            pixelatedRigidbody.Setup(forceSetup: true);
        }
    }
}