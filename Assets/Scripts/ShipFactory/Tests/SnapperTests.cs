using NUnit.Framework;
using ShipFactory.Helpers;
using UnityEngine;

namespace ShipFactory.Tests
{
    [TestFixture]
    public class SnapperTests
    {
        [Test]
        public void SnapModuleLocalCenter_EvenMultipleOfEight_AlignsEdgesToGrid()
        {
            var snapped = Snapper.SnapModuleLocalCenter(new Vector2(3f, -2f), new Vector2Int(16, 16));

            Assert.That(snapped, Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(snapped.x - 8f, Is.EqualTo(-8f).Within(0.001f));
            Assert.That(snapped.x + 8f, Is.EqualTo(8f).Within(0.001f));
        }

        [Test]
        public void SnapModuleLocalCenter_OddMultipleOfEight_AlignsEdgesToGrid()
        {
            var snapped = Snapper.SnapModuleLocalCenter(new Vector2(13f, 11f), new Vector2Int(24, 24));

            Assert.That(snapped, Is.EqualTo(new Vector2(12f, 12f)));
            Assert.That(snapped.x - 12f, Is.EqualTo(0f).Within(0.001f));
            Assert.That(snapped.x + 12f, Is.EqualTo(24f).Within(0.001f));
        }

        [Test]
        public void SnapModuleLocalCenter_NonSquareOddMultiple_AlignsEachAxisIndependently()
        {
            var snapped = Snapper.SnapModuleLocalCenter(new Vector2(21f, 17f), new Vector2Int(40, 24));

            Assert.That(snapped, Is.EqualTo(new Vector2(20f, 20f)));
            Assert.That(snapped.x - 20f, Is.EqualTo(0f).Within(0.001f));
            Assert.That(snapped.x + 20f, Is.EqualTo(40f).Within(0.001f));
            Assert.That(snapped.y - 12f, Is.EqualTo(8f).Within(0.001f));
            Assert.That(snapped.y + 12f, Is.EqualTo(32f).Within(0.001f));
        }

        [Test]
        public void SnapModuleLocalCenter_Rotated24x16_AlignsFootprintBoundsToGrid()
        {
            var rotation = Quaternion.Euler(0f, 0f, 90f);
            var snapped = Snapper.SnapModuleLocalCenter(new Vector2(12f, 8f), new Vector2Int(24, 16), rotation);

            Assert.That(snapped, Is.EqualTo(new Vector2(8f, 12f)));

            var (min, max) = ModuleRotationUtility.GetFootprintBoundsInParentSpace(
                snapped, new Vector2Int(24, 16), rotation);

            Assert.That(max.x - min.x, Is.EqualTo(16f).Within(0.001f));
            Assert.That(max.y - min.y, Is.EqualTo(24f).Within(0.001f));
            AssertFootprintCornerOnGrid(min);
            AssertFootprintCornerOnGrid(max);
        }

        private static void AssertFootprintCornerOnGrid(Vector2 corner)
        {
            Assert.That(Mathf.Abs(corner.x % Snapper.SnapUnits), Is.EqualTo(0f).Within(0.001f));
            Assert.That(Mathf.Abs(corner.y % Snapper.SnapUnits), Is.EqualTo(0f).Within(0.001f));
        }
    }
}