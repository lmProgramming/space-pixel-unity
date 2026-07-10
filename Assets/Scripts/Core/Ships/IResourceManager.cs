namespace Ships.Systems.Resources
{
    public interface IResourceManager
    {
        float EnergyEfficiency { get; }
        float Energy { get; }
        float EnergyCapacity { get; }
        float EnergyDraw { get; }
        float EnergyProduction { get; }
        int CrewCapacity { get; }
        int Crew { get; }
    }
}