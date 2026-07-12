using System;
using Core.Services;
using Core.Ships;
using Core.Ships.Snapshots.PixelatedRigidbody;
using Pixelation;
using Ships.Modules;
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

        private bool IsDesignMode => GetComponentInParent<Module>()?.Ship is { IsDesignMode: true };

        protected override void Awake()
        {
            base.Awake();

            RestLocalPosition = transform.localPosition;
            _restLocalRotationZ = transform.localEulerAngles.z;

            Rigidbody.bodyType = RigidbodyType2D.Kinematic;
        }

        private void Start()
        {
            EnsureExhaustParticlesInitialized();
            if (IsDesignMode)
                SuppressExhaust();
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
            if (IsDesignMode)
            {
                SuppressExhaust();
                return;
            }

            var thrustRatio = Mathf.Pow(isActive ? currentThrustRatio : 0f, 2);

            EnsureExhaustParticlesInitialized();

            var emission = _exhaustParticles.emission;
            emission.enabled = isActive;
            emission.rateOverTimeMultiplier = _exhaustBaseRateOverTimeMultiplier * thrustRatio;
            emission.rateOverDistanceMultiplier = _exhaustBaseRateOverDistanceMultiplier * thrustRatio;

            var main = _exhaustParticles.main;
            main.startSpeedMultiplier = _exhaustBaseStartSpeedMultiplier * thrustRatio;
        }

        public void SuppressExhaust()
        {
            if (!_exhaustParticles)
                _exhaustParticles = GetComponentInChildren<ParticleSystem>();

            if (!_exhaustParticles)
                return;

            _exhaustParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var emission = _exhaustParticles.emission;
            emission.enabled = false;
            emission.rateOverTimeMultiplier = 0f;
            emission.rateOverDistanceMultiplier = 0f;

            var main = _exhaustParticles.main;
            main.startSpeedMultiplier = 0f;
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

        public override PixelatedRigidbodySnapshot CaptureSnapshot(IGameContentCatalog contentCatalog)
        {
            // remember that _exhaustParticles is supposed to have a parent with the GameObjectInstanceIdentity between Nozzle and Particle System
            var baseSnapshot = base.CaptureSnapshot(contentCatalog);

            var instanceIdentity = _exhaustParticles.gameObject.GetComponentInParent<GameObjectInstanceIdentity>();

            if (!instanceIdentity)
                throw new InvalidOperationException("[Nozzle] Particle Effect content id was not found.");

            var typePayloadJson = new NozzleData
            {
                particleEffectContentId = instanceIdentity.ArchetypeId,
                particleEffectPosition = _exhaustParticles.transform.parent.localPosition
            };

            baseSnapshot.typePayloadJson = JsonUtility.ToJson(typePayloadJson);

            return baseSnapshot;
        }

        public override void RestoreFromSnapshot(PixelatedRigidbodySnapshot snapshot,
            IGameContentCatalog contentCatalog)
        {
            base.RestoreFromSnapshot(snapshot, contentCatalog);

            var typeData = JsonUtility.FromJson<NozzleData>(snapshot.typePayloadJson);

            contentCatalog.TryGetPrefab(typeData.particleEffectContentId, out var prefab);

            if (!prefab)
                throw new ArgumentNullException(
                    $"[Nozzle] prefab was not found. Particle content id: {typeData.particleEffectContentId}");

            var newGo = Instantiate(prefab, transform);
            newGo.transform.localPosition = typeData.particleEffectPosition;

            EnsureExhaustParticlesInitialized();
            if (IsDesignMode)
                SuppressExhaust();
        }

        protected override PixelatedRigidbodyType GetSnapshotRigidbodyType()
        {
            return PixelatedRigidbodyType.Nozzle;
        }
    }
}