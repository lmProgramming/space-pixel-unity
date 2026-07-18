using System.Collections;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using Ships.Modules;
using Ships.Tests.TestHelpers.Factories;
using Ships.Tests.TestHelpers.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Ships.Tests
{
    /// <summary>
    ///     Edge cases around module / ship destruction: joints, crew, detachment VFX order, and
    ///     ResourceManager recalculate while modules are mid-teardown.
    /// </summary>
    [TestFixture]
    public class ModuleDestructionLifecycleTests : ShipTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            GameplayConstants.chanceOfSpawningExplosionOnDetachingConnectionPoint = 1f;
        }

        private const int ModuleSize = 5;

        private static List<Vector2Int> AllPixels()
        {
            var pixels = new List<Vector2Int>();
            for (var y = 0; y < ModuleSize; y++)
            for (var x = 0; x < ModuleSize; x++)
                pixels.Add(new Vector2Int(x, y));
            return pixels;
        }

        [UnityTest]
        public IEnumerator RawDestroyGameObject_StillClearsJointsOnSurvivor()
        {
            var (_, command, other) =
                ShipTestFactory.CreateTwoModuleShip(Container, CreatedObjects);
            yield return WaitForLifecycle();

            Object.Destroy(other.gameObject);
            yield return WaitForLifecycle();

            Assert.That(command.GetComponents<FixedJoint2D>(), Is.Empty,
                "OnDestroy must still tear down joints when Destroy(gameObject) bypasses DestroyModule");
        }

        [UnityTest]
        public IEnumerator DestroyModule_SpawnsDetachmentExplosions()
        {
            var (_, _, other) =
                ShipTestFactory.CreateTwoModuleShip(Container, CreatedObjects);
            yield return WaitForLifecycle();

            other.DestroyModule();
            yield return WaitForLifecycle();

            EffectsSpawner.Received().SpawnExplosion(Arg.Any<Vector2>());
        }

        [UnityTest]
        public IEnumerator CommandDestroyed_SurvivingModule_SpawnsDetachmentExplosions()
        {
            var (_, command, other) =
                ShipTestFactory.CreateTwoModuleShip(Container, CreatedObjects);
            yield return WaitForLifecycle();

            Assert.IsTrue(other.ConnectionPoints.ContainsKey(command),
                "Survivor must still be connected before command death so junk release can VFX");

            command.PixelatedRigidbody.RemovePixels(AllPixels());
            yield return WaitForLifecycle();

            EffectsSpawner.Received().SpawnExplosion(Arg.Any<Vector2>());
        }

        [UnityTest]
        public IEnumerator CommandDestroyed_SurvivingModule_LeavesNoJoints()
        {
            var (_, command, other) =
                ShipTestFactory.CreateTwoModuleShip(Container, CreatedObjects);
            yield return WaitForLifecycle();

            var otherGo = other.gameObject;

            command.PixelatedRigidbody.RemovePixels(AllPixels());
            yield return WaitForLifecycle();

            Assert.IsTrue(otherGo != null);
            Assert.That(otherGo.GetComponents<FixedJoint2D>(), Is.Empty,
                "Junk release must destroy joints; orphaned FixedJoint2D re-anchors to world origin");
        }

        [UnityTest]
        public IEnumerator CommandDestroyed_DoesNotThrowWhileRecalculatingResources()
        {
            var (_, command, _) =
                ShipTestFactory.CreateTwoModuleShip(Container, CreatedObjects);
            yield return WaitForLifecycle();

            // Regression: clearing Ship before OnModuleConnectionLost made Recalculate NRE on
            // GetCrewEfficiency during junk release.
            Assert.DoesNotThrow(() => command.PixelatedRigidbody.RemovePixels(AllPixels()));
            yield return WaitForLifecycle();
        }

        [UnityTest]
        public IEnumerator CommandDestroyed_KillsCrewOnSurvivingJunkModule()
        {
            var (_, command, other) =
                ShipTestFactory.CreateTwoModuleShip(Container, CreatedObjects);
            yield return WaitForLifecycle();

            var crew = MakeCrew("Junk", "Crew");
            other.AssignCrew(crew);

            command.PixelatedRigidbody.RemovePixels(AllPixels());
            yield return WaitForLifecycle();

            Assert.IsFalse(crew.IsAlive,
                "DetachAsJunkFromShip must kill assigned crew when the module leaves the ship");
        }

        [UnityTest]
        public IEnumerator ThreeModuleChain_CommandDestroyed_BothSurvivorsBecomeJointFreeJunk()
        {
            const int moduleWidth = ModuleSize;
            const int moduleHeight = ModuleSize;

            var layout = ShipTestBuilder.CreateShip(Container, CreatedObjects)
                .WithCommand("Command", Vector2.zero, moduleWidth, moduleHeight)
                .WithTestModule("ModuleA", new Vector2(moduleWidth, 0), moduleWidth, moduleHeight)
                .WithTestModule("ModuleB", new Vector2(moduleWidth * 2, 0), moduleWidth, moduleHeight)
                .BuildLayoutResult();

            yield return WaitForLifecycle();

            var command = (Command)layout.CommandModule;
            var moduleAGo = layout.OtherModules[0].gameObject;
            var moduleBGo = layout.OtherModules[1].gameObject;

            command.PixelatedRigidbody.RemovePixels(AllPixels());
            yield return WaitForLifecycle();

            Assert.IsTrue(moduleAGo != null, "ModuleA should survive as junk");
            Assert.IsTrue(moduleBGo != null, "ModuleB should survive as junk");
            Assert.That(moduleAGo.GetComponents<FixedJoint2D>(), Is.Empty);
            Assert.That(moduleBGo.GetComponents<FixedJoint2D>(), Is.Empty);

            EffectsSpawner.Received().SpawnExplosion(Arg.Any<Vector2>());
        }
    }
}