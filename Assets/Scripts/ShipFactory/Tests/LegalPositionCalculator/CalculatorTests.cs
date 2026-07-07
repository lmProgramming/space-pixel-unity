using System.Collections.Generic;
using Core.Ships;
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
        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
                if (obj != null)
                    Object.DestroyImmediate(obj);

            _createdObjects.Clear();
        }

        private readonly List<Object> _createdObjects = new();

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

        [Test]
        public void CalculateLegalityPosition_RotatedNonSquareModule_ConnectsWhereUnrotatedWouldNot()
        {
            var command = CreateBundle("Command", ModuleType.Command, Vector2.zero, new Vector2Int(2, 2));
            var unrotatedAbove =
                CreateBundle("Unrotated", ModuleType.Engine, new Vector2(0f, 3f), new Vector2Int(4, 2));
            var rotatedAbove =
                CreateBundle("Rotated", ModuleType.Engine, new Vector2(0f, 3f), new Vector2Int(4, 2), 90f);

            var unrotatedLegality =
                Calculator.CalculateLegalityPosition(unrotatedAbove, new[] { command, unrotatedAbove });
            var rotatedLegality = Calculator.CalculateLegalityPosition(rotatedAbove, new[] { command, rotatedAbove });

            Assert.That(unrotatedLegality, Is.EqualTo(PositionLegality.OutsideShip));
            Assert.That(rotatedLegality, Is.EqualTo(PositionLegality.Correct));
        }

        [Test]
        public void CalculateLegalityPosition_OddMultipleOfEightModules_TouchWhenEdgeAligned()
        {
            var command = CreateBundle("Command", ModuleType.Command, new Vector2(12f, 12f), new Vector2Int(24, 24));
            var adjacent = CreateBundle("Adjacent", ModuleType.Engine, new Vector2(36f, 12f), new Vector2Int(24, 24));

            var legality = Calculator.CalculateLegalityPosition(adjacent, new[] { command, adjacent });

            Assert.That(legality, Is.EqualTo(PositionLegality.Correct));
        }

        [Test]
        public void CalculateLegalityPosition_RotatedModule_OverlapStillDetected()
        {
            var command = CreateBundle("Command", ModuleType.Command, Vector2.zero, new Vector2Int(2, 2));
            var rotatedOverlap = CreateBundle("RotatedOverlap", ModuleType.Resources, new Vector2(1f, 1f),
                new Vector2Int(4, 2), 90f);

            var legality = Calculator.CalculateLegalityPosition(rotatedOverlap, new[] { command, rotatedOverlap });

            Assert.That(legality, Is.EqualTo(PositionLegality.InsideOther));
        }

        [Test]
        public void CalculateLegalityPosition_CornerTouchOnly_ReturnsOutsideShip()
        {
            var command = CreateBundle("Command", ModuleType.Command, new Vector2(12f, 12f), new Vector2Int(24, 24));
            var cornerTouch = CreateBundle("Corner", ModuleType.Engine, new Vector2(36f, 36f), new Vector2Int(24, 24));

            var legality = Calculator.CalculateLegalityPosition(cornerTouch, new[] { command, cornerTouch });

            Assert.That(legality, Is.EqualTo(PositionLegality.OutsideShip));
        }

        [TestCase(0f)]
        [TestCase(90f)]
        [TestCase(180f)]
        [TestCase(270f)]
        public void CalculateLegalityPosition_RotatedModule_CornerTouchOnly_ReturnsOutsideShip(float rotationZ)
        {
            var command = CreateBundle("Command", ModuleType.Command, new Vector2(12f, 12f), new Vector2Int(24, 24));
            var cornerTouch = CreateBundle("Corner", ModuleType.Engine, new Vector2(32f, 36f), new Vector2Int(24, 16),
                rotationZ);

            var legality = Calculator.CalculateLegalityPosition(cornerTouch, new[] { command, cornerTouch });

            Assert.That(legality, Is.EqualTo(PositionLegality.OutsideShip));
        }

        [TestCase(0f)]
        [TestCase(90f)]
        [TestCase(180f)]
        [TestCase(270f)]
        public void CalculateLegalityPosition_AdjacentSquareModule_RemainsCorrectAtAnyQuarterTurn(float rotationZ)
        {
            var command = CreateBundle("Command", ModuleType.Command, new Vector2(12f, 12f), new Vector2Int(24, 24));
            var adjacent = CreateBundle("Adjacent", ModuleType.Engine, new Vector2(36f, 12f), new Vector2Int(24, 24),
                rotationZ);

            var legality = Calculator.CalculateLegalityPosition(adjacent, new[] { command, adjacent });

            Assert.That(legality, Is.EqualTo(PositionLegality.Correct));
        }

        [Test]
        public void CalculateLegalityPosition_Rotated24x16_AdjacentToSquareModule_IsCorrect()
        {
            var command = CreateBundle("Command", ModuleType.Command, new Vector2(12f, 12f), new Vector2Int(24, 24));
            var adjacent = CreateBundle("Adjacent", ModuleType.Engine, new Vector2(32f, 12f), new Vector2Int(24, 16),
                90f);

            var legality = Calculator.CalculateLegalityPosition(adjacent, new[] { command, adjacent });

            Assert.That(legality, Is.EqualTo(PositionLegality.Correct));
        }

        [Test]
        public void CalculateLegalityPosition_RectangularModule_RemainsCorrectForEachQuarterTurnPlacement()
        {
            var command = CreateBundle("Command", ModuleType.Command, new Vector2(0f, 0f), new Vector2Int(24, 24));

            AssertTouchingPlacementIsCorrect(command, new Vector2(32f, 12f), new Vector2Int(40, 24), 0f);
            AssertTouchingPlacementIsCorrect(command, new Vector2(24f, 12f), new Vector2Int(40, 24), 90f);
            AssertTouchingPlacementIsCorrect(command, new Vector2(32f, 12f), new Vector2Int(40, 24), 180f);
            AssertTouchingPlacementIsCorrect(command, new Vector2(24f, 12f), new Vector2Int(40, 24), 270f);
        }

        private void AssertTouchingPlacementIsCorrect(ShipModuleSOInstanceBundle command, Vector2 worldPosition,
            Vector2Int dimensions, float rotationZ)
        {
            var adjacent = CreateBundle("Adjacent", ModuleType.Engine, worldPosition, dimensions, rotationZ);
            var legality = Calculator.CalculateLegalityPosition(adjacent, new[] { command, adjacent });
            Assert.That(legality, Is.EqualTo(PositionLegality.Correct));
        }

        private ShipModuleSOInstanceBundle CreateBundle(string name, ModuleType moduleType, Vector2 worldPosition,
            Vector2Int dimensions, float rotationZ = 0f)
        {
            var go = new GameObject(name)
            {
                transform =
                {
                    position = worldPosition,
                    rotation = Quaternion.Euler(0f, 0f, rotationZ)
                }
            };
            _createdObjects.Add(go);

            var moduleSO = ScriptableObject.CreateInstance<ShipModuleSO>();
            _createdObjects.Add(moduleSO);

            moduleSO.ConfigureForTesting(name, $"{name} description", dimensions, go);

            var module = Substitute.For<IModule>();
            module.Type.Returns(moduleType);

            return new ShipModuleSOInstanceBundle(go, moduleSO, module);
        }
    }
}