using UnityEngine;

namespace AI.EasyState
{
    public interface IAgent
    {
        // Empty interface is fine, or add common properties like transform
        Transform Transform { get; }
    }
}