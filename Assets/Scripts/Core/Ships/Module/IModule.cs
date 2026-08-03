using System.Collections.Generic;
using Core.Pixelation;
using Core.Ships.Blueprints;
using Core.Ships.Snapshots.Module;
using Core.Snapshot;
using JetBrains.Annotations;
using LMPro.External.IsAlive;
using UnityEngine;

namespace Core.Ships.Module
{
    public interface IModule : IHasAliveCheck, ISnapshottable<ModuleSnapshot>
    {
        public ModuleType Type { get; }

        [CanBeNull]
        public Transform Transform { get; }

        public ShipResources ShipResources { get; }
        public float ModuleEfficiency { get; }
        public int CrewNeededCount { get; }
        public float EnergyCapacity { get; }
        public IReadOnlyList<CrewMember> AssignedCrew { get; }
        public int CrewMissingCount { get; }
        public IPixelatedRigidbody PixelatedRigidbody { get; }
        public int AliveCrewCount { get; }
        public IShip Ship { get; }
        public Collider2D Collider2D { get; }
        public ModuleBlueprint Blueprint { get; }
        public void FillCrewBySkill(List<CrewMember> crew, out List<CrewMember> remainingCrew);
        public bool AssignCrew(CrewMember member);
        public bool RemoveCrew(CrewMember member);
        public float GetCrewEfficiency();
        public float GetEnergyDraw();
        public float GetEnergyProduction();
        public void KillAllCrew();
        public void KillRandomCrew(int count);
        public void SetResources(ShipResources newShipResources);
        public void SetLocalPosition(Vector2 localPosition);
        public void SetShip(IShip ship);
        public void SetBlueprint(ModuleBlueprint blueprint);
        public void EnsureBlueprintIdentity();
        public void SyncBlueprintLayoutFromTransform();
    }
}