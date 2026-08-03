using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core.Constants;
using Core.Pixelation;
using Core.Services;
using Core.Ships;
using Core.Ships.Blueprints;
using Core.Ships.Module;
using Core.Ships.Snapshots.Module;
using Core.Ships.Snapshots.Module.StandaloneModuleSystemData;
using LMPro;
using Pixelation;
using Ships.Systems.Standalone;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;
using ZLinq;
using Random = UnityEngine.Random;

[assembly: InternalsVisibleTo("Editor.InspectorExtensions")]
[assembly: InternalsVisibleTo("Ships.Tests")]
[assembly: InternalsVisibleTo("Services")]

namespace Ships.Modules
{
    [RequireComponent(typeof(PixelatedRigidbody))]
    [DisallowMultipleComponent]
    public abstract class Module : MonoBehaviour, IModule
    {
        private const float CrewSkillBonusPerLevel = 0.02f;
        protected const float CaptainBonusPerLevel = 0.05f;

        [SerializeField] private CrewSkillType mainSkillType;

        [SerializeField]
        private List<CrewMember> assignedCrew = new();

        private readonly Dictionary<Module, List<Vector2Int>> _connectionPoints = new();
        private readonly Dictionary<Module, FixedJoint2D> _connections = new();

        private readonly List<IStandaloneModuleSystem> _standaloneSystems = new();

        [Inject] protected GameplayConstants GameplayConstants;

        private float _crewAppropriateSkillSum;

        [Inject]
        private IEffectsSpawner _effectsSpawner;

        private bool _hasTornDownConnectionsAndCrew;

        protected List<CrewMember> AliveCrew { get; private set; }

        internal CrewSkillType MainSkillTypeForTesting
        {
            set => mainSkillType = value;
        }

        internal IReadOnlyDictionary<Module, List<Vector2Int>> ConnectionPoints => _connectionPoints;

        public float ActualEfficiency => (Ship?.GeneralEfficiency ?? 1f) * ModuleEfficiency;

        private float PixelEfficiency =>
            Mathf.Pow((float)PixelatedRigidbody.CurrentPixelCount / PixelatedRigidbody.StartPixelCount, 2);

        private bool IsBelowDestructionThreshold =>
            PixelatedRigidbody.CurrentPixelCount > 0 &&
            PixelatedRigidbody.CurrentPixelCount <
            PixelatedRigidbody.StartPixelCount *
            GameplayConstants.moduleDestroyedWhenCurrentPixelRatioOfOriginalIsBelow;

        protected bool IsDesignMode => Ship is { IsDesignMode: true };

        protected virtual ConcreteModuleType ConcreteType { get; set; } = ConcreteModuleType.Basic;

        protected virtual void Awake()
        {
            EnsurePixelatedRigidbodyCached();
            OnCrewChange();
        }

        protected virtual void Start()
        {
            if (PixelatedRigidbody != null)
                PixelatedRigidbody.OnPixelsLost += CheckCohesion;
            else
                Debug.LogError("PixelatedRigidbody not found on Module!", this);
        }

        private void Update()
        {
            if (IsDesignMode) return;

            UpdateModule();
        }

        private void OnEnable()
        {
            PixelatedRigidbody.Destroyed += HandleDestroy;
        }

        private void OnDisable()
        {
            PixelatedRigidbody.Destroyed -= HandleDestroy;
        }

        private void OnDestroy()
        {
            TearDownConnectionsAndCrew(false);
        }

        private void OnDrawGizmosSelected()
        {
            if (PixelatedRigidbody == null || _connectionPoints == null) return;

            var gizmoSize = Vector3.one * 0.8f;

            foreach (var (otherModule, points) in _connectionPoints)
            {
                if (otherModule == null || points == null) continue;

                var hashCode = GetHashCode() + otherModule.GetHashCode();
                var hue = (Mathf.Abs(hashCode) % 1000 + 50) / 1050f;
                Gizmos.color = Color.HSVToRGB(hue, 1.0f, 0.95f);

                foreach (var worldPos in points.AsValueEnumerable().Select(localPixelPos =>
                             PixelatedRigidbody.LocalToWorldPoint(localPixelPos))) Gizmos.DrawCube(worldPos, gizmoSize);
            }
        }

