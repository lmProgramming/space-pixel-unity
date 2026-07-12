using UnityEngine;

namespace ShipFactory.Helpers
{
    public static class Snapper
    {
        public const int SnapUnits = 8;

        public static Vector2 SnapToGrid(Vector2 position)
        {
            return new Vector2(
                Mathf.Round(position.x / SnapUnits) * SnapUnits,
                Mathf.Round(position.y / SnapUnits) * SnapUnits);
        }

        public static Vector2 SnapModuleLocalCenter(Vector2 localCenter, Vector2Int dimensions)
        {
            return SnapModuleLocalCenter(localCenter, dimensions, Quaternion.identity);
        }

        public static Vector2 SnapModuleLocalCenter(Vector2 localCenter, Vector2Int dimensions,
            Quaternion localRotation)
        {
            var (boundsMin, _) = ModuleRotationUtility.GetFootprintBoundsInParentSpace(
                localCenter, dimensions, localRotation);
            var snappedBoundsMin = SnapToGrid(boundsMin);
            return localCenter + (snappedBoundsMin - boundsMin);
        }
    }
}