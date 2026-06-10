using System.Collections;
using System.Collections.Generic;
using Core.Constants;
using Core.Ship;
using NUnit.Framework;
using Ships.Modules;
using Ships.Tests.TestHelpers;
using UnityEngine;
using UnityEngine.TestTools;
using Module = Ships.Modules.Module;

namespace Ships.Tests
{
    /// <summary>
    ///     Tests that a module is destroyed outright once its remaining pixels drop below
    ///     <see cref="GameplayConstants.ModuleDestroyedBelowPixelRatio" /> of its starting count.
    /// </summary>
    [TestFixture]
    public class ModuleDestructionThresholdTests : ShipTestBase
    {
        private const int ModuleSize = 5;
        private const int TotalPixels = ModuleSize * ModuleSize;

        private static int PixelsToKeepJustBelowThreshold =>
            Mathf.CeilToInt(TotalPixels * GameplayConstants.ModuleDestroyedBelowPixelRatio) - 1;

        private static int PixelsToKeepAtOrAboveThreshold =>
            Mathf.CeilToInt(TotalPixels * GameplayConstants.ModuleDestroyedBelowPixelRatio);

        private (Ship ship, Module command, Module other) CreateTwoModuleShip()
        {
            var shipGo = ModuleFactory.CreateGameObject("TestShip", CreatedObjects);

            var commandGo = ModuleFactory.CreateModuleBase("Command", shipGo.transform, Vector2.zero, 0f,
                Container, CreatedObjects, ModuleSize, ModuleSize);
            var command = commandGo.AddComponent<Command>();

            var otherGo = ModuleFactory.CreateModuleBase("Module2", shipGo.transform, new Vector2(ModuleSize, 0), 0f,
                Container, CreatedObjects, ModuleSize, ModuleSize);
            var other = otherGo.AddComponent<TestModule>();
            other.SetModuleType(ModuleType.Resources);

            var ship = ModuleFactory.WireShip<Ship>(shipGo, Container);

            Container.InjectGameObject(shipGo);

            return (ship, command, other);
        }

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
            var (ship, _, other) = CreateTwoModuleShip();
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
            var (ship, _, other) = CreateTwoModuleShip();
            yield return WaitForLifecycle();

            other.PixelatedRigidbody.RemovePixels(PixelsToRemoveKeeping(PixelsToKeepAtOrAboveThreshold));
            yield return WaitForLifecycle();

            Assert.IsTrue(other, "Module at/above the pixel threshold should survive");
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(other), "Surviving module should stay in the graph");
        }

        [UnityTest]
        public IEnumerator CommandModuleBelowPixelThreshold_DestroysShip()
        {
            var (ship, command, _) = CreateTwoModuleShip();
            yield return WaitForLifecycle();

            var shipGo = ship.gameObject;

            command.PixelatedRigidbody.RemovePixels(PixelsToRemoveKeeping(PixelsToKeepJustBelowThreshold));
            yield return WaitForLifecycle();

            Assert.IsTrue(shipGo == null, "Ship should be destroyed when its command module falls below the threshold");
        }
    }
}