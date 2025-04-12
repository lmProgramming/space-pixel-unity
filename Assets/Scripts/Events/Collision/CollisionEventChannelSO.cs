using System;
using UnityEngine;
using UnityEngine.Events;

namespace Events.Collision
{
    [Serializable]
    public class UnityCollisionEvent : UnityEvent<CollisionData>
    {
    }

    [CreateAssetMenu(menuName = "Events/Collision Event Channel")]
    public class CollisionEventChannelSO : ScriptableObject
    {
        public UnityCollisionEvent onEventRaisedUnityEvent;
        private event Action<CollisionData> OnEventRaised;

        public void RaiseEvent(CollisionData data)
        {
            OnEventRaised?.Invoke(data);
            onEventRaisedUnityEvent?.Invoke(data);
        }

        public void RegisterListener(Action<CollisionData> listener)
        {
            OnEventRaised += listener;
        }

        public void UnregisterListener(Action<CollisionData> listener)
        {
            OnEventRaised -= listener;
        }
    }
}