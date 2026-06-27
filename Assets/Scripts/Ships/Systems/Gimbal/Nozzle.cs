using Pixelation;
using UnityEngine;

namespace Ships.Systems.Gimbal
{
    public class Nozzle : PixelatedRigidbody
    {
        private Quaternion _exhaustBaseLocalRotation;
        private float _exhaustBaseRateOverDistanceMultiplier;
        private float _exhaustBaseRateOverTimeMultiplier;
        private float _exhaustBaseStartSpeedMultiplier;
        private ParticleSystem _exhaustParticles;

        protected override void Awake()
        {
            base.Awake();
            _exhaustParticles = GetComponentInChildren<ParticleSystem>();

            if (!_exhaustParticles) throw new UnityException("[Nozzle] assign exhaustParticles");

            var emission = _exhaustParticles.emission;
            _exhaustBaseRateOverTimeMultiplier = emission.rateOverTimeMultiplier;
            _exhaustBaseRateOverDistanceMultiplier = emission.rateOverDistanceMultiplier;

            var main = _exhaustParticles.main;
            _exhaustBaseStartSpeedMultiplier = main.startSpeedMultiplier;
        }


        public void ApplyExhaustVisuals(float thrusterAngle, float currentThrustRatio, bool isActive)
        {
            Debug.Log(thrusterAngle);
            transform.localRotation = Quaternion.Euler(0f, 0f, thrusterAngle);

            var thrustRatio = Mathf.Pow(isActive ? currentThrustRatio : 0f, 2);

            var emission = _exhaustParticles.emission;
            emission.enabled = isActive;
            emission.rateOverTimeMultiplier = _exhaustBaseRateOverTimeMultiplier * thrustRatio;
            emission.rateOverDistanceMultiplier = _exhaustBaseRateOverDistanceMultiplier * thrustRatio;

            var main = _exhaustParticles.main;
            main.startSpeedMultiplier = _exhaustBaseStartSpeedMultiplier * thrustRatio;
        }
    }
}