using System.Collections.Generic;
using Core.Pixelation;
using UnityEngine;

namespace Core.Ship
{
    public interface IModule
    {
        ModuleType Type { get; }
        IPixelatedRigidbody PixelatedRigidbody { get; }
        Transform Transform { get; }
        Resources Resources { get; }
        float Efficiency { get; }
        IReadOnlyList<CrewMember> AssignedCrew { get; }
        int CrewMissingCount { get; }
        int GetCrewCount();
        int GetCrewNeededCount();
        void FillCrewBySkill(List<CrewMember> crew, out List<CrewMember> remainingCrew);
        bool AssignCrew(CrewMember member);
        bool RemoveCrew(CrewMember member);
    }
}