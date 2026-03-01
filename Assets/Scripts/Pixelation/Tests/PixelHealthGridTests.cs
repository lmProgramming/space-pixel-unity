using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Pixelation.Tests
{
    [TestFixture]
    public class PixelHealthGridTests
    {
        private static PixelHealthGrid CreateGrid(int width, int height, float defaultHealth)
        {
            return new PixelHealthGrid(width, height, defaultHealth);
        }

        [Test]
        public void DamagePixel_ReducesHealth()
        {
            var grid = CreateGrid(4, 4, 10f);
            var point = new Vector2Int(1, 1);
            grid.SetHealth(point, 10f);

            grid.DamagePixel(point, 3f);

            Assert.AreEqual(7f, grid.GetHealth(point), 0.001);
        }

        [Test]
        public void DamagePixel_ReturnsFalse_WhenPixelSurvives()
        {
            var grid = CreateGrid(4, 4, 10f);
            var point = new Vector2Int(1, 1);
            grid.SetHealth(point, 10f);

            var killed = grid.DamagePixel(point, 3f);

            Assert.IsFalse(killed);
        }

        [Test]
        public void DamagePixel_ReturnsTrue_WhenPixelDies()
        {
            var grid = CreateGrid(4, 4, 5f);
            var point = new Vector2Int(2, 2);
            grid.SetHealth(point, 5f);

            var killed = grid.DamagePixel(point, 5f);

            Assert.IsTrue(killed);
        }

        [Test]
        public void DamagePixel_ClampsToZero()
        {
            var grid = CreateGrid(4, 4, 3f);
            var point = new Vector2Int(0, 0);
            grid.SetHealth(point, 3f);

            grid.DamagePixel(point, 999f);

            Assert.AreEqual(0f, grid.GetHealth(point));
        }

        [Test]
        public void DamagePixel_IgnoresAlreadyDeadPixel()
        {
            var grid = CreateGrid(4, 4, 5f);
            var point = new Vector2Int(1, 1);
            grid.SetHealth(point, 0f);

            var killed = grid.DamagePixel(point, 10f);

            Assert.IsFalse(killed);
        }

        [Test]
        public void DamagePixels_ReturnsOnlyDestroyedPositions()
        {
            var grid = CreateGrid(4, 4, 10f);
            var alive = new Vector2Int(0, 0);
            var dead = new Vector2Int(1, 1);
            grid.SetHealth(alive, 10f);
            grid.SetHealth(dead, 2f);

            var destroyed = grid.DamagePixels(new[] { alive, dead }, 5f);

            Assert.AreEqual(1, destroyed.Count);
            Assert.AreEqual(dead, destroyed[0]);
        }

        [Test]
        public void RemovePixel_SetsHealthToZero()
        {
            var grid = CreateGrid(4, 4, 10f);
            var point = new Vector2Int(2, 2);
            grid.SetHealth(point, 10f);

            grid.RemovePixel(point);

            Assert.AreEqual(0f, grid.GetHealth(point));
        }

        [Test]
        public void IsAlive_ReturnsTrueForPositiveHealth()
        {
            var grid = CreateGrid(4, 4, 5f);
            var point = new Vector2Int(1, 1);
            grid.SetHealth(point, 5f);

            Assert.That(grid.IsAlive(point), Is.True);
        }

        [Test]
        public void IsAlive_ReturnsFalseForZeroHealth()
        {
            var grid = CreateGrid(4, 4, 5f);
            var point = new Vector2Int(1, 1);
            grid.SetHealth(point, 0f);

            Assert.That(grid.IsAlive(point), Is.False);
        }

        [Test]
        public void MultipleHits_AccumulateDamage()
        {
            var grid = CreateGrid(4, 4, 10f);
            var point = new Vector2Int(1, 1);
            grid.SetHealth(point, 10f);

            grid.DamagePixel(point, 3f);
            grid.DamagePixel(point, 3f);

            Assert.AreEqual(4f, grid.GetHealth(point), 0.001);
            Assert.That(grid.IsAlive(point), Is.True);
        }

        [Test]
        public void MultipleHits_EventuallyKillsPixel()
        {
            var grid = CreateGrid(4, 4, 10f);
            var point = new Vector2Int(1, 1);
            grid.SetHealth(point, 10f);

            grid.DamagePixel(point, 4f);
            grid.DamagePixel(point, 4f);
            var killed = grid.DamagePixel(point, 4f);

            Assert.IsTrue(killed);
            Assert.AreEqual(0f, grid.GetHealth(point));
        }

        [Test]
        public void CreateSubGrid_CopiesHealthValues()
        {
            var grid = CreateGrid(8, 8, 10f);
            var point = new Vector2Int(3, 3);
            grid.SetHealth(point, 7f);

            var points = new HashSet<Vector2Int> { point };
            var subGrid = grid.CreateSubGrid(new Vector2Int(2, 2), 4, 4, points);

            Assert.AreEqual(7f, subGrid.GetHealth(new Vector2Int(1, 1)), 0.001);
        }

        [Test]
        public void ApplyArmorMap_WhitePixelGetsMaxArmorHealth()
        {
            var grid = CreateGrid(2, 2, 1f);
            grid.SetHealth(new Vector2Int(0, 0), 1f);

            var armorPixels = new Color32[4];
            armorPixels[0] = new Color32(255, 255, 255, 255);
            armorPixels[1] = new Color32(0, 0, 0, 255);
            armorPixels[2] = new Color32(0, 0, 0, 255);
            armorPixels[3] = new Color32(0, 0, 0, 255);

            grid.ApplyArmorMap(armorPixels, 2, 2, 10f);

            Assert.AreEqual(10f, grid.GetHealth(new Vector2Int(0, 0)), 0.001);
        }

        [Test]
        public void ApplyArmorMap_BlackPixelKeepsDefaultHealth()
        {
            var grid = CreateGrid(2, 2, 1f);
            grid.SetHealth(new Vector2Int(0, 0), 1f);

            var armorPixels = new Color32[4];
            armorPixels[0] = new Color32(0, 0, 0, 255);
            armorPixels[1] = new Color32(0, 0, 0, 255);
            armorPixels[2] = new Color32(0, 0, 0, 255);
            armorPixels[3] = new Color32(0, 0, 0, 255);

            grid.ApplyArmorMap(armorPixels, 2, 2, 10f);

            Assert.AreEqual(1f, grid.GetHealth(new Vector2Int(0, 0)), 0.001);
        }

        [Test]
        public void ApplyArmorMap_MidGrayPixelGetsInterpolatedHealth()
        {
            var grid = CreateGrid(2, 2, 1f);
            grid.SetHealth(new Vector2Int(0, 0), 1f);

            var armorPixels = new Color32[4];
            armorPixels[0] = new Color32(128, 128, 128, 255);
            armorPixels[1] = new Color32(0, 0, 0, 255);
            armorPixels[2] = new Color32(0, 0, 0, 255);
            armorPixels[3] = new Color32(0, 0, 0, 255);

            grid.ApplyArmorMap(armorPixels, 2, 2, 10f);

            var expected = Mathf.Lerp(1f, 10f, 128f / 255f);
            Assert.AreEqual(expected, grid.GetHealth(new Vector2Int(0, 0)), 0.01);
        }

        [Test]
        public void ApplyArmorMap_SkipsDeadPixels()
        {
            var grid = CreateGrid(2, 2, 1f);
            grid.SetHealth(new Vector2Int(0, 0), 0f);

            var armorPixels = new Color32[4];
            armorPixels[0] = new Color32(255, 255, 255, 255);
            armorPixels[1] = new Color32(0, 0, 0, 255);
            armorPixels[2] = new Color32(0, 0, 0, 255);
            armorPixels[3] = new Color32(0, 0, 0, 255);

            grid.ApplyArmorMap(armorPixels, 2, 2, 10f);

            Assert.AreEqual(0f, grid.GetHealth(new Vector2Int(0, 0)));
        }

        [Test]
        public void ApplyArmorMap_ArmoredPixelSurvivesMultipleHits()
        {
            var grid = CreateGrid(2, 2, 1f);
            grid.SetHealth(new Vector2Int(0, 0), 1f);

            var armorPixels = new Color32[4];
            armorPixels[0] = new Color32(255, 255, 255, 255);
            armorPixels[1] = new Color32(0, 0, 0, 255);
            armorPixels[2] = new Color32(0, 0, 0, 255);
            armorPixels[3] = new Color32(0, 0, 0, 255);

            grid.ApplyArmorMap(armorPixels, 2, 2, 10f);

            grid.DamagePixel(new Vector2Int(0, 0), 1f);
            grid.DamagePixel(new Vector2Int(0, 0), 1f);
            grid.DamagePixel(new Vector2Int(0, 0), 1f);

            Assert.That(grid.IsAlive(new Vector2Int(0, 0)), Is.True);
            Assert.AreEqual(7f, grid.GetHealth(new Vector2Int(0, 0)), 0.001);
        }

        [Test]
        public void TotalHealth_TrackedOnSetHealth()
        {
            var grid = CreateGrid(2, 2, 0f);

            grid.SetHealth(new Vector2Int(0, 0), 5f);
            grid.SetHealth(new Vector2Int(1, 0), 3f);

            Assert.AreEqual(8f, grid.TotalHealth, 0.001);
        }

        [Test]
        public void TotalHealth_DecreasesOnDamage()
        {
            var grid = CreateGrid(2, 2, 0f);
            grid.SetHealth(new Vector2Int(0, 0), 10f);

            grid.DamagePixel(new Vector2Int(0, 0), 3f);

            Assert.AreEqual(7f, grid.TotalHealth, 0.001);
        }

        [Test]
        public void TotalHealth_DecreasesOnRemovePixel()
        {
            var grid = CreateGrid(2, 2, 0f);
            grid.SetHealth(new Vector2Int(0, 0), 10f);
            grid.SetHealth(new Vector2Int(1, 0), 5f);

            grid.RemovePixel(new Vector2Int(0, 0));

            Assert.AreEqual(5f, grid.TotalHealth, 0.001);
        }

        [Test]
        public void TotalHealth_IncreasesWithArmorMap()
        {
            var grid = CreateGrid(2, 2, 1f);
            grid.SetHealth(new Vector2Int(0, 0), 1f);
            grid.SetHealth(new Vector2Int(1, 0), 1f);

            var armorPixels = new Color32[4];
            armorPixels[0] = new Color32(255, 255, 255, 255);
            armorPixels[1] = new Color32(0, 0, 0, 255);
            armorPixels[2] = new Color32(0, 0, 0, 255);
            armorPixels[3] = new Color32(0, 0, 0, 255);

            grid.ApplyArmorMap(armorPixels, 2, 2, 10f);

            // pixel (0,0) went from 1 to 10, pixel (1,0) stayed at 1
            Assert.AreEqual(11f, grid.TotalHealth, 0.001);
        }
    }
}