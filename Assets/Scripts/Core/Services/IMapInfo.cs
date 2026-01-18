using UnityEngine;

namespace Core.Services
{
    public interface IMapInfo
    {
        Transform MapTransform { get; }
    }
}