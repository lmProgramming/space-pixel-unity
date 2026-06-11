using System.IO;
using Pixelation;
using Ships.Modules;
using UnityEditor;
using UnityEngine;

namespace Editor.Windows
{
    public class ModulePrefabBuilder : EditorWindow
    {
        [MenuItem("Tools/Generate Module Prefabs")]
        public static void BuildPrefabs()
        {
            const string spriteDir = "Assets/Sprites/Generated";
            const string prefabDir = "Assets/Prefabs/Gameplay/Modules";

            if (!Directory.Exists(spriteDir))
            {
                Debug.LogError($"Sprite directory '{spriteDir}' not found!");
                return;
            }

            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);

            var allGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { spriteDir });

            foreach (var guid in allGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                var needsReimport = false;

                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    needsReimport = true;
                }

                if (importer.filterMode != FilterMode.Point)
                {
                    importer.filterMode = FilterMode.Point;
                    needsReimport = true;
                }

                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    needsReimport = true;
                }

                if (!Mathf.Approximately(importer.spritePixelsPerUnit, 1f))
                {
                    importer.spritePixelsPerUnit = 1f;
                    needsReimport = true;
                }

                if (needsReimport) importer.SaveAndReimport();
            }

            var prefabsCreated = 0;
            foreach (var guid in allGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (path.Contains("_armor")) continue;

                var visualSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (visualSprite == null) continue;

                var baseName = Path.GetFileNameWithoutExtension(path);

                var armorPath = path.Replace(".png", "_armor.png");
                var armorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(armorPath);

                var go = new GameObject(baseName);

                var moduleClass = typeof(Basic);
                var baseNameLower = moduleClass.Name.ToLower();
                if (baseNameLower.Contains("command") || baseNameLower.Contains("control"))
                    moduleClass = typeof(Command);
                else if (baseNameLower.Contains("engine") || baseNameLower.Contains("propulsion"))
                    moduleClass = typeof(Engine);
                else if (baseNameLower.Contains("weapon") || baseNameLower.Contains("gun") ||
                         baseNameLower.Contains("cannon"))
                    moduleClass = typeof(Cannon);
                else if (baseNameLower.Contains("laser"))
                    moduleClass = typeof(LaserBeam);

                go.AddComponent(moduleClass);
                var pixelatedRigidbody = go.GetComponent<PixelatedRigidbody>();
                pixelatedRigidbody.SetSprites(visualSprite, armorSprite);

                var prefabPath = $"{prefabDir}/{baseName}.prefab";

                pixelatedRigidbody.Setup(forceSetup: true, recalculateColliders: true);

                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = visualSprite;

                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);

                DestroyImmediate(go);
                prefabsCreated++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"Success! {prefabsCreated} Prefabs generated in {prefabDir}");
        }
    }
}