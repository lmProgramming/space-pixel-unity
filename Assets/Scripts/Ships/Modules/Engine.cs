using Core.Ship;
using UnityEngine;
using UnityEngine.Assertions;

namespace Ships.Modules
{
    public class Engine : Module
    {
        [SerializeField] private float maxThrust;
        [SerializeField] private float maxGimbalAngle = 35f;
        [SerializeField] private float gimbalSpeed = 240f;

        [SerializeField] private ParticleSystem exhaustParticles;
        [SerializeField] internal float currentThrustRatioForDebug;

        private bool _active;
        private float _currentThrustRatio;
        private Quaternion _exhaustBaseLocalRotation;
        private float _exhaustBaseRateOverDistanceMultiplier;
        private float _exhaustBaseRateOverTimeMultiplier;
        private float _exhaustBaseStartSpeedMultiplier;
        public override ModuleType Type => ModuleType.Engine;

        internal float CurrentThrustRatioForTesting => _currentThrustRatio;
        private Vector2 ThrustPoint => exhaustParticles.transform.localPosition;

        public float MaxThrust => maxThrust * ShipModuleEfficiency;
        private float CurrentThrusterAngle { get; set; }

        public Vector2 WorldThrustPoint => transform.TransformPoint(ThrustPoint);

        public Vector2 WorldThrustDirection =>
            (Quaternion.AngleAxis(CurrentThrusterAngle, Vector3.forward) * transform.up).normalized;

        protected override void Awake()
        {
            base.Awake();
            Type = ModuleType.Engine;

            exhaustParticles ??= GetComponentInChildren<ParticleSystem>();

            Assert.IsNotNull(exhaustParticles, "Engine requires an exhaustParticles ParticleSystem reference");
            _exhaustBaseLocalRotation = exhaustParticles.transform.localRotation;

            var emission = exhaustParticles.emission;
            _exhaustBaseRateOverTimeMultiplier = emission.rateOverTimeMultiplier;
            _exhaustBaseRateOverDistanceMultiplier = emission.rateOverDistanceMultiplier;

            var main = exhaustParticles.main;
            _exhaustBaseStartSpeedMultiplier = main.startSpeedMultiplier;

            ApplyExhaustVisuals();
        }

#if UNITY_EDITOR
        private void Update()
        {
            currentThrustRatioForDebug = _currentThrustRatio;
        }
#endif

#if UNITY_INCLUDE_TESTS
        internal void ConfigureForTesting(float maxThrustValue,
            float maxGimbalAngleValue = 35f,
            float gimbalSpeedValue = 9999f)
        {
            maxThrust = maxThrustValue;
            maxGimbalAngle = maxGimbalAngleValue;
            gimbalSpeed = gimbalSpeedValue;
        }
#endif

        public override float GetEnergyDraw()
        {
            return base.GetEnergyDraw() *
                   (0.25f + (_active ? 0.75f * _currentThrustRatio : 0));
        }

        public void SetActive(bool active)
        {
            _active = active;
            ApplyExhaustVisuals();
        }

        public void SetCurrentThrust(float currentThrust)
        {
            if (MaxThrust <= Mathf.Epsilon)
            {
                _currentThrustRatio = 0f;
                ApplyExhaustVisuals();
                return;
            }

            _currentThrustRatio = Mathf.Clamp01(currentThrust / MaxThrust);
            ApplyExhaustVisuals();
        }

        public void RotateThrusterTowards(float targetAngle, float deltaTime)
        {
            var clampedTarget = Mathf.Clamp(targetAngle, -maxGimbalAngle, maxGimbalAngle);
            var maxStep = Mathf.Max(0f, gimbalSpeed) * Mathf.Max(0f, deltaTime);
            CurrentThrusterAngle = Mathf.MoveTowards(CurrentThrusterAngle, clampedTarget, maxStep);
            ApplyExhaustVisuals();
        }

        private void ApplyExhaustVisuals()
        {
            exhaustParticles.transform.localRotation =
                _exhaustBaseLocalRotation * Quaternion.Euler(0f, 0f, CurrentThrusterAngle);

            var thrustRatio = Mathf.Pow(_active ? _currentThrustRatio : 0f, 2);

            var emission = exhaustParticles.emission;
            emission.enabled = _active;
            emission.rateOverTimeMultiplier = _exhaustBaseRateOverTimeMultiplier * thrustRatio;
            emission.rateOverDistanceMultiplier = _exhaustBaseRateOverDistanceMultiplier * thrustRatio;

            var main = exhaustParticles.main;
            main.startSpeedMultiplier = _exhaustBaseStartSpeedMultiplier * thrustRatio;
        }
    }
}