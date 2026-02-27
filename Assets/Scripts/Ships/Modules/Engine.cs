using Core.Ship;
using UnityEngine;

namespace Ships.Modules
{
    public class Engine : Module
    {
        public float thrust;
        [SerializeField] private float maxThrust;
        private bool _active;

        public float MaxThrust => maxThrust * ShipModuleEfficiency * (1f + GetCrewBonus());

        protected override void Awake()
        {
            base.Awake();
            Type = ModuleType.Engine;
        }

        public override float GetEnergyDraw()
        {
            return _active ? base.GetEnergyDraw() : 0;
        }

        public void SetActive(bool active)
        {
            _active = active;
        }
    }
}