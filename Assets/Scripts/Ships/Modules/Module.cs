using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core.Pixelation;
using Core.Ship;
using Pixelation;
using UnityEngine;
using ZLinq;
using Resources = Core.Ship.Resources;

[assembly: InternalsVisibleTo("Game.Editor.InspectorExtensions")]
[assembly: InternalsVisibleTo("Game.Ships.Tests")]

namespace Ships.Modules
{
    [RequireComponent(typeof(PixelatedRigidbody))]
    public class Module : MonoBehaviour, IModule
    {
        private const float CrewSkillBonusPerLevel = 0.02f;
        protected const float CaptainBonusPerLevel = 0.05f;

        [SerializeField] private CrewSkillType mainSkillType;

        [SerializeField]
        private List<CrewMember> assignedCrew = new();

        private readonly Dictionary<Module, List<Vector2Int>> _connectionPoints = new();
        private readonly Dictionary<Module, FixedJoint2D> _connections = new();

        private float _crewAppropriateSkillSum;

        protected IReadOnlyList<CrewMember> AliveCrew { get; private set; }

        internal CrewSkillType MainSkillTypeForTesting
        {
            set => mainSkillType = value;
        }

        protected IShip Ship { get; private set; }

        internal IReadOnlyDictionary<Module, List<Vector2Int>> ConnectionPoints => _connectionPoints;

        protected float ShipModuleEfficiency => Ship.GeneralEfficiency * Efficiency;

        private float PixelEfficiency =>
            Mathf.Pow((float)PixelatedRigidbody.CurrentPixelCount / PixelatedRigidbody.StartPixelCount, 2);

        public virtual float EnergyCapacity => Resources.energyCapacity * Efficiency;

        protected virtual void Awake()
        {
            PixelatedRigidbody = GetComponent<PixelatedRigidbody>();
            OnCrewChange();
        }

        private void Start()
        {
            if (PixelatedRigidbody != null)
                PixelatedRigidbody.OnPixelsLost += CheckCohesion;
            else
                Debug.LogError("PixelatedRigidbody not found on Module!", this);
        }

        private void OnDestroy()
        {
            if (PixelatedRigidbody != null) PixelatedRigidbody.OnPixelsLost -= CheckCohesion;

            KillAllCrew();

            Ship?.OnModuleDestroyed(this);
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

        public float Efficiency => PixelEfficiency * GetCrewEfficiency();

        public IReadOnlyList<CrewMember> AssignedCrew => assignedCrew;
        public int CrewMissingCount => CrewNeededCount - AliveCrewCount();

        [field: SerializeField]
        public Resources Resources { get; private set; }

        public IPixelatedRigidbody PixelatedRigidbody { get; private set; }

        public Transform Transform => transform;
        public ModuleType Type { get; protected set; }


        public int AliveCrewCount()
        {
            return AliveCrew.Count;
        }

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
            return true;
        }

        public bool RemoveCrew(CrewMember member)
        {
            var crewRemoved = assignedCrew.Remove(member);
            OnCrewChange();
            return crewRemoved;
        }

        public virtual float GetCrewEfficiency()
        {
            if (assignedCrew.Count == 0) return CrewNeededCount == 0 ? 1f : 0f;

            return (1 - (float)CrewMissingCount / CrewNeededCount) *
                   (1 + _crewAppropriateSkillSum * Ship.CaptainMultiplier * CrewSkillBonusPerLevel);
        }

        public virtual float GetEnergyDraw()
        {
            return Resources.energyDraw * Efficiency;
        }

        public virtual float GetEnergyProduction()
        {
            return Resources.energyProduction * Efficiency;
        }

        public void Setup(Ship ship)
        {
            Ship = ship;
        }

        public void OnShipConnectionLost()
        {
            Ship = null;
            Destroy(this);
        }

        public void SetupConnections(Module otherModule, ref FixedJoint2D joint)
        {
            if (PixelatedRigidbody == null || otherModule == null || otherModule.PixelatedRigidbody == null)
            {
                Debug.LogError("Cannot SetupConnections: Missing PixelatedRigidbody on self or other module.", this);
                return;
            }

            if (PixelatedRigidbody.PixelGrid == null || otherModule.PixelatedRigidbody.PixelGrid == null)
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

            if (!joint)
            {
                joint = gameObject.AddComponent<FixedJoint2D>();
                if (otherPixelatedRigidbody.Rigidbody != null)
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

            if (_connections.TryGetValue(otherModule, out var jointToDestroy) && jointToDestroy)
                Destroy(jointToDestroy);
            _connections.Remove(otherModule);
            _connectionPoints.Remove(otherModule);

            if (Ship == null)
            {
                Debug.Log($"[Module] {name} has no Ship reference, skipping graph update", this);
                return;
            }

            Debug.Log($"[Module] Calling RemoveEdge({name}, {otherModule.name})", this);
            Ship.ModuleGraph.RemoveEdge(this, otherModule);
        }

        private void OnCrewChange()
        {
            AliveCrew = assignedCrew.AsValueEnumerable().Where(crew => crew.IsAlive).ToList();
            _crewAppropriateSkillSum = assignedCrew.AsValueEnumerable().Sum(crew => crew.GetSkillLevel(mainSkillType));
        }

        private void KillAllCrew()
        {
            foreach (var crew in assignedCrew)
                crew.Kill();
            assignedCrew.Clear();

            OnCrewChange();
        }

        public void SetResources(Resources newResources)
        {
            Resources = newResources;
        }

#if UNITY_EDITOR
        internal float InternalEfficiency => Efficiency;

        internal Resources InternalResources => Resources;
#endif
    }
}