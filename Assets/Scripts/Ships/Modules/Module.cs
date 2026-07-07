using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core.Constants;
using Core.Pixelation;
using Core.Services;
using Core.Ships;
using Core.Ships.Snapshots.Module;
using Core.Ships.Snapshots.Module.Systems;
using LMPro;
using Pixelation;
using UnityEngine;
using Zenject;
using ZLinq;
using Random = UnityEngine.Random;
using Resources = Core.Ships.Resources;

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

        private float _crewAppropriateSkillSum;

        [Inject]
        private IEffectsSpawner _effectsSpawner;

        protected List<CrewMember> AliveCrew { get; private set; }

        internal CrewSkillType MainSkillTypeForTesting
        {
            set => mainSkillType = value;
        }

        internal IReadOnlyDictionary<Module, List<Vector2Int>> ConnectionPoints => _connectionPoints;

        public float ActualEfficiency => Ship.GeneralEfficiency * ModuleEfficiency;

        private float PixelEfficiency =>
            Mathf.Pow((float)PixelatedRigidbody.CurrentPixelCount / PixelatedRigidbody.StartPixelCount, 2);

#if UNITY_INCLUDE_TESTS
        internal IShip ShipForTesting => Ship;
#endif

        private bool IsBelowDestructionThreshold =>
            PixelatedRigidbody.CurrentPixelCount > 0 &&
            PixelatedRigidbody.CurrentPixelCount <
            PixelatedRigidbody.StartPixelCount * GameplayConstants.ModuleDestroyedBelowPixelRatio;

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

        protected virtual void OnDestroy()
        {
            if (PixelatedRigidbody != null) PixelatedRigidbody.OnPixelsLost -= CheckCohesion;

            DetachAllConnections();
            KillAllCrew();

            OnShipConnectionLost();
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

        public IShip Ship { get; protected set; }
        public Collider2D Collider2D => PixelatedRigidbody.Collider2D;

        public int AliveCrewCount => AliveCrew.Count;

        public virtual float EnergyCapacity => Resources.energyCapacity * ModuleEfficiency;

        // todo: consider caching this in the future
        public float ModuleEfficiency => PixelEfficiency * GetCrewEfficiency();

        public IReadOnlyList<CrewMember> AssignedCrew => assignedCrew;
        public int CrewMissingCount => Mathf.Max(0, CrewNeededCount - AliveCrewCount);

        [field: SerializeField]
        public Resources Resources { get; private set; }

        public IPixelatedRigidbody PixelatedRigidbody { get; private set; }

        public Transform Transform => transform;

        public virtual ModuleType Type { get; protected set; } = ModuleType.Resources;

        public virtual int CrewNeededCount => Mathf.CeilToInt(Resources.crewNeeded);

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

            return (1 - (float)CrewMissingCount / CrewNeededCount) *
                   (1 + _crewAppropriateSkillSum * Ship.CaptainMultiplier * CrewSkillBonusPerLevel);
        }

        public virtual float GetEnergyDraw()
        {
            return Resources.energyDraw * ModuleEfficiency;
        }

        public virtual float GetEnergyProduction()
        {
            return Resources.energyProduction * ModuleEfficiency;
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
            transform.localPosition = localPosition;
        }

        public void SetShip(IShip ship)
        {
            Ship = ship;
        }

        public ModuleSnapshot CaptureToSnapshot(IGameContentCatalog contentCatalog)
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

            var typeName = GetType().Name;
            var moduleSnapshot = new ModuleSnapshot
            {
                instanceId = identity.InstanceId,
                moduleName = Transform.name,
                moduleType = Type,
                moduleTypeName = typeName,
                origin = identity.Origin,
                archetypeId = identity.ArchetypeId,
                localPosition = Transform.localPosition,
                localRotation = Transform.localRotation,
                resources = Resources,
                pixelatedRigidbody = PixelatedRigidbody.CaptureToSnapshot(contentCatalog),
                typePayloadJson = CaptureTypePayloadJson(contentCatalog),
                systems = new SystemData[]
                {
                }
            };

            return moduleSnapshot;
        }

        public virtual void RestoreFromSnapshot(ModuleSnapshot snapshot, IGameContentCatalog contentCatalog)
        {
            EnsurePixelatedRigidbodyCached();

            SetResources(snapshot.resources);

            ApplyTypePayloadJson(snapshot.typePayloadJson, contentCatalog);

            PixelatedRigidbody.RestoreFromSnapshot(snapshot.pixelatedRigidbody, contentCatalog);
        }

        public void SetResources(Resources newResources)
        {
            Resources = newResources;
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

        public virtual string CaptureTypePayloadJson(IGameContentCatalog contentCatalog)
        {
            return string.Empty;
        }

        public virtual void ApplyTypePayloadJson(string typePayloadJson, IGameContentCatalog contentCatalog)
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

        public void OnShipConnectionLost()
        {
            Destroy(this);

            if (Ship == null) return;
            Ship?.OnModuleConnectionLost(this);
            Ship = null;
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
        public void DetachAllConnections()
        {
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
            foreach (var worldPos in from allConnectionsPoints in connectionPoints.Values.AsValueEnumerable()
                     from worldPoint in allConnectionsPoints
                     where Random.value < GameplayConstants.ChanceOfSpawningExplosionOnDetachingConnectionPoint
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

        internal Resources InternalResources => Resources;
#endif
    }
}