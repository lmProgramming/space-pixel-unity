using System.Collections.Generic;
using Core.Constants;
using Core.Services;
using Core.Ships;
using Core.Ships.Snapshots.Module;
using Core.Ships.Snapshots.Module.ModuleData;
using Core.Ships.Snapshots.PixelatedRigidbody;
using LMPro.External.ReadOnly;
using Pixelation;
using Ships.Snapshot;
using Ships.Systems.Gimbal;
using UnityEngine;
using ZLinq;
using ZLinq.Linq;

namespace Ships.Modules
{
    public class Engine : Module
    {
        private const float ThrustRatioToShowNozzleVisualsAsResting = 0.005f;
        [SerializeField] private float maxThrust;
        [SerializeField] private float maxGimbalAngle = 45f;
        [SerializeField] private float gimbalSpeed = 240f;

        [ReadOnly] [SerializeField] private float currentThrustRatio;

        [ReadOnly] [SerializeField] internal float currentGimbalAngleForDebug;
        [ReadOnly] [SerializeField] internal float desiredGimbalAngleForDebug;
        [ReadOnly] [field: SerializeField] private float CurrentThrusterAngle { get; set; }

        private List<Nozzle> _nozzles = new();
        private List<PixelatedRigidbodySnapshot> _pendingNozzleSnapshots = new();

        private ValueEnumerable<ListWhere<Nozzle>, Nozzle> ActiveNozzles =>
            _nozzles.AsValueEnumerable().Where(nozzle => nozzle);

        public override ModuleType Type => ModuleType.Engine;

        internal float CurrentThrustRatioForTesting => currentThrustRatio;

        internal float CurrentThrusterAngleForTesting => CurrentThrusterAngle;
        internal float DesiredGimbalAngleForTesting { get; private set; }

        internal float MaxGimbalAngleForDebug => maxGimbalAngle;
        private bool IsActive { get; set; }

        internal float MaxThrustBaseForDebug => maxThrust;
        internal float ShipModuleEfficiencyForDebug => ActualEfficiency;

        private Vector2 ThrustPoint => CalculateAverageThrustPoint();

        public float MaxThrust => maxThrust * ActualEfficiency * GameplayConstants.EngineThrustEfficiencyMultiplier;

        public Vector2 WorldThrustPoint => transform.TransformPoint(ThrustPoint);

        public Vector2 WorldThrustDirection =>
            (Quaternion.AngleAxis(CurrentThrusterAngle, Vector3.forward) * transform.up).normalized;

        public override ConcreteModuleType ConcreteType => ConcreteModuleType.Engine;

        protected override void Start()
        {
            base.Start();

            RegisterNozzles();
        }

        private void Update()
        {
            currentThrustRatio = CurrentThrustRatioForTesting;
            currentGimbalAngleForDebug = CurrentThrusterAngle;
            desiredGimbalAngleForDebug = DesiredGimbalAngleForTesting;

            ApplyNozzleVisuals();
        }

        protected override void OnDestroy()
        {
            if (_nozzles != null)
                for (var i = _nozzles.Count - 1; i >= 0; i--)
                    UnregisterNozzle(_nozzles[i]);

            SetActive(false);

            base.OnDestroy();
        }

        private void RegisterNozzles()
        {
            _nozzles = GetComponentsInChildren<Nozzle>().AsValueEnumerable().ToList();

            if (_nozzles.Count == 0)
                throw new UnityException("[Engine] No Nozzles found");

            foreach (var nozzle in _nozzles)
                RegisterNozzle(nozzle);
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

            var actualNozzles = ActiveNozzles;

            foreach (var nozzle in actualNozzles) averageThrustPoint += (Vector2)nozzle.RestLocalPosition;

            averageThrustPoint /= actualNozzles.Count();

            return averageThrustPoint;
        }

        public override float GetEnergyDraw()
        {
            return base.GetEnergyDraw() *
                   (0.25f + (IsActive ? 0.75f * CurrentThrustRatioForTesting : 0));
        }

        public void SetActive(bool active)
        {
            IsActive = active;
        }

        private void ApplyNozzleTransforms(float thrusterAngle)
        {
            foreach (var nozzle in ActiveNozzles)
                nozzle.ApplyGimbalTransform(thrusterAngle);
        }

