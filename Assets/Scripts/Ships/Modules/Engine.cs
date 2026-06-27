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
        [ReadOnly] [SerializeField] internal float currentGimbalAngle;
        [ReadOnly] [SerializeField] internal float desiredGimbalAngleForDebug;

        private List<Nozzle> _nozzles;

        public override ModuleType Type => ModuleType.Engine;

        internal float CurrentThrustRatioForTesting => currentThrustRatio;

        internal float CurrentThrusterAngleForDebug => CurrentThrusterAngle;
        internal float DesiredGimbalAngleForDebug { get; private set; }

        internal float MaxGimbalAngleForDebug => maxGimbalAngle;
        private bool IsActive { get; set; }

        internal float MaxThrustBaseForDebug => maxThrust;
        internal float ShipModuleEfficiencyForDebug => ShipModuleEfficiency;

        private Vector2 ThrustPoint => CalculateAverageThrustPoint();

        public float MaxThrust => maxThrust * ShipModuleEfficiency * GameplayConstants.EngineThrustEfficiencyMultiplier;
        private float CurrentThrusterAngle { get; set; }

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

            ApplyExhaustVisuals();
        }

#if UNITY_EDITOR
        private void Update()
        {
            currentThrustRatio = CurrentThrustRatioForTesting;
            currentGimbalAngle = CurrentThrusterAngle;
            desiredGimbalAngleForDebug = DesiredGimbalAngleForDebug;
        }
#endif

        protected override void OnDestroy()
        {
            SetActive(false);

            base.OnDestroy();
        }

        private Vector2 CalculateAverageThrustPoint()
        {
            var averageThrustPoint = Vector3.zero;
            if (_nozzles.Count == 0) return averageThrustPoint;

            averageThrustPoint = _nozzles.AsValueEnumerable().Aggregate(averageThrustPoint,
                (current, nozzle) => current + nozzle.Transform.localPosition);

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
            ApplyExhaustVisuals();
        }

        private void ApplyExhaustVisuals()
        {
            foreach (var nozzle in _nozzles)
                nozzle.ApplyExhaustVisuals(CurrentThrusterAngle, currentThrustRatio, IsActive);
        }

        public void SetCurrentThrust(float currentThrust)
        {
            if (MaxThrust <= Mathf.Epsilon)
            {
                currentThrustRatio = 0f;
                ApplyExhaustVisuals();
                return;
            }

            currentThrustRatio = Mathf.Clamp01(currentThrust / MaxThrust);
            ApplyExhaustVisuals();
        }

        public void RotateThrusterTowards(float targetAngle, float deltaTime)
        {
            DesiredGimbalAngleForDebug = targetAngle;

            var clampedTarget = Mathf.Clamp(targetAngle, -maxGimbalAngle, maxGimbalAngle);
            var maxStep = Mathf.Max(0f, gimbalSpeed) * Mathf.Max(0f, deltaTime);
            CurrentThrusterAngle = Mathf.MoveTowards(CurrentThrusterAngle, clampedTarget, maxStep);
            ApplyExhaustVisuals();
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
            //
            // if (contentCatalog != null &&
            //     contentCatalog.TryGetPrefab(data.exhaustTemplateContentId, out var exhaustTemplate))
            // {
            //     if (exhaustParticles != null)
            //         Destroy(exhaustParticles.gameObject);
            //     var exhaustObject = Instantiate(exhaustTemplate, transform);
            //     exhaustParticles = exhaustObject.GetComponent<ParticleSystem>();
            //     if (exhaustParticles == null)
            //         throw new UnityException("[Engine] Exhaust template must contain ParticleSystem.");
            // }
        }

#if UNITY_EDITOR
        [Header("Gimbal Gizmos")]
        [SerializeField] internal bool drawGimbalGizmos = true;

        [SerializeField] internal float gizmoArcRadius = 1.5f;
        [SerializeField] internal float gizmoThrustUnitsPerNewton = 0.0025f;
#endif
    }
}