using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Ships.Tests.TestHelpers.Factories;
using Ships.Tests.TestHelpers.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;
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
        [UnityTest]
        public IEnumerator InitializeModules_CalledRepeatedly_DoesNotDuplicateJoints()
        {
            var (ship, _, _) =
                ShipTestFactory.CreateTwoModuleShip(Container, CreatedObjects);
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
            var (_, command, other) =
                ShipTestFactory.CreateTwoModuleShip(Container, CreatedObjects);
            yield return WaitForLifecycle();

            Object.Destroy(other.gameObject);
            yield return WaitForLifecycle();

            Assert.That(command.GetComponents<FixedJoint2D>(), Is.Empty,
                "Joints to a destroyed module must be destroyed with it, " +
                "otherwise they re-anchor the survivor to the world origin");
        }

        [UnityTest]
        public IEnumerator DisconnectedModule_LeavesNoJointsOnEitherBody()
        {
            var (_, command, other) =
                ShipTestFactory.CreateTwoModuleShip(Container, CreatedObjects);
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