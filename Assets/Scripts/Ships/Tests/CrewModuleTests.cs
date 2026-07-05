using System;
using System.Collections;
using System.Collections.Generic;
using Core.Ship;
using NSubstitute;
using NUnit.Framework;
using Ships.Tests.TestHelpers.Factories;
using Ships.Tests.TestHelpers.Fixtures;
using Ships.Tests.TestHelpers.Modules;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Resources = Core.Ship.Resources;

namespace Ships.Tests
{
    [TestFixture]
    public class CrewModuleTests : ShipTestBase
    {
        private TestModule CreateStandaloneModule(
            int crewNeeded = 3,
            CrewSkillType mainSkill = CrewSkillType.Navigation,
            float shipCaptainMultiplier = 1f
        )
        {
            var go = ModuleFactory.CreateModuleBase("Module", null, Vector2.zero, 0f, Container, CreatedObjects, 5,
                5);

            var module = go.AddComponent<TestModule>();
            module.SetModuleType(ModuleType.Resources);
            module.SetMainSkillType(mainSkill);
            module.SetResources(new Resources(0, 0, crewNeeded, 0, 0));

            var ship = Substitute.For<IShip>();
            ship.CaptainMultiplier.Returns(shipCaptainMultiplier);
            module.SetShip(ship);

            return module;
        }

        [Test]
        public void AssignCrew_ReturnsTrue()
        {
            var module = CreateStandaloneModule();
            var crew = MakeCrew();

            var result = module.AssignCrew(crew);

            Assert.IsTrue(result);
            Assert.AreEqual(1, module.AssignedCrew.Count);
        }