        public IShip Ship { get; private set; }
        public Collider2D Collider2D => PixelatedRigidbody.Collider2D;

        public int AliveCrewCount => AliveCrew.Count;

        public virtual float EnergyCapacity => ShipResources.energyCapacity * ModuleEfficiency;

        // todo: consider caching this in the future
        public float ModuleEfficiency => PixelEfficiency * GetCrewEfficiency();

        public IReadOnlyList<CrewMember> AssignedCrew => assignedCrew;
        public int CrewMissingCount => Mathf.Max(0, CrewNeededCount - AliveCrewCount);

        [field: FormerlySerializedAs("<Resources>k__BackingField")]
        [field: SerializeField]
        public ShipResources ShipResources { get; private set; }

        public IPixelatedRigidbody PixelatedRigidbody { get; private set; }

        public Transform Transform => transform;

        public virtual ModuleType Type { get; protected set; } = ModuleType.Resources;

        public ModuleBlueprint Blueprint { get; private set; }

        public virtual int CrewNeededCount => Mathf.CeilToInt(ShipResources.crewNeeded);

        public void FillCrewBySkill(List<CrewMember> crew, out List<CrewMember> remainingCrew)
        {
            if (crew == null) throw new ArgumentNullException(nameof(crew));

            var membersOrderBySkill = crew.AsValueEnumerable().OrderByDescending(c => c.GetSkillLevel(mainSkillType));

            var crewToAssign = membersOrderBySkill.Take(CrewMissingCount).ToList();

            foreach (var crewMember in crewToAssign)
                AssignCrew(crewMember);

            remainingCrew = membersOrderBySkill.Skip(crewToAssign.Count).ToList();
        }

        public bool AssignCrew(CrewMember member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            if (assignedCrew.Contains(member)) return false;

            assignedCrew.Add(member);
            OnCrewChange();

            member.OnDied += HandleCrewMemberDeath;

            return true;
        }

        public bool RemoveCrew(CrewMember member)
        {
            var crewRemoved = assignedCrew.Remove(member);
            if (crewRemoved)
                UnsubscribeCrew(member);
            OnCrewChange();
            return crewRemoved;
        }

        public virtual float GetCrewEfficiency()
        {
            if (CrewNeededCount == 0) return 1;
            if (assignedCrew.Count == 0) return 0;

            var captainMultiplier = Ship?.CaptainMultiplier ?? 1f;

            return (1 - (float)CrewMissingCount / CrewNeededCount) *
                   (1 + _crewAppropriateSkillSum * captainMultiplier * CrewSkillBonusPerLevel);
        }

        public virtual float GetEnergyDraw()
        {
            return ShipResources.energyDraw * ModuleEfficiency;
        }

        public virtual float GetEnergyProduction()
        {
            return ShipResources.energyProduction * ModuleEfficiency;
        }

        public void KillAllCrew()
        {
            foreach (var crew in assignedCrew) KillCrewMember(crew);
            assignedCrew.Clear();

            OnCrewChange();
        }

        public void KillRandomCrew(int count)
        {
            AliveCrew.Shuffle();

            for (var i = 0; i < count && AliveCrew.Count > 0; i++)
            {
                var crewToKill = AliveCrew[0];
                KillCrewMember(crewToKill);
                OnCrewChange();
            }
        }

        public void SetLocalPosition(Vector2 localPosition)
        {
            ApplyLayoutTransform(new Vector3(localPosition.x, localPosition.y, transform.localPosition.z),
                Ship != null
                    ? ShipLayoutSpace.WorldToLocalRotation(Ship, transform.rotation)
                    : transform.localRotation);
        }

        public void SyncBlueprintLayoutFromTransform()
        {
            if (Blueprint == null || Ship == null) return;

            Blueprint.localPosition = ShipLayoutSpace.WorldToLocal(Ship, transform.position);
            Blueprint.localRotation = ShipLayoutSpace.WorldToLocalRotation(Ship, transform.rotation);
        }

        public void SetShip(IShip ship)
        {
            Ship = ship;
        }

        public void SetBlueprint(ModuleBlueprint blueprint)
        {
            Blueprint = blueprint ?? throw new ArgumentNullException(nameof(blueprint));
        }