        private void ApplyNozzleVisuals()
        {
            var visualizedThrustRatio = currentThrustRatio;
            var visualizedThrustAngle = CurrentThrusterAngle;
            var visualizedIsActive = IsActive;

            if (currentThrustRatio <= ThrustRatioToShowNozzleVisualsAsResting)
            {
                visualizedThrustRatio = 0f;
                visualizedIsActive = false;
            }

            visualizedThrustRatio *= ActualEfficiency;

            ApplyNozzleTransforms(visualizedThrustAngle);
            foreach (var nozzle in ActiveNozzles)
                nozzle.ApplyExhaustVisuals(visualizedThrustRatio, visualizedIsActive);
        }

        public void SetCurrentThrust(float currentThrust)
        {
            if (MaxThrust <= Mathf.Epsilon)
            {
                currentThrustRatio = 0f;
                return;
            }

            currentThrustRatio = Mathf.Clamp01(currentThrust / MaxThrust);
        }

        public void RotateThrusterTowards(float targetAngle, float deltaTime)
        {
            DesiredGimbalAngleForTesting = targetAngle;

            var clampedTarget = ClampTargetGimbalAngle(targetAngle);
            var maxStep = GetGimbalStepSize(clampedTarget, deltaTime);
            CurrentThrusterAngle = Mathf.MoveTowardsAngle(CurrentThrusterAngle, clampedTarget, maxStep);
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

        protected override string CaptureTypePayloadJson(IGameContentCatalog contentCatalog)
        {
            var nozzles = GetComponentsInChildren<Nozzle>(true);

            var data = new EngineModuleData
            {
                maxThrust = maxThrust,
                maxGimbalAngle = maxGimbalAngle,
                gimbalSpeed = gimbalSpeed,
                nozzles = nozzles.AsValueEnumerable().Select(nozzle => nozzle.CaptureSnapshot(contentCatalog))
                    .ToArray()
            };

            return JsonUtility.ToJson(data);
        }

        protected override void ApplyTypePayloadJson(string typePayloadJson, IGameContentCatalog contentCatalog)
        {
            if (string.IsNullOrWhiteSpace(typePayloadJson))
                return;

            var data = JsonUtility.FromJson<EngineModuleData>(typePayloadJson);
            if (data == null)
                return;

            maxThrust = data.maxThrust;
            maxGimbalAngle = data.maxGimbalAngle;
            gimbalSpeed = data.gimbalSpeed;

            _pendingNozzleSnapshots =
                data.nozzles?.AsValueEnumerable().ToList() ?? new List<PixelatedRigidbodySnapshot>();

            ClearExistingNozzleChildren();

            foreach (var nozzleSnapshot in _pendingNozzleSnapshots)
                NestedPixelatedRigidbodyFactory.CreateShell(transform, nozzleSnapshot);
        }

        private void RestorePendingNozzleSnapshots(IGameContentCatalog contentCatalog)
        {
            foreach (var snapshot in _pendingNozzleSnapshots)
            {
                var nozzleTransform = transform.Find(snapshot.name);
                if (!nozzleTransform)
                    throw new UnityException(
                        $"[Engine] Missing nozzle child '{snapshot.name}' during snapshot restore.");

                var pixelatedRigidbody = nozzleTransform.GetComponent<PixelatedRigidbody>();
                if (!pixelatedRigidbody)
                    throw new UnityException(
                        $"[Engine] Nozzle child '{snapshot.name}' has no PixelatedRigidbody.");

                pixelatedRigidbody.RestoreFromSnapshot(snapshot, contentCatalog);
            }

            _pendingNozzleSnapshots.Clear();

            RegisterNozzles();
        }

        private void ClearExistingNozzleChildren()
        {
            var existingNozzles = GetComponentsInChildren<Nozzle>(true);

            foreach (var nozzle in existingNozzles.AsValueEnumerable().ToArray())
            {
                if (!nozzle || nozzle.transform == transform)
                    continue;

                DestroyImmediate(nozzle.gameObject);
            }
        }

        public override void RestoreFromSnapshot(ModuleSnapshot snapshot, IGameContentCatalog contentCatalog)
        {
            base.RestoreFromSnapshot(snapshot, contentCatalog);

            RestorePendingNozzleSnapshots(contentCatalog);
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