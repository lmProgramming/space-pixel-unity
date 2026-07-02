using System;
using Core.Ship;
using Pixelation;
using UnityEngine;

namespace Ships.Systems.Gimbal
{
    public class Nozzle : PixelatedRigidbody
    {
        private float _exhaustBaseRateOverDistanceMultiplier;
        private float _exhaustBaseRateOverTimeMultiplier;
        private float _exhaustBaseStartSpeedMultiplier;
        private ParticleSystem _exhaustParticles;
        private float _restLocalRotationZ;

        public Vector3 RestLocalPosition { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            RestLocalPosition = transform.localPosition;
            _restLocalRotationZ = transform.localEulerAngles.z;

            Rigidbody.bodyType = RigidbodyType2D.Kinematic;

            EnsureExhaustParticlesInitialized();
        }

        protected override void OnDestroy()
        {
            Destroyed?.Invoke(this);
            base.OnDestroy();
        }

        public event Action<Nozzle> Destroyed;

        public void ApplyGimbalTransform(float gimbalAngleDegrees)
        {
            transform.localPosition = RestLocalPosition;
            transform.localRotation = Quaternion.Euler(0f, 0f, _restLocalRotationZ + gimbalAngleDegrees);

            Rigidbody.position = transform.position;
            Rigidbody.rotation = transform.eulerAngles.z;
        }

        public void ApplyExhaustVisuals(float currentThrustRatio, bool isActive)
        {
            EnsureExhaustParticlesInitialized();

            var thrustRatio = Mathf.Pow(isActive ? currentThrustRatio : 0f, 2);

            var emission = _exhaustParticles.emission;
            emission.enabled = isActive;
            emission.rateOverTimeMultiplier = _exhaustBaseRateOverTimeMultiplier * thrustRatio;
            emission.rateOverDistanceMultiplier = _exhaustBaseRateOverDistanceMultiplier * thrustRatio;

            var main = _exhaustParticles.main;
            main.startSpeedMultiplier = _exhaustBaseStartSpeedMultiplier * thrustRatio;
        }

        private void EnsureExhaustParticlesInitialized()
        {
            if (_exhaustParticles) return;

            _exhaustParticles = GetComponentInChildren<ParticleSystem>();

            if (!_exhaustParticles) throw new UnityException("[Nozzle] assign exhaustParticles");

            var emission = _exhaustParticles.emission;
            _exhaustBaseRateOverTimeMultiplier = emission.rateOverTimeMultiplier;
            _exhaustBaseRateOverDistanceMultiplier = emission.rateOverDistanceMultiplier;

            var main = _exhaustParticles.main;
            _exhaustBaseStartSpeedMultiplier = main.startSpeedMultiplier;
        }

        protected override PixelatedRigidbodyType GetSnapshotRigidbodyType()
        {
            return PixelatedRigidbodyType.Nozzle;
        }
    }
}