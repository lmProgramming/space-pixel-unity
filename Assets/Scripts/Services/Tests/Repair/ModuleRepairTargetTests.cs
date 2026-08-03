using System.Collections.Generic;
using Core.Pixelation;
using NSubstitute;
using NUnit.Framework;
using Services.Repair;
using UnityEngine;

namespace Services.Tests.Repair
{
    [TestFixture]
    public class ModuleRepairTargetTests
    {
        [Test]
        public void RestoreNextPixel_PrefersEnclosedHoleOverEdgeCell()
        {
            // 3x3 solid square with center missing: center has 8 neighbours, edge hole would have fewer.
            var pristineColors = CreateSolidColors(3, 3);
            var pristineHealth = CreateUniformHealth(3, 3, 1f);
            var present = new HashSet<Vector2Int>
            {
                new(0, 0), new(1, 0), new(2, 0),
                new(0, 1), /* hole */ new(2, 1),
                new(0, 2), new(1, 2), new(2, 2)
            };

            var body = CreateBodyMock(present);
            var target = new ModuleRepairTarget(body, pristineColors, pristineHealth);

            Assert.That(target.TryPeekNext(out var next), Is.True);
            Assert.AreEqual(new Vector2Int(1, 1), next);

            target.RestoreNextPixel();
            body.Received(1).RestorePixels(Arg.Is<IReadOnlyList<Pixel>>(list =>
                list.Count == 1 && list[0].Point == new Vector2Int(1, 1)));
        }

        [Test]
        public void RestoreNextPixel_UsesHighestNeighborCountFirst()
        {
            var pristineColors = CreateSolidColors(3, 3);
            var pristineHealth = CreateUniformHealth(3, 3, 1f);
            // Only bottom-left 2x2 present -> missing (2,0) has 1 neighbour, missing (1,2) has 1, missing (2,1) has 2
            var present = new HashSet<Vector2Int>
            {
                new(0, 0), new(1, 0),
                new(0, 1), new(1, 1)
            };

            var body = CreateBodyMock(present);
            var target = new ModuleRepairTarget(body, pristineColors, pristineHealth);

            Assert.That(target.TryPeekNext(out var next), Is.True);
            Assert.AreEqual(new Vector2Int(2, 1), next);
        }

        private static IPixelatedRigidbody CreateBodyMock(HashSet<Vector2Int> present)
        {
            var body = Substitute.For<IPixelatedRigidbody>();
            body.WeightedCenter.Returns(new Vector2(1f, 1f));
            body.IsPixel(Arg.Any<Vector2Int>()).Returns(call => present.Contains(call.Arg<Vector2Int>()));
            body.When(b => b.RestorePixels(Arg.Any<IReadOnlyList<Pixel>>()))
                .Do(call =>
                {
                    foreach (var pixel in call.Arg<IReadOnlyList<Pixel>>())
                        present.Add(pixel.Point);
                });
            return body;
        }

        private static Color32[,] CreateSolidColors(int width, int height)
        {
            var colors = new Color32[width, height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                colors[x, y] = new Color32(255, 255, 255, 255);
            return colors;
        }

        private static float[,] CreateUniformHealth(int width, int height, float health)
        {
            var values = new float[width, height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                values[x, y] = health;
            return values;
        }
    }
}