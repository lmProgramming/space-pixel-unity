using System.Collections.Generic;
using Core.Ships;
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
        private const float EdgeEpsilon = 0.001f;

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
            return ModuleRotationUtility.GetAxisAlignedBounds(bundleToCheck);
        }

        private static bool Overlap(Vector2 aMin, Vector2 aMax, Vector2 bMin, Vector2 bMax)
        {
            return aMin.x < bMax.x - EdgeEpsilon &&
                   aMax.x > bMin.x + EdgeEpsilon &&
                   aMin.y < bMax.y - EdgeEpsilon &&
                   aMax.y > bMin.y + EdgeEpsilon;
        }

        private static bool TouchSides(Vector2 aMin, Vector2 aMax, Vector2 bMin, Vector2 bMax)
        {
            var touchVertical =
                (Mathf.Approximately(aMax.x, bMin.x) || Mathf.Approximately(aMin.x, bMax.x)) &&
                GetSharedSpan(aMin.y, aMax.y, bMin.y, bMax.y) >= EdgeEpsilon;

            var touchHorizontal =
                (Mathf.Approximately(aMax.y, bMin.y) || Mathf.Approximately(aMin.y, bMax.y)) &&
                GetSharedSpan(aMin.x, aMax.x, bMin.x, bMax.x) >= EdgeEpsilon;

            return touchVertical || touchHorizontal;
        }

        private static float GetSharedSpan(float aMin, float aMax, float bMin, float bMax)
        {
            return Mathf.Max(0f, Mathf.Min(aMax, bMax) - Mathf.Max(aMin, bMin));
        }

        private static bool KeepsSingleConnectedShip(List<ShipModuleSOInstanceBundle> bundles)
        {
            if (bundles.Count <= 1)
                return true;

            var commandIndex = bundles.FindIndex(bundle => bundle.PlacedModule.Type == ModuleType.Command);
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