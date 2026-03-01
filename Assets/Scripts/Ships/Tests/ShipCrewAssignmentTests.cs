using System.Collections;
using System.Collections.Generic;
using Core.Ship;
using NUnit.Framework;
using Pixelation;
using Ships.Internal;
using Ships.Modules;
using Ships.Tests.TestHelpers;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;
using ZLinq;
using Resources = Core.Ship.Resources;

namespace Ships.Tests
{
    [TestFixture]
    public class ShipCrewAssignmentTests
    {
        [SetUp]
        public void SetUp()
        {
            _createdObjects = new List<GameObject>();
            _testRoot = new GameObject("TestRoot");
            _container = TestContainerFactory.CreateTestContainer(_testRoot.transform);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
                if (obj != null)
                    Object.DestroyImmediate(obj);

            if (_testRoot != null)
                Object.DestroyImmediate(_testRoot);
        }

        private DiContainer _container;
        private GameObject _testRoot;
        private List<GameObject> _createdObjects;

        private Ship CreateShipWithModules(params (int crewNeeded, CrewSkillType mainSkill)[] moduleConfigs)
        {
            var shipGo = CreateGameObject("TestShip");

            CreateModule("Command", shipGo.transform, Vector2.zero, 5, 5, true, 0, CrewSkillType.Navigation);

            for (var i = 0; i < moduleConfigs.Length; i++)
            {
                var config = moduleConfigs[i];
                CreateModule($"Module{i}", shipGo.transform,
                    new Vector2(5 * (i + 1), 0), 5, 5, false,
                    config.crewNeeded, config.mainSkill);
            }

            var connectionFactory = shipGo.AddComponent<ModuleConnectionFactory>();
            shipGo.AddComponent<ResourceManager>();

            shipGo.SetActive(false);
            var ship = shipGo.AddComponent<Ship>();
            _container.Inject(ship);
            ship.ModuleConnectionFactoryForTesting = connectionFactory;
            shipGo.SetActive(true);

            return ship;
        }

