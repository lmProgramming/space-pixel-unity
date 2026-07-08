using System.Collections.Generic;
using Core.Pixelation;
using Core.Ships.Snapshots.Module;
using Core.Snapshot;
using JetBrains.Annotations;
using LMPro.External.IsAlive;
using UnityEngine;

namespace Core.Ships
{
    public interface IModule : IHasAliveCheck, ISnapshottable<ModuleSnapshot>
    {
        ModuleType Type { get; }

        [CanBeNull]
        Transform Transform { get; }

        Resources Resources { get; }
        float ModuleEfficiency { get; }
        int CrewNeededCount { get; }
        float EnergyCapacity { get; }
        IReadOnlyList<CrewMember> AssignedCrew { get; }
        int CrewMissingCount { get; }
        IPixelatedRigidbody PixelatedRigidbody { get; }
        int AliveCrewCount { get; }
        IShip Ship { get; }
        Collider2D Collider2D { get; }
        void FillCrewBySkill(List<CrewMember> crew, out List<CrewMember> remainingCrew);
        bool AssignCrew(CrewMember member);
        bool RemoveCrew(CrewMember member);
        float GetCrewEfficiency();
        float GetEnergyDraw();
        float GetEnergyProduction();
        void KillAllCrew();
        void KillRandomCrew(int count);
        void SetResources(Resources newResources);
        void SetLocalPosition(Vector2 localPosition);
        void SetShip(IShip ship);
    }
}