using System.Collections.Generic;
using UnityEngine;

namespace ShipFactory.LegalPositionCalculator
{
    public enum PositionLegality
    {
        Correct,
        InsideOther,
        OutsideShip,
        DisconnectsShip
    }

    public static class Calculator
    {
        public static PositionLegality CalculateLegalityPosition(ShipModuleSOInstanceBundle bundleToCheck,
            IEnumerable<ShipModuleSOInstanceBundle> placedElements)
        {
            var allBundles = new List<ShipModuleSOInstanceBundle>(placedElements);
            var (leftBottomPos, rightTop) = GetBottomLeftAndTopRightPositions(bundleToCheck);

            var hasAnyTouch = false;

            foreach (var placedElement in allBundles)
            {
                if (placedElement == bundleToCheck) continue;

                var (otherLeftBottomPos, otherRightTop) = GetBottomLeftAndTopRightPositions(placedElement);

                if (Overlap(leftBottomPos, rightTop, otherLeftBottomPos, otherRightTop))
                    return PositionLegality.InsideOther;

                if (TouchSides(leftBottomPos, rightTop, otherLeftBottomPos, otherRightTop))
                    hasAnyTouch = true;
            }

            if (!hasAnyTouch)
                return PositionLegality.OutsideShip;

            return KeepsSingleConnectedShip(allBundles)
                ? PositionLegality.Correct
                : PositionLegality.DisconnectsShip;
        }

        private static (Vector2, Vector2) GetBottomLeftAndTopRightPositions(
            ShipModuleSOInstanceBundle bundleToCheck)
        {
            var dimensions = bundleToCheck.ModuleSO.Dimensions;
            var position = (Vector2)bundleToCheck.Instance.transform.position;

            var bottomLeft = position - (Vector2)dimensions / 2;
            var topRight = position + (Vector2)dimensions / 2;

            return (bottomLeft, topRight);
        }

        private static bool Overlap(Vector2 aMin, Vector2 aMax, Vector2 bMin, Vector2 bMax)
        {
            return aMin.x < bMax.x &&
                   aMax.x > bMin.x &&
                   aMin.y < bMax.y &&
                   aMax.y > bMin.y;
        }

        private static bool TouchSides(Vector2 aMin, Vector2 aMax, Vector2 bMin, Vector2 bMax)
        {
            var touchVertical =
                (Mathf.Approximately(aMax.x, bMin.x) || Mathf.Approximately(aMin.x, bMax.x)) &&
                aMax.y > bMin.y && aMin.y < bMax.y;

            var touchHorizontal =
                (Mathf.Approximately(aMax.y, bMin.y) || Mathf.Approximately(aMin.y, bMax.y)) &&
                aMax.x > bMin.x && aMin.x < bMax.x;

            return touchVertical || touchHorizontal;
        }

        private static bool KeepsSingleConnectedShip(List<ShipModuleSOInstanceBundle> bundles)
        {
            if (bundles.Count <= 1)
                return true;

            var commandIndex = bundles.FindIndex(bundle => bundle.PlacedModule.Type == Core.Ship.ModuleType.Command);
            if (commandIndex < 0)
                throw new UnityException("[ShipFactory] No command module found while validating placement.");

            var visited = new bool[bundles.Count];
            var queue = new Queue<int>();

            visited[commandIndex] = true;
            queue.Enqueue(commandIndex);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var (currentMin, currentMax) = GetBottomLeftAndTopRightPositions(bundles[current]);

                for (var i = 0; i < bundles.Count; i++)
                {
                    if (visited[i] || i == current)
                        continue;

                    var (otherMin, otherMax) = GetBottomLeftAndTopRightPositions(bundles[i]);
                    if (!TouchSides(currentMin, currentMax, otherMin, otherMax))
                        continue;

                    visited[i] = true;
                    queue.Enqueue(i);
                }
            }

            for (var i = 0; i < visited.Length; i++)
                if (!visited[i])
                    return false;

            return true;
        }
    }
}