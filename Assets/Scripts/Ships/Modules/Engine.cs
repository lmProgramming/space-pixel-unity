using Core.Services;
using Core.Ship;
using Core.Ship.ModuleSnapshotPayloads;
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
        [SerializeField] internal float currentGimbalAngle;
        [SerializeField] internal float desiredGimbalAngleForDebug;

#if UNITY_EDITOR
        [Header("Gimbal Gizmos")]
        [SerializeField] internal bool drawGimbalGizmos = true;

        [SerializeField] internal float gizmoArcRadius = 1.5f;
        [SerializeField] internal float gizmoThrustUnitsPerNewton = 0.0025f;
#endif

        private bool _active;
        private float _currentThrustRatio;
        private float _desiredGimbalAngleForDebug;
        private Quaternion _exhaustBaseLocalRotation;
        private float _exhaustBaseRateOverDistanceMultiplier;
        private float _exhaustBaseRateOverTimeMultiplier;
        private float _exhaustBaseStartSpeedMultiplier;
        public override ModuleType Type => ModuleType.Engine;

        internal float CurrentThrustRatioForTesting => _currentThrustRatio;
        internal float CurrentThrusterAngleForDebug => CurrentThrusterAngle;
        internal float DesiredGimbalAngleForDebug => _desiredGimbalAngleForDebug;
        internal float MaxGimbalAngleForDebug => maxGimbalAngle;
        internal bool IsActiveForDebug => _active;
        internal float MaxThrustBaseForDebug => maxThrust;
        internal float ShipModuleEfficiencyForDebug => ShipModuleEfficiency;
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
            currentGimbalAngle = CurrentThrusterAngle;
            desiredGimbalAngleForDebug = _desiredGimbalAngleForDebug;
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
            _desiredGimbalAngleForDebug = targetAngle;

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

        public override string CaptureTypePayloadJson(IGameContentCatalog contentCatalog)
        {
            var data = new EngineModuleData
            {
                maxThrust = maxThrust,
                maxGimbalAngle = maxGimbalAngle,
                gimbalSpeed = gimbalSpeed
            };

            if (contentCatalog != null && exhaustParticles != null &&
                contentCatalog.TryGetContentId(exhaustParticles.gameObject, out var exhaustContentId))
                data.exhaustTemplateContentId = exhaustContentId;

            return JsonUtility.ToJson(data);
        }

        public override void ApplyTypePayloadJson(string typePayloadJson, IGameContentCatalog contentCatalog)
        {
            if (string.IsNullOrWhiteSpace(typePayloadJson))
                return;

            var data = JsonUtility.FromJson<EngineModuleData>(typePayloadJson);
            if (data == null)
                return;

            maxThrust = data.maxThrust;
            maxGimbalAngle = data.maxGimbalAngle;
            gimbalSpeed = data.gimbalSpeed;

            if (contentCatalog != null &&
                contentCatalog.TryGetPrefab(data.exhaustTemplateContentId, out var exhaustTemplate))
            {
                if (exhaustParticles != null)
                    Destroy(exhaustParticles.gameObject);
                var exhaustObject = Instantiate(exhaustTemplate, transform);
                exhaustParticles = exhaustObject.GetComponent<ParticleSystem>();
                if (exhaustParticles == null)
                    throw new UnityException("[Engine] Exhaust template must contain ParticleSystem.");
            }
        }
    }
}