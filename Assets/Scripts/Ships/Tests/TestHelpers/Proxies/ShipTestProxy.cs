namespace Ships.Tests.TestHelpers.Proxies
{
    public sealed class ShipTestProxy : Ship
    {
        protected override void ApplyMovementPhysics()
        {
        }

        public void RunUpdateForTesting()
        {
            Update();
        }
    }
}