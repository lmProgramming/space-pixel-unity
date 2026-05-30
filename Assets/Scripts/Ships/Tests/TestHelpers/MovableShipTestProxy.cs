using UnityEngine;

namespace Ships.Tests.TestHelpers
{
    public sealed class MovableShipTestProxy : Ship
    {
        public float ForwardInput { get; set; }
        public float TurnInput { get; set; }
        public bool SasEnabled { get; set; }

        protected override void ReadMovementInput()
        {
            PendingForwardInput = ForwardInput;
            PendingTurnInput = TurnInput;
        }

        protected override void ApplyMovementPhysics()
        {
            MarkEnginesActivity(ApplyEngineForces(PendingForwardInput, PendingTurnInput, Time.fixedDeltaTime,
                SasEnabled));
        }
    }
}
