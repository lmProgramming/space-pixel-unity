using System;
using System.Collections.Generic;
using Core.Pixelation;
using Core.Services;
using Core.Services.Repair;
using Core.Ships;
using Core.Ships.Blueprints;
using Core.Ships.Module;
using Pixelation;
using UnityEngine;
using Zenject;
using ZLinq;

namespace Services.Repair
{
    public class ShipRepairService : IShipRepairService
    {
        private static readonly Vector2Int[] CardinalOffsets =
        {
            new(1, 0), new(0, 1), new(-1, 0), new(0, -1)
        };

        private readonly Dictionary<string, ModuleRepairTarget> _liveTargets = new();
        private readonly IModuleRestoreFactory _moduleRestoreFactory;
        private readonly Dictionary<string, PendingRebuild> _pendingRebuilds = new();
        private readonly IShipModuleCatalog _shipModuleCatalog;
        private IShip _ship;

        [Inject]
        public ShipRepairService(IModuleRestoreFactory moduleRestoreFactory, IShipModuleCatalog shipModuleCatalog)
        {
            _moduleRestoreFactory = moduleRestoreFactory ??
                                    throw new ArgumentNullException(nameof(moduleRestoreFactory));
            _shipModuleCatalog = shipModuleCatalog ?? throw new ArgumentNullException(nameof(shipModuleCatalog));
        }

        public event Action WorkChanged;

        public bool HasWorkRemaining =>
            _liveTargets.Values.AsValueEnumerable().Any(t => t.HasWorkRemaining) ||
            _pendingRebuilds.Count > 0;

        public int RemainingPixelCount
        {
            get
            {
                var live = _liveTargets.Values.AsValueEnumerable().Sum(t => t.RemainingPixelCount);
                var pending = _pendingRebuilds.Values.AsValueEnumerable().Sum(p => p.MissingPixelCount);
                return live + pending;
            }
        }

        public void BindShip(IShip ship)
        {
            _ship = ship ?? throw new ArgumentNullException(nameof(ship));

            foreach (var module in _ship.AllModules)
                module.EnsureBlueprintIdentity();

            _ship.SyncBlueprintFromLiveModules();
            RebuildPlan();
        }

        public int EstimateRepairCostForModule(IModule module)
        {
            EnsureBound();
            if (module?.Blueprint == null) return 0;
            if (_liveTargets.TryGetValue(module.Blueprint.blueprintId, out var target))
                return target.RemainingPixelCount;
            return 0;
        }

        public int EstimateRepairCostForBlueprint(ModuleBlueprint blueprint)
        {
            EnsureBound();
            if (blueprint == null || blueprint.removedByPlayer) return 0;
            if (_liveTargets.TryGetValue(blueprint.blueprintId, out var target))
                return target.RemainingPixelCount;
            if (_pendingRebuilds.TryGetValue(blueprint.blueprintId, out var pending))
                return pending.MissingPixelCount;
            return 0;
        }

        public int EstimateRepairCostForShip()
        {
            return RemainingPixelCount;
        }

        public bool TryRestoreOnePixel(out string failureReason)
        {
            EnsureBound();

            TryMaterializeSeedablePending();

            foreach (var target in _liveTargets.Values.AsValueEnumerable()
                         .Where(t => t.HasWorkRemaining)
                         .OrderBy(t => t.RemainingPixelCount))
            {
                target.RestoreNextPixel();
                AfterPixelRestored();
                failureReason = null;
                return true;
            }

            if (_pendingRebuilds.Count > 0)
            {
                failureReason =
                    "[ShipRepairService] Remaining destroyed modules cannot be seeded from live neighbours: " +
                    string.Join(", ",
                        _pendingRebuilds.Values.AsValueEnumerable().Select(p => p.Blueprint.archetypeId));
                throw new UnityException(failureReason);
            }

            failureReason = "Nothing left to repair.";
            return false;
        }

