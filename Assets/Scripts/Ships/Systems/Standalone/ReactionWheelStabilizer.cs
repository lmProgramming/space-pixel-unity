using System;
using Core.Services;
using Core.Ships;
using Core.Ships.Snapshots.Module.StandaloneModuleSystemData;
using Ships.Modules;
using UnityEngine;

namespace Ships.Systems.Standalone
{
    [RequireComponent(typeof(Module))]
    public class ReactionWheelStabilizer : StandaloneModuleSystem
    {
        [SerializeField] private ReactionWheelSettings settings;

        private float _initialMass;

        private void Start()
        {
            _initialMass = Module.PixelatedRigidbody.Rigidbody.mass;
        }

        protected override void TickStandaloneSystem()
        {
            if (Module.Ship.IsSASOn)
                Apply(Module.PixelatedRigidbody.Rigidbody, _initialMass * Module.ActualEfficiency);
        }

        private void Apply(Rigidbody2D commandRigidbody, float multiplier)
        {
            if (commandRigidbody == null)
                throw new InvalidOperationException("[ReactionWheelStabilizer] commandRigidbody is required.");
            if (settings == null)
                throw new UnityException("[ReactionWheelStabilizer] settings must be assigned.");

            var angularVelocity = commandRigidbody.angularVelocity;
            if (Mathf.Abs(angularVelocity) <= settings.angularVelocityDeadZoneDegreesPerSecond)
                return;

            if (Mathf.Abs(angularVelocity) <= settings.angularVelocityAtWhichSetItToZero)
            {
                commandRigidbody.angularVelocity = 0f;
                return;
            }

            var counterTorque = -angularVelocity * settings.dampingStrength;
            counterTorque = Mathf.Clamp(counterTorque, -settings.maxTorque, settings.maxTorque) * multiplier;
            commandRigidbody.AddTorque(counterTorque);
        }

        public override StandaloneModuleSystemData CaptureSnapshot(IGameContentCatalog contentCatalog)
        {
            return new ReactionWheelData
            {
                data = settings
            };
        }

        public override void RestoreFromSnapshot(StandaloneModuleSystemData snapshot,
            IGameContentCatalog contentCatalog)
        {
            if (snapshot is ReactionWheelData reactionWheelData)
                settings = reactionWheelData.data;
            else
                throw new ArgumentException("[ReactionWheelStabilizer] snapshot is of wrong type");
        }

#if UNITY_INCLUDE_TESTS
        internal ReactionWheelSettings GetSettingsForTesting()
        {
            return settings;
        }
#endif
    }
}