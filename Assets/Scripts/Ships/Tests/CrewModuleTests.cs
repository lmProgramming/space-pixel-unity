using System.Collections;
using System.Collections.Generic;
using Core.Ship;
using NUnit.Framework;
using Pixelation;
using Ships.Modules;
using Ships.Tests.TestHelpers;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;
using Resources = Core.Ship.Resources;

namespace Ships.Tests
{
    [TestFixture]
    public class CrewModuleTests
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

        private Module CreateStandaloneModule(int crewCapacity = 3)
        {
            var go = new GameObject("Module");
            _createdObjects.Add(go);

            go.AddComponent<SpriteRenderer>();
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            go.AddComponent<PolygonCollider2D>();

            var pxRb = go.AddComponent<PixelatedRigidbody>();
            _container.Inject(pxRb);

            var colors = new Color32[5, 5];
            var solid = new Color32(100, 100, 100, 255);
            for (var x = 0; x < 5; x++)
            for (var y = 0; y < 5; y++)
                colors[x, y] = solid;
            pxRb.SetTextureFromColors(colors);

            var module = go.AddComponent<TestModule>();
            module.SetModuleType(ModuleType.Production);
            module.SetResources(new Resources(0, 0, 0, 0, crewCapacity));
            return module;
        }

        private static CrewMember MakeCrew(string first = "John", string last = "Doe", int age = 30,
            Dictionary<CrewSkillType, int> skills = null)
        {
            return new CrewMember(first, last, age, skills);
        }

        [Test]
        public void AssignCrew_UnderCapacity_ReturnsTrue()
        {
            var module = CreateStandaloneModule();
            var crew = MakeCrew();

            var result = module.AssignCrew(crew);

            Assert.IsTrue(result);
            Assert.AreEqual(1, module.AssignedCrew.Count);
        }

        [Test]
        public void AssignCrew_AtCapacity_ReturnsFalse()
        {
            var module = CreateStandaloneModule(2);
            module.AssignCrew(MakeCrew("A", "A", 20));
            module.AssignCrew(MakeCrew("B", "B", 21));

            var overflow = MakeCrew("C", "C", 22);
            var result = module.AssignCrew(overflow);

            Assert.IsFalse(result);
            Assert.AreEqual(2, module.AssignedCrew.Count);
        }

        [Test]
        public void AssignCrew_SameMemberTwice_ReturnsFalse()
        {
            var module = CreateStandaloneModule();
            var crew = MakeCrew();

            module.AssignCrew(crew);
            var result = module.AssignCrew(crew);

            Assert.IsFalse(result);
            Assert.AreEqual(1, module.AssignedCrew.Count);
        }

        [Test]
        public void RemoveCrew_ExistingMember_ReturnsTrue()
        {
            var module = CreateStandaloneModule();
            var crew = MakeCrew();
            module.AssignCrew(crew);

            var result = module.RemoveCrew(crew);

            Assert.IsTrue(result);
            Assert.AreEqual(0, module.AssignedCrew.Count);
        }

        [Test]
        public void RemoveCrew_NonExistingMember_ReturnsFalse()
        {
            var module = CreateStandaloneModule();
            var crew = MakeCrew();

            var result = module.RemoveCrew(crew);

            Assert.IsFalse(result);
        }

        [UnityTest]
        public IEnumerator DestroyModule_KillsAssignedCrew()
        {
            var module = CreateStandaloneModule();
            var crew1 = MakeCrew("Alice", "A", 25);
            var crew2 = MakeCrew("Bob", "B");
            module.AssignCrew(crew1);
            module.AssignCrew(crew2);

            yield return null; // let Start run

            Object.DestroyImmediate(module.gameObject);

            yield return null;

            Assert.IsFalse(crew1.IsAlive, "crew1 should be dead after module destruction");
            Assert.IsFalse(crew2.IsAlive, "crew2 should be dead after module destruction");
        }

        [Test]
        public void GetCrewBonus_NoCrewAssigned_ReturnsZero()
        {
            var module = CreateStandaloneModule();

            Assert.AreEqual(0f, module.GetCrewBonus());
        }

        [Test]
        public void GetCrewBonus_WithSkill_ReturnsPositiveBonus()
        {
            var module = CreateStandaloneModule();
            var skills = new Dictionary<CrewSkillType, int> { { CrewSkillType.Navigation, 5 } };
            module.AssignCrew(MakeCrew("Nav", "Expert", 35, skills));

            var bonus = module.GetCrewBonus();

            Assert.Greater(bonus, 0f);
        }

        [Test]
        public void GetCrewBonus_CaptainSkillAmplifies_OtherSkills()
        {
            var module = CreateStandaloneModule(5);

            var plainSkills = new Dictionary<CrewSkillType, int> { { CrewSkillType.Navigation, 4 } };
            var captainSkills = new Dictionary<CrewSkillType, int>
            {
                { CrewSkillType.Navigation, 4 },
                { CrewSkillType.Captain, 5 }
            };

            var moduleNoCaptain = CreateStandaloneModule();
            moduleNoCaptain.AssignCrew(MakeCrew("Nav", "Plain", 30, plainSkills));
            var bonusNoCaptain = moduleNoCaptain.GetCrewBonus();

            module.AssignCrew(MakeCrew("Nav", "Cap", 30, captainSkills));
            var bonusWithCaptain = module.GetCrewBonus();

            Assert.Greater(bonusWithCaptain, bonusNoCaptain);
        }
    }
}