        public bool TryRestoreOnePixelForModule(IModule module, out string failureReason)
        {
            EnsureBound();
            if (module?.Blueprint == null)
            {
                failureReason = "Module has no blueprint.";
                return false;
            }

            if (!_liveTargets.TryGetValue(module.Blueprint.blueprintId, out var target) || !target.HasWorkRemaining)
            {
                failureReason = "Selected module has no missing pixels.";
                return false;
            }

            target.RestoreNextPixel();
            AfterPixelRestored();
            failureReason = null;
            return true;
        }

        public bool TryRestoreOnePixelForBlueprint(ModuleBlueprint blueprint, out string failureReason)
        {
            EnsureBound();
            if (blueprint == null || blueprint.removedByPlayer)
            {
                failureReason = "Blueprint is missing or removed.";
                return false;
            }

            if (_liveTargets.TryGetValue(blueprint.blueprintId, out var liveTarget))
            {
                if (!liveTarget.HasWorkRemaining)
                {
                    failureReason = "Module is already fully repaired.";
                    return false;
                }

                liveTarget.RestoreNextPixel();
                AfterPixelRestored();
                failureReason = null;
                return true;
            }

            if (!_pendingRebuilds.ContainsKey(blueprint.blueprintId))
            {
                failureReason = "Blueprint is not pending rebuild.";
                return false;
            }

            if (!TryMaterializePending(blueprint.blueprintId, out var materializeFailure))
            {
                failureReason = materializeFailure;
                return false;
            }

            if (!_liveTargets.TryGetValue(blueprint.blueprintId, out var newlyLive) || !newlyLive.HasWorkRemaining)
            {
                failureReason = null;
                return true;
            }

            newlyLive.RestoreNextPixel();
            AfterPixelRestored();
            failureReason = null;
            return true;
        }

        public void MarkBlueprintRemoved(ModuleBlueprint blueprint)
        {
            EnsureBound();
            if (blueprint == null) throw new ArgumentNullException(nameof(blueprint));

            blueprint.removedByPlayer = true;
            _pendingRebuilds.Remove(blueprint.blueprintId);
            WorkChanged?.Invoke();
        }

        private void RebuildPlan()
        {
            EnsureBound();
            _liveTargets.Clear();
            _pendingRebuilds.Clear();

            var liveByBlueprintId = new Dictionary<string, IModule>();
            foreach (var module in _ship.AllModules.AsValueEnumerable().Where(m => m.Blueprint != null))
            {
                var blueprintId = module.Blueprint.blueprintId;
                if (string.IsNullOrWhiteSpace(blueprintId))
                    throw new UnityException(
                        $"[ShipRepairService] Module '{module.Transform?.name}' has empty blueprint id.");

                if (!liveByBlueprintId.TryAdd(blueprintId, module))
                    throw new UnityException(
                        $"[ShipRepairService] Duplicate blueprint id '{blueprintId}' on live modules.");
            }

            foreach (var blueprint in _ship.Blueprint.modules.AsValueEnumerable()
                         .Where(b => b is { removedByPlayer: false }))
            {
                if (string.IsNullOrWhiteSpace(blueprint.blueprintId))
                    throw new UnityException(
                        "[ShipRepairService] Ship blueprint contains a module with empty blueprint id.");

                if (liveByBlueprintId.TryGetValue(blueprint.blueprintId, out var liveModule))
                {
                    var (colors, health) = ResolvePristineGrids(blueprint, liveModule.PixelatedRigidbody);
                    var target = new ModuleRepairTarget(liveModule.PixelatedRigidbody, colors, health);
                    if (target.HasWorkRemaining)
                        _liveTargets[blueprint.blueprintId] = target;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(blueprint.archetypeId))
                    continue;

                var (pristineColors, pristineHealth) = LoadPristineGrids(blueprint.archetypeId);
                var missing = CountPristinePixels(pristineColors);
                _pendingRebuilds[blueprint.blueprintId] = new PendingRebuild(blueprint, pristineColors, pristineHealth,
                    missing);
            }

            WorkChanged?.Invoke();
        }

