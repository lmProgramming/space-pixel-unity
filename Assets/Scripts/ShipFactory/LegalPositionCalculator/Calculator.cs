using System.Collections.Generic;
using UnityEngine;

namespace ShipFactory.LegalPositionCalculator
{
    public enum PositionLegality
    {
        Correct,
        InsideOther,
        OutsideShip
    }

    public static class Calculator
    {
        public static PositionLegality CalculateLegalityPosition(ShipModuleSOInstanceBundle bundleToCheck,
            IEnumerable<ShipModuleSOInstanceBundle> placedElements)
        {
            var (leftBottomPos, rightTop) = GetBottomLeftAndTopRightPositions(bundleToCheck);

            var positionLegality = PositionLegality.OutsideShip;

            foreach (var placedElement in placedElements)
            {
                if (placedElement == bundleToCheck) continue;

                var (otherLeftBottomPos, otherRightTop) = GetBottomLeftAndTopRightPositions(placedElement);

                if (Overlap(leftBottomPos, rightTop, otherLeftBottomPos, otherRightTop))
                    return PositionLegality.InsideOther;

                if (TouchSides(leftBottomPos, rightTop, otherLeftBottomPos, otherRightTop))
                    positionLegality = PositionLegality.Correct;
            }

            return positionLegality;
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
    }
}