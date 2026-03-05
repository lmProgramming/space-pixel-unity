using Core.Gameplay.EasyTeam;
using External.IsAlive;
using LMPro.Graph;
using UnityEngine;

namespace Core.Ship
{
    public interface IShip : IHasAliveCheck
    {
        ITeam Team { get; }
        IModule CommandModule { get; }
        Collider2D[] OwnColliders { get; }
        float GeneralEfficiency { get; }
        Graph<IModule> ModuleGraph { get; }
        Vector2 AttackTargetPosition { get; }
        float CaptainMultiplier { get; }
        Vector2 GetPosition();
        void OnModuleDestroyed(IModule module);
    }
}