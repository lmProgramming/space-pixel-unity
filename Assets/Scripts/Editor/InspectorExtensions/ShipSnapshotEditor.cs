using System;
using System.IO;
using System.Linq;
using Ships;
using Ships.Serialization;
using UnityEditor;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Editor.InspectorExtensions
{
    [CustomEditor(typeof(Ship), true)]
    public class ShipSnapshotEditor : UnityEditor.Editor
    {
        private const string DefaultSaveFolder = "Assets/ShipSnapshots";

        private IShipSnapshotService CreateSnapshotService()
        {
            if (Application.isPlaying)
            {
                var sceneContext = FindAnyObjectByType<SceneContext>();
                if (sceneContext != null)
                    return new ShipSnapshotService(sceneContext.Container);
            }

            return new ShipSnapshotService();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Ship Snapshot", EditorStyles.boldLabel);

            var ship = (Ship)target;

            if (GUILayout.Button("Capture Snapshot to JSON")) CaptureAndSaveSnapshot(ship);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            if (GUILayout.Button("Load Snapshot from JSON")) LoadSnapshotOntoShip(ship);
            EditorGUI.EndDisabledGroup();

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Loading snapshots is only available in Play Mode.", MessageType.Info);

            EditorGUILayout.Space(5);

            if (!GUILayout.Button("Open Snapshots Folder")) return;

            EnsureSnapshotFolderExists();
            EditorUtility.RevealInFinder(DefaultSaveFolder);
        }

        private void CaptureAndSaveSnapshot(Ship ship)
        {
            var snapshotService = CreateSnapshotService();
            var snapshot = snapshotService.CaptureSnapshot(ship);
            if (snapshot == null)
            {
                Debug.LogError("[ShipSnapshotEditor] Failed to capture snapshot");
                return;
            }

            var json = snapshotService.ToJson(snapshot);

            EnsureSnapshotFolderExists();

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var sanitizedName = SanitizeFileName(ship.name);
            var filename = $"{sanitizedName}_{timestamp}.json";
            var fullPath = Path.Combine(DefaultSaveFolder, filename);

            File.WriteAllText(fullPath, json);
            AssetDatabase.Refresh();

            Debug.Log($"[ShipSnapshotEditor] Snapshot saved to: {fullPath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(fullPath));
        }

        private void LoadSnapshotOntoShip(Ship ship)
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[ShipSnapshotEditor] Loading snapshots is only supported in Play Mode.");
                return;
            }

            var path = EditorUtility.OpenFilePanel("Load Ship Snapshot", DefaultSaveFolder, "json");

            if (string.IsNullOrEmpty(path)) return;

            var json = File.ReadAllText(path);
            var snapshotService = CreateSnapshotService();
            var snapshot = snapshotService.FromJson(json);

            if (snapshot == null)
            {
                Debug.LogError("[ShipSnapshotEditor] Failed to parse snapshot JSON");
                return;
            }

            snapshotService.ApplySnapshot(ship, snapshot);
        }

        private static void EnsureSnapshotFolderExists()
        {
            if (!Directory.Exists(DefaultSaveFolder)) Directory.CreateDirectory(DefaultSaveFolder);
        }

        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return invalidChars.Aggregate(name, (current, c) => current.Replace(c, '_'));
        }
    }
}