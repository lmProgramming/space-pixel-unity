using UnityEngine;

namespace Ships.Tests.TestHelpers.Proxies
{
    public sealed class MovableShipTestProxy : Ship
    {
        public float ForwardInput { get; set; }
        public float HorizontalInput { get; set; }
        public float TurnInput { get; set; }
        public bool SASEnabled { get; set; }

        public override bool IsSASOn => SASEnabled;

        protected override void ReadMovementInput()
        {
            PendingForwardInput = ForwardInput;
            PendingHorizontalInput = HorizontalInput;
            PendingTurnInput = TurnInput;
        }

        protected override void ApplyMovementPhysics()
        {
            MarkEnginesActivity(ApplyEngineForces(PendingForwardInput, PendingHorizontalInput, PendingTurnInput,
                Time.fixedDeltaTime,
                SASEnabled));
        }
    }
}