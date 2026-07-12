using ShipFactory.Models;
using UnityEngine;

namespace ShipFactory.Helpers
{
    public static class ModuleRotationUtility
    {
        private const float EdgeEpsilon = 0.001f;

        public static int CalculateQuarterTurns(Transform transform)
        {
            var z = transform.localEulerAngles.z;
            return CalculateQuarterTurns(z);
        }

        private static int CalculateQuarterTurns(float zDegrees)
        {
            return (Mathf.RoundToInt(zDegrees / 90f) % 4 + 4) % 4;
        }

        public static Vector2Int GetWorldFootprintDimensions(Quaternion worldRotation, Vector2Int baseDimensions)
        {
            var z = worldRotation.eulerAngles.z;
            var quarterTurns = CalculateQuarterTurns(z);
            return quarterTurns is 1 or 3 ? new Vector2Int(baseDimensions.y, baseDimensions.x) : baseDimensions;
        }

        public static (Vector2 min, Vector2 max) GetAxisAlignedBounds(ShipModuleSOInstanceBundle bundle)
        {
            return GetFootprintBoundsInParentSpace(
                bundle.Instance.transform.position,
                bundle.ModuleSO.Dimensions,
                bundle.Instance.transform.rotation);
        }

        public static (Vector2 min, Vector2 max) GetFootprintBoundsInParentSpace(Vector2 centerInParentSpace,
            Vector2Int dimensions, Quaternion rotationInParentSpace)
        {
            var z = rotationInParentSpace.eulerAngles.z;
            var quarterTurns = CalculateQuarterTurns(z);
            var footprint = quarterTurns is 1 or 3 ? new Vector2Int(dimensions.y, dimensions.x) : dimensions;
            var half = (Vector2)footprint * 0.5f;
            return (centerInParentSpace - half, centerInParentSpace + half);
        }

        public static bool ContainsWorldPoint(ShipModuleSOInstanceBundle bundle, Vector2 worldPoint)
        {
            var local = bundle.Instance.transform.InverseTransformPoint(worldPoint);
            var half = (Vector2)bundle.ModuleSO.Dimensions * 0.5f;
            return Mathf.Abs(local.x) <= half.x + EdgeEpsilon && Mathf.Abs(local.y) <= half.y + EdgeEpsilon;
        }

        public static void ApplyQuarterTurn(ShipModuleSOInstanceBundle bundle, int deltaSteps)
        {
            if (deltaSteps == 0) return;

            var currentQuarterTurns = CalculateQuarterTurns(bundle.Instance.transform);
            var newQuarterTurns = ((currentQuarterTurns + deltaSteps) % 4 + 4) % 4;
            bundle.Instance.transform.localRotation = Quaternion.Euler(0f, 0f, newQuarterTurns * 90f);
        }
    }
}