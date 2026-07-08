using System;
using Core.Services;
using Core.Ships;
using Core.Ships.Snapshots.Module.Systems;
using Ships.Modules;
using UnityEngine;
using UnityEngine.Assertions;

namespace Ships.Systems.Standalone
{
    [RequireComponent(typeof(Module))]
    public class ReactionWheelStabilizer : StandaloneModuleSystem
    {
        [SerializeField] private ReactionWheelSettings settings;

        private float _initialMass;
        private Module _module;

        private void Start()
        {
            _module = GetComponent<Module>();

            _initialMass = _module.PixelatedRigidbody.Rigidbody.mass;
        }

        private void FixedUpdate()
        {
            if (_module.Ship.IsSasOn)
                Apply(_module.PixelatedRigidbody.Rigidbody, _initialMass * _module.ActualEfficiency);
        }

        private void Apply(Rigidbody2D commandRigidbody, float multiplier)
        {
            Assert.IsNotNull(commandRigidbody, "commandRigidbody != null");
            Assert.IsNotNull(settings, "settings != null");

            var angularVelocity = commandRigidbody.angularVelocity;
            if (Mathf.Abs(angularVelocity) <= settings.AngularVelocityDeadZoneDegreesPerSecond)
                return;

            if (Mathf.Abs(angularVelocity) <= settings.AngularVelocityAtWhichSetItToZero)
            {
                commandRigidbody.angularVelocity = 0f;
                return;
            }

            var counterTorque = -angularVelocity * settings.DampingStrength;
            counterTorque = Mathf.Clamp(counterTorque, -settings.MaxTorque, settings.MaxTorque) * multiplier;
            commandRigidbody.AddTorque(counterTorque);
        }

        public override SystemData CaptureSnapshot(IGameContentCatalog contentCatalog)
        {
            throw new NotImplementedException();
        }

        public override void RestoreFromSnapshot(SystemData snapshot, IGameContentCatalog contentCatalog)
        {
            throw new NotImplementedException();
        }
    }
}