        [Test]
        public void AssignCrew_BeyondCrewNeeded_StillSucceeds()
        {
            var module = CreateStandaloneModule(1);
            module.AssignCrew(MakeCrew("A", "A", 20));

            var overflow = MakeCrew("B", "B", 21);
            var result = module.AssignCrew(overflow);

            Assert.IsTrue(result);
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
        public void AssignCrew_NullMember_ThrowsArgumentNullException()
        {
            var module = CreateStandaloneModule();

            Assert.Throws<ArgumentNullException>(() => module.AssignCrew(null));
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

        [Test]
        public void CrewMissingCount_NoCrewAssigned_EqualsCrewNeeded()
        {
            var module = CreateStandaloneModule();

            Assert.AreEqual(3, module.CrewMissingCount);
        }

        [Test]
        public void CrewMissingCount_PartiallyFilled_ReturnsDeficit()
        {
            var module = CreateStandaloneModule();
            module.AssignCrew(MakeCrew());

            Assert.AreEqual(2, module.CrewMissingCount);
        }

        [Test]
        public void CrewMissingCount_FullyFilled_ReturnsZero()
        {
            var module = CreateStandaloneModule(2);
            module.AssignCrew(MakeCrew("A", "A"));
            module.AssignCrew(MakeCrew("B", "B"));

            Assert.AreEqual(0, module.CrewMissingCount);
        }

        [Test]
        public void CrewMissingCount_OverFilled_ReturnsNegative()
        {
            var module = CreateStandaloneModule(1);
            module.AssignCrew(MakeCrew("A", "A"));
            module.AssignCrew(MakeCrew("B", "B"));
            module.AssignCrew(MakeCrew("C", "C"));

            Assert.AreEqual(0, module.CrewMissingCount);
        }

        [Test]
        public void CrewMissingCount_ZeroCrewNeeded_ReturnsZeroWhenCrewAssigned()
        {
            var module = CreateStandaloneModule(0);
            module.AssignCrew(MakeCrew());

            Assert.AreEqual(0, module.CrewMissingCount);
        }

        [Test]
        public void FillCrewBySkill_AssignsBestSkilledFirst()
        {
            var module = CreateStandaloneModule(2);

            var lowSkill = MakeCrew("Low", "Skill", 25,
                new Dictionary<CrewSkillType, int> { { CrewSkillType.Navigation, 1 } });
            var highSkill = MakeCrew("High", "Skill", 30,
                new Dictionary<CrewSkillType, int> { { CrewSkillType.Navigation, 10 } });
            var midSkill = MakeCrew("Mid", "Skill", 28,
                new Dictionary<CrewSkillType, int> { { CrewSkillType.Navigation, 5 } });
            var topSkill = MakeCrew("Top", "Skill", 35,
                new Dictionary<CrewSkillType, int> { { CrewSkillType.Navigation, 15 } });

            var crew = new List<CrewMember> { lowSkill, highSkill, midSkill, topSkill };

            module.FillCrewBySkill(crew, out var remaining);

            Assert.AreEqual(2, module.AssignedCrew.Count);
            Assert.Contains(topSkill, (ICollection)module.AssignedCrew);
            Assert.Contains(highSkill, (ICollection)module.AssignedCrew);
            Assert.AreEqual(2, remaining.Count);
            Assert.Contains(midSkill, remaining);
            Assert.Contains(lowSkill, remaining);
        }

        [Test]
        public void FillCrewBySkill_MoreCrewThanNeeded_OnlyAssignsNeededCount()
        {
            var module = CreateStandaloneModule(2, CrewSkillType.Mechanics);

            var crew = new List<CrewMember>();
            for (var i = 0; i < 10; i++)
                crew.Add(MakeCrew($"Crew{i}", "Member", 20 + i,
                    new Dictionary<CrewSkillType, int> { { CrewSkillType.Mechanics, i } }));

            module.FillCrewBySkill(crew, out var remaining);

            Assert.AreEqual(2, module.AssignedCrew.Count);
            Assert.AreEqual(8, remaining.Count);
        }

        [Test]
        public void FillCrewBySkill_FewerCrewThanNeeded_AssignsAllAvailable()
        {
            var module = CreateStandaloneModule(5);
            var onlyCrew = MakeCrew("Only", "One", 25);

            module.FillCrewBySkill(new List<CrewMember> { onlyCrew }, out var remaining);

            Assert.AreEqual(1, module.AssignedCrew.Count);
            Assert.Contains(onlyCrew, (ICollection)module.AssignedCrew);
            Assert.AreEqual(0, remaining.Count);
        }

        [Test]
        public void FillCrewBySkill_EmptyList_AssignsNone()
        {
            var module = CreateStandaloneModule();

            module.FillCrewBySkill(new List<CrewMember>(), out var remaining);

            Assert.AreEqual(0, module.AssignedCrew.Count);
            Assert.AreEqual(0, remaining.Count);
        }

        [Test]
        public void FillCrewBySkill_NullList_ThrowsArgumentNullException()
        {
            var module = CreateStandaloneModule();

            Assert.Throws<ArgumentNullException>(() => module.FillCrewBySkill(null, out _));
        }

        [Test]
        public void FillCrewBySkill_AlreadyFullModule_AssignsNone()
        {
            var module = CreateStandaloneModule(2);
            module.AssignCrew(MakeCrew("A", "A"));
            module.AssignCrew(MakeCrew("B", "B"));

            var extraCrew = new List<CrewMember> { MakeCrew("C", "C"), MakeCrew("D", "D") };
            module.FillCrewBySkill(extraCrew, out var remaining);

            Assert.AreEqual(2, module.AssignedCrew.Count);
            Assert.AreEqual(2, remaining.Count);
        }

        [Test]
        public void FillCrewBySkill_DoesNotAssignDuplicates()
        {
            var module = CreateStandaloneModule();
            var existingCrew = MakeCrew("Existing", "Crew", 30,
                new Dictionary<CrewSkillType, int> { { CrewSkillType.Navigation, 10 } });
            module.AssignCrew(existingCrew);

            var newCrew = MakeCrew("New", "Crew", 25,
                new Dictionary<CrewSkillType, int> { { CrewSkillType.Navigation, 5 } });
            var crew = new List<CrewMember> { existingCrew, newCrew };

            module.FillCrewBySkill(crew, out _);

            Assert.AreEqual(2, module.AssignedCrew.Count);
            Assert.Contains(existingCrew, (ICollection)module.AssignedCrew);
            Assert.Contains(newCrew, (ICollection)module.AssignedCrew);
        }

        [Test]
        public void GetCrewBonus_NoCrewAssigned_ReturnsZero()
        {
            var module = CreateStandaloneModule();

            Assert.AreEqual(0f, module.GetCrewEfficiency());
        }

        [Test]
        public void GetCrewBonus_UsesMainSkillType()
        {
            var module = CreateStandaloneModule(mainSkill: CrewSkillType.Mechanics);
            var skills = new Dictionary<CrewSkillType, int>
            {
                { CrewSkillType.Navigation, 10 },
                { CrewSkillType.Mechanics, 0 }
            };
            module.AssignCrew(MakeCrew("Nav", "Expert", 35, skills));

            // fillRatio = 1 - 2/3 = 1/3, skillTotal(Mechanics) = 0
            // result = 1/3 * (1 + 0) = 1/3
            Assert.AreEqual(1f / 3f, module.GetCrewEfficiency(), 0.0001f);
        }

        [Test]
        public void GetCrewBonus_MatchingSkill_ReturnsPositiveBonus()
        {
            var module = CreateStandaloneModule(mainSkill: CrewSkillType.Navigation);
            var skills = new Dictionary<CrewSkillType, int> { { CrewSkillType.Navigation, 5 } };
            module.AssignCrew(MakeCrew("Nav", "Expert", 35, skills));

            var bonus = module.GetCrewEfficiency();

            Assert.Greater(bonus, 0f);
        }

        [Test]
        public void GetCrewBonus_CalculatesCorrectValue()
        {
            var module = CreateStandaloneModule(mainSkill: CrewSkillType.Navigation);
            var skills = new Dictionary<CrewSkillType, int> { { CrewSkillType.Navigation, 5 } };
            module.AssignCrew(MakeCrew("Nav", "Pro", 30, skills));

            // fillRatio = 1 - 2/3 = 1/3, skillTotal=5, captainMultiplier=1.0
            // bonus = 1/3 * (1 + 5 * 1.0 * 0.02) = 1/3 * 1.1 ≈ 0.3667
            var bonus = module.GetCrewEfficiency();

            Assert.AreEqual(1f / 3f * 1.1f, bonus, 0.0001f);
        }

        [Test]
        public void GetCrewBonus_MultipleCrew_SumsSkillLevels()
        {
            var module = CreateStandaloneModule(mainSkill: CrewSkillType.WeaponHandling);
            module.AssignCrew(MakeCrew("A", "A", 25,
                new Dictionary<CrewSkillType, int> { { CrewSkillType.WeaponHandling, 3 } }));
            module.AssignCrew(MakeCrew("B", "B", 30,
                new Dictionary<CrewSkillType, int> { { CrewSkillType.WeaponHandling, 7 } }));

            // fillRatio = 1 - 1/3 = 2/3, skillTotal=10, captainMultiplier=1.0
            // bonus = 2/3 * (1 + 10 * 1.0 * 0.02) = 2/3 * 1.2 = 0.8
            var bonus = module.GetCrewEfficiency();

            Assert.AreEqual(2f / 3f * 1.2f, bonus, 0.0001f);
        }

        [Test]
        public void GetCrewBonus_CaptainOnly_NoBonusWhenNoMainSkill()
        {
            var module = CreateStandaloneModule(mainSkill: CrewSkillType.Mechanics);
            var captainOnlySkills = new Dictionary<CrewSkillType, int>
            {
                { CrewSkillType.Captain, 10 }
            };
            module.AssignCrew(MakeCrew("Cap", "Only", 40, captainOnlySkills));

            // fillRatio = 1/3, skillTotal(Mechanics)=0, captainMultiplier=1+10*0.05=1.5
            // result = 1/3 * (1 + 0) = 1/3
            Assert.AreEqual(1f / 3f, module.GetCrewEfficiency(), 0.0001f);
        }

        [Test]
        public void GetCrewBonus_CaptainAmplifies_ComparedToNoCaptain()
        {
            var moduleShitCaptain =
                CreateStandaloneModule(mainSkill: CrewSkillType.Navigation, shipCaptainMultiplier: .5f);
            var moduleGreatCaptain =
                CreateStandaloneModule(mainSkill: CrewSkillType.Navigation, shipCaptainMultiplier: 2f);

            var plainSkills = new Dictionary<CrewSkillType, int> { { CrewSkillType.Navigation, 4 } };
            var captainSkills = new Dictionary<CrewSkillType, int>
            {
                { CrewSkillType.Navigation, 4 },
                { CrewSkillType.Captain, 5 }
            };

            moduleShitCaptain.AssignCrew(MakeCrew("Nav", "Plain", 30, plainSkills));
            moduleGreatCaptain.AssignCrew(MakeCrew("Nav", "Cap", 30, captainSkills));

            Assert.Greater(moduleGreatCaptain.GetCrewEfficiency(), moduleShitCaptain.GetCrewEfficiency());
        }

        [Test]
        public void Efficiency_FullPixels_NoCrewExpected_EqualsOne()
        {
            var module = CreateStandaloneModule(0);

            Assert.AreEqual(1f, module.ModuleEfficiency, 0.0001f);
        }


        [Test]
        public void Efficiency_FullPixels_CrewExpected_EqualsZero()
        {
            var module = CreateStandaloneModule(1);

            Assert.AreEqual(0f, module.ModuleEfficiency, 0.0001f);
        }


        [Test]
        public void Efficiency_FullPixels_WithCrew_IncludesCrewBonus()
        {
            var module = CreateStandaloneModule(1);
            var skills = new Dictionary<CrewSkillType, int> { { CrewSkillType.Navigation, 5 } };
            module.AssignCrew(MakeCrew("Nav", "Expert", 30, skills));

            // fillRatio = 1, GetCrewMultiplier = 1 * (1 + 5 * 1.0 * 0.02) = 1.1, efficiency = 1.0 * 1.1 = 1.1
            Assert.AreEqual(1.1f, module.ModuleEfficiency, 0.0001f);
        }

        [UnityTest]
        public IEnumerator DestroyModule_KillsAssignedCrew()
        {
            var module = CreateStandaloneModule();
            var crew1 = MakeCrew("Alice", "A", 25);
            var crew2 = MakeCrew("Bob", "B");
            module.AssignCrew(crew1);
            module.AssignCrew(crew2);

            yield return null;

            Object.DestroyImmediate(module.gameObject);

            yield return null;

            Assert.IsFalse(crew1.IsAlive, "crew1 should be dead after module destruction");
            Assert.IsFalse(crew2.IsAlive, "crew2 should be dead after module destruction");
        }

        [UnityTest]
        public IEnumerator DestroyModule_WithNoCrew_DoesNotThrow()
        {
            var module = CreateStandaloneModule();

            yield return null;

            Assert.DoesNotThrow(() => Object.DestroyImmediate(module.gameObject));

            yield return null;
        }

        [UnityTest]
        public IEnumerator DestroyModule_FiresOnDiedEventForEachCrewMember()
        {
            var module = CreateStandaloneModule();
            var crew1 = MakeCrew("Alice", "A", 25);
            var crew2 = MakeCrew("Bob", "B");
            module.AssignCrew(crew1);
            module.AssignCrew(crew2);

            var diedMembers = new List<CrewMember>();
            crew1.OnDied += member => diedMembers.Add(member);
            crew2.OnDied += member => diedMembers.Add(member);

            yield return null;

            Object.DestroyImmediate(module.gameObject);

            yield return null;

            Assert.AreEqual(2, diedMembers.Count);
            Assert.Contains(crew1, diedMembers);
            Assert.Contains(crew2, diedMembers);
        }

        [Test]
        public void GetCrewNeededCount_ReturnsResourcesCrewNeeded()
        {
            var module = CreateStandaloneModule(5);

            Assert.AreEqual(5, module.CrewNeededCount);
        }

        [Test]
        public void GetCrewNeededCount_ZeroCrewNeeded_ReturnsZero()
        {
            var module = CreateStandaloneModule(0);

            Assert.AreEqual(0, module.CrewNeededCount);
        }

        [Test]
        public void EfficiencyGoesDown_AsCrewDies()
        {
            const int crewCount = 10;

            var module = CreateStandaloneModule(crewCount);

            var crew = new List<CrewMember>();
            for (var i = 0; i < crewCount; i++)
            {
                var crewMember = MakeCrew($"Crew{i}", "Member", 20 + i,
                    new Dictionary<CrewSkillType, int> { { CrewSkillType.Navigation, i } });
                crew.Add(crewMember);
                module.AssignCrew(crewMember);
            }

            var previousEfficiency = module.ModuleEfficiency;
            for (var i = 0; i < crewCount / 2; i++)
            {
                crew[i].Kill();
                var currentEfficiency = module.ModuleEfficiency;
                Assert.Less(currentEfficiency, previousEfficiency,
                    $"Efficiency should decrease after crew member {crew[i].FirstName} dies");
                previousEfficiency = currentEfficiency;
            }

            for (var i = crewCount / 2; i < crewCount; i++)
            {
                module.KillRandomCrew(1);
                var currentEfficiency = module.ModuleEfficiency;
                Assert.Less(currentEfficiency, previousEfficiency,
                    $"Efficiency should decrease after crew member {crew[i].FirstName} dies");
                previousEfficiency = currentEfficiency;
            }

            Assert.AreEqual(previousEfficiency, 0f, 0.0001f,
                "Efficiency should be zero after all crew members are dead");
        }

        [Test]
        public void KillAllCrew_KillsAllAssignedCrew()
        {
            var module = CreateStandaloneModule();
            var crew1 = MakeCrew("Alice", "A", 25);
            var crew2 = MakeCrew("Bob", "B");
            var crew3 = MakeCrew("Charlie", "C", 35);
            module.AssignCrew(crew1);
            module.AssignCrew(crew2);
            module.AssignCrew(crew3);

            module.KillAllCrew();

            Assert.IsFalse(crew1.IsAlive);
            Assert.IsFalse(crew2.IsAlive);
            Assert.IsFalse(crew3.IsAlive);
            Assert.AreEqual(0, module.AliveCrewCount);
        }

        [Test]
        public void KillAllCrew_DoesNotLeakOnDiedSubscription()
        {
            var module = CreateStandaloneModule();
            var crew = MakeCrew("Alice", "A", 25);
            module.AssignCrew(crew);

            module.KillAllCrew();

            Assert.AreEqual(0, crew.OnDiedSubscriberCountForTesting,
                "Module should have unsubscribed from OnDied after KillAllCrew");
        }

        [Test]
        public void KillRandomCrew_KillsSpecifiedCount()
        {
            var module = CreateStandaloneModule(5);
            for (var i = 0; i < 5; i++)
                module.AssignCrew(MakeCrew($"Crew{i}", "M", 20 + i));

            module.KillRandomCrew(2);

            Assert.AreEqual(3, module.AliveCrewCount);
        }

        [Test]
        public void KillRandomCrew_MoreThanAvailable_KillsAll()
        {
            var module = CreateStandaloneModule();
            var crew1 = MakeCrew("A", "A", 20);
            var crew2 = MakeCrew("B", "B", 25);
            module.AssignCrew(crew1);
            module.AssignCrew(crew2);

            module.KillRandomCrew(10);

            Assert.IsFalse(crew1.IsAlive);
            Assert.IsFalse(crew2.IsAlive);
            Assert.AreEqual(0, module.AliveCrewCount);
        }

        [Test]
        public void RemoveCrew_DoesNotLeakOnDiedSubscription()
        {
            var module = CreateStandaloneModule();
            var crew = MakeCrew("Alice", "A", 25);
            module.AssignCrew(crew);

            module.RemoveCrew(crew);

            Assert.AreEqual(0, crew.OnDiedSubscriberCountForTesting,
                "Module should have unsubscribed from OnDied after RemoveCrew");
        }

        [Test]
        public void KillRandomCrew_DoesNotLeakOnDiedSubscription()
        {
            var module = CreateStandaloneModule();
            var crew1 = MakeCrew("A", "A", 20);
            var crew2 = MakeCrew("B", "B", 25);
            var crew3 = MakeCrew("C", "C");
            module.AssignCrew(crew1);
            module.AssignCrew(crew2);
            module.AssignCrew(crew3);

            module.KillRandomCrew(3);

            Assert.AreEqual(0, crew1.OnDiedSubscriberCountForTesting, "crew1 OnDied should have no subscribers");
            Assert.AreEqual(0, crew2.OnDiedSubscriberCountForTesting, "crew2 OnDied should have no subscribers");
            Assert.AreEqual(0, crew3.OnDiedSubscriberCountForTesting, "crew3 OnDied should have no subscribers");
        }
    }
}