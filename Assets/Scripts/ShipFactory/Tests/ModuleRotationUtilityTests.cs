using System.Collections.Generic;
using Core.Ships;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using ZLinq;
using Object = UnityEngine.Object;

namespace ShipFactory.Tests
{
    [TestFixture]
    public class ModuleRotationUtilityTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects.AsValueEnumerable().Where(obj => obj != null))
                Object.DestroyImmediate(obj);

            _createdObjects.Clear();
        }

        private readonly List<Object> _createdObjects = new();

        [Test]
        public void GetQuarterTurns_NegativeEulerAngle_NormalizesToPositiveQuarterTurn()
        {
            var transform = CreateTransform(-90f);

            Assert.That(ModuleRotationUtility.CalculateQuarterTurns(transform), Is.EqualTo(3));
        }

        [Test]
        public void GetWorldFootprintDimensions_RotatedNonSquareModule_SwapsExtents()
        {
            var bundle = CreateBundle(new Vector2(0f, 0f), new Vector2Int(40, 24), 90f);

            var footprint = ModuleRotationUtility.GetWorldFootprintDimensions(
                bundle.Instance.transform.rotation,
                bundle.ModuleSO.Dimensions);

            Assert.That(footprint, Is.EqualTo(new Vector2Int(24, 40)));
        }

        [Test]
        public void GetAxisAlignedBounds_RotatedNonSquareModule_UsesExactIntegerExtents()
        {
            var bundle = CreateBundle(new Vector2(0f, 3f), new Vector2Int(4, 2), 90f);

            var (min, max) = ModuleRotationUtility.GetAxisAlignedBounds(bundle);

            Assert.IsTrue(min == new Vector2(-1f, 1f));
            Assert.IsTrue(max == new Vector2(1f, 5f));
        }

        [Test]
        public void ContainsWorldPoint_RotatedModule_UsesOrientedBounds()
        {
            var bundle = CreateBundle(new Vector2(0f, 0f), new Vector2Int(40, 24), 90f);

            Assert.That(ModuleRotationUtility.ContainsWorldPoint(bundle, new Vector2(10f, 0f)), Is.True);
            Assert.That(ModuleRotationUtility.ContainsWorldPoint(bundle, new Vector2(0f, 18f)), Is.True);
            Assert.That(ModuleRotationUtility.ContainsWorldPoint(bundle, new Vector2(20f, 0f)), Is.False);
            Assert.That(ModuleRotationUtility.ContainsWorldPoint(bundle, new Vector2(0f, 25f)), Is.False);
        }

        [Test]
        public void ApplyQuarterTurn_FromNegativeEulerAngle_StillAdvancesQuarterTurn()
        {
            var bundle = CreateBundle(Vector2.zero, new Vector2Int(16, 16), -90f);

            ModuleRotationUtility.ApplyQuarterTurn(bundle, 1);

            Assert.That(ModuleRotationUtility.CalculateQuarterTurns(bundle.Instance.transform), Is.EqualTo(0));
        }

        private Transform CreateTransform(float rotationZ)
        {
            var go = new GameObject("RotationTest");
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
            _createdObjects.Add(go);
            return go.transform;
        }

        private ShipModuleSOInstanceBundle CreateBundle(Vector2 worldPosition, Vector2Int dimensions, float rotationZ)
        {
            var go = new GameObject("Module");
            go.transform.position = worldPosition;
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
            _createdObjects.Add(go);

            var moduleSO = ScriptableObject.CreateInstance<ShipModuleSO>();
            _createdObjects.Add(moduleSO);
            moduleSO.ConfigureForTesting("Module", "desc", dimensions, go);

            var module = Substitute.For<IModule>();
            return new ShipModuleSOInstanceBundle(go, moduleSO, module);
        }
    }
}