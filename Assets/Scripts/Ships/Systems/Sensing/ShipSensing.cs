using UnityEngine;

namespace Ships.Systems.Sensing
{
    public sealed class ShipSensing : MonoBehaviour
    {
        [Header("Masks")]
        [SerializeField] private LayerMask obstacleMask;

        [SerializeField] private LayerMask losBlockersMask;
        [SerializeField] private LayerMask shipMask;
        [SerializeField] private bool useDefaultMasks = true;

        [Header("Forward Sensing")]
        [SerializeField] private float forwardSenseRange = 18f;

        [SerializeField] private float[] forwardSenseAngles = { 0f, 15f, -15f, 30f, -30f };
        [SerializeField] private bool useCircleCast;
        [SerializeField] private float circleCastRadius = 0.6f;

        [Header("Line Of Sight")]
        [SerializeField] private float losMaxDistance = 200f;

        public LayerMask ObstacleMask => obstacleMask;
        public LayerMask ShipMask => shipMask;
        public LayerMask LosBlockersMask => losBlockersMask;
        public float ForwardSenseRange => forwardSenseRange;

        private void Awake()
        {
            ApplyDefaultMasksIfNeeded();
        }

        private void Reset()
        {
            useDefaultMasks = true;
            forwardSenseRange = 18f;
            circleCastRadius = 0.6f;
            forwardSenseAngles = new[] { 0f, 15f, -15f, 30f, -30f };
        }

        private void OnValidate()
        {
            if (forwardSenseRange < 0f) forwardSenseRange = 0f;
            if (circleCastRadius < 0f) circleCastRadius = 0f;
            if (forwardSenseAngles == null || forwardSenseAngles.Length == 0)
                forwardSenseAngles = new[] { 0f, 15f, -15f, 30f, -30f };
        }

        public ObstacleSenseResult SenseObstacles(Vector2 origin, Vector2 forward)
        {
            return SenseObstaclesWithMask(origin, forward, obstacleMask);
        }

        public ObstacleSenseResult SenseShips(Vector2 origin, Vector2 forward)
        {
            return SenseObstaclesWithMask(origin, forward, shipMask);
        }

        public bool HasLineOfSight(Vector2 origin, Vector2 target, out RaycastHit2D hit)
        {
            var toTarget = target - origin;
            var distance = toTarget.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                hit = default;
                return true;
            }

            var direction = toTarget / distance;
            var castDistance = Mathf.Min(distance, losMaxDistance);
            hit = Physics2D.Raycast(origin, direction, castDistance, losBlockersMask);
            return hit.collider == null;
        }

        private ObstacleSenseResult SenseObstaclesWithMask(Vector2 origin, Vector2 forward, LayerMask mask)
        {
            var result = new ObstacleSenseResult
            {
                HasHit = false,
                ClosestHit = default,
                ClosestHitDistance = forwardSenseRange,
                Avoidance = Vector2.zero,
                HitCount = 0
            };

            if (forwardSenseRange <= 0f || mask == 0) return result;

            var normalizedForward = forward.sqrMagnitude > 0f ? forward.normalized : Vector2.right;
            var closestDistance = float.PositiveInfinity;
            RaycastHit2D closestHit = default;
            var avoidance = Vector2.zero;
            var hitCount = 0;

            foreach (var forwardSenseAngle in forwardSenseAngles)
            {
                var direction = Rotate(normalizedForward, forwardSenseAngle);
                var hit = useCircleCast
                    ? Physics2D.CircleCast(origin, circleCastRadius, direction, forwardSenseRange, mask)
                    : Physics2D.Raycast(origin, direction, forwardSenseRange, mask);

                if (!hit.collider) continue;

                hitCount++;

                var weight = 1f - hit.distance / forwardSenseRange;
                var away = hit.normal.sqrMagnitude > 0f ? hit.normal : (origin - hit.point).normalized;
                avoidance += away * weight;

                if (hit.distance >= closestDistance) continue;
                closestDistance = hit.distance;
                closestHit = hit;
            }

            if (hitCount <= 0) return result;

            result.HasHit = true;
            result.ClosestHit = closestHit;
            result.ClosestHitDistance = closestDistance;
            result.Avoidance = avoidance;
            result.HitCount = hitCount;

            return result;
        }

        private void ApplyDefaultMasksIfNeeded()
        {
            if (!useDefaultMasks) return;

            if (obstacleMask == 0)
                obstacleMask = LayerMask.GetMask("Debris", "Obstacles");

            if (losBlockersMask == 0)
                losBlockersMask = LayerMask.GetMask("Debris", "Obstacles");

            if (shipMask == 0)
                shipMask = LayerMask.GetMask("Friendly", "Enemy");
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var sin = Mathf.Sin(radians);
            var cos = Mathf.Cos(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }
    }
}