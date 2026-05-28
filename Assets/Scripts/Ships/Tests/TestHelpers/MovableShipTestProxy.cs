using UnityEngine;

namespace Ships.Tests.TestHelpers
{
    public sealed class MovableShipTestProxy : Ship
    {
        public float ForwardInput { get; set; }
        public float TurnInput { get; set; }
        public bool SasEnabled { get; set; }

        protected override void Move()
        {
            MarkEnginesActivity(ApplyEngineForces(ForwardInput, TurnInput, Time.deltaTime, SasEnabled));
        }
    }
}
