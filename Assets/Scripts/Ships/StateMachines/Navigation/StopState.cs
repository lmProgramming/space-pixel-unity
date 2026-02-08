namespace Ships.StateMachines.Navigation
{
    public class StopState : ShipNavigationState
    {
        public override string StateName => "Stop";

        public override bool CanTransitionTo(string stateName)
        {
            return true;
        }
    }
}