        private void AfterPixelRestored()
        {
            var finishedIds = _liveTargets.AsValueEnumerable()
                .Where(pair => !pair.Value.HasWorkRemaining)
                .Select(pair => pair.Key)
                .ToList();

            foreach (var id in finishedIds)
                _liveTargets.Remove(id);

            if (finishedIds.Count > 0)
                _ship.InitializeModules();

            TryMaterializeSeedablePending();
            WorkChanged?.Invoke();
        }

        private void TryMaterializeSeedablePending()
        {
            var progressed = true;
            while (progressed)
            {
                progressed = false;
                var ids = _pendingRebuilds.Keys.AsValueEnumerable().ToList();
                foreach (var id in ids)
                {
                    if (!TryMaterializePending(id, out _))
                        continue;
                    progressed = true;
                }
            }
        }

        private bool TryMaterializePending(string blueprintId, out string failureReason)
        {
            if (!_pendingRebuilds.TryGetValue(blueprintId, out var pending))
            {
                failureReason = "Pending rebuild not found.";
                return false;
            }

            if (!TryFindSeedPixel(pending, out var seedPoint))
            {
                failureReason =
                    $"[ShipRepairService] Cannot seed module '{pending.Blueprint.archetypeId}' from live neighbours yet.";
                return false;
            }

            var moduleGo = _moduleRestoreFactory.CreateModuleShellFromBlueprint(pending.Blueprint, _ship is Component c
                ? c.transform
                : throw new UnityException("[ShipRepairService] Ship must be a Component to parent modules."));

            moduleGo.SetActive(false);
            ShipLayoutSpace.ApplyLayoutTransform(_ship, moduleGo.transform, pending.Blueprint.localPosition,
                pending.Blueprint.localRotation);

            var identity = moduleGo.GetComponent<GameObjectInstanceIdentity>();
            if (!identity)
                identity = moduleGo.AddComponent<GameObjectInstanceIdentity>();
            identity.RestoreFromSnapshot(pending.Blueprint.blueprintId, InstanceOrigin.CatalogPrefab,
                pending.Blueprint.archetypeId);

            var module = moduleGo.GetComponent<IModule>();
            if (module == null)
                throw new UnityException(
                    $"[ShipRepairService] Failed to create module for archetype '{pending.Blueprint.archetypeId}'.");

            module.SetShip(_ship);
            module.SetBlueprint(pending.Blueprint);

            var body = module.PixelatedRigidbody;
            if (body == null)
                throw new UnityException(
                    $"[ShipRepairService] Module '{pending.Blueprint.archetypeId}' has no PixelatedRigidbody.");

            var pristineCount = CountPristinePixels(pending.PristineColors);
            body.KeepOnlyPixels(new[] { seedPoint });
            body.SetStartPixelCount(pristineCount);

            moduleGo.SetActive(true);
            if (_ship is Component shipComponent)
                moduleGo.layer = shipComponent.gameObject.layer;

            _ship.ManualAddModule(module);

            var target = new ModuleRepairTarget(body, pending.PristineColors, pending.PristineHealth);
            if (target.HasWorkRemaining)
                _liveTargets[blueprintId] = target;

            _pendingRebuilds.Remove(blueprintId);
            failureReason = null;
            WorkChanged?.Invoke();
            return true;
        }

        private bool TryFindSeedPixel(PendingRebuild pending, out Vector2Int seedPoint)
        {
            seedPoint = default;
            var bestContacts = 0;
            var bestDistance = float.MaxValue;
            var width = pending.PristineColors.GetLength(0);
            var height = pending.PristineColors.GetLength(1);
            var center = new Vector2(width / 2f, height / 2f);

            var liveBodies = _ship.AllModules.AsValueEnumerable()
                .Select(m => m.PixelatedRigidbody)
                .Where(b => b != null)
                .ToList();

            if (liveBodies.Count == 0)
                return false;

            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                if (pending.PristineColors[x, y].a == 0) continue;

                var localPoint = new Vector2Int(x, y);
                var worldCenter = LocalPixelToWorld(pending.Blueprint, width, height, localPoint);
                var contacts = CountLiveAdjacency(worldCenter, liveBodies);
                if (contacts == 0) continue;

                var distance = (new Vector2(x, y) - center).sqrMagnitude;
                if (contacts < bestContacts) continue;
                if (contacts == bestContacts && distance >= bestDistance) continue;

                bestContacts = contacts;
                bestDistance = distance;
                seedPoint = localPoint;
            }

            return bestContacts > 0;
        }

        private static int CountLiveAdjacency(Vector2 worldCenter, List<IPixelatedRigidbody> liveBodies)
        {
            var contacts = 0;
            foreach (var body in liveBodies)
            foreach (var offset in CardinalOffsets)
            {
                var neighborWorld = worldCenter + offset;
                var local = body.WorldToLocalPixel(neighborWorld);
                if (body.IsPixel(local))
                    contacts++;
            }

            return contacts;
        }

        private Vector2 LocalPixelToWorld(ModuleBlueprint blueprint, int width, int height, Vector2Int local)
        {
            var localCentered = new Vector3(local.x - width / 2f + 0.5f, local.y - height / 2f + 0.5f, 0f);
            var layoutPoint = blueprint.localPosition + blueprint.localRotation * localCentered;
            return ShipLayoutSpace.LocalToWorld(_ship, layoutPoint);
        }

        private (Color32[,] colors, float[,] health) ResolvePristineGrids(ModuleBlueprint blueprint,
            IPixelatedRigidbody liveBody)
        {
            if (!string.IsNullOrWhiteSpace(blueprint.archetypeId))
                return LoadPristineGrids(blueprint.archetypeId);

            if (liveBody == null || !liveBody.HasSprite)
                throw new UnityException(
                    $"[ShipRepairService] Cannot resolve pristine grids for blueprint '{blueprint.blueprintId}'.");

            var colors = liveBody.BuildPristineColors();
            var health = liveBody.BuildPristineHealth(colors);
            return (colors, health);
        }

        private (Color32[,] colors, float[,] health) LoadPristineGrids(string archetypeId)
        {
            if (!_shipModuleCatalog.TryGetModulePrefab(archetypeId, out var prefab) || !prefab)
                throw new UnityException(
                    $"[ShipRepairService] Missing module prefab for archetype '{archetypeId}'.");

            var body = prefab.GetComponent<PixelatedRigidbody>();
            if (body == null)
                throw new UnityException(
                    $"[ShipRepairService] Prefab for '{archetypeId}' has no PixelatedRigidbody.");

            var colors = body.BuildPristineColors();
            var health = body.BuildPristineHealth(colors);
            return (colors, health);
        }

        private static int CountPristinePixels(Color32[,] colors)
        {
            var width = colors.GetLength(0);
            var height = colors.GetLength(1);
            var count = 0;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                if (colors[x, y].a > 0)
                    count++;
            return count;
        }

        private void EnsureBound()
        {
            if (_ship == null)
                throw new InvalidOperationException("[ShipRepairService] BindShip must be called first.");
        }

        private sealed class PendingRebuild
        {
            public PendingRebuild(ModuleBlueprint blueprint, Color32[,] pristineColors, float[,] pristineHealth,
                int missingPixelCount)
            {
                Blueprint = blueprint;
                PristineColors = pristineColors;
                PristineHealth = pristineHealth;
                MissingPixelCount = missingPixelCount;
            }

            public ModuleBlueprint Blueprint { get; }
            public Color32[,] PristineColors { get; }
            public float[,] PristineHealth { get; }
            public int MissingPixelCount { get; }
        }
    }
}