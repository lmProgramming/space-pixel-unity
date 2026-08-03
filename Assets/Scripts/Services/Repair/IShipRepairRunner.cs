using System;
using System.Threading;
using Core.Ships.Blueprints;
using Core.Ships.Module;
using Cysharp.Threading.Tasks;

namespace Services.Repair
{
    public enum ShipRepairStopReason
    {
        Completed,
        OutOfCredits,
        Cancelled,
        Failed
    }

    public readonly struct ShipRepairRunResult
    {
        public ShipRepairRunResult(ShipRepairStopReason reason, int pixelsRestored, string message)
        {
            Reason = reason;
            PixelsRestored = pixelsRestored;
            Message = message;
        }

        public ShipRepairStopReason Reason { get; }
        public int PixelsRestored { get; }
        public string Message { get; }
    }

    public interface IShipRepairRunner
    {
        bool IsRunning { get; }

        UniTask<ShipRepairRunResult> RepairModuleAsync(IModule module, Func<int> getCredits,
            Action<int> spendCredits, CancellationToken cancellationToken);

        UniTask<ShipRepairRunResult> RepairBlueprintAsync(ModuleBlueprint blueprint, Func<int> getCredits,
            Action<int> spendCredits, CancellationToken cancellationToken);

        UniTask<ShipRepairRunResult> RepairAllAsync(Func<int> getCredits, Action<int> spendCredits,
            CancellationToken cancellationToken);

        void Cancel();
    }
}