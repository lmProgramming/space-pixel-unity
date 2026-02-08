using Core.Ship;

namespace Ships.Modules
{
    public class Engine : Module
    {
        public float thrust;
        public float maxThrust;
        private bool _active;

        protected override void Awake()
        {
            base.Awake();
            Type = ModuleType.Engine;
        }

        public override float GetEnergyDraw()
        {
            return _active ? Resources.energyDraw : 0;
        }

        public void SetActive(bool active)
        {
            _active = active;
        }
    }
}