using System.Collections.Generic;
using Core.Pixelation;
using UnityEngine;

namespace Core.Ship
{
    public interface IModule
    {
        ModuleType Type { get; }
        Transform Transform { get; }
        Resources Resources { get; }
        float Efficiency { get; }
        int CrewNeededCount { get; }
        float EnergyCapacity { get; }
        IReadOnlyList<CrewMember> AssignedCrew { get; }
        int CrewMissingCount { get; }
        IPixelatedRigidbody PixelatedRigidbody { get; }
        int AliveCrewCount { get; }
        void FillCrewBySkill(List<CrewMember> crew, out List<CrewMember> remainingCrew);
        bool AssignCrew(CrewMember member);
        bool RemoveCrew(CrewMember member);
        float GetCrewEfficiency();
        float GetEnergyDraw();
        float GetEnergyProduction();
        void KillAllCrew();
        void KillRandomCrew(int count);
        void SetLocalPosition(Vector2 localPosition);
        void Setup(IShip ship);
    }
}