        public void EnsureBlueprintIdentity()
        {
            if (!Transform) throw new NullReferenceException("[Module] Transform was null!");

            var identity = Transform.GetComponent<GameObjectInstanceIdentity>();
            if (!identity)
            {
                identity = Transform.gameObject.AddComponent<GameObjectInstanceIdentity>();
                identity.EnsureAssigned(InstanceOrigin.Custom);
            }
            else if (string.IsNullOrWhiteSpace(identity.InstanceId))
            {
                identity.EnsureAssigned(identity.Origin, identity.ArchetypeId);
            }

            var archetypeId = !string.IsNullOrWhiteSpace(identity.ArchetypeId)
                ? identity.ArchetypeId
                : ResolveArchetypeIdForSnapshot(identity);

            EnsureBlueprintForCapture(identity.InstanceId, archetypeId);
        }

        public ModuleSnapshot CaptureSnapshot(IGameContentCatalog contentCatalog)
        {
            if (!Transform)
                throw new UnityException(
                    $"[Module] Cannot capture snapshot for module '{Transform?.name}' because its transform is null. " +
                    "Ensure that all modules have valid transforms before capturing snapshots.");

            var identity = Transform.GetComponent<GameObjectInstanceIdentity>();
            if (!identity)
            {
                identity = Transform.gameObject.AddComponent<GameObjectInstanceIdentity>();
                identity.EnsureAssigned(InstanceOrigin.Custom);
            }
            else if (string.IsNullOrWhiteSpace(identity.InstanceId))
            {
                identity.EnsureAssigned(identity.Origin, identity.ArchetypeId);
            }

            var archetypeId = ResolveArchetypeIdForSnapshot(identity);
            if (!string.IsNullOrWhiteSpace(archetypeId) && string.IsNullOrWhiteSpace(identity.ArchetypeId))
                identity.EnsureAssigned(InstanceOrigin.CatalogPrefab, archetypeId);

            EnsureBlueprintForCapture(identity.InstanceId, archetypeId);

            var layoutPosition = Ship != null
                ? ShipLayoutSpace.WorldToLocal(Ship, Transform.position)
                : Transform.localPosition;
            var layoutRotation = Ship != null
                ? ShipLayoutSpace.WorldToLocalRotation(Ship, Transform.rotation)
                : Transform.localRotation;

            var moduleSnapshot = new ModuleSnapshot
            {
                instanceId = identity.InstanceId,
                moduleName = Transform.name,
                concreteModuleType = ConcreteType,
                origin = identity.Origin,
                archetypeId = archetypeId,
                localPosition = layoutPosition,
                localRotation = layoutRotation,
                shipResources = ShipResources,
                pixelatedRigidbody = PixelatedRigidbody.CaptureSnapshot(contentCatalog),
                typePayloadJson = CaptureTypePayloadJson(contentCatalog),
                systems = CaptureSystemSnapshots(contentCatalog),
                blueprint = Blueprint
            };

            return moduleSnapshot;
        }

        public virtual void RestoreFromSnapshot(ModuleSnapshot snapshot, IGameContentCatalog contentCatalog)
        {
            EnsurePixelatedRigidbodyCached();

            SetResources(snapshot.shipResources);

            ApplyTypePayloadJson(snapshot.typePayloadJson, contentCatalog);

            RestoreSystems(snapshot.systems, contentCatalog);

            PixelatedRigidbody.RestoreFromSnapshot(snapshot.pixelatedRigidbody, contentCatalog);

            if (snapshot.blueprint != null)
                SetBlueprint(snapshot.blueprint);

            EnsureBlueprintIdentity();
        }

        public void SetResources(ShipResources newShipResources)
        {
            ShipResources = newShipResources;
        }

        private void EnsureBlueprintForCapture(string instanceId, string archetypeId)
        {
            if (!Transform) throw new NullReferenceException("[Module] Transform was null!");

            instanceId = ResolveBlueprintInstanceId(instanceId);

            var layoutPosition = Ship != null
                ? ShipLayoutSpace.WorldToLocal(Ship, Transform.position)
                : Transform.localPosition;
            var layoutRotation = Ship != null
                ? ShipLayoutSpace.WorldToLocalRotation(Ship, Transform.rotation)
                : Transform.localRotation;

            if (Blueprint != null)
            {
                Blueprint.blueprintId = instanceId;
                if (!string.IsNullOrWhiteSpace(archetypeId))
                    Blueprint.archetypeId = archetypeId;

                Blueprint.localPosition = layoutPosition;
                Blueprint.localRotation = layoutRotation;
                return;
            }

            SetBlueprint(new ModuleBlueprint(
                instanceId,
                archetypeId ?? string.Empty,
                layoutPosition,
                layoutRotation));
        }