        private void CreateModule(string name, Transform parent, Vector2 localPosition,
            int width, int height, bool isCommand, int crewNeeded, CrewSkillType mainSkill)
        {
            var moduleGo = CreateGameObject(name);
            moduleGo.transform.SetParent(parent);
            moduleGo.transform.localPosition = localPosition;

            moduleGo.AddComponent<SpriteRenderer>();
            var rigidbody = moduleGo.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Dynamic;
            rigidbody.gravityScale = 0f;
            moduleGo.AddComponent<PolygonCollider2D>();

            var pixelatedRb = moduleGo.AddComponent<PixelatedRigidbody>();
            _container.Inject(pixelatedRb);

            Module module;
            if (isCommand)
            {
                module = moduleGo.AddComponent<Command>();
            }
            else
            {
                var testModule = moduleGo.AddComponent<TestModule>();
                testModule.SetModuleType(ModuleType.Production);
                testModule.SetMainSkillType(mainSkill);
                module = testModule;
            }

            var colors = new Color32[width, height];
            var solid = new Color32(100, 100, 100, 255);
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                colors[x, y] = solid;
            pixelatedRb.SetTextureFromColors(colors);

            module.SetResources(new Resources(0, 0, crewNeeded, 0, 0));
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        private static CrewMember MakeCrew(string first = "John", string last = "Doe", int age = 30,
            Dictionary<CrewSkillType, int> skills = null)
        {
            return new CrewMember(first, last, age, skills);
        }

        private IEnumerator WaitForStart()
        {
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator AssignCrewRandomly_DistributesCrewAcrossModules()
        {
            var ship = CreateShipWithModules(
                (crewNeeded: 2, mainSkill: CrewSkillType.Navigation),
                (crewNeeded: 2, mainSkill: CrewSkillType.Mechanics));
            yield return WaitForStart();

            var crew = new List<CrewMember>();
            for (var i = 0; i < 4; i++)
                crew.Add(MakeCrew($"Crew{i}", "Member", 25 + i));

            ship.AssignCrewBySkill(crew);

            var totalAssigned = ship.ModuleGraph.GetAllNodes().AsValueEnumerable()
                .Sum(m => m.AliveCrewCount);
            Assert.AreEqual(4, totalAssigned);
        }

        [UnityTest]
        public IEnumerator AssignCrewRandomly_EmptyCrew_NoAssignment()
        {
            var ship = CreateShipWithModules(
                (crewNeeded: 3, mainSkill: CrewSkillType.Navigation));
            yield return WaitForStart();

            ship.AssignCrewBySkill(new List<CrewMember>());

            var totalAssigned = ship.ModuleGraph.GetAllNodes().AsValueEnumerable()
                .Sum(m => m.AliveCrewCount);
            Assert.AreEqual(0, totalAssigned);
        }

        [UnityTest]
        public IEnumerator AssignCrewRandomly_ExcessCrew_OnlyAssignsNeeded()
        {
            var ship = CreateShipWithModules(
                (crewNeeded: 1, mainSkill: CrewSkillType.Navigation),
                (crewNeeded: 1, mainSkill: CrewSkillType.Mechanics));
            yield return WaitForStart();

            var crew = new List<CrewMember>();
            for (var i = 0; i < 10; i++)
                crew.Add(MakeCrew($"Crew{i}", "Member", 20 + i));

            ship.AssignCrewBySkill(crew);

            var totalNeeded = ship.ModuleGraph.GetAllNodes().AsValueEnumerable()
                .Sum(m => m.CrewNeededCount);
            var totalAssigned = ship.ModuleGraph.GetAllNodes().AsValueEnumerable()
                .Sum(m => m.AliveCrewCount);
            Assert.AreEqual(totalNeeded, totalAssigned);
        }

        [UnityTest]
        public IEnumerator AssignCrewRandomly_AssignsBestSkilledToMatchingModules()
        {
            var ship = CreateShipWithModules(
                (crewNeeded: 1, mainSkill: CrewSkillType.Navigation));
            yield return WaitForStart();

            var navExpert = MakeCrew("Nav", "Expert", 30,
                new Dictionary<CrewSkillType, int> { { CrewSkillType.Navigation, 10 } });
            var mechExpert = MakeCrew("Mech", "Expert", 30,
                new Dictionary<CrewSkillType, int> { { CrewSkillType.Mechanics, 10 } });

            ship.AssignCrewBySkill(new List<CrewMember> { mechExpert, navExpert });

            var navModule = ship.ModuleGraph.GetAllNodes().AsValueEnumerable()
                .First(m => m.Type == ModuleType.Production);

            Assert.Contains(navExpert, (ICollection)navModule.AssignedCrew);
        }

        [UnityTest]
        public IEnumerator CrewMissingCount_SumsAcrossAllModules()
        {
            var ship = CreateShipWithModules(
                (crewNeeded: 3, mainSkill: CrewSkillType.Navigation),
                (crewNeeded: 5, mainSkill: CrewSkillType.Mechanics));
            yield return WaitForStart();

            // Command module has 0 crewNeeded, so total = 3 + 5 = 8
            Assert.AreEqual(8, ship.CrewMissingCount);
        }

        [UnityTest]
        public IEnumerator CrewMissingCount_AfterAssignment_ReflectsRemaining()
        {
            var ship = CreateShipWithModules(
                (crewNeeded: 3, mainSkill: CrewSkillType.Navigation),
                (crewNeeded: 2, mainSkill: CrewSkillType.Mechanics));
            yield return WaitForStart();

            var crew = new List<CrewMember>();
            for (var i = 0; i < 3; i++)
                crew.Add(MakeCrew($"Crew{i}", "Member", 25 + i));

            ship.AssignCrewBySkill(crew);

            // 5 needed total, 3 assigned → 2 missing
            Assert.AreEqual(2, ship.CrewMissingCount);
        }

        [UnityTest]
        public IEnumerator CrewMissingCount_FullyStaffed_ReturnsZero()
        {
            var ship = CreateShipWithModules(
                (crewNeeded: 2, mainSkill: CrewSkillType.Navigation),
                (crewNeeded: 1, mainSkill: CrewSkillType.Mechanics));
            yield return WaitForStart();

            var crew = new List<CrewMember>();
            for (var i = 0; i < 3; i++)
                crew.Add(MakeCrew($"Crew{i}", "Member", 25 + i));

            ship.AssignCrewBySkill(crew);

            Assert.AreEqual(0, ship.CrewMissingCount);
        }

        [UnityTest]
        public IEnumerator AssignCrewRandomly_InsufficientCrew_PartiallyFills()
        {
            var ship = CreateShipWithModules(
                (crewNeeded: 5, mainSkill: CrewSkillType.Navigation),
                (crewNeeded: 5, mainSkill: CrewSkillType.Mechanics));
            yield return WaitForStart();

            var crew = new List<CrewMember> { MakeCrew("Only", "One", 25) };

            ship.AssignCrewBySkill(crew);

            var totalAssigned = ship.ModuleGraph.GetAllNodes().AsValueEnumerable()
                .Sum(m => m.AliveCrewCount);
            Assert.AreEqual(1, totalAssigned);
            Assert.Greater(ship.CrewMissingCount, 0);
        }
    }
}