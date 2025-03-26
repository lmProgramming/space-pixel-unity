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

        [SerializeField] private Vector2Int leftBottom;

        private readonly Dictionary<Module, List<Vector2Int>> _connectionPoints = new();

        private void Start()
        {
            PixelatedRigidbody.OnPixelsDestroyed += CheckCohesion;
        }

        public void SetupConnections(Module otherModule, Vector2Int otherModulePosition)
        {
            Debug.Log(otherModule.transform.position);
            Debug.Log(transform.position);

            var otherPixelatedRigidbody = otherModule.PixelatedRigidbody;

            var overlappingPoints =
                OverlapCalculator.CalculateOverlappingPoints(PixelatedRigidbody, otherPixelatedRigidbody);

            if (overlappingPoints.Count == 0) return;

            _connectionPoints[otherModule] = overlappingPoints;

            //for (var i = 0; i < overlappingPoints.Count; i++)
            //    Debug.Log(overlappingPoints[i]);
        }

        private void CheckCohesion(List<Vector2Int> points)
        {
            foreach (var point in points) RemovePixelFromConnections(point);
        }

        private void RemovePixelFromConnections(Vector2Int pixel)
        {
            foreach (var connectedModule in _connectionPoints)
                for (var index = 0; index < connectedModule.Value.Count; index++)
                {
                    var connectionPixels = connectedModule.Value[index];
                    if (pixel != connectionPixels) continue;
                    connectedModule.Value.Remove(pixel);

                    // assuming more than 1 module can have the same connection point
                    // but 1 module can not have duplicate connection points
                    break;
                }
        }
    }
}