        private string ResolveBlueprintInstanceId(string instanceId)
        {
            if (!string.IsNullOrWhiteSpace(instanceId))
                return instanceId;

            if (!Transform)
                throw new InvalidOperationException("[Module] Transform was null!");

            var identity = Transform.GetComponent<GameObjectInstanceIdentity>();
            if (!identity)
            {
                identity = Transform.gameObject.AddComponent<GameObjectInstanceIdentity>();
                identity.EnsureAssigned(InstanceOrigin.Custom);
            }
            else if (string.IsNullOrWhiteSpace(identity.InstanceId))
            {
                identity.EnsureAssigned(identity.Origin, identity.ArchetypeId);
            }

            return identity.InstanceId;
        }

        private void ApplyLayoutTransform(Vector3 layoutPosition, Quaternion layoutRotation)
        {
            if (Ship == null)
            {
                transform.localPosition = layoutPosition;
                transform.localRotation = layoutRotation;
                return;
            }

            ShipLayoutSpace.ApplyLayoutTransform(Ship, transform, layoutPosition, layoutRotation);
        }

        /// <summary>
        ///     Spawns detachment VFX while this module is still alive, then destroys the GameObject.
        ///     Prefer this over Destroy(gameObject) - Unity complains about spawning explosions when being destroyed
        /// </summary>
        public void DestroyModule(bool spawnExplosions = true)
        {
            TearDownConnectionsAndCrew(spawnExplosions);
            NotifyShipAndCleanupSystems();

            if (!this || !gameObject) return;

            transform.SetParent(null, true);
            Destroy(gameObject);
        }

        protected virtual void HandleDestroy(IPixelatedRigidbody pixelatedRigidbody)
        {
            if (Ship is Ship concreteShip && ReferenceEquals(concreteShip.CommandModule, this))
                concreteShip.ReleaseSurvivingModulesAsJunk();

            TearDownConnectionsAndCrew(true);
            OnShipConnectionLost();
        }

        private void TearDownConnectionsAndCrew(bool spawnExplosions)
        {
            if (_hasTornDownConnectionsAndCrew) return;
            _hasTornDownConnectionsAndCrew = true;

            if (PixelatedRigidbody != null) PixelatedRigidbody.OnPixelsLost -= CheckCohesion;

            DetachAllConnections(spawnExplosions);
            KillAllCrew();
        }

        private void NotifyShipAndCleanupSystems()
        {
            foreach (var standaloneSystem in _standaloneSystems) Destroy(standaloneSystem as Component);
            _standaloneSystems.Clear();

            if (Ship == null) return;

            // Keep Ship set during the callback: DestroyShip / HandleModuleChange may still
            // Recalculate this module while it remains in the cohesion graph.
            var ship = Ship;
            ship.OnModuleConnectionLost(this);
            Ship = null;
        }

        protected virtual void UpdateModule()
        {
        }

        private string ResolveArchetypeIdForSnapshot(GameObjectInstanceIdentity identity)
        {
            if (!string.IsNullOrWhiteSpace(identity.ArchetypeId))
                return identity.ArchetypeId;

            if (!Transform)
                throw new UnityException(
                    "[Module] Transform null in ResolveArchetypeIdForSnapshot. This should never happen.");

            var archetypeSource = Transform.GetComponent<IHasModuleArchetypeId>();
            if (archetypeSource != null && !string.IsNullOrWhiteSpace(archetypeSource.ModuleArchetypeId))
                return archetypeSource.ModuleArchetypeId;

            return string.Empty;
        }

        private void RestoreSystems(StandaloneModuleSystemData[] systemData, IGameContentCatalog contentCatalog)
        {
            foreach (var system in systemData)
            {
                var systemType = system.type switch
                {
                    StandaloneModuleSystemType.ReactionWheel => typeof(ReactionWheelStabilizer),
                    _ => throw new ArgumentException(
                        "[Module] Cannot restore systems for system data '" + system + "'.")
                };

                var component = (IStandaloneModuleSystem)gameObject.AddComponent(systemType);
                _standaloneSystems.Add(component);
                component.RestoreFromSnapshot(system, contentCatalog);
            }
        }

