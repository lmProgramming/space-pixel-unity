using System.Collections;
using System.Collections.Generic;
using Core.Ship;
using NUnit.Framework;
using Pixelation;
using Ships.Modules;
using Ships.Tests.TestHelpers;
using UnityEngine;
using UnityEngine.TestTools;
using Module = Ships.Modules.Module;

namespace Ships.Tests
{
    /// <summary>
    ///     Tests that modules surviving their ship's destruction become proper junk.
    ///     Destroy(ship.gameObject) disables every component in the hierarchy immediately, so survivors
    ///     must be released (deparented) before the ship is destroyed - rescuing them later from the
    ///     dying hierarchy leaves zombie junk with disabled PolygonCollider2D / PixelatedRigidbody.
    /// </summary>
    [TestFixture]
    public class ShipDestructionJunkTests : ShipTestBase
    {
        private const int ModuleSize = 5;

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

        private static List<Vector2Int> AllPixels()
        {
            var pixels = new List<Vector2Int>();
            for (var y = 0; y < ModuleSize; y++)
            for (var x = 0; x < ModuleSize; x++)
                pixels.Add(new Vector2Int(x, y));
            return pixels;
        }

        [UnityTest]
        public IEnumerator CommandModuleDestroyed_SurvivingModuleBecomesEnabledJunk()
        {
            var (ship, command, other) = CreateTwoModuleShip();
            yield return WaitForLifecycle();

            var shipGo = ship.gameObject;
            var otherGo = other.gameObject;

            command.PixelatedRigidbody.RemovePixels(AllPixels());
            yield return WaitForLifecycle();

            Assert.IsTrue(shipGo == null, "Ship should be destroyed with its command module");
            Assert.IsTrue(otherGo != null, "Surviving module should be released as junk, not destroyed");
            // comparing to null because map parent is null
            Assert.IsTrue(otherGo.transform.parent == null,
                "Junk module must be deparented to the map before the ship is destroyed");

            Assert.IsTrue(otherGo.activeInHierarchy, "Junk module must stay active");
            Assert.IsTrue(otherGo.GetComponent<PolygonCollider2D>().enabled,
                "Junk module's PolygonCollider2D must stay enabled");
            var pixelatedRigidbody = otherGo.GetComponent<PixelatedRigidbody>();
            Assert.IsTrue(pixelatedRigidbody.enabled,
                "Junk module's PixelatedRigidbody must stay enabled - " +
                "it must be released before Destroy(ship) disables the hierarchy");
        }
    }
}