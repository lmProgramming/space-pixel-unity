using System.Collections.Generic;
using System.Linq;
using Core;
using Pixelation;
using UnityEngine;
using Zenject;

namespace Ships.Modules
{
    [RequireComponent(typeof(PixelatedRigidbody))]
    public class Module : MonoBehaviour, IModule
    {
        private readonly Dictionary<Module, List<Vector2Int>> _connectionPoints = new();
        private readonly Dictionary<Module, FixedJoint2D> _connections = new();

        protected readonly ResourceDraw _resourceDraw;

        [Inject] private IMapInfo _mapInfo;

        protected Ship Ship { get; private set; }

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

                foreach (var localPixelPos in points)
                {
                    var worldPos = PixelatedRigidbody.LocalToWorldPoint(localPixelPos);
                    Gizmos.DrawCube(worldPos, gizmoSize);
                }
            }
        }

        public IPixelatedRigidbody PixelatedRigidbody { get; private set; }

        public Transform Transform => transform;
        public ModuleType Type { get; protected set; }

        public void Setup(Ship ship)
        {
            Ship = ship;
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
            foreach (var connectedModule in connectedModulesToCheck.Where(connectedModule =>
                         !modulesToDetach.Contains(connectedModule)))
            {
                if (!_connectionPoints.TryGetValue(connectedModule, out var connectionPixelList)) continue;
                var indexToRemove = connectionPixelList.FindIndex(p => p == point);

                if (indexToRemove == -1) continue;
                connectionPixelList.RemoveAt(indexToRemove);

                if (connectionPixelList.Count != 0) continue;
                modulesToDetach.Add(connectedModule);
            }

            foreach (var moduleToDetach in modulesToDetach.Where(moduleToDetach =>
                         _connectionPoints.ContainsKey(moduleToDetach)))
                DetachConnections(moduleToDetach);
        }

        private void DetachConnections(Module otherModule)
        {
            if (_connections.TryGetValue(otherModule, out var jointToDestroy) && jointToDestroy)
                Destroy(jointToDestroy);
            _connections.Remove(otherModule);
            _connectionPoints.Remove(otherModule);

            Ship.ModuleGraph.RemoveEdge(this, otherModule);

            var thisStillInGraph = Ship.ModuleGraph.ContainsNode(this);
            var otherStillInGraph = Ship.ModuleGraph.ContainsNode(otherModule);

            if (!thisStillInGraph && transform.parent != _mapInfo.MapTransform)
            {
                transform.SetParent(_mapInfo.MapTransform);
                gameObject.layer = LayerMask.NameToLayer("Default");
            }

            if (otherModule && !otherStillInGraph &&
                otherModule.transform.parent != _mapInfo.MapTransform)
            {
                otherModule.transform.SetParent(_mapInfo.MapTransform);
                otherModule.gameObject.layer = LayerMask.NameToLayer("Default");
            }

            Ship.RecacheModulesDictionary();
        }
    }
}