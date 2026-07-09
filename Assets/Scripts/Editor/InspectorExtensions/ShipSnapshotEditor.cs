using System;
using System.IO;
using Core.Constants;
using Core.Services;
using Ships;
using UnityEditor;
using UnityEngine;
using Zenject;
using ZLinq;
using Object = UnityEngine.Object;

namespace Editor.InspectorExtensions
{
    [CustomEditor(typeof(Ship), true)]
    public class ShipSnapshotEditor : UnityEditor.Editor
    {
        [Inject]
        private IShipSnapshotService _shipSnapshotService;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Ship Snapshot", EditorStyles.boldLabel);

            var ship = (Ship)target;

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            if (GUILayout.Button("Capture Snapshot to JSON")) CaptureAndSaveSnapshot(ship);

            if (GUILayout.Button("Load Snapshot from JSON")) LoadSnapshotOntoShip(ship);
            EditorGUI.EndDisabledGroup();

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Loading and capturing snapshots is only available in Play Mode.",
                    MessageType.Info);

            EditorGUILayout.Space(5);

            if (!GUILayout.Button("Open Snapshots Folder")) return;

            EnsureSnapshotFolderExists();
            EditorUtility.RevealInFinder(Constants.DefaultSaveFolder);
        }

        private void CaptureAndSaveSnapshot(Ship ship)
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[ShipSnapshotEditor] Capturing snapshots is only supported in Play Mode.");
                return;
            }

            ProjectContext.Instance.Container.Inject(this);

            var snapshot = _shipSnapshotService.CaptureSnapshot(ship);
            if (snapshot == null)
            {
                Debug.LogError("[ShipSnapshotEditor] Failed to capture snapshot");
                return;
            }

            var json = JsonUtility.ToJson(snapshot, true);

            EnsureSnapshotFolderExists();

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var sanitizedName = SanitizeFileName(ship.name);
            var filename = $"{sanitizedName}_{timestamp}.json";
            var fullPath = Path.Combine(Constants.DefaultSaveFolder, filename);

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

            ProjectContext.Instance.Container.Inject(this);

            var path = EditorUtility.OpenFilePanel("Load Ship Snapshot", Constants.DefaultSaveFolder, "json");

            if (string.IsNullOrEmpty(path)) return;

            var snapshot = _shipSnapshotService.LoadSnapshotFromFile(path);

            if (snapshot == null)
            {
                Debug.LogError("[ShipSnapshotEditor] Failed to parse snapshot JSON");
                return;
            }

            _shipSnapshotService.ApplySnapshot(ship, snapshot);
            ship.InitializeModules();
        }

        private static void EnsureSnapshotFolderExists()
        {
            if (!Directory.Exists(Constants.DefaultSaveFolder)) Directory.CreateDirectory(Constants.DefaultSaveFolder);
        }

        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return invalidChars.AsValueEnumerable().Aggregate(name, (current, c) => current.Replace(c, '_'));
        }
    }
}