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

namespace Ships.Modules
{
    [RequireComponent(typeof(PixelatedRigidbody))]
    public class Module : MonoBehaviour, IModule
    {
        private const float CrewSkillBonusPerLevel = 0.02f;
        private const float CaptainBonusPerLevel = 0.05f;

        private readonly Dictionary<Module, List<Vector2Int>> _connectionPoints = new();
        private readonly Dictionary<Module, FixedJoint2D> _connections = new();
        private readonly List<CrewMember> _assignedCrew = new();

        protected Ship Ship { get; private set; }

        /// <summary>
        ///     Gets a read-only view of connection points to other modules.
        ///     Used for serialization and testing.
        /// </summary>
        public IReadOnlyDictionary<Module, List<Vector2Int>> ConnectionPoints => _connectionPoints;

        protected float Efficiency => Mathf.Pow(
            (float)PixelatedRigidbody.CurrentPixelCount / PixelatedRigidbody.StartPixelCount,
            2) * (1f + GetCrewBonus(CrewSkillType.Mechanics));

        protected float ShipModuleEfficiency => Ship.GeneralEfficiency * Efficiency;

        protected virtual void Awake()
        {
            PixelatedRigidbody = GetComponent<PixelatedRigidbody>();
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

        [field: SerializeField]
        public Resources Resources { get; private set; }

        public IPixelatedRigidbody PixelatedRigidbody { get; private set; }

        public Transform Transform => transform;
        public ModuleType Type { get; protected set; }


        public virtual int GetCrewCount()
        {
            return Mathf.FloorToInt(Resources.crew * Efficiency);
        }

        public virtual int GetCrewCapacity()
        {
            return Mathf.FloorToInt(Resources.crewCapacity * Efficiency);
        }

        public virtual float GetEnergyCapacity()
        {
            return Resources.energyCapacity * Efficiency;
        }

        public virtual float GetEnergyDraw()
        {
            return Resources.energyDraw * Efficiency;
        }

        public virtual float GetEnergyProduction()
        {
            return Resources.energyProduction * Efficiency * (1f + GetCrewBonus(CrewSkillType.PowerEngineering));
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

        public IReadOnlyList<CrewMember> AssignedCrew => _assignedCrew;

        /// <summary>
        ///     Assigns a crew member to this module if capacity allows.
        /// </summary>
        /// <returns>True if successfully assigned; false if over capacity or already assigned.</returns>
        public bool AssignCrew(CrewMember member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            if (_assignedCrew.Contains(member)) return false;
            if (_assignedCrew.Count >= Resources.crewCapacity) return false;

            _assignedCrew.Add(member);
            return true;
        }

        /// <summary>
        ///     Removes a crew member from this module.
        /// </summary>
        public bool RemoveCrew(CrewMember member)
        {
            return _assignedCrew.Remove(member);
        }

        /// <summary>
        ///     Returns the effective skill total for the given skill type,
        ///     with captain-level crew boosting all other skills.
        /// </summary>
        public float GetCrewBonus(CrewSkillType skillType)
        {
            if (_assignedCrew.Count == 0) return 0f;

            var captainTotal = 0;
            foreach (var crew in _assignedCrew)
                captainTotal += crew.GetSkillLevel(CrewSkillType.Captain);

            var captainMultiplier = 1f + captainTotal * CaptainBonusPerLevel;

            var skillTotal = 0;
            foreach (var crew in _assignedCrew)
                skillTotal += crew.GetSkillLevel(skillType);

            return skillTotal * captainMultiplier * CrewSkillBonusPerLevel;
        }

        private void KillAllCrew()
        {
            foreach (var crew in _assignedCrew)
                crew.Kill();
            _assignedCrew.Clear();
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