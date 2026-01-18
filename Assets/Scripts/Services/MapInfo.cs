using Core;
using UnityEngine;

namespace Services
{
    public sealed class MapInfo : MonoBehaviour, IMapInfo
    {
        [field: SerializeField] public Transform mapTransform;

        public Transform MapTransform => mapTransform;
    }
}