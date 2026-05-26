using System;
using System.Collections.Generic;
using UnityEngine;
using ZLinq;
using Random = UnityEngine.Random;

namespace Services
{
    public static class SkirmishSpawnPlacement
    {
        public static bool TryFindPosition(
            Rect spawnRect,
            float radius,
            IReadOnlyList<SpawnReservation> reservations,
            int maxAttempts,
            LayerMask blockingMask,
            out Vector2 position,
            Func<Rect, float, Vector2> samplePosition = null,
            Func<Vector2, float, bool> isBlocked = null)
        {
            position = Vector2.zero;

            if (!IsRectValidForRadius(spawnRect, radius))
                return false;

            samplePosition ??= SamplePosition;
            isBlocked ??= (point, checkRadius) => Physics2D.OverlapCircle(point, checkRadius, blockingMask);

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var candidate = samplePosition(spawnRect, radius);
                if (!HasRequiredDistance(candidate, radius, reservations))
                    continue;

                if (isBlocked(candidate, radius))
                    continue;

                position = candidate;
                return true;
            }

            return false;
        }

        private static bool IsRectValidForRadius(Rect spawnRect, float radius)
        {
            var diameter = radius * 2f;
            return spawnRect.width >= diameter && spawnRect.height >= diameter;
        }

        private static Vector2 SamplePosition(Rect spawnRect, float radius)
        {
            var x = Random.Range(spawnRect.xMin + radius, spawnRect.xMax - radius);
            var y = Random.Range(spawnRect.yMin + radius, spawnRect.yMax - radius);
            return new Vector2(x, y);
        }

        private static bool HasRequiredDistance(
            Vector2 candidate,
            float candidateRadius,
            IReadOnlyList<SpawnReservation> reservations)
        {
            return !(from reservation in reservations.AsValueEnumerable()
                let minDistance = candidateRadius + reservation.Radius
                where (candidate - reservation.Position).sqrMagnitude < minDistance * minDistance
                select reservation).Any();
        }

        public readonly struct SpawnReservation
        {
            public SpawnReservation(Vector2 position, float radius)
            {
                Position = position;
                Radius = radius;
            }

            public Vector2 Position { get; }
            public float Radius { get; }
        }
    }
}