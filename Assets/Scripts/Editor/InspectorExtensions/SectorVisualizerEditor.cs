using Editor.Standalone;
using UnityEditor;
using UnityEngine;

namespace Editor.InspectorExtensions
{
    [CustomEditor(typeof(SectorVisualizer))]
    public class SectorVisualizerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var visualizer = (SectorVisualizer)target;

            EditorGUILayout.Space();

            if (GUILayout.Button("Recalculate & Show 10×10 Sector Grid", GUILayout.Height(28)))
            {
                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Enter Play Mode first — physics queries require the game to be running.",
                        MessageType.Warning);
                }
                else
                {
                    Undo.RecordObject(visualizer, "Recalculate Sector Grid");
                    visualizer.RecalculateSectorGrid();
                    EditorUtility.SetDirty(visualizer);
                    SceneView.RepaintAll();
                }
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode first — physics queries require the game to be running.",
                    MessageType.Warning);
        }
    }
}