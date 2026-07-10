using System.Collections.Generic;
using Core.Ships.Module;
using LMPro.External.IsAlive;

namespace Core.Ships
{
    public interface IResourceManager : IHasAliveCheck
    {
        float EnergyEfficiency { get; }
        float Energy { get; }
        float EnergyCapacity { get; }
        float EnergyDraw { get; }
        float EnergyProduction { get; }
        int CrewCapacity { get; }
        int Crew { get; }
        void Recalculate(IReadOnlyList<IModule> modules);
        void UpdateEnergy();
    }
}