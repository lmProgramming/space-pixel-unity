using System;
using System.Threading;
using Core.Constants;
using Core.Services.Repair;
using Core.Ships.Blueprints;
using Core.Ships.Module;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Services.Repair
{
    public class ShipRepairRunner : IShipRepairRunner
    {
        private readonly ProgressionConstants _progressionConstants;
        private readonly IShipRepairService _repairService;
        private CancellationTokenSource _runCts;

        [Inject]
        public ShipRepairRunner(IShipRepairService repairService, ProgressionConstants gameplayConstants)
        {
            _repairService = repairService ?? throw new ArgumentNullException(nameof(repairService));
            _progressionConstants = gameplayConstants ?? throw new ArgumentNullException(nameof(gameplayConstants));
        }

        public bool IsRunning => _runCts is { IsCancellationRequested: false };

        public void Cancel()
        {
            if (_runCts == null) return;
            _runCts.Cancel();
            _runCts.Dispose();
            _runCts = null;
        }

        public UniTask<ShipRepairRunResult> RepairModuleAsync(IModule module, Func<int> getCredits,
            Action<int> spendCredits, CancellationToken cancellationToken)
        {
            return RunAsync(
                () => _repairService.TryRestoreOnePixelForModule(module, out var reason)
                    ? (true, null)
                    : (false, reason),
                () => _repairService.EstimateRepairCostForModule(module) > 0,
                getCredits, spendCredits, cancellationToken);
        }

        public UniTask<ShipRepairRunResult> RepairBlueprintAsync(ModuleBlueprint blueprint, Func<int> getCredits,
            Action<int> spendCredits, CancellationToken cancellationToken)
        {
            return RunAsync(
                () => _repairService.TryRestoreOnePixelForBlueprint(blueprint, out var reason)
                    ? (true, null)
                    : (false, reason),
                () => _repairService.EstimateRepairCostForBlueprint(blueprint) > 0,
                getCredits, spendCredits, cancellationToken);
        }

        public UniTask<ShipRepairRunResult> RepairAllAsync(Func<int> getCredits, Action<int> spendCredits,
            CancellationToken cancellationToken)
        {
            return RunAsync(
                () => _repairService.TryRestoreOnePixel(out var reason)
                    ? (true, null)
                    : (false, reason),
                () => _repairService.HasWorkRemaining,
                getCredits, spendCredits, cancellationToken);
        }

        private async UniTask<ShipRepairRunResult> RunAsync(
            Func<(bool restored, string failureReason)> restoreOne,
            Func<bool> hasMoreWork,
            Func<int> getCredits,
            Action<int> spendCredits,
            CancellationToken cancellationToken)
        {
            if (getCredits == null) throw new ArgumentNullException(nameof(getCredits));
            if (spendCredits == null) throw new ArgumentNullException(nameof(spendCredits));

            Cancel();
            _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _runCts.Token;

            var pixelsPerFrame = Mathf.Max(1, _progressionConstants.repairedPixelsPerFrame);
            var costPerPixel = Mathf.Max(0, _progressionConstants.creditsPerRepairedPixel);
            var restored = 0;

            try
            {
                while (hasMoreWork())
                {
                    token.ThrowIfCancellationRequested();

                    for (var i = 0; i < pixelsPerFrame && hasMoreWork(); i++)
                    {
                        if (getCredits() < costPerPixel)
                            return new ShipRepairRunResult(ShipRepairStopReason.OutOfCredits, restored,
                                "Not enough credits");

                        var (didRestore, failureReason) = restoreOne();
                        if (!didRestore)
                        {
                            if (string.IsNullOrWhiteSpace(failureReason))
                                return new ShipRepairRunResult(ShipRepairStopReason.Completed, restored, null);
                            return new ShipRepairRunResult(ShipRepairStopReason.Failed, restored, failureReason);
                        }

                        spendCredits(costPerPixel);
                        restored++;
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                return new ShipRepairRunResult(ShipRepairStopReason.Completed, restored, null);
            }
            catch (OperationCanceledException)
            {
                return new ShipRepairRunResult(ShipRepairStopReason.Cancelled, restored, "Cancelled");
            }
            finally
            {
                _runCts?.Dispose();
                _runCts = null;
            }
        }
    }
}