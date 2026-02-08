using Core.Gameplay.EasyTeam;
using UnityEngine;

namespace Core.Ship
{
    public interface IShip
    {
        ITeam Team { get; }
        IModule CommandModule { get; }
        Vector2 GetPosition();
    }
}