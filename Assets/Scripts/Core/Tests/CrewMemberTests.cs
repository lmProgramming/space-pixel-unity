using System.Collections.Generic;
using Core.Ship;
using NUnit.Framework;

namespace Core.Tests
{
    [TestFixture]
    public class CrewMemberTests
    {
        [Test]
        public void CrewMember_HasCorrectNameAndAge()
        {
            var crew = new CrewMember("Jane", "Doe", 30);

            Assert.AreEqual("Jane", crew.FirstName);
            Assert.AreEqual("Doe", crew.LastName);
            Assert.AreEqual(30, crew.Age);
        }

        [Test]
        public void CrewMember_IsAliveByDefault()
        {
            var crew = new CrewMember("John", "Smith", 25);

            Assert.IsTrue(crew.IsAlive);
        }

        [Test]
        public void Kill_SetsIsAliveToFalse()
        {
            var crew = new CrewMember("John", "Smith", 25);

            crew.Kill();

            Assert.IsFalse(crew.IsAlive);
        }

        [Test]
        public void Kill_FiresOnDiedEvent()
        {
            var crew = new CrewMember("John", "Smith", 25);
            CrewMember diedMember = null;
            crew.OnDied += m => diedMember = m;

            crew.Kill();

            Assert.AreSame(crew, diedMember);
        }

        [Test]
        public void Kill_CalledTwice_OnDiedFiredOnce()
        {
            var crew = new CrewMember("John", "Smith", 25);
            var count = 0;
            crew.OnDied += _ => count++;

            crew.Kill();
            crew.Kill();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void GetSkillLevel_ReturnsCorrectLevel()
        {
            var skills = new Dictionary<CrewSkillType, int>
            {
                { CrewSkillType.Navigation, 5 },
                { CrewSkillType.Captain, 3 }
            };
            var crew = new CrewMember("Ana", "Nova", 28, skills);

            Assert.AreEqual(5, crew.GetSkillLevel(CrewSkillType.Navigation));
            Assert.AreEqual(3, crew.GetSkillLevel(CrewSkillType.Captain));
        }

        [Test]
        public void GetSkillLevel_UnsetSkill_ReturnsZero()
        {
            var crew = new CrewMember("Ana", "Nova", 28);

            Assert.AreEqual(0, crew.GetSkillLevel(CrewSkillType.WeaponHandling));
        }

        [Test]
        public void CrewMember_NoSkillsDictionary_AllSkillsZero()
        {
            var crew = new CrewMember("Bob", "Ray", 40);

            foreach (CrewSkillType skill in System.Enum.GetValues(typeof(CrewSkillType)))
                Assert.AreEqual(0, crew.GetSkillLevel(skill));
        }
    }
}
