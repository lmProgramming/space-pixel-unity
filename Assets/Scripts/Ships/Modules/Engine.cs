using Core.Ship;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Ships.Modules
{
    public class Engine : Module
    {
        [SerializeField] private float maxThrust;
        [SerializeField] private float maxGimbalAngle = 35f;
        [SerializeField] private float gimbalSpeed = 240f;
        [SerializeField] private Vector2 thrustPoint;

        [FormerlySerializedAs("particleSystem")]
        [SerializeField] private ParticleSystem exhaustParticles;

        private bool _active;
        private float _currentThrusterAngle;
        private Quaternion _exhaustBaseLocalRotation;

        public float MaxThrust => maxThrust * ShipModuleEfficiency;
        public float CurrentThrusterAngle => _currentThrusterAngle;

        public Vector2 WorldThrustPoint => transform.TransformPoint(thrustPoint);

        public Vector2 WorldThrustDirection =>
            (Quaternion.AngleAxis(_currentThrusterAngle, Vector3.forward) * transform.up).normalized;

        protected override void Awake()
        {
            base.Awake();
            Type = ModuleType.Engine;

            Assert.IsNotNull(exhaustParticles, "Engine requires an exhaustParticles ParticleSystem reference");
            _exhaustBaseLocalRotation = exhaustParticles.transform.localRotation;
            ApplyExhaustRotation();
        }

        public override float GetEnergyDraw()
        {
            return base.GetEnergyDraw() *
                   (_active ? 1f : 0.25f);
        }

        public void SetActive(bool active)
        {
            _active = active;
            var emission = exhaustParticles.emission;
            emission.enabled = active;
        }

        public void RotateThrusterTowards(float targetAngle, float deltaTime)
        {
            var clampedTarget = Mathf.Clamp(targetAngle, -maxGimbalAngle, maxGimbalAngle);
            var maxStep = Mathf.Max(0f, gimbalSpeed) * Mathf.Max(0f, deltaTime);
            _currentThrusterAngle = Mathf.MoveTowards(_currentThrusterAngle, clampedTarget, maxStep);
            ApplyExhaustRotation();
        }

        private void ApplyExhaustRotation()
        {
            exhaustParticles.transform.localRotation =
                _exhaustBaseLocalRotation * Quaternion.Euler(0f, 0f, _currentThrusterAngle);
        }
    }
}