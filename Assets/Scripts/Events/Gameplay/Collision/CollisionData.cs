using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Events.Gameplay.Collision
{
    [Serializable]
    public struct CollisionData
    {
        public GameObject instigator;
        [CanBeNull] public GameObject otherObject;
        public Vector2 contactPoint;
        public Vector2[] pixelsDestroyed;
        public Vector2? SpeedDifference;

        public CollisionData(GameObject instigator, [CanBeNull] GameObject otherObject, Vector2 contactPoint,
            Vector2[] pixelsDestroyed, Vector2? speedDifference)
        {
            this.instigator = instigator;
            this.otherObject = otherObject;
            this.contactPoint = contactPoint;
            this.pixelsDestroyed = pixelsDestroyed;
            SpeedDifference = speedDifference;
        }
    }
}