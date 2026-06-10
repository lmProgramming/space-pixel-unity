using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Ships.Modules;
using Ships.Tests.TestHelpers;
using UnityEngine;
using UnityEngine.TestTools;
using ZLinq;

namespace Ships.Tests
{
    [TestFixture]
    public class ModuleConnectionTests : ShipTestBase
    {
        private const int ModulePixelSize = 5;
        private const float ModuleSpacing = 5f;
        private const float EngineMaxThrust = 800f;

        [UnityTest]
        public IEnumerator RotatedEngineOnRight_ConnectsToCommandModule()
        {
            var (ship, command, engine) = CreateCommandWithEngineOnRight(-180f);
            yield return WaitForLifecycle();

            AssertModulesConnected(ship, command, engine);
        }

        [UnityTest]
        public IEnumerator UnrotatedModuleOnRight_ConnectsToCommandModule()
        {
            var (ship, command, engine) = CreateCommandWithEngineOnRight(0f);
            yield return WaitForLifecycle();

            AssertModulesConnected(ship, command, engine);
        }

        private (Ship ship, Command command, Engine engine) CreateCommandWithEngineOnRight(float engineRotationZ)
        {
            var shipGo = ModuleFactory.CreateGameObject("TestShip", CreatedObjects);

            ModuleFactory.CreateCommandModule(shipGo.transform, Vector2.zero, Container, CreatedObjects,
                ModulePixelSize, ModulePixelSize);
            ModuleFactory.CreateEngineModule(shipGo.transform, new Vector2(ModuleSpacing, 0f), Container,
                CreatedObjects, EngineMaxThrust, ModulePixelSize, ModulePixelSize, engineRotationZ);

            var ship = ModuleFactory.WireShip<Ship>(shipGo, Container);
            var command = shipGo.GetComponentInChildren<Command>();
            var engine = shipGo.GetComponentInChildren<Engine>();

            Container.InjectGameObject(shipGo);

            return (ship, command, engine);
        }

        private static void AssertModulesConnected(Ship ship, Command command, Engine engine)
        {
            Assert.IsNotNull(command, "Command module should exist");
            Assert.IsNotNull(engine, "Engine module should exist");

            Assert.IsTrue(ship.ModuleGraph.ContainsNode(command), "Command module should be in graph");
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(engine), "Engine module should be in graph");

            var commandNeighbors = ship.ModuleGraph.GetConnectedNodes(command);
            var engineNeighbors = ship.ModuleGraph.GetConnectedNodes(engine);

            Assert.IsTrue(commandNeighbors.Contains(engine),
                "Command module should be connected to engine in module graph");
            Assert.IsTrue(engineNeighbors.Contains(command),
                "Engine module should be connected to command in module graph");

            var connectionPoints = command.ConnectionPoints.TryGetValue(engine, out var points)
                ? points.AsValueEnumerable().ToList()
                : new List<Vector2Int>();

            Assert.IsTrue(connectionPoints.Count > 0,
                "Command module should have connection points to engine");
        }
    }
}