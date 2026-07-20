using System.Collections;
using System.Collections.Generic;
using Core.Constants;
using NUnit.Framework;
using Ships.Tests.TestHelpers.Factories;
using Ships.Tests.TestHelpers.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ships.Tests
{
    /// <summary>
    ///     Tests that a module is destroyed outright once its remaining pixels drop below
    ///     <see cref="GameplayConstants.moduleDestroyedWhenCurrentPixelRatioOfOriginalIsBelow" /> of its starting count.
    /// </summary>
    [TestFixture]
    public class ModuleDestructionThresholdTests : ShipTestBase
    {
        private const int ModuleSize = 5;
        private const int TotalPixels = ModuleSize * ModuleSize;

        private int PixelsToKeepJustBelowThreshold =>
            Mathf.CeilToInt(TotalPixels * GameplayConstants.moduleDestroyedWhenCurrentPixelRatioOfOriginalIsBelow) - 1;

        private int PixelsToKeepAtOrAboveThreshold =>
            Mathf.CeilToInt(TotalPixels * GameplayConstants.moduleDestroyedWhenCurrentPixelRatioOfOriginalIsBelow);

        /// <summary>
        ///     Removes pixels in row-major order so the kept pixels stay one contiguous region
        ///     (no division/debris side effects).
        /// </summary>
        private static List<Vector2Int> PixelsToRemoveKeeping(int pixelsToKeep)
        {
            var toRemove = new List<Vector2Int>();
            var index = 0;
            for (var y = 0; y < ModuleSize; y++)
            for (var x = 0; x < ModuleSize; x++)
            {
                if (index >= pixelsToKeep) toRemove.Add(new Vector2Int(x, y));
                index++;
            }

            return toRemove;
        }

        [UnityTest]
        public IEnumerator ModuleBelowPixelThreshold_IsDestroyed()
        {
            var (ship, _, other) =
                ShipTestFactory.CreateTwoModuleShip(Container, CreatedObjects);
            yield return WaitForLifecycle();

            other.PixelatedRigidbody.RemovePixels(PixelsToRemoveKeeping(PixelsToKeepJustBelowThreshold));
            yield return WaitForLifecycle();

            Assert.IsFalse(other, "Module below the pixel threshold should be destroyed");
            Assert.IsFalse(ship.ModuleGraph.ContainsNode(other), "Destroyed module should leave the graph");
            Assert.IsTrue(ship, "Ship should survive losing a non-command module");
        }

        [UnityTest]
        public IEnumerator ModuleAtPixelThreshold_Survives()
        {
            var (ship, _, other) =
                ShipTestFactory.CreateTwoModuleShip(Container, CreatedObjects);
            yield return WaitForLifecycle();

            other.PixelatedRigidbody.RemovePixels(PixelsToRemoveKeeping(PixelsToKeepAtOrAboveThreshold));
            yield return WaitForLifecycle();

            Assert.IsTrue(other, "Module at/above the pixel threshold should survive");
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(other), "Surviving module should stay in the graph");
        }

        [UnityTest]
        public IEnumerator CommandModuleBelowPixelThreshold_DestroysShip()
        {
            var (ship, command, _) =
                ShipTestFactory.CreateTwoModuleShip(Container, CreatedObjects);
            yield return WaitForLifecycle();

            var shipGo = ship.gameObject;

            command.PixelatedRigidbody.RemovePixels(PixelsToRemoveKeeping(PixelsToKeepJustBelowThreshold));
            yield return WaitForLifecycle();

            Assert.IsTrue(shipGo == null, "Ship should be destroyed when its command module falls below the threshold");
        }
    }
}