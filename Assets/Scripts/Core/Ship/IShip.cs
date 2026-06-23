using System.Collections.Generic;
using Core.Gameplay.EasyTeam;
using LMPro.DataStructures.Graph;
using LMPro.External.IsAlive;
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
        string Name { get; }
        IReadOnlyList<IModule> AllModules { get; }
        Vector2 GetPosition();
        void OnModuleConnectionLost(IModule module);
        void ManualAddModule(IModule module);
        void ManualRemoveModule(IModule module);
        void DestroyAllModules();
        void InitializeModules();
        void SetTeam(ITeam newTeam);
        void DestroyAllModulesSilently();
    }
}