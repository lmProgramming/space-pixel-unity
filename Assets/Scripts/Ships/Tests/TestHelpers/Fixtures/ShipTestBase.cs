using System.Collections;
using System.Collections.Generic;
using Core.Ships;
using NUnit.Framework;
using UnityEngine;
using Zenject;
using ZLinq;
using Object = UnityEngine.Object;

namespace Ships.Tests.TestHelpers.Fixtures
{
    public abstract class ShipTestBase
    {
        protected readonly List<GameObject> CreatedObjects = new();
        protected DiContainer Container;
        protected GameObject TestRoot;

        [SetUp]
        public virtual void SetUp()
        {
            TestRoot = new GameObject("TestRoot");
            CreatedObjects.Add(TestRoot);
            Container = TestContainerFactory.CreateTestContainer();
        }

        [TearDown]
        public virtual void TearDown()
        {
            foreach (var obj in CreatedObjects.AsValueEnumerable().Where(obj => obj != null))
                Object.DestroyImmediate(obj);
        }

        protected static IEnumerator WaitForLifecycle()
        {
            yield return null;
            yield return null;
        }

        protected static CrewMember MakeCrew(string first = "John", string last = "Doe", int age = 30,
            Dictionary<CrewSkillType, int> skills = null)
        {
            return new CrewMember(first, last, age, skills);
        }
    }
}