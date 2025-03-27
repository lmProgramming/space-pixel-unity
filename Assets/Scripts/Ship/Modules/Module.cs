using System.Collections.Generic;
using LM;
using Pixelation;
using UnityEngine;

namespace Ship.Modules
{
    [RequireComponent(typeof(PixelatedRigidbody))]
    public class Module : MonoBehaviour
    {
        [field: SerializeField] public PixelatedRigidbody PixelatedRigidbody { get; private set; }

        private readonly Dictionary<Module, List<Vector2Int>> _connectionPoints = new();
        private readonly Dictionary<Module, FixedJoint2D> _connections = new();

        private void Start()
        {
            PixelatedRigidbody.OnPixelsLost += CheckCohesion;
        }

        public void SetupConnections(Module otherModule, ref FixedJoint2D joint)
        {
            var otherPixelatedRigidbody = otherModule.PixelatedRigidbody;

            var overlappingPoints =
                OverlapCalculator.CalculateOverlappingPoints(PixelatedRigidbody, otherPixelatedRigidbody);

            if (overlappingPoints.Count == 0) return;

            _connectionPoints[otherModule] = overlappingPoints;

            if (!joint)
            {
                joint = gameObject.AddComponent<FixedJoint2D>();

                joint.connectedBody = otherPixelatedRigidbody.Rigidbody;
            }

            _connections[otherModule] = joint;
        }

        private void CheckCohesion(List<Vector2Int> points, PixelatedRigidbody.PixelLoseReason reason)
        {
            if (points.Count > 1) Debug.Log(points.Count);
            foreach (var point in points) RemovePixelFromConnections(point);
        }

        private void DetachConnections(Module otherModule)
        {
            Debug.Log(_connections[otherModule]);
            Destroy(_connections[otherModule]);
            _connections.Remove(otherModule);
            _connectionPoints.Remove(otherModule);
        }

        private void RemovePixelFromConnections(Vector2Int pixel)
        {
            foreach (var connectedModule in _connectionPoints)
                for (var index = 0; index < connectedModule.Value.Count; index++)
                {
                    var connectionPixels = connectedModule.Value[index];
                    if (pixel != connectionPixels) continue;

                    connectedModule.Value.Remove(pixel);

                    if (connectedModule.Value.Count == 0)
                    {
                        DetachConnections(connectedModule.Key);
                        RemovePixelFromConnections(pixel);
                        return;
                    }

                    // assuming more than 1 module can have the same connection point
                    // but 1 module can not have duplicate connection points
                    break;
                }
        }
    }
}