        private StandaloneModuleSystemData[] CaptureSystemSnapshots(IGameContentCatalog contentCatalog)
        {
            var systems = GetComponentsInChildren<IStandaloneModuleSystem>();

            return systems.AsValueEnumerable().Select(system => system.CaptureSnapshot(contentCatalog)).ToArray();
        }

        private void EnsurePixelatedRigidbodyCached()
        {
            if (PixelatedRigidbody != null)
                return;

            PixelatedRigidbody = GetComponent<PixelatedRigidbody>();
            if (PixelatedRigidbody == null)
                throw new UnityException(
                    $"[Module] Cannot restore snapshot on '{name}': missing PixelatedRigidbody component.");
        }

        protected virtual string CaptureTypePayloadJson(IGameContentCatalog contentCatalog)
        {
            return string.Empty;
        }

        protected virtual void ApplyTypePayloadJson(string typePayloadJson, IGameContentCatalog contentCatalog)
        {
        }

        private void HandleCrewMemberDeath(CrewMember member)
        {
            UnsubscribeCrew(member);
            OnCrewChange();
        }

        private void UnsubscribeCrew(CrewMember member)
        {
            member.OnDied -= HandleCrewMemberDeath;
        }

        private void KillCrewMember(CrewMember crewToKill)
        {
            UnsubscribeCrew(crewToKill);
            crewToKill.Kill();
            OnCrewChange();
        }

        private void OnShipConnectionLost()
        {
            NotifyShipAndCleanupSystems();
            Destroy(this);
        }

        /// <summary>
        ///     Module is leaving the ship as junk: spawn detachment VFX while connection points still
        ///     exist, then drop the Module component (PixelatedRigidbody remains).
        /// </summary>
        public void DetachAsJunkFromShip()
        {
            TearDownConnectionsAndCrew(true);
            OnShipConnectionLost();
        }

        public void SetupConnections(Module otherModule, ref FixedJoint2D joint)
        {
            if (PixelatedRigidbody == null || !otherModule || otherModule.PixelatedRigidbody == null)
            {
                Debug.LogError("Cannot SetupConnections: Missing PixelatedRigidbody on self or other module.", this);
                return;
            }

            if (PixelatedRigidbody.TexturePixelGrid == null || otherModule.PixelatedRigidbody.TexturePixelGrid == null)
            {
                Debug.LogError(
                    $"Cannot SetupConnections: PixelGrid not initialized on '{name}' or '{otherModule.name}'!", this);
                return;
            }

            var otherPixelatedRigidbody = otherModule.PixelatedRigidbody;

            var overlappingPoints =
                OverlapCalculator.CalculateOverlappingPoints(PixelatedRigidbody, otherPixelatedRigidbody);

            if (overlappingPoints == null || overlappingPoints.Count == 0) return;

            if (!_connectionPoints.ContainsKey(otherModule)) _connectionPoints[otherModule] = new List<Vector2Int>();
            _connectionPoints[otherModule] = overlappingPoints;

            // Reuse the joint from a previous InitializeModules pass. Creating a new one would leave
            // the old joint orphaned and untracked, so it could never be destroyed on detach.
            if (!joint) joint = FindExistingJointWith(otherModule);

            if (!joint)
            {
                joint = gameObject.AddComponent<FixedJoint2D>();
                if (otherPixelatedRigidbody.Rigidbody)
                {
                    joint.connectedBody = otherPixelatedRigidbody.Rigidbody;
                }
                else
                {
                    Debug.LogError($"Connected body Rigidbody2D is null on {otherModule.name}!", otherModule);
                    Destroy(joint);
                    joint = null;
                    _connectionPoints.Remove(otherModule);
                    return;
                }
            }

            _connections.TryAdd(otherModule, null);
            _connections[otherModule] = joint;
        }

        private void CheckCohesion(List<Vector2Int> points, PixelLoseReason reason)
        {
            if (IsBelowDestructionThreshold)
            {
                // NoPixelsLeft (not a plain Destroy) so OnNoPixelsLeft subscribers
                // (ship destruction on command module death, mission defeat) still fire.
                PixelatedRigidbody.NoPixelsLeft();
                return;
            }

            var connectedModulesToCheck = new List<Module>(_connectionPoints.Keys);
            var modulesToDetach = new HashSet<Module>();

            foreach (var point in points)
            foreach (var connectedModule in connectedModulesToCheck.AsValueEnumerable().Where(connectedModule =>
                         !modulesToDetach.Contains(connectedModule)))
            {
                if (!_connectionPoints.TryGetValue(connectedModule, out var connectionPixelList)) continue;
                var indexToRemove = connectionPixelList.FindIndex(p => p == point);

                if (indexToRemove == -1) continue;
                connectionPixelList.RemoveAt(indexToRemove);

                if (connectionPixelList.Count != 0) continue;
                modulesToDetach.Add(connectedModule);
            }

            foreach (var moduleToDetach in modulesToDetach.AsValueEnumerable().Where(moduleToDetach =>
                         _connectionPoints.ContainsKey(moduleToDetach)))
                DetachConnections(moduleToDetach);
        }

        private void DetachConnections(Module otherModule)
        {
            if (!this || !otherModule) return;
            Debug.Log($"[Module] DetachConnections: {name} detaching from {otherModule.name}", this);

            RemoveConnectionTo(otherModule);
            otherModule.RemoveConnectionTo(this);

            if (Ship == null)
            {
                Debug.Log($"[Module] {name} has no Ship reference, skipping graph update", this);
                return;
            }

            Debug.Log($"[Module] Calling RemoveEdge({name}, {otherModule.name})", this);
            Ship.ModuleGraph.RemoveEdge(this, otherModule);
        }

        /// <summary>
        ///     Destroys every joint between this module and its neighbors, on both sides of each pair.
        ///     Must run when the module dies or leaves the ship: a FixedJoint2D whose connectedBody
        ///     gets destroyed re-anchors to the static world body at the origin, violently yanking and
        ///     spinning whatever it is attached to.
        /// </summary>
        public void DetachAllConnections(bool spawnExplosions = true)
        {
            if (spawnExplosions)
                SpawnExplosionsOnDetachment(_connectionPoints);

            foreach (var otherModule in new List<Module>(_connections.Keys))
            {
                RemoveConnectionTo(otherModule);
                if (otherModule) otherModule.RemoveConnectionTo(this);
            }

            _connectionPoints.Clear();
        }

        private void SpawnExplosionsOnDetachment(Dictionary<Module, List<Vector2Int>> connectionPoints)
        {
            if (_effectsSpawner == null || PixelatedRigidbody == null) return;

            var chance = GameplayConstants.chanceOfSpawningExplosionOnDetachingConnectionPoint;

            foreach (var worldPos in from allConnectionsPoints in connectionPoints.Values.AsValueEnumerable()
                     from worldPoint in allConnectionsPoints
                     where Random.value < chance
                     select PixelatedRigidbody.LocalToWorldPoint(worldPoint))
                _effectsSpawner.SpawnExplosion(worldPos);
        }

        private void RemoveConnectionTo(Module otherModule)
        {
            if (_connections.TryGetValue(otherModule, out var jointToDestroy) && jointToDestroy)
                Destroy(jointToDestroy);
            _connections.Remove(otherModule);
            _connectionPoints.Remove(otherModule);
        }

        private FixedJoint2D FindExistingJointWith(Module otherModule)
        {
            if (_connections.TryGetValue(otherModule, out var existingJoint) && existingJoint)
                return existingJoint;
            if (otherModule._connections.TryGetValue(this, out var reverseJoint) && reverseJoint)
                return reverseJoint;
            return null;
        }

        private void OnCrewChange()
        {
            AliveCrew = assignedCrew.AsValueEnumerable().Where(crew => crew.IsAlive).ToList();
            _crewAppropriateSkillSum = AliveCrew.AsValueEnumerable().Sum(crew => crew.GetSkillLevel(mainSkillType));
        }

#if UNITY_EDITOR
        internal float InternalEfficiency => ModuleEfficiency;

        internal ShipResources InternalShipResources => ShipResources;
#endif
    }
}