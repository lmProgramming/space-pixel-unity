using UnityEngine;

namespace ShipFactory
{
    public static class Snapper
    {
        public const int SnapUnits = 8;

        public static Vector2 SnapToGrid(Vector2 worldPosition)
        {
            return new Vector2(
                Mathf.Round(worldPosition.x / SnapUnits) * SnapUnits,
                Mathf.Round(worldPosition.y / SnapUnits) * SnapUnits);
        }
    }
}