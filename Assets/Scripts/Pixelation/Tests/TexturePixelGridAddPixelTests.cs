using System;
using Grid;
using NUnit.Framework;
using UnityEngine;

namespace Pixelation.Tests
{
    [TestFixture]
    public class TexturePixelGridAddPixelTests
    {
        [Test]
        public void AddPixelAt_IncrementsPixelCount()
        {
            var renderer = CreateRenderer(2, 2, new Color32[]
            {
                new(255, 0, 0, 255), Color.clear,
                Color.clear, Color.clear
            });
            var grid = new TexturePixelGrid(renderer);
            grid.SetTextureFromColors(new Color32[]
            {
                new(255, 0, 0, 255), Color.clear,
                Color.clear, Color.clear
            }, 2, 2);
            grid.Setup();

            Assert.AreEqual(1, grid.PixelCount);
            grid.AddPixelAt(new Vector2Int(1, 0), new Color32(0, 255, 0, 255));
            Assert.AreEqual(2, grid.PixelCount);
            Assert.That(grid.IsPixel(new Vector2Int(1, 0)), Is.True);
        }

        [Test]
        public void AddPixelAt_ThrowsWhenCellAlreadyFilled()
        {
            var renderer = CreateRenderer(1, 1, new[] { new Color32(255, 0, 0, 255) });
            var grid = new TexturePixelGrid(renderer);
            grid.SetTextureFromColors(new[] { new Color32(255, 0, 0, 255) }, 1, 1);
            grid.Setup();

            Assert.Throws<InvalidOperationException>(() =>
                grid.AddPixelAt(new Vector2Int(0, 0), new Color32(0, 255, 0, 255)));
        }

        private static SpriteRenderer CreateRenderer(int width, int height, Color32[] colors)
        {
            var go = new GameObject("TestGrid");
            var renderer = go.AddComponent<SpriteRenderer>();
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point
            };
            texture.SetPixels32(colors);
            texture.Apply();
            renderer.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1f);
            return renderer;
        }
    }
}