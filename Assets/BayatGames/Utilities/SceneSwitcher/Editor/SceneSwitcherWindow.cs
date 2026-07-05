using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BayatGames.Utilities.SceneSwitcher.Editor
{
    /// <summary>
    ///     Scene switcher window, an editor window for switching between scenes.
    /// </summary>
    public class SceneSwitcherWindow : EditorWindow
    {
        public enum ScenesSource
        {
            Assets,
            BuildSettings
        }

        protected OpenSceneMode OpenSceneMode = OpenSceneMode.Single;
        protected ScenesSource SceneSource = ScenesSource.Assets;

        protected Vector2 ScrollPosition;
        protected int SelectedTab;

        protected string[] Tabs =
        {
            "Scenes",
            "Settings"
        };

        protected virtual void OnEnable()
        {
            SceneSource =
                (ScenesSource)EditorPrefs.GetInt("SceneSwitcher.scenesSource", (int)ScenesSource.Assets);
            OpenSceneMode = (OpenSceneMode)EditorPrefs.GetInt(
                "SceneSwitcher.openSceneMode",
                (int)OpenSceneMode.Single);
        }

        protected virtual void OnDisable()
        {
            EditorPrefs.SetInt("SceneSwitcher.scenesSource", (int)SceneSource);
            EditorPrefs.SetInt("SceneSwitcher.openSceneMode", (int)OpenSceneMode);
        }

        protected virtual void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            SelectedTab = GUILayout.Toolbar(SelectedTab, Tabs, EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();
            ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition);
            EditorGUILayout.BeginVertical();
            switch (SelectedTab)
            {
                case 0:
                    ScenesTabGUI();
                    break;
                case 1:
                    SettingsTabGUI();
                    break;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            GUILayout.Label("Made with ❤️ by Bayat Games", EditorStyles.centeredGreyMiniLabel);
        }

        [MenuItem("Tools/Scene Switcher")]
        public static void Init()
        {
            var window = GetWindow<SceneSwitcherWindow>("Scene Switcher");
            window.minSize = new Vector2(250f, 200f);
            window.Show();
        }

        protected virtual void SettingsTabGUI()
        {
            SceneSource = (ScenesSource)EditorGUILayout.EnumPopup("Scenes Source", SceneSource);
            OpenSceneMode = (OpenSceneMode)EditorGUILayout.EnumPopup("Open Scene Mode", OpenSceneMode);
        }

        protected virtual void ScenesTabGUI()
        {
            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var guids = AssetDatabase.FindAssets("t:Scene");
            if (guids.Length == 0)
            {
                GUILayout.Label("No Scenes Found", EditorStyles.centeredGreyMiniLabel);
                GUILayout.Label("Create New Scenes", EditorStyles.centeredGreyMiniLabel);
                GUILayout.Label("And Switch Between them here", EditorStyles.centeredGreyMiniLabel);
            }

            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                var buildScene = buildScenes.Find(editorBuildScene => { return editorBuildScene.path == path; });
                var scene = SceneManager.GetSceneByPath(path);
                var isOpen = scene.IsValid() && scene.isLoaded;
                EditorGUI.BeginDisabledGroup(isOpen);
                if (SceneSource == ScenesSource.Assets)
                {
                    if (GUILayout.Button(sceneAsset.name)) Open(path);
                }
                else
                {
                    if (buildScene != null)
                        if (GUILayout.Button(sceneAsset.name))
                            Open(path);
                }

                EditorGUI.EndDisabledGroup();
            }

            if (GUILayout.Button("Create New Scene"))
            {
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(newScene);
            }
        }

        public virtual void Open(string path)
        {
            if (EditorSceneManager.EnsureUntitledSceneHasBeenSaved(
                    "You don't have saved the Untitled Scene, Do you want to leave?"))
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(path, OpenSceneMode);
            }
        }
    }
}