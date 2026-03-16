using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Ship;
using NSubstitute;
using NUnit.Framework;
using ShipFactory.LegalPositionCalculator;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ShipFactory.Tests.LegalPositionCalculator
{
    [TestFixture]
    public class CalculatorTests
    {
        private readonly List<Object> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
                if (obj != null)
                    Object.DestroyImmediate(obj);

            _createdObjects.Clear();
        }

        [Test]
        public void CalculateLegalityPosition_OverlappingModule_ReturnsInsideOther()
        {
            var command = CreateBundle("Command", ModuleType.Command, Vector2.zero, new Vector2Int(2, 2));
            var overlap = CreateBundle("Overlap", ModuleType.Resources, Vector2.zero, new Vector2Int(2, 2));

            var legality = Calculator.CalculateLegalityPosition(overlap, new[] { command, overlap });

            Assert.That(legality, Is.EqualTo(PositionLegality.InsideOther));
        }

        [Test]
        public void CalculateLegalityPosition_IsolatedModule_ReturnsOutsideShip()
        {
            var command = CreateBundle("Command", ModuleType.Command, Vector2.zero, new Vector2Int(2, 2));
            var isolated = CreateBundle("Isolated", ModuleType.Resources, new Vector2(10f, 0f), new Vector2Int(2, 2));

            var legality = Calculator.CalculateLegalityPosition(isolated, new[] { command, isolated });

            Assert.That(legality, Is.EqualTo(PositionLegality.OutsideShip));
        }

        [Test]
        public void CalculateLegalityPosition_ChainStillConnected_ReturnsCorrect()
        {
            var command = CreateBundle("A", ModuleType.Command, new Vector2(0f, 0f), new Vector2Int(2, 2));
            var middle = CreateBundle("B", ModuleType.Resources, new Vector2(2f, 0f), new Vector2Int(2, 2));
            var tail = CreateBundle("C", ModuleType.Engine, new Vector2(4f, 0f), new Vector2Int(2, 2));

            var legality = Calculator.CalculateLegalityPosition(middle, new[] { command, middle, tail });

            Assert.That(legality, Is.EqualTo(PositionLegality.Correct));
        }

        [Test]
        public void CalculateLegalityPosition_ChainMiddleMovedAndTailDisconnected_ReturnsDisconnectsShip()
        {
            var command = CreateBundle("A", ModuleType.Command, new Vector2(0f, 0f), new Vector2Int(2, 2));
            var movedMiddle = CreateBundle("B", ModuleType.Resources, new Vector2(-2f, 0f), new Vector2Int(2, 2));
            var tail = CreateBundle("C", ModuleType.Engine, new Vector2(4f, 0f), new Vector2Int(2, 2));

            var legality = Calculator.CalculateLegalityPosition(movedMiddle, new[] { command, movedMiddle, tail });

            Assert.That(legality, Is.EqualTo(PositionLegality.DisconnectsShip));
        }

        private ShipModuleSOInstanceBundle CreateBundle(string name, ModuleType moduleType, Vector2 worldPosition,
            Vector2Int dimensions)
        {
            var go = new GameObject(name);
            go.transform.position = worldPosition;
            _createdObjects.Add(go);

            var moduleSO = ScriptableObject.CreateInstance<ShipModuleSO>();
            _createdObjects.Add(moduleSO);

            SetPrivateField(moduleSO, "partName", name);
            SetPrivateField(moduleSO, "description", $"{name} description");
            SetPrivateField(moduleSO, "dimensions", dimensions);
            SetPrivateField(moduleSO, "prefab", go);

            var module = Substitute.For<IModule>();
            module.Type.Returns(moduleType);

            return new ShipModuleSOInstanceBundle(go, moduleSO, module);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new InvalidOperationException($"Field '{fieldName}' not found on {target.GetType().Name}.");

            field.SetValue(target, value);
        }
    }
}

