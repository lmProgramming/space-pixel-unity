using System.Collections;
using System.Collections.Generic;
using Core.Ship;
using NUnit.Framework;
using Ships.Modules;
using Ships.Tests.TestHelpers;
using UnityEngine;
using UnityEngine.TestTools;
using Module = Ships.Modules.Module;
using Object = UnityEngine.Object;

namespace Ships.Tests
{
    /// <summary>
    ///     Tests for the FixedJoint2D lifecycle between modules.
    ///     A joint must never outlive the connection it represents: a FixedJoint2D whose connectedBody
    ///     is destroyed re-anchors to the static world body at the origin and violently yanks the ship,
    ///     and duplicated joints keep modules attached after they were supposed to detach.
    /// </summary>
    [TestFixture]
    public class ModuleJointLifecycleTests : ShipTestBase
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

        [UnityTest]
        public IEnumerator InitializeModules_CalledRepeatedly_DoesNotDuplicateJoints()
        {
            var (ship, _, _) = CreateTwoModuleShip();
            yield return WaitForLifecycle();

            Assert.AreEqual(1, ship.GetComponentsInChildren<FixedJoint2D>(true).Length,
                "Two adjacent modules should be held together by exactly one joint");

            // SkirmishSpawner calls InitializeModules right after ApplySnapshot, and Ship.Start calls
            // it again the same frame - re-initialization must not stack additional joints.
            ship.InitializeModules();
            ship.InitializeModules();
            yield return null;

            Assert.AreEqual(1, ship.GetComponentsInChildren<FixedJoint2D>(true).Length,
                "Re-initializing modules must reuse the existing joint instead of creating duplicates");
        }

        [UnityTest]
        public IEnumerator DestroyedModule_LeavesNoJointsOnRemainingModules()
        {
            var (_, command, other) = CreateTwoModuleShip();
            yield return WaitForLifecycle();

            Debug.Log("mkay0");
            Object.Destroy(other.gameObject);
            Debug.Log("mkay");
            yield return WaitForLifecycle();
            Debug.Log("mkay2");

            Assert.That(command.GetComponents<FixedJoint2D>(), Is.Empty,
                "Joints to a destroyed module must be destroyed with it, " +
                "otherwise they re-anchor the survivor to the world origin");
        }

        [UnityTest]
        public IEnumerator DisconnectedModule_LeavesNoJointsOnEitherBody()
        {
            var (_, command, other) = CreateTwoModuleShip();
            yield return WaitForLifecycle();

            var commandGo = command.gameObject;
            var otherGo = other.gameObject;

            Assert.IsTrue(command.ConnectionPoints.TryGetValue(other, out var connectionPoints),
                "Command should have connection points to the other module");

            command.PixelatedRigidbody.RemovePixels(new List<Vector2Int>(connectionPoints));
            yield return WaitForLifecycle();

            Assert.That(commandGo.GetComponents<FixedJoint2D>(), Is.Empty,
                "Ship side must not keep a joint to the detached module");
            if (otherGo != null)
                Assert.That(otherGo.GetComponents<FixedJoint2D>(), Is.Empty,
                    "Detached module must not keep a joint to the ship");
        }
    }
}