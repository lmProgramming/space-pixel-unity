using Core.Ship;
using UnityEngine;

namespace Ships.Modules
{
    public class Engine : Module
    {
        public float thrust;
        [SerializeField] private float maxThrust;
        private bool _active;

        public float MaxThrust => maxThrust * ShipModuleEfficiency;

        protected override void Awake()
        {
            base.Awake();
            Type = ModuleType.Engine;
        }

        public override float GetEnergyDraw()
        {
            return base.GetEnergyDraw() *
                   (_active ? 1f : 0.25f);
        }

        public void SetActive(bool active)
        {
            _active = active;
        }
    }
}