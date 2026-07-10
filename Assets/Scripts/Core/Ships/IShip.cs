using System.Collections.Generic;
using Core.Gameplay.EasyTeam;
using Core.Ships.Module;
using Core.Snapshot;
using LMPro.DataStructures.Graph;
using LMPro.External.IsAlive;
using UnityEngine;

namespace Core.Ships
{
    public interface IShip : IHasAliveCheck, ISnapshottable<ShipSnapshot>
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
        bool IsSasOn { get; }
        List<IWeapon> Weapons { get; }
        List<IEngine> Engines { get; }
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