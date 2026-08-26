using System;
using System.Collections.Generic;
using System.IO;
using Core.Ships;
using Core.Ships.Module;
using Core.Ships.Snapshots.Module;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Services
{
    public static class ShipPreviewIconCompositor
    {
        public static Sprite ComposeFromShip(IShip ship)
        {
            if (ship == null)
                throw new ArgumentNullException(nameof(ship));

            var pixelMap = new Dictionary<Vector2Int, Color32>();

            foreach (var module in ship.AllModules)
                CollectModulePixelsFromLiveModule(module, pixelMap);

            return CreateSpriteFromPixelMap(pixelMap);
        }

        public static Sprite ComposeFromSnapshot(ShipSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            var pixelMap = new Dictionary<Vector2Int, Color32>();

            foreach (var moduleSnapshot in snapshot.modules)
                CollectModulePixelsFromSnapshotModule(moduleSnapshot, pixelMap);

            return CreateSpriteFromPixelMap(pixelMap);
        }

        public static void SavePng(Texture2D texture, string path)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("PNG path is required.", nameof(path));

            var directoryPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directoryPath))
                Directory.CreateDirectory(directoryPath);

            File.WriteAllBytes(path, texture.EncodeToPNG());
        }

        public static Sprite LoadSpriteFromPng(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("PNG path is required.", nameof(path));

            if (!File.Exists(path))
                return null;

            var pngBytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point
            };

            if (!texture.LoadImage(pngBytes))
            {
                Object.Destroy(texture);
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                1f);
        }

        public static void DestroySprite(Sprite sprite)
        {
            if (!sprite)
                return;

            var texture = sprite.texture;
            Object.Destroy(sprite);

            if (texture)
                Object.Destroy(texture);
        }

        private static void CollectModulePixelsFromLiveModule(
            IModule module,
            Dictionary<Vector2Int, Color32> pixelMap)
        {
            if (module?.Transform == null)
                return;

            var pixelatedRigidbody = module.PixelatedRigidbody;
            var texturePixelGrid = pixelatedRigidbody?.TexturePixelGrid;
            if (texturePixelGrid == null)
            {
                Debug.LogWarning(
                    $"[ShipPreviewIconCompositor] Module '{module.Transform.name}' has no pixel grid. Skipping.");
                return;
            }

            // The command module sits at the layout origin with identity rotation, so raw module
            // locals equal the layout-space coordinates snapshots store. Reading them directly
            // keeps rotated modules rotated instead of normalizing them against themselves.
            var moduleTransform = module.Transform;
            var dimensions = texturePixelGrid.Dimensions();
            CollectModulePixels(
                moduleTransform.localPosition,
                moduleTransform.localRotation,
                dimensions.x,
                dimensions.y,
                texturePixelGrid.IsPixel,
                texturePixelGrid.GetValue,
                pixelMap);
        }

        private static void CollectModulePixelsFromSnapshotModule(
            ModuleSnapshot moduleSnapshot,
            Dictionary<Vector2Int, Color32> pixelMap)
        {
            if (moduleSnapshot?.pixelatedRigidbody?.colorGrid == null)
            {
                Debug.LogWarning(
                    $"[ShipPreviewIconCompositor] Module snapshot '{moduleSnapshot?.moduleName}' has no color grid. Skipping.");
                return;
            }

            var colorGrid = moduleSnapshot.pixelatedRigidbody.colorGrid;
            CollectModulePixels(
                moduleSnapshot.localPosition,
                moduleSnapshot.localRotation,
                colorGrid.Width,
                colorGrid.Height,
                colorGrid.IsPixel,
                colorGrid.GetValue,
                pixelMap);
        }

        private static void CollectModulePixels(
            Vector3 localPosition,
            Quaternion localRotation,
            int width,
            int height,
            Func<Vector2Int, bool> isPixel,
            Func<Vector2Int, Color32> getPixel,
            Dictionary<Vector2Int, Color32> pixelMap)
        {
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var pixelPosition = new Vector2Int(x, y);
                if (!isPixel(pixelPosition))
                    continue;

                var offsetFromCenter = new Vector2(
                    x - width / 2f,
                    y - height / 2f);
                var shipLocal = (Vector2)localPosition + (Vector2)(localRotation * offsetFromCenter);
                var shipLocalPixel = new Vector2Int(
                    Mathf.RoundToInt(shipLocal.x),
                    Mathf.RoundToInt(shipLocal.y));

                pixelMap[shipLocalPixel] = getPixel(pixelPosition);
            }
        }

        private static Sprite CreateSpriteFromPixelMap(Dictionary<Vector2Int, Color32> pixelMap)
        {
            if (pixelMap.Count == 0)
                return null;

            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;

            foreach (var shipLocalPixel in pixelMap.Keys)
            {
                minX = Mathf.Min(minX, shipLocalPixel.x);
                minY = Mathf.Min(minY, shipLocalPixel.y);
                maxX = Mathf.Max(maxX, shipLocalPixel.x);
                maxY = Mathf.Max(maxY, shipLocalPixel.y);
            }

            var textureWidth = maxX - minX + 1;
            var textureHeight = maxY - minY + 1;
            var texture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point
            };

            var clearPixels = new Color32[textureWidth * textureHeight];
            texture.SetPixels32(clearPixels);

            foreach (var (shipLocalPixel, color) in pixelMap)
                texture.SetPixel(
                    shipLocalPixel.x - minX,
                    shipLocalPixel.y - minY,
                    color);

            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, textureWidth, textureHeight),
                new Vector2(0.5f, 0.5f),
                1f);
        }
    }
}