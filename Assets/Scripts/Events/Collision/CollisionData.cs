using System;
using UnityEngine;

namespace Events.Collision
{
    [Serializable]
    public struct CollisionData
    {
        public GameObject instigator;
        public GameObject otherObject;
        public Vector2 contactPoint;

        public CollisionData(GameObject instigator, GameObject otherObject, Vector2 contactPoint)
        {
            this.instigator = instigator;
            this.otherObject = otherObject;
            this.contactPoint = contactPoint;
        }
    }
}