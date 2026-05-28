using System.Collections;
using System.Collections.Generic;
using Core.Ship;
using NUnit.Framework;
using Ships.Modules;
using Ships.Tests.TestHelpers;
using UnityEngine;
using UnityEngine.TestTools;
using ZLinq;
using Resources = Core.Ship.Resources;

namespace Ships.Tests
{
    [TestFixture]
    public class ShipCrewAssignmentTests : ShipTestBase
    {
        private Ship CreateShipWithModules(params (int crewNeeded, CrewSkillType mainSkill)[] moduleConfigs)
        {
            var shipGo = ModuleFactory.CreateGameObject("TestShip", CreatedObjects);

            CreateModule("Command", shipGo.transform, Vector2.zero, 5, 5, true, 0, CrewSkillType.Navigation);

            for (var i = 0; i < moduleConfigs.Length; i++)
            {
                var config = moduleConfigs[i];
                CreateModule($"Module{i}", shipGo.transform,
                    new Vector2(5 * (i + 1), 0), 5, 5, false,
                    config.crewNeeded, config.mainSkill);
            }

            return ModuleFactory.WireShip<Ship>(shipGo, Container);
        }

        private void CreateModule(string name, Transform parent, Vector2 localPosition,
            int width, int height, bool isCommand, int crewNeeded, CrewSkillType mainSkill)
        {
            var moduleGo = ModuleFactory.CreateModuleBase(name, parent, localPosition, 0f, Container, CreatedObjects,
                width, height);

            Module module;
            if (isCommand)
            {
                module = moduleGo.AddComponent<Command>();
            }
            else
            {
                var testModule = moduleGo.AddComponent<TestModule>();
                testModule.SetModuleType(ModuleType.Resources);
                testModule.SetMainSkillType(mainSkill);
                module = testModule;
            }

            module.SetResources(new Resources(0, 0, crewNeeded, 0, 0));
        }

        [UnityTest]
        public IEnumerator AssignCrewRandomly_DistributesCrewAcrossModules()
        {
            var ship = CreateShipWithModules(
                (crewNeeded: 2, mainSkill: CrewSkillType.Navigation),
                (crewNeeded: 2, mainSkill: CrewSkillType.Mechanics));
            yield return WaitForLifecycle();

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
            yield return WaitForLifecycle();

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
            yield return WaitForLifecycle();

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
            yield return WaitForLifecycle();

            var navExpert = MakeCrew("Nav", "Expert", 30,
                new Dictionary<CrewSkillType, int> { { CrewSkillType.Navigation, 10 } });
            var mechExpert = MakeCrew("Mech", "Expert", 30,
                new Dictionary<CrewSkillType, int> { { CrewSkillType.Mechanics, 10 } });

            ship.AssignCrewBySkill(new List<CrewMember> { mechExpert, navExpert });

            var navModule = ship.ModuleGraph.GetAllNodes().AsValueEnumerable()
                .First(m => m.Type == ModuleType.Resources);

            Assert.Contains(navExpert, (ICollection)navModule.AssignedCrew);
        }

        [UnityTest]
        public IEnumerator CrewMissingCount_SumsAcrossAllModules()
        {
            var ship = CreateShipWithModules(
                (crewNeeded: 3, mainSkill: CrewSkillType.Navigation),
                (crewNeeded: 5, mainSkill: CrewSkillType.Mechanics));
            yield return WaitForLifecycle();

            // Command module has 0 crewNeeded, so total = 3 + 5 = 8
            Assert.AreEqual(8, ship.CrewMissingCount);
        }

        [UnityTest]
        public IEnumerator CrewMissingCount_AfterAssignment_ReflectsRemaining()
        {
            var ship = CreateShipWithModules(
                (crewNeeded: 3, mainSkill: CrewSkillType.Navigation),
                (crewNeeded: 2, mainSkill: CrewSkillType.Mechanics));
            yield return WaitForLifecycle();

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
            yield return WaitForLifecycle();

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
            yield return WaitForLifecycle();

            var crew = new List<CrewMember> { MakeCrew("Only", "One", 25) };

            ship.AssignCrewBySkill(crew);

            var totalAssigned = ship.ModuleGraph.GetAllNodes().AsValueEnumerable()
                .Sum(m => m.AliveCrewCount);
            Assert.AreEqual(1, totalAssigned);
            Assert.Greater(ship.CrewMissingCount, 0);
        }
    }
}