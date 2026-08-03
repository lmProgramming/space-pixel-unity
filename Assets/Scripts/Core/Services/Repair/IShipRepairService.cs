using System;
using Core.Ships;
using Core.Ships.Blueprints;
using Core.Ships.Module;

namespace Core.Services.Repair
{
    public interface IShipRepairService
    {
        bool HasWorkRemaining { get; }
        int RemainingPixelCount { get; }
        void BindShip(IShip ship);
        int EstimateRepairCostForModule(IModule module);
        int EstimateRepairCostForBlueprint(ModuleBlueprint blueprint);
        int EstimateRepairCostForShip();
        bool TryRestoreOnePixel(out string failureReason);
        bool TryRestoreOnePixelForModule(IModule module, out string failureReason);
        bool TryRestoreOnePixelForBlueprint(ModuleBlueprint blueprint, out string failureReason);
        void MarkBlueprintRemoved(ModuleBlueprint blueprint);
        event Action WorkChanged;
    }
}