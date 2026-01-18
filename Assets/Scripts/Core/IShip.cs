using UnityEngine;

namespace Core
{
    public interface IShip
    {
        Team Team { get; }
        IModule CommandModule { get; }
        Vector2 GetPosition();
    }
}