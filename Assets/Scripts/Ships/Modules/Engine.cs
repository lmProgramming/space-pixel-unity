using System.Collections.Generic;
using Core.Constants;
using Core.Services;
using Core.Ship;
using Core.Ship.ModuleSnapshotPayloads;
using LMPro.External.ReadOnly;
using Ships.Systems.Gimbal;
using UnityEngine;
using ZLinq;

namespace Ships.Modules
{
    public class Engine : Module
    {
        [SerializeField] private float maxThrust;
        [SerializeField] private float maxGimbalAngle = 45f;
        [SerializeField] private float gimbalSpeed = 240f;

        [ReadOnly] [SerializeField] private float currentThrustRatio;

        [ReadOnly] [SerializeField] internal float currentGimbalAngleForDebug;
        [ReadOnly] [SerializeField] internal float desiredGimbalAngleForDebug;
        [ReadOnly] [field: SerializeField] private float CurrentThrusterAngle { get; set; }

        private List<Nozzle> _nozzles;

        public override ModuleType Type => ModuleType.Engine;

        internal float CurrentThrustRatioForTesting => currentThrustRatio;

        internal float CurrentThrusterAngleForTesting => CurrentThrusterAngle;
        internal float DesiredGimbalAngleForTesting { get; private set; }

        internal float MaxGimbalAngleForDebug => maxGimbalAngle;
        private bool IsActive { get; set; }

        internal float MaxThrustBaseForDebug => maxThrust;
        internal float ShipModuleEfficiencyForDebug => ShipModuleEfficiency;

        private Vector2 ThrustPoint => CalculateAverageThrustPoint();

        public float MaxThrust => maxThrust * ShipModuleEfficiency * GameplayConstants.EngineThrustEfficiencyMultiplier;

        public Vector2 WorldThrustPoint => transform.TransformPoint(ThrustPoint);

        public Vector2 WorldThrustDirection =>
            (Quaternion.AngleAxis(CurrentThrusterAngle, Vector3.forward) * transform.up).normalized;

        protected override void Awake()
        {
            base.Awake();
            Type = ModuleType.Engine;

            _nozzles = GetComponentsInChildren<Nozzle>().AsValueEnumerable().ToList();

            if (_nozzles.Count == 0)
                throw new UnityException("[Engine] No Nozzles found");

            foreach (var nozzle in _nozzles)
                RegisterNozzle(nozzle);
        }

        protected override void Start()
        {
            base.Start();
            ApplyNozzleVisuals();
        }

#if UNITY_EDITOR
        private void Update()
        {
            currentThrustRatio = CurrentThrustRatioForTesting;
            currentGimbalAngleForDebug = CurrentThrusterAngle;
            desiredGimbalAngleForDebug = DesiredGimbalAngleForTesting;
        }
#endif

        protected override void OnDestroy()
        {
            for (var i = _nozzles.Count - 1; i >= 0; i--)
                UnregisterNozzle(_nozzles[i]);

            SetActive(false);

            base.OnDestroy();
        }

        private void RegisterNozzle(Nozzle nozzle)
        {
            nozzle.Destroyed += OnNozzleDestroyed;
        }

        private void UnregisterNozzle(Nozzle nozzle)
        {
            if (!nozzle) return;

            nozzle.Destroyed -= OnNozzleDestroyed;
        }

        private void OnNozzleDestroyed(Nozzle nozzle)
        {
            UnregisterNozzle(nozzle);
            _nozzles.Remove(nozzle);
        }

        private Vector2 CalculateAverageThrustPoint()
        {
            if (_nozzles.Count == 0) return Vector2.zero;

            var averageThrustPoint = Vector2.zero;

            foreach (var nozzle in _nozzles)
            {
                if (!nozzle) continue;

                averageThrustPoint += (Vector2)nozzle.RestLocalPosition;
            }

            averageThrustPoint /= _nozzles.Count;

            return averageThrustPoint;
        }

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
                   (0.25f + (IsActive ? 0.75f * CurrentThrustRatioForTesting : 0));
        }

        public void SetActive(bool active)
        {
            IsActive = active;
            ApplyNozzleVisuals();
        }

        private void ApplyNozzleTransforms()
        {
            foreach (var nozzle in _nozzles)
            {
                if (!nozzle) continue;

                nozzle.ApplyGimbalTransform(CurrentThrusterAngle);
            }
        }

        private void ApplyExhaustVisuals()
        {
            foreach (var nozzle in _nozzles)
            {
                if (!nozzle) continue;

                nozzle.ApplyExhaustVisuals(currentThrustRatio, IsActive);
            }
        }

        private void ApplyNozzleVisuals()
        {
            ApplyNozzleTransforms();
            ApplyExhaustVisuals();
        }

        public void SetCurrentThrust(float currentThrust)
        {
            if (MaxThrust <= Mathf.Epsilon)
            {
                currentThrustRatio = 0f;
                ApplyNozzleVisuals();
                return;
            }

            currentThrustRatio = Mathf.Clamp01(currentThrust / MaxThrust);
            ApplyNozzleVisuals();
        }

        public void RotateThrusterTowards(float targetAngle, float deltaTime)
        {
            DesiredGimbalAngleForTesting = targetAngle;

            var clampedTarget = ClampTargetGimbalAngle(targetAngle);
            var maxStep = GetGimbalStepSize(clampedTarget, deltaTime);
            CurrentThrusterAngle = Mathf.MoveTowardsAngle(CurrentThrusterAngle, clampedTarget, maxStep);
            ApplyNozzleVisuals();
        }

        private float ClampTargetGimbalAngle(float targetAngle)
        {
            return Mathf.Clamp(targetAngle, -maxGimbalAngle, maxGimbalAngle);
        }

        private float GetGimbalStepSize(float clampedTarget, float deltaTime)
        {
            var maxStep = Mathf.Max(0f, gimbalSpeed) * Mathf.Max(0f, deltaTime);

            if (Mathf.Abs(clampedTarget) > Mathf.Epsilon || Mathf.Abs(CurrentThrusterAngle) <= Mathf.Epsilon)
                return maxStep;

            return maxStep * GameplayConstants.NozzleGoingBackToRestRotationMultiplierSpeed;
        }

        public override string CaptureTypePayloadJson(IGameContentCatalog contentCatalog)
        {
            var data = new EngineModuleData
            {
                maxThrust = maxThrust,
                maxGimbalAngle = maxGimbalAngle,
                gimbalSpeed = gimbalSpeed
            };

            // if (contentCatalog != null && exhaustParticles != null &&
            //     contentCatalog.TryGetContentId(exhaustParticles.gameObject, out var exhaustContentId))
            //     data.exhaustTemplateContentId = exhaustContentId;

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
                contentCatalog.TryGetPrefab(data.nozzleTemplateContentId, out var nozzleTemplate))
            {
                foreach (var nozzle in _nozzles.AsValueEnumerable().Where(nozzle => nozzle))
                    Destroy(nozzle.gameObject);

                var exhaustObject = Instantiate(nozzleTemplate, transform);
                // exhaustParticles = exhaustObject.GetComponent<ParticleSystem>();
                // if (exhaustParticles == null)
                //     throw new UnityException("[Engine] Exhaust template must contain ParticleSystem.");
            }
        }
#if UNITY_INCLUDE_TESTS
        internal void SetCurrentThrusterAngleForTesting(float nearFullCircleCurrentAngle)
        {
            CurrentThrusterAngle = nearFullCircleCurrentAngle;
        }
#endif

#if UNITY_EDITOR
        [Header("Gimbal Gizmos")]
        [SerializeField] internal bool drawGimbalGizmos = true;

        [SerializeField] internal float gizmoArcRadius = 1.5f;
        [SerializeField] internal float gizmoThrustUnitsPerNewton = 0.0025f;
#endif
    }
}