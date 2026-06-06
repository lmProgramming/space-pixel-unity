using UnityEngine;

namespace Ships.Systems.Sensing
{
    public struct ObstacleSenseResult
    {
        public bool HasHit;
        public RaycastHit2D ClosestHit;
        public float ClosestHitDistance;
        public Vector2 Avoidance;
        public int HitCount